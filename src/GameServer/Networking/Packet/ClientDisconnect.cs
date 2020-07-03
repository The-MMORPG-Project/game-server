using System.Linq;

using ENet;

namespace GameServer.Networking.Packet
{
    public class ClientDisconnect : HandlePacket
    {
        public override void Run(params object[] args)
        {
            var id = (uint)args[0];
            var netEvent = (Event)args[1];

            var peersToSend = Server.clients.FindAll(x => x.Status == ClientStatus.InGame && x.ID != id).Select(x => x.Peer).ToArray();
            Network.Broadcast(Server.server, GamePacket.Create(ServerPacketType.ClientDisconnected, PacketFlags.Reliable, netEvent.Peer.ID), peersToSend);
            netEvent.Peer.Disconnect(netEvent.Peer.ID);
            //Console.Log($"Client '{netEvent.Peer.ID}' disconnected");
        }
    }
}