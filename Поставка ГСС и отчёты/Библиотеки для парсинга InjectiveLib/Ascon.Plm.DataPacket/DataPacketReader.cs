using Ascon.Plm.DataPacket.Enums;
using Ascon.Plm.DataPacket.Exceptions;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;

namespace Ascon.Plm.DataPacket
{
    public sealed class DataPacketReader : IDataReader
    {
        public DataPacketReader(byte[] data)
        {
            _data = data;
            _reader = new BinaryReader(new MemoryStream(data));

            ReadMetadata();
        }

        private readonly byte[] _data;
        private DataPacketHeader _header;
        private readonly BinaryReader _reader;
        private static readonly Encoding Encoding = Encoding.GetEncoding(1251);

        private DataPacketColumn[] _columns;
        private byte[] _nullFlags;

        private Dictionary<string, int> _ordinals;

        private void ReadMetadata()
        {
            ReadHeader();

            ReadColumns();

            if (_header.Properties.HasFlag(DataPacketProperties.ContainDataOffset))
                _reader.BaseStream.Position = _header.DataOffset;
            else
                _reader.ReadInt16();
        }

        private unsafe void ReadHeader()
        {
            if (_data.Length < sizeof(DataPacketHeader))
                throw new InvalidDataPacketException();

            fixed (void* p = _data)
            {
                _header = *(DataPacketHeader*)p;
            }

            if (_header.MagicNo != DataPacketHeader.Magic)
                throw new InvalidDataPacketException();

            _reader.BaseStream.Seek(_header.HeaderSize, SeekOrigin.Begin);
        }

        private static Type GetFieldType(uint fieldType)
        {
            switch (fieldType)
            {
                case ColumnTypes.Bool: return typeof(bool);

                case ColumnTypes.Int16: return typeof(short);
                case ColumnTypes.Int32: return typeof(int);
                case ColumnTypes.Int64: return typeof(long);

                case ColumnTypes.UInt16: return typeof(ushort);

                case ColumnTypes.Float: return typeof(double);
                case ColumnTypes.TimeStamp: return typeof(DateTime);
            }

            if ((fieldType & ColumnTypes.VaryingType) == ColumnTypes.VaryingType)
            {
                switch (fieldType & ColumnTypes.TypeMask)
                {
                    case ColumnTypes.ZString:
                    case ColumnTypes.NString: return typeof(string);
                    case ColumnTypes.Bytes: return typeof(byte[]);
                }
            }

            throw new InvalidDataPacketException();
        }

        private DataPacketColumn ReadColumn(int ordinal)
        {
            var name = ReadAttributeName();

            uint fieldType = _reader.ReadUInt32();
            ushort _ = _reader.ReadUInt16(); //attribute
            ushort attrCount = _reader.ReadUInt16();

            var column = new DataPacketColumn(ordinal) { ColumnName = name };
            column.SetDataType(GetFieldType(fieldType), fieldType, isUnicode: (fieldType & ColumnTypes.TypeMask) == ColumnTypes.NString);

            for (int a = 0; a < attrCount; a++)
            {
                var attr = ReadAttributeName();
                uint attrType = _reader.ReadUInt32();

                if (attr == "WIDTH")
                {
                    column.MaxLength = ReadIntAttribute(attrType);
                }
                else if (attr == "FIELDNAME")
                {
                    column.ColumnName = ReadStringAttribute(attrType);
                }
                else if (attr == "SUBTYPE")
                {
                    var subtype = ReadStringAttribute(attrType);

                    if (subtype == "Text" || subtype == "HMemo" || subtype == "Memo")
                        column.SetDataType(typeof(string), fieldType, isLong: true);
                    else if (subtype == "WideText")
                        column.SetDataType(typeof(string), fieldType, isLong: true, isUnicode: true);
                    else if (subtype == "Binary" || subtype == "HBinary")
                        column.SetDataType(typeof(byte[]), fieldType, isLong: true);
                    else if (subtype == "Guid")
                        column.SetDataType(typeof(Guid), fieldType);
                    else
                        throw new InvalidDataPacketException();
                }
                else
                {
                    throw new InvalidDataPacketException();
                }
            }

            if ((fieldType & ColumnTypes.VaryingType) == ColumnTypes.VaryingType)
            {
                column.LengthSize = (int)(fieldType & ColumnTypes.SizeMask);

                if (column.MaxLength == 0)
                    column.MaxLength = int.MaxValue;
            }

            if (column.MaxLength == 0)
            {
                if (Type.GetTypeCode(column.DataType) == TypeCode.DateTime)
                    column.MaxLength = 8;
                else
                    column.MaxLength = Marshal.SizeOf(column.DataType);
            }

            return column;
        }

