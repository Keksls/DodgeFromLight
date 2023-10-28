using DFLNetwork;

namespace DFLNetwork.Protocole.Serialization
{
    public abstract class Serializer
    {
        public abstract byte[] Serialize(NetworkMessage message);
        public abstract NetworkMessage Deserialize(byte[] message);
    }
}