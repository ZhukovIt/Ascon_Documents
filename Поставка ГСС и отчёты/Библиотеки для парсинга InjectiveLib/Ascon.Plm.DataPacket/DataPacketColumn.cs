using Ascon.Plm.DataPacket.Exceptions;
using System.Runtime.InteropServices;

namespace Ascon.Plm.DataPacket
{
    internal sealed class DataPacketColumn
    {
        public DataPacketColumn(int ordinal)
        {
            Ordinal = ordinal;

            _nullMask = 0x3 << ordinal % 4 * 2;
            _nullByteIndex = ordinal / 4;
        }

        public int Ordinal { get; }
        public string ColumnName { get; set; }

        public uint MidasType { get; private set; }
        public Type DataType { get; private set; }
        public TypeCode TypeCode { get; private set; }
        public bool IsLong { get; private set; }
        public bool IsUnicode { get; set; }

        public void SetDataType(Type type, uint midasType, bool isLong = false, bool isUnicode = false)
        {
            MidasType = midasType;
            DataType = type;
            TypeCode = Type.GetTypeCode(type);
            IsLong = isLong;
            IsUnicode = isUnicode;
        }

        public int MaxLength { get; set; }
        public int LengthSize { get; set; }

        private readonly int _nullMask;
        private readonly int _nullByteIndex;

        public bool IsNull { get; private set; }

        public bool SetNullFlag(byte[] status)
        {
            return IsNull = (status[_nullByteIndex] & _nullMask) > 0;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DataStorage
        {
            [FieldOffset(0)]
            public bool Bool;
            [FieldOffset(0)]
            public ushort UInt16;
            [FieldOffset(0)]
            public short Int16;
            [FieldOffset(0)]
            public int Int32;
            [FieldOffset(0)]
            public long Int64;
            [FieldOffset(0)]
            public double Double;
            [FieldOffset(0)]
            public DateTime DateTime;
            [FieldOffset(0)]
            public Guid Guid;
        }

        public DataStorage Storage;
        public string StringStorage;
        public ArraySegment<byte> BytesStorage;

        public long GetBytes(long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            if (!CheckNullAndType(TypeCode.Object))
                throw new InvalidCastException($"Method GetBytes does not applicable for column [{ColumnName}] with type {DataType.Name}");

            if (buffer == null)
                return BytesStorage.Count;

            long actualLength = Math.Min(BytesStorage.Count - fieldOffset, length);

            Array.Copy(BytesStorage.Array, BytesStorage.Offset + fieldOffset, buffer, bufferoffset, actualLength);

            return actualLength;
        }

        public byte[] GetBytes()
        {
            var length = GetBytes(0, null, 0, 0);
            var bytes = new byte[length];
            GetBytes(0, bytes, 0, (int)length);
            return bytes;
        }

        public object GetValue()
        {
            if (IsNull)
                return DBNull.Value;

            return TypeCode switch
            {
                TypeCode.Boolean => Storage.Bool,
                TypeCode.UInt16 => Storage.UInt16,
                TypeCode.Int16 => Storage.Int16,
                TypeCode.Int32 => Storage.Int32,
                TypeCode.Int64 => Storage.Int64,
                TypeCode.Double => Storage.Double,
                TypeCode.DateTime => Storage.DateTime,
                TypeCode.String => StringStorage,
                TypeCode.Object when DataType == typeof(Guid) => Storage.Guid,
                TypeCode.Object when DataType == typeof(byte[]) => GetBytes(),
                _ => throw new InvalidDataPacketException(),
            };
        }

        private bool CheckNullAndType(TypeCode code)
        {
            if (IsNull)
                throw new DataPacketNullValueException(ColumnName);
            return TypeCode == code;
        }

        public bool Bool => CheckNullAndType(TypeCode.Boolean) ? Storage.Bool : (bool)Object;
        public short Int16 => CheckNullAndType(TypeCode.Int16) ? Storage.Int16 : (short)Object;
        public int Int32 => CheckNullAndType(TypeCode.Int32) ? Storage.Int32 : (int)Object;
        public long Int64 => CheckNullAndType(TypeCode.Int64) ? Storage.Int64 : (long)Object;
        public double Double => CheckNullAndType(TypeCode.Double) ? Storage.Double : (double)Object;
        public DateTime DateTime => CheckNullAndType(TypeCode.DateTime) ? Storage.DateTime : (DateTime)Object;
        public string String => CheckNullAndType(TypeCode.String) ? StringStorage : (string)Object;
        public Guid Guid => CheckNullAndType(TypeCode.Object) ? Storage.Guid : (Guid)Object;
        public object Object => GetValue();
    }
}
