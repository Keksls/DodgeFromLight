using Newtonsoft.Json;
using System.Collections;
using System.IO;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class EditorMapCreator : MonoBehaviour
{
#if UNITY_EDITOR
    public bool GeneratePreviews = false;
    public bool BindMapData = false;
    public string IDMapToConvert;
    public Texture2D mazePrev;
    public int mapSize = 64;

    void Start()
    {
        if (GeneratePreviews)
        {
            GeneratePrev("MapDeco");
            GeneratePrev("MapFloor");
            GeneratePrev("MapTiles");
        }

        if (BindMapData)
            StartCoroutine(BindResourcesMapData());

        string path = @"C:\Users\Keks\DodgeFromLight\DodgeFromLight_3D\Assets\StreamingAssets\Tutorial";
        foreach (var dir in Directory.GetDirectories(path))
        {
            string gridPath = dir + @"\grid.json";
            if (File.Exists(gridPath))
            {
                Grid grid = JsonConvert.DeserializeObject<Grid>(File.ReadAllText(gridPath));
                grid.Environment = DodgeFromLight.EnvironmentController.GetBuildinEnvironment(BuildInEnvironments.Default);
                File.WriteAllText(gridPath, JsonConvert.SerializeObject(grid));
            }
        }

        //if (!string.IsNullOrEmpty(IDMapToConvert))
        //{
        //    Grid grid = GridManager.GetGrid(IDMapToConvert);
        //    foreach (var en in grid.Ennemies)
        //    {
        //        switch (en.Type)
        //        {
        //            case EnnemyType.Cyclops:
        //                en.TA = 1;
        //                en.PO = 3;
        //                break;
        //            case EnnemyType.Gloutton:
        //                en.TA = 1;
        //                en.PO = 1;
        //                break;
        //            case EnnemyType.Keeper:
        //                en.TA = 0;
        //                en.PO = 2;
        //                break;
        //            case EnnemyType.Knight:
        //                en.TA = 1;
        //                en.PO = 1;
        //                break;
        //            case EnnemyType.Scoute:
        //                en.TA = 1;
        //                en.PO = 3;
        //                break;
        //            case EnnemyType.Trap:
        //                en.TA = 0;
        //                en.PO = 4;
        //                break;
        //            case EnnemyType.Lizardman:
        //                en.TA = 1;
        //                en.PO = 2;
        //                break;
        //            case EnnemyType.Thunderbird:
        //                en.TA = 1;
        //                en.PO = 1;
        //                break;
        //            case EnnemyType.Axe:
        //                en.TA = 0;
        //                en.PO = 0;
        //                break;
        //            case EnnemyType.Hammer:
        //                en.TA = 0;
        //                en.PO = 0;
        //                break;
        //            case EnnemyType.Catapult:
        //                en.TA = 0;
        //                en.PO = 0;
        //                break;
        //            case EnnemyType.Pillar:
        //                en.TA = 0;
        //                en.PO = 0;
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //    File.WriteAllText(GridManager.GetGridPath(IDMapToConvert), JsonConvert.SerializeObject(grid));
        //}

        //if (mazePrev != null)
        //{
        //    float scale = (float)mazePrev.width / (float)mapSize;
        //    Grid grid = new Grid(mapSize, mapSize);
        //    float gs = 0f;
        //    for (int x = 0; x < grid.Width; x++)
        //        for (int y = 0; y < grid.Width; y++)
        //        {
        //            gs = mazePrev.GetPixel((int)((float)x * scale + (scale / 2f)), (int)((float)y * scale + (scale / 2f))).grayscale;
        //            grid.Cells[x, y].SetType(gs > 0.5f ? CellType.Walkable : CellType.NotWalkable);
        //            if (gs < 0.9 && gs > 0.2)
        //            {
        //                grid.Cells[x, y].FloorID = "EmptyTile3";
        //            }
        //        }
        //    GridManager.SaveMap(new FullMap(grid, new Map("maze", grid.Width, grid.Height) { State = MapState.InWork }), mazePrev.EncodeToPNG());
        //}
    }

    void GeneratePrev(string folder)
    {
        GameObject[] objs = Resources.LoadAll<GameObject>(folder);
        foreach (GameObject obj in objs)
        {
            Texture2D previewImage = null;
            while (previewImage == null)
            {
                previewImage = AssetPreview.GetAssetPreview(obj);
                Thread.Sleep(10);
            }
            string path = Application.dataPath + @"\Resources\" + folder + @"\previews\" + obj.name + ".png";
            File.WriteAllBytes(path, previewImage.EncodeToPNG());
        }
    }

    IEnumerator BindResourcesMapData()
    {
        yield return null;
        //string[] paths = new string[] { "Deco", "Floor", "Tiles" };
        //ResourcesData.Deco.Clear();
        //ResourcesData.Floor.Clear();
        //ResourcesData.Tiles.Clear();

        //foreach (string path in paths)
        //{
        //    GameObject[] objs = Resources.LoadAll<GameObject>("Map" + path);
        //    yield return null;
        //    Texture2D[] txts = Resources.LoadAll<Texture2D>("Map" + path);
        //    yield return null;
        //    for (int i = 0; i < objs.Length; i++)
        //    {
        //        MapDecoData d = new MapDecoData()
        //        {
        //            ID = objs[i].name,
        //            Prefab = objs[i],
        //            Texture = txts[i]
        //        };

        //        switch (path)
        //        {
        //            case "Deco":
        //                ResourcesData.Deco.Add(d);
        //                break;
        //            case "Floor":
        //                ResourcesData.Floor.Add(d);
        //                break;
        //            case "Tiles":
        //                ResourcesData.Tiles.Add(d);
        //                break;
        //        }
        //    }
        //}
        //EditorUtility.SetDirty(ResourcesData);
    }
#endif
}