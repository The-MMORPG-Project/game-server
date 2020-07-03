namespace GameServer.Networking.Packet 
{
    enum ErrorType 
    {
        AccountCreateNameAlreadyRegistered,
        AccountLoginDoesNotExist,
        AccountLoginWrongPassword
    }

    class GamePacket
    {
        public static ENet.Packet Create(ServerPacketType type, ENet.PacketFlags packetFlagType, params object[] values)
        {
            using var protocol = new Protocol();
            var buffer = protocol.Serialize((byte)type, values);
            var packet = default(ENet.Packet);
            packet.Create(buffer, packetFlagType);
            return packet;
        }
    }
}