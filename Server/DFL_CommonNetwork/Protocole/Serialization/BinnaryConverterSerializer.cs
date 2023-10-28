using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace DFLNetwork.Protocole.Serialization
{
    public class BinnaryConverterSerializer : Serializer
    {
        IFormatter formatter = new BinaryFormatter();

        public override NetworkMessage Deserialize(byte[] message)
        {
            using (MemoryStream stream = new MemoryStream(message))
            {
                return (NetworkMessage)formatter.Deserialize(stream);
            }

            //using (var memStream = new MemoryStream())
            //{
            //    memStream.Write(message, 0, message.Length);
            //    memStream.Seek(0, SeekOrigin.Begin);
            //    NetworkMessage retVal = (NetworkMessage)new BinaryFormatter().Deserialize(memStream);
            //    return retVal;
            //}
        }

        public override byte[] Serialize(NetworkMessage message)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, message);
                return stream.ToArray();
            }

            //BinaryFormatter bf = new BinaryFormatter();
            //using (var ms = new MemoryStream())
            //{
            //    bf.Serialize(ms, message);
            //    return ms.ToArray();
            //}
        }
    }
}