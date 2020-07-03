using System.IO;

namespace GameServer.Networking.IO
{
    public class PacketReader : BinaryReader 
    {
        private static MemoryStream stream;
        
        public PacketReader(byte[] data) : base(stream = new MemoryStream(data)) { }
    }
}