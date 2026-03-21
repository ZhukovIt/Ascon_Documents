namespace Ascon.Plm.DataPacket.Exceptions
{
    public sealed class InvalidDataPacketException : Exception
    {
        public InvalidDataPacketException()
            : base("Invalid DataPacket")
        {

        }

        public InvalidDataPacketException(string message)
            : base(message)
        {

        }
    }
}
