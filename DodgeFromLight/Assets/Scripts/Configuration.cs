using UnityEngine;

public class Configuration
{
    public string AdresseIP { get; set; }
    public int Port { get; set; }
}

public static class ConfigurationManager
{
    private static Configuration config = null;

    public static void Initialize()
    {
        config = Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText(Application.streamingAssetsPath + @"\Config.json"));
    }

    public static Configuration Config
    {
        get
        {
            return config;
        }
    }
}