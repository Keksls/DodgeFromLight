using System;
using System.IO;
using System.IO.Compression;
namespace DFLNetwork.Protocole.Compression
{
    public class GZipCompressor : Compressor
    {
        public override byte[] Compress(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream();
            GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true);
            zip.Write(buffer, 0, buffer.Length);
            zip.Close();
            ms.Position = 0;
            MemoryStream outStream = new MemoryStream();
            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);
            byte[] gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return gzBuffer;
        }

        public override byte[] Decompress(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream();
            int msgLength = BitConverter.ToInt32(buffer, 0);
            ms.Write(buffer, 4, buffer.Length - 4);
            byte[] gzbuffer = new byte[msgLength];
            ms.Position = 0;
            GZipStream zip = new GZipStream(ms, CompressionMode.Decompress);
            zip.Read(gzbuffer, 0, gzbuffer.Length);
            return gzbuffer;
        }
    }
}
