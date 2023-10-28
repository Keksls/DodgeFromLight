using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public static class GridManager
{
    public static readonly string Folder = Application.persistentDataPath + @"\Temp";

    static GridManager()
    {
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
    }

    /// <summary>
    /// copy default hub grid to current grid folder
    /// </summary>
    public static void InitDefaultHub()
    {
        CleanFolder();
        string hubPath = GetGridPath();
        if (!File.Exists(hubPath))
        {
            string gridPath = Application.streamingAssetsPath + @"\Hub\DefaultHub.json";
            File.Copy(gridPath, hubPath, true);
        }
    }

    /// <summary>
    /// clean current grid folder
    /// </summary>
    public static void CleanFolder()
    {
        if (Directory.Exists(Folder))
            Directory.Delete(Folder, true);
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
    }

    /// <summary>
    /// get grid path
    /// </summary>
    /// <returns></returns>
    public static string GetGridPath()
    {
        return GetGridPath(Folder);
    }

    /// <summary>
    /// get metadata path
    /// </summary>
    /// <returns></returns>
    public static string GetMapPath()
    {
        return GetMapPath(Folder);
    }

    /// <summary>
    /// Get preview path
    /// </summary>
    /// <returns></returns>
    public static string GetPreviewPath()
    {
        return GetPreviewPath(Folder);
    }

    /// <summary>
    /// get grid path
    /// </summary>
    /// <returns></returns>
    public static string GetGridPath(string folder)
    {
        return Path.Combine(folder, "grid.json");
    }

    /// <summary>
    /// get metadata path
    /// </summary>
    /// <returns></returns>
    public static string GetMapPath(string folder)
    {
        return Path.Combine(folder, "metadata.json");
    }

    /// <summary>
    /// Get preview path
    /// </summary>
    /// <returns></returns>
    public static string GetPreviewPath(string folder)
    {
        return Path.Combine(folder, "preview.png");
    }

    /// <summary>
    /// get full map from given folder
    /// </summary>
    /// <returns></returns>
    public static FullMap GetFullMap(string folder)
    {
        return new FullMap(GetGrid(folder), GetMap(folder));

    }

    /// <summary>
    /// Get current grid from local folder
    /// </summary>
    /// <returns></returns>
    public static Grid GetGrid()
    {
        return GetGrid(Folder);
    }

    /// <summary>
    /// Get grid from given folder
    /// </summary>
    /// <returns></returns>
    public static Grid GetGrid(string folder)
    {
        string path = GetGridPath(folder);
        if (!File.Exists(path))
            return null;
        string json = File.ReadAllText(path);
        Grid grid = JsonConvert.DeserializeObject<Grid>(json);
        return grid;
    }

    /// <summary>
    /// Get Map from local folder
    /// </summary>
    /// <returns></returns>
    public static Map GetMap()
    {
        return GetMap(Folder);
    }

    /// <summary>
    /// Get Map from given folder
    /// </summary>
    /// <returns></returns>
    public static Map GetMap(string folder)
    {
        string path = GetMapPath(folder);
        if (!File.Exists(path))
            return null;
        string json = File.ReadAllText(path);
        Map map = JsonConvert.DeserializeObject<Map>(json);
        return map;
    }

    /// <summary>
    /// get current full map
    /// </summary>
    /// <returns></returns>
    public static FullMap GetFullMap()
    {
        return new FullMap(GetGrid(), GetMap());
    }

    /// <summary>
    /// Save a full map
    /// </summary>
    /// <param name="map">map metadata</param>
    /// <param name="preview">byte array (png prev)</param>
    /// <param name="path">folder to save</param>
    public static void SaveFullMap(FullMap map, byte[] preview)
    {
        map.Grid.ClearCellsForSave();
        CleanFolder();
        File.WriteAllText(GetGridPath(), JsonConvert.SerializeObject(map.Grid));
        File.WriteAllBytes(GetPreviewPath(), preview);
        File.WriteAllText(GetMapPath(), JsonConvert.SerializeObject(map.Map));
    }

    /// <summary>
    /// Save a map
    /// </summary>
    /// <param name="map">map metadata</param>
    public static void SaveMap(Map map)
    {
        File.WriteAllText(GetMapPath(), JsonConvert.SerializeObject(map));
    }

    /// <summary>
    /// Save a grid
    /// </summary>
    /// <param name="grid">map grid</param>
    public static void SaveGrid(Grid grid)
    {
        File.WriteAllText(GetGridPath(), JsonConvert.SerializeObject(grid));
    }
}