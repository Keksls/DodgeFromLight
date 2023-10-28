using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourcesData", menuName = "DodgeFromLight/ResourcesData", order = 1)]
public class ResourcesData : ScriptableObject
{
    public List<MapDecoData> Data;
    public HashSet<string> Types;

    private Dictionary<string, MapDecoData> dicData;

    public void Initialize()
    {
        dicData = new Dictionary<string, MapDecoData>();
        Types = new HashSet<string>();

        foreach (MapDecoData deco in Data)
        {
            dicData.Add(deco.ID, deco);
            Types.Add(deco.Type);
        }
    }

    public MapDecoData GetMapDeco(string id)
    {
        return dicData[id];
    }

    public bool HasMapDeco(string id)
    {
        return dicData.ContainsKey(id);
    }

    public Dictionary<string, MapDecoData> GetDataForType(string type)
    {
        Dictionary<string, MapDecoData> dic = new Dictionary<string, MapDecoData>();
        foreach (var pair in dicData)
            if (pair.Value.Type == type)
                dic.Add(pair.Key, pair.Value);
        return dic;
    }
}

[System.Serializable]
public class MapDecoData
{
    [SerializeField]
    public Texture2D Texture;
    [SerializeField]
    public GameObject Prefab;
    [SerializeField]
    public string ID;
    [SerializeField]
    public string Type;
    [SerializeField]
    public List<MapDecoCellPos> UnWalkableCells;
    [SerializeField]
    public float BaseScale = 1.0f;
    [SerializeField]
    public bool Scalable = true;
    [SerializeField]
    public bool FreeRotation = true;
    public bool SelfUnwalkable { get { return UnWalkableCells.Count == 0; } }

    public bool CanFreeRotate()
    {
        return FreeRotation && UnWalkableCells.Count <= 1;
    }
}

[System.Serializable]
public class MapDecoCellPos
{
    [SerializeField]
    public int X;
    [SerializeField]
    public int Y;

    public MapDecoCellPos(int x, int y)
    {
        X = x;
        Y = y;
    }
}