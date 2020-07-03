namespace GameServer.Networking.Packet 
{
    public enum ServerPacketType 
    {
        CreateAccountDenied,
        CreateAccountAccepted,
        LoginDenied,
        LoginAccepted,
        PositionUpdate,
        ClientDisconnected,
        ClientName,
        InitialPositionUpdate
    }
}