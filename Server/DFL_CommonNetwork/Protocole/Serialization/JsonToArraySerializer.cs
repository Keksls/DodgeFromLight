using Newtonsoft.Json;

namespace DFLNetwork.Protocole.Serialization
{
    public class JsonToArraySerializer : Serializer
    {
        public override NetworkMessage Deserialize(byte[] message)
        {
            return JsonConvert.DeserializeObject<NetworkMessage>(System.Text.Encoding.Default.GetString(message));
        }

        public override byte[] Serialize(NetworkMessage message)
        {
            return System.Text.Encoding.Default.GetBytes(JsonConvert.SerializeObject(message));
        }
    }
}