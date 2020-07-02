namespace GameServer.Framework.Networking.Packets
{
    public abstract class HandlePacket
    {
        public virtual void Run(params object[] args) {}
    }
}