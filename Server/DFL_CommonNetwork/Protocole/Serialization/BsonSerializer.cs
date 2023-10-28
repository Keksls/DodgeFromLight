using System.IO;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;

namespace DFLNetwork.Protocole.Serialization
{
    public class BsonSerializer : Serializer
    {
        private JsonSerializer serializer = new JsonSerializer();

        public override NetworkMessage Deserialize(byte[] message)
        {
            MemoryStream ms = new MemoryStream(message);
            using (BsonReader reader = new BsonReader(ms))
                return serializer.Deserialize<NetworkMessage>(reader);
        }

        public override byte[] Serialize(NetworkMessage message)
        {
            MemoryStream ms = new MemoryStream();
            using (BsonWriter writer = new BsonWriter(ms))
                serializer.Serialize(writer, message);
            return ms.ToArray();
        }
    }
}