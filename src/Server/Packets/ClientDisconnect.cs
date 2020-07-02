using System.Linq;

using ENet;

namespace Valk.Networking
{
    public class ClientDisconnect : HandlePacket
    {
        public override void Run(params object[] args)
        {
            var id = (uint)args[0];
            var netEvent = (Event)args[1];

            var peersToSend = Server.clients.FindAll(x => x.Status == ClientStatus.InGame && x.ID != id).Select(x => x.Peer).ToArray();
            Network.Broadcast(Server.server, Packet.Create(PacketType.ServerClientDisconnected, PacketFlags.Reliable, netEvent.Peer.ID), peersToSend);
            netEvent.Peer.Disconnect(netEvent.Peer.ID);
            //Logger.Log($"Client '{netEvent.Peer.ID}' disconnected");
        }
    }
}