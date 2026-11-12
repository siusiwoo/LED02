namespace LED02.Packet
{
    internal static class Protocol
    {
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
        public const byte ACK = 0x06;
        public const byte NAK = 0x15;
        public const byte Status_READ = 0x01;
        public const byte CMD_CALC = 0x10;
        public const byte CMD_START = 0x11;
        public const byte CMD_STOP = 0x12;
        public const byte CMD_DONE = 0x13;
    }
}