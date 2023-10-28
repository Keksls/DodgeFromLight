using Newtonsoft.Json;

public class InterractibleObject
{
    public InterractibleObjectType Type { get; set; }
    public string JsonData { get; set; }

    public SerializableMovingPlatform GetSerializableMovingPlatform()
    {
        return JsonConvert.DeserializeObject<SerializableMovingPlatform>(JsonData);
    }

    public void SetJsonData(MovingPlatform platform)
    {
        JsonData = JsonConvert.SerializeObject(platform.ToSerializable());
    }
}

public enum InterractibleObjectType
{
    MovingPlatform
}