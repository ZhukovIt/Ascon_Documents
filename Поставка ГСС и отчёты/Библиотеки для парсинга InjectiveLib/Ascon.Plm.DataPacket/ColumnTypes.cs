namespace Ascon.Plm.DataPacket
{
    internal static class ColumnTypes
    {
        public const uint Int = 1 << 16;
        public const uint Int16 = Int | 2;
        public const uint Int32 = Int | 4;
        public const uint Int64 = Int | 8;

        public const uint UInt = 2 << 16;
        public const uint UInt16 = UInt | 2;

        public const uint Bool = 3 << 16 | 2;
        public const uint Float = 4 << 16 | 8;
        public const uint TimeStamp = 8 << 16 | 8;

        public const uint ZString = 9 << 16;
        public const uint NString = 10 << 16;
        public const uint Bytes = 11 << 16;
        public const uint SizeMask = 0x0000FFFF;
        public const uint TypeMask = 0x003F0000;
        public const uint VaryingType = 0x00400000;

        public const uint Guid = ZString | VaryingType | sizeof(byte);
    }
}
