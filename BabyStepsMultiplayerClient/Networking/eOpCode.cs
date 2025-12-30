namespace BabyStepsMultiplayerClient.Networking
{
    internal enum eOpCode : byte
    {
        // Player
        BonePositionUpdate = 0x01, // Bone position update
        PlayerInformation = 0x02, // Update color information (used for nickname too now)

        ChatMessage = 0x08,
        AudioFrame = 0x09,

        // World
        GenericWorldEvent = 0x03, // Generic World Event

        // Accessory
        AddAccessory = 0x04, // Accessory Add(Don) Event
        RemoveAccessory = 0x05, // Accessory Remove(Doff) Event
        JiminyRibbon = 0x06, // Jiminy Ribbon Event

        // User Settings
        ToggleCollisions = 0x07, // Collision Toggle Event

        // Server Ping Request
        Ping = 0x0A,
    }
}