        private void ReadColumns()
        {
            if (!_header.Properties.HasFlag(DataPacketProperties.IncludeMetadata))
                throw new InvalidDataPacketException();

            _columns = Enumerable.Range(0, _header.ColumnCount)
                .Select(i => ReadColumn(i)).ToArray();

            _ordinals = Enumerable.Range(0, _columns.Length)
                .ToDictionary(i => _columns[i].ColumnName, i => i);

            _nullFlags = new byte[(_columns.Length + 3) / 4];
        }

        public DataTable GetSchemaTable()
        {
            var schema = new DataTable();
            schema.Columns.AddRange(new[]
            {
                new DataColumn("ColumnName",        typeof(string)),
                new DataColumn("ColumnOrdinal",     typeof(int)),
                new DataColumn("ColumnSize",        typeof(int)),
                new DataColumn("NumericPrecision",  typeof(short)),
                new DataColumn("NumericScale",      typeof(short)),
                new DataColumn("DataType",          typeof(Type)),
                new DataColumn("ProviderType",      typeof(int)),
                new DataColumn("IsLong",            typeof(bool)),
                new DataColumn("AllowDBNull",       typeof(bool)) { DefaultValue = true },
                new DataColumn("IsReadOnly",        typeof(bool)) { DefaultValue = false },
                new DataColumn("IsRowVersion",      typeof(bool)) { DefaultValue = false },
                new DataColumn("IsUnique",          typeof(bool)) { DefaultValue = false },
                new DataColumn("IsKey",             typeof(bool)) { DefaultValue = false },
                new DataColumn("IsAutoIncrement",   typeof(bool)) { DefaultValue = false },
                new DataColumn("BaseCatalogName",   typeof(string)),
                new DataColumn("BaseTableName",     typeof(string)),
                new DataColumn("BaseColumnName",    typeof(string)),
            });

            foreach (var c in _columns)
            {
                var row = schema.NewRow();
                row["ColumnName"] = c.ColumnName;
                row["ColumnOrdinal"] = c.Ordinal;
                row["ColumnSize"] = c.MaxLength;
                row["DataType"] = c.DataType;
                row["IsLong"] = c.IsLong;
                row["BaseColumnName"] = c.ColumnName;
                schema.Rows.Add(row);
            }

            return schema;
        }

        private bool ReadInternal()
        {
            int rowStatus;
            do
            {
                if (RecordsAffected >= _header.RowCount)
                    return false;

                rowStatus = _reader.ReadByte();
                RecordsAffected++;

                _nullFlags = _reader.ReadBytes(_nullFlags.Length);

                for (var i = 0; i < _columns.Length; i++)
                {
                    var column = _columns[i];

                    if (column.SetNullFlag(_nullFlags))
                        continue;

                    switch (column.TypeCode)
                    {
                        case TypeCode.Boolean:
                            column.Storage.Bool = _reader.ReadUInt16() == 1;
                            break;
                        case TypeCode.UInt16:
                            column.Storage.UInt16 = _reader.ReadUInt16();
                            break;
                        case TypeCode.Int16:
                            column.Storage.Int16 = _reader.ReadInt16();
                            break;
                        case TypeCode.Int32:
                            column.Storage.Int32 = _reader.ReadInt32();
                            break;
                        case TypeCode.Int64:
                            column.Storage.Int64 = _reader.ReadInt64();
                            break;
                        case TypeCode.Double:
                            column.Storage.Double = _reader.ReadDouble();
                            break;
                        case TypeCode.DateTime:
                            column.Storage.DateTime = ReadDateTime();
                            break;
                        case TypeCode.String:
                            column.StringStorage = ReadStringValue(column);
                            break;
                        case TypeCode.Object when column.DataType == typeof(Guid):
                            column.Storage.Guid = ReadGuid(column);
                            break;
                        case TypeCode.Object when column.DataType == typeof(byte[]):
                            column.BytesStorage = ReadBinaryBlobValue(column);
                            break;
                        default:
                            throw new InvalidDataPacketException();
                    }
                }
            } while ((rowStatus & 0x01 + 0x02 + 0x20) != 0);

            return true;
        }

