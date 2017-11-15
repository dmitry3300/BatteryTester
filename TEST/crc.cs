using System;



namespace DMSS.CRC
{
    public class Crc16
    {
        const ushort polynomial = 0x8408;
        static ushort[] table = new ushort[256];
        public static bool TestChecksum(byte[] bytes)
        {
            ushort crc = 0xffff;
            ushort test_crc;
            int i;
            if (bytes.Length < 3) return false;

            for (i = 0; i < (bytes.Length - 2); ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            crc = (ushort)(~(((crc & 0xff) << 8) | (crc >> 8)));
            test_crc = (ushort)(bytes[i++] * 256 + bytes[i]);
            if (crc == test_crc) return true;
            else return false;
        }

        public static ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = 0xffff;
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            return (ushort)(~(((crc & 0xff) << 8) | (crc >> 8)));
        }

        public static byte[] ComputeChecksumBytes(byte[] bytes)
        {
            ushort crc = ComputeChecksum(bytes);
            crc = (ushort)((crc << 8) | (crc >> 8));
            return BitConverter.GetBytes(crc);
        }

        static Crc16()
        {
            ushort value;
            ushort temp;
            for (ushort i = 0; i < table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }
        }
    }

}


