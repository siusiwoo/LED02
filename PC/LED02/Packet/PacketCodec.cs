namespace LED02.Packet
{
    internal static class PacketCodec
    {
        public static void AddInt(List<byte> data, int value)
        {
            int len = 1;
            while (value >> (len * 8 - 1) != 0 && value >> (len * 8 - 1) != -1 && len < 4)
            {
                len++;
            }
            data.Add((byte)len);
            for (int i = 0; i < len; i++)
            {
                data.Add((byte)(value >> (len - 1 - i) * 8));
            }
        }
        public static byte CalcCrc(byte[] data, int start, int len)
        {
            byte crc = 0;
            for (int i = start; i < start + len; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc = (byte)(crc << 1 ^ 0x07);
                    }
                    else
                    {
                        crc = crc <<= 1;
                    }
                }
            }
            return crc;
        }
        public static long ReadValue(byte[] data, ref int start)
        {
            long value = 0;
            for (int i = 0; i < data[start]; i++)
            {
                value = value << 8;
                value += data[start + 1 + i];

            }
            if ((data[start + 1] & 0x80) != 0)
            {
                value |= (long)-1 << (data[start] * 8);
            }
            start += 1 + data[start];
            return value;
        }
    }
}