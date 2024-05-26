using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gablibela.io;
using SevenZip.Compression.LZMA;

namespace gablibela.cmpr
{
    public static class LZMA
    {
        public static bool isLZMA(byte[] source)
        {
            if(source == null) return false;
            if(source.Length < 0xE) return false;
            if (source[0] != 0x5D && source[0] != 0x2C && source[0] != 0x18) return false;
            if (source[0xD] != 0x00) return false;
            if (source[0x1] != 0x00) return false;  
            if (source[0x2] != 0x00) return false;  


            return true;
        }

        public static byte[] Decode(byte[] source)
        {
            SevenZip.Compression.LZMA.Decoder decoder = new();

            MemoryStream stream = new MemoryStream(source);

            MemoryStream output = new();

            byte[] properties = new byte[5];
            byte[] fileLenghtBytes = new byte[8];
            long fileLenght;
            stream.Read(properties, 0, 5);
            stream.Read(fileLenghtBytes, 0, 8);
            fileLenght = BitConverter.ToInt64(fileLenghtBytes);

            decoder.SetDecoderProperties(properties);
            decoder.Code(stream, output, stream.Length, fileLenght, null);

            return output.ToArray();
        }

        public static byte[] Encode(byte[] source)
        {
            SevenZip.Compression.LZMA.Encoder encoder = new();

            MemoryStream stream = new MemoryStream(source);
            MemoryStream output = new();

            encoder.WriteCoderProperties(output);
            for (int i = 0; i < 8; i++) output.WriteByte(0xFF);

            encoder.Code(stream, output, -1, -1, null);
            return output.ToArray();

        }
    }
}
