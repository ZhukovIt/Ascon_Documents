namespace Ascon.Plm.DataPacket.Exceptions
{
    public sealed class DataPacketNullValueException : Exception
    {
        public DataPacketNullValueException(string name)
            : base($"Column {name} has null value")
        {

        }
    }
}