        private DateTime ReadDateTime()
        {
            long millisecondsSince02010001 = (long)_reader.ReadDouble();
            return new DateTime(millisecondsSince02010001 * TimeSpan.TicksPerMillisecond - TimeSpan.TicksPerDay);
        }

        private static readonly char[] ReadStringTrimChars = { '\0' };

        private string ReadStringValue(DataPacketColumn column)
        {
            var length = ReadLength(column.LengthSize);
            var offset = _reader.BaseStream.Position;
            _reader.BaseStream.Seek(length, SeekOrigin.Current);

            var encoding = column.IsUnicode ? Encoding.Unicode : Encoding;

            return encoding.GetString(_data, (int)offset, length).TrimEnd(ReadStringTrimChars);
        }

        private Guid ReadGuid(DataPacketColumn column)
            => Guid.Parse(ReadStringValue(column));

        private ArraySegment<byte> ReadBinaryBlobValue(DataPacketColumn column)
        {
            var length = ReadLength(column.LengthSize);
            var offset = _reader.BaseStream.Position;
            _reader.BaseStream.Seek(length, SeekOrigin.Current);
            return new ArraySegment<byte>(_data, (int)offset, length);
        }

        private string ReadAttributeName()
        {
            int len = _reader.ReadByte();
            var bytes = _reader.ReadBytes(Math.Min(32, len));
            return Encoding.GetString(bytes);
        }

        private int ReadIntAttribute(uint attrType)
        {
            return attrType switch
            {
                ColumnTypes.Int32 => _reader.ReadInt32(),
                ColumnTypes.UInt16 => _reader.ReadUInt16(),
                _ => throw new InvalidDataPacketException(),
            };
        }

        private int ReadLength(int lengthSize)
        {
            return lengthSize switch
            {
                1 => _reader.ReadByte(),
                2 => _reader.ReadInt16(),
                4 => _reader.ReadInt32(),
                _ => throw new InvalidDataPacketException(),
            };
        }

        private string ReadStringAttribute(uint attrType)
        {
            if ((attrType & (ColumnTypes.ZString | ColumnTypes.VaryingType)) != (ColumnTypes.ZString | ColumnTypes.VaryingType))
                throw new InvalidDataPacketException();

            var length = ReadLength((int)(attrType & ColumnTypes.SizeMask));
            var bytes = _reader.ReadBytes(length);

            return Encoding.GetString(bytes).TrimEnd('\0');
        }

        public int Depth => 0;

        public void Close() { }
        public bool IsClosed => false;

        public int RecordsAffected { get; private set; }

        public object this[string name] => GetValue(GetOrdinal(name));
        public object this[int i] => GetValue(i);

        public bool NextResult() => false;

        public bool Read() => ReadInternal();

        public void Dispose() { }

        public string GetDataTypeName(int i) => _columns[i].DataType.Name;

        public int FieldCount => _columns.Length;

        public string GetName(int i) => _columns[i].ColumnName;

        public Type GetFieldType(int i) => _columns[i].DataType;

        public int GetOrdinal(string name)
        {
            return _ordinals.TryGetValue(name, out var value) ? value : throw new KeyNotFoundException($"{name} отсутствует в словаре");
        }

        public object GetValue(int i) => _columns[i].GetValue();

        public int GetValues(object[] values)
        {
            for (int i = 0; i < _columns.Length; i++)
                values[i] = this[i];

            return _columns.Length;
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) =>
            _columns[i].GetBytes(fieldOffset, buffer, bufferoffset, length);

        public bool GetBoolean(int i) => _columns[i].Bool;

        public byte GetByte(int i) => (byte)_columns[i].Int16;

        public short GetInt16(int i) => _columns[i].Int16;

        public int GetInt32(int i) => _columns[i].Int32;

        public long GetInt64(int i) => _columns[i].Int64;

        public float GetFloat(int i) => (float)_columns[i].Double;

        public double GetDouble(int i) => _columns[i].Double;

        public string GetString(int i) => _columns[i].String;

        public DateTime GetDateTime(int i) => _columns[i].DateTime;

        public Guid GetGuid(int i) => _columns[i].Guid;

        public bool IsDBNull(int i) => _columns[i].IsNull;

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }
    }
}
