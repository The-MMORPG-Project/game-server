namespace GameServer.Networking.Packet 
{
    public enum ClientPacketType 
    {
        CreateAccount,
        LoginAccount,
        PositionUpdate,
        RequestPositions,
        Disconnect,
        RequestNames
    }
}