using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Collections.Generic;

using ENet;

namespace Valk.Networking
{
    class Server
    {
        public static Dictionary<string, HandlePacket> HandlePackets = typeof(HandlePacket).Assembly.GetTypes().Where(x => typeof(HandlePacket).IsAssignableFrom(x) && !x.IsAbstract).Select(Activator.CreateInstance).Cast<HandlePacket>().ToDictionary(x => x.GetType().Name, x => x);

        public static Host server;
        public Timer positionUpdatePump;

        private const int POSITION_UPDATE_DELAY = 100;

        public static byte channelID = 0;

        private ushort port;
        private int maxClients;

        private bool serverRunning;

        public static List<Client> clients;
        public static List<Client> positionPacketQueue;

        public Server(ushort port, int maxClients)
        {
            this.port = port;
            this.maxClients = maxClients;

            clients = new List<Client>();
            positionPacketQueue = new List<Client>();

            positionUpdatePump = new Timer(POSITION_UPDATE_DELAY, PositionUpdates);
            positionUpdatePump.Start();
        }

        public void Start()
        {
            Library.Initialize();

            server = new Host();

            var address = new Address();
            address.Port = port;
            server.Create(address, maxClients);
            serverRunning = true;

            Console.Log(LogType.Info, $"Server listening on {port}");

            //int packetCounter = 0;
            //int i = 0;
            Event netEvent;

            while (serverRunning)
            {
                var polled = false;

                while (!polled)
                {
                    if (!server.IsSet)
                        return;

                    if (server.CheckEvents(out netEvent) <= 0)
                    {
                        if (server.Service(15, out netEvent) <= 0)
                            break;

                        polled = true;
                    }

                    var ip = netEvent.Peer.IP;
                    var id = netEvent.Peer.ID;

                    switch (netEvent.Type)
                    {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            Console.Log(LogType.Info, $"Client connected - ID: {id}, IP: {ip}");
                            clients.Add(new Client(netEvent.Peer));
                            break;

                        case EventType.Disconnect:
                            Console.Log(LogType.Info, $"Client disconnected - ID: {id}, IP: {ip}");
                            clients.Remove(clients.Find(x => x.ID.Equals(netEvent.Peer.ID)));
                            break;

                        case EventType.Timeout:
                            Console.Log(LogType.Info, $"Client timeout - ID: {id}, IP: {ip}");
                            clients.Remove(clients.Find(x => x.ID.Equals(netEvent.Peer.ID)));
                            break;

                        case EventType.Receive:
                            //Console.Log($"{packetCounter++} Packet received from - ID: {id}, IP: {ip}, Channel ID: {netEvent.ChannelID}, Data length: {netEvent.Packet.Length}");
                            HandlePacket(netEvent);
                            netEvent.Packet.Dispose();
                            break;
                    }
                }

                server.Flush();
            }

            CleanUp();
        }

        // Send a position update to all peers in the game every x ms
        private void PositionUpdates(Object source, ElapsedEventArgs e)
        {
            SendPositionUpdate();
        }

        private void SendPositionUpdate()
        {
            // If nothing is being queued there is no reason to check if we need to send position updates
            if (positionPacketQueue.Count == 0)
                return;

            var clientsInGame = clients.FindAll(x => x.Status == ClientStatus.InGame);

            // If there's only one or no client(s) there is no reason to send position updates to no one
            if (clientsInGame.Count <= 1)
                return;

            var data = new List<object>(); // Prepare the data list that will eventually be serialized and sent

            // Send clientQueued data to every other client but clientQueued client
            foreach (var clientQueued in positionPacketQueue)
            {
                // Add the clientQueued data to data list
                data.Add(clientQueued.ID);
                data.Add(clientQueued.x);
                data.Add(clientQueued.y);

                var sendPeers = new List<Peer>();

                // Figure out which clients we need to send this clientQueued data to
                foreach (Client clientInGame in clientsInGame)
                {
                    if (clientInGame.ID == clientQueued.ID) // We do not want to send data back to the queued client
                        continue;

                    sendPeers.Add(clientInGame.Peer);
                }

                // Send the data to the clients
                //Logger.Log($"Broadcasting to client {clientQueued.ID}");
                Network.Broadcast(server, Packet.Create(PacketType.ServerPositionUpdate, PacketFlags.None, data.ToArray()), sendPeers.ToArray());
                positionPacketQueue.Remove(clientQueued);
            }
        }

        public static Peer[] GetPeersInGame()
        {
            return clients.FindAll(x => x.Status == ClientStatus.InGame).Select(x => x.Peer).ToArray();
        }

        private void HandlePacket(Event netEvent)
        {
            uint id = netEvent.Peer.ID;

            try
            {
                var readBuffer = new byte[1024];
                var readStream = new MemoryStream(readBuffer);
                var reader = new BinaryReader(readStream);

                readStream.Position = 0;
                netEvent.Packet.CopyTo(readBuffer);
                var packetID = (PacketType)reader.ReadByte();

                switch (packetID)
                {
                    case PacketType.ClientRequestNames:
                        HandlePackets["ClientRequestNames"].Run(id);
                        break;

                    case PacketType.ClientRequestPositions:
                        HandlePackets["ClientRequestPositions"].Run(id);
                        break;

                    case PacketType.ClientPositionUpdate:
                        HandlePackets["ClientPositionUpdate"].Run(id, reader);
                        break;

                    case PacketType.ClientDisconnect:
                        HandlePackets["ClientDisconnect"].Run(id, netEvent);
                        break;
                }   

                readStream.Dispose();
                reader.Dispose();
            }

            catch (ArgumentOutOfRangeException)
            {
                Console.Log(LogType.Warning, $"Received packet from client '{id}' but buffer was too long. {netEvent.Packet.Length}");
            }
        }

        public void Stop()
        {
            serverRunning = false;
        }

        private void CleanUp()
        {
            positionUpdatePump.Dispose();
            server.Dispose();
            Library.Deinitialize();
        }
    }
}
