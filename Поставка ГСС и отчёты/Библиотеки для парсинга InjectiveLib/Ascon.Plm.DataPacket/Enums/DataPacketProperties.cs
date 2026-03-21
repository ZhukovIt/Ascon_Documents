namespace Ascon.Plm.DataPacket.Enums
{
    [Flags]
    internal enum DataPacketProperties : uint
    {
        IncludeMetadata = 1,
        ContainDataOffset = 2
    }
}
