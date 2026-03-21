using Ascon.Plm.DataPacket.Enums;
using System.Runtime.InteropServices;

namespace Ascon.Plm.DataPacket
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 24)]
    internal struct DataPacketHeader
    {
        public const uint Magic = 0xBDE01996;

        public uint MagicNo;
        public ushort MajorVer;
        public ushort MinorVer;
        public uint HeaderSize;
        public ushort ColumnCount;
        public uint RowCount;
        public DataPacketProperties Properties;
        public ushort DataOffset;
    }
}
