using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MapProbsPreparator : MonoBehaviour
{
    public Camera cam;
    public Transform DataItemContainer;
    public GameObject[] Deco;

    public GameObject UnWalkableFeedbackPrefab;

    public string[] Types;

    public bool CreateScreenshot = true;
    public bool CreateDefaultData = false;
    public bool EditData = false;
    public bool AsignateTextures;
    private int currentDataItem = 0;

    private GameObject CurrentItem;
    private MapDecoData CurrentItemData;

    private void Start()
    {
        //RescaleMaps();return;

        if (CreateScreenshot)
            StartCoroutine(ScreenShot(System.Environment.CurrentDirectory + @"\Assets\Resources\MapAssets\previews\"));
        if (CreateDefaultData)
            CreateBaseResourcesData();
        if (EditData)
            LoadDataItem();
        if (AsignateTextures)
            AssignateTextures();
    }

    void RescaleMaps()
    {
        string mapsPath = @"C:\Users\Keks\DodgeFromLight\DodgeFromLight_3D\Assets\StreamingAssets\Tutorial";
        foreach (string dir in Directory.GetDirectories(mapsPath))
        {
            string gridPath = dir + @"\grid.json";
            Grid g = JsonConvert.DeserializeObject<Grid>(File.ReadAllText(gridPath));
            for (int x = 0; x < g.Width; x++)
                for (int y = 0; y < g.Height; y++)
                    if (!string.IsNullOrEmpty(g.GetCell(x, y).ElementID))
                    {
                        g.GetCell(x, y).ElementScale /= 3f;
                        g.GetCell(x, y).HasElement = true;
                    }
            File.WriteAllText(gridPath, JsonConvert.SerializeObject(g));
        }
    }

    void AssignateTextures()
    {
        string folderPath = @"MapAssets/previews/";
        foreach (var mdd in DodgeFromLight.Databases.ResourcesData.Data)
        {
            mdd.Texture = Resources.Load<Texture2D>(folderPath + mdd.ID);
        }
    }

    void Update()
    {
        if (!EditData)
            return;

        UI();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                // CurrentItem.transform.Rotate(Vector3.up * -90f);
            }
            else
            {
                PreviewDataItem();
                LoadDataItem();
            }
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                //CurrentItem.transform.Rotate(Vector3.up * 90f);
            }
            else
            {
                NextDataItem();
                LoadDataItem();
            }
        }

        // handle types input
        for (int i = 1; i <= Types.Length; i++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)))
                CurrentItemData.Type = Types[i - 1];

        // click on tile
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                string name = hit.collider.gameObject.name;
                string[] spl = name.Split('_');
                if (spl.Length == 2)
                {
                    int x = int.Parse(spl[0]);
                    int y = int.Parse(spl[1]);

                    bool added = false;
                    int toRem = -1;
                    int i = 0;
                    foreach (var pos in CurrentItemData.UnWalkableCells)
                    {
                        if (pos.X == x && pos.Y == y)
                        {
                            added = true;
                            toRem = i;
                        }
                        i++;
                    }
                    if (!added)
                        CurrentItemData.UnWalkableCells.Add(new MapDecoCellPos(x, y));
                    else
                        CurrentItemData.UnWalkableCells.RemoveAt(toRem);

                    DrawUnwalkableFeedback();
                }
            }
        }
    }

    void UI()
    {
        //Vector2 Size = new Vector2(512f, 512f);
        //ImGui.SetNextWindowSize(Size, SetCondition.Always);
        //ImGui.SetNextWindowPos(new Vector2(Screen.width - Size.x - 16f, 16f), SetCondition.Always);
        //ImGui.Begin("Data");
        //if (CurrentItemData != null)
        //{
        //    ImGui.Text(CurrentItemData.ID);
        //    ImGui.Text(CurrentItemData.Type);

        //    float baseScale = CurrentItemData.BaseScale;
        //    if (ImGui.DragFloat("Base Scale", ref baseScale, 0.1f, 0.1f, 6f))
        //    {
        //        CurrentItemData.BaseScale = baseScale;
        //        CurrentItem.transform.localScale = Vector3.one * baseScale;
        //    }

        //    bool scalable = CurrentItemData.Scalable;
        //    if (ImGui.Checkbox("Scalable", ref scalable))
        //        CurrentItemData.Scalable = scalable;

        //    bool FreeRotation = CurrentItemData.FreeRotation;
        //    if (ImGui.Checkbox("Free Rotation", ref FreeRotation))
        //        CurrentItemData.FreeRotation = FreeRotation;

        //    // draw types
        //    for (int i = 1; i <= Types.Length; i++)
        //        ImGui.Text(i + " : " + Types[i - 1]);
        //}
        //else
        //{
        //    ImGui.Text("No current prob");
        //}
        //ImGui.End();
    }

#if UNITY_EDITOR
    private void OnDisable()
    {
        EditorUtility.SetDirty(DodgeFromLight.Databases.ResourcesData);
        AssetDatabase.SaveAssets();
    }

    private void OnDestroy()
    {
        EditorUtility.SetDirty(DodgeFromLight.Databases.ResourcesData);
        AssetDatabase.SaveAssets();
    }
#endif

    private void LoadDataItem()
    {
        if (CurrentItem != null)
            Destroy(CurrentItem);

        CurrentItem = Instantiate(DodgeFromLight.Databases.ResourcesData.Data[currentDataItem].Prefab);
        CurrentItem.transform.position = Vector3.zero;
        CurrentItem.transform.SetParent(DataItemContainer);
        CurrentItem.isStatic = false;
        CurrentItemData = DodgeFromLight.Databases.ResourcesData.Data[currentDataItem];
        CurrentItem.transform.localScale = Vector3.one * CurrentItemData.BaseScale;

        DrawUnwalkableFeedback();
    }

    List<GameObject> UnwalkableFeedbacks = new List<GameObject>();
    private void DrawUnwalkableFeedback()
    {
        //if (CurrentItemData.UnWalkableCells.Count == 0)
        //    CurrentItemData.UnWalkableCells.Add(new MapDecoCellPos(0, 0));

        foreach (var go in UnwalkableFeedbacks)
            Destroy(go);

        foreach (var pos in CurrentItemData.UnWalkableCells)
        {
            GameObject feedback = Instantiate(UnWalkableFeedbackPrefab);
            feedback.transform.position = new Vector3(pos.X, 0f, pos.Y);
            UnwalkableFeedbacks.Add(feedback);
        }
    }

    private void NextDataItem()
    {
        currentDataItem++;
        if (currentDataItem >= Deco.Length)
            currentDataItem = 0;
    }

    private void PreviewDataItem()
    {
        currentDataItem--;
        if (currentDataItem < 0)
            currentDataItem = Deco.Length - 1;
    }

    IEnumerator ScreenShot(string folderPath)
    {
        foreach (var mdd in DodgeFromLight.Databases.ResourcesData.Data)
        {
            GameObject item = Instantiate(mdd.Prefab);
            item.name = mdd.ID;
            item.transform.position = Vector3.zero;
            item.transform.eulerAngles = Vector3.up * 90f;
            item.transform.localScale = Vector3.one * mdd.BaseScale;

            Vector3 center = item.GetComponentInChildren<Renderer>().bounds.center;
            cam.transform.LookAt(center);
            cam.fieldOfView = GetFieldOfView(center, Mathf.Max(item.GetComponentInChildren<Renderer>().bounds.size.x, item.GetComponentInChildren<Renderer>().bounds.size.y));
            cam.clearFlags = CameraClearFlags.Skybox;
            yield return null;
            cam.clearFlags = CameraClearFlags.Depth;

            // take screenshot
            int resWidth = cam.pixelWidth;
            int resHeight = cam.pixelHeight;
            RenderTexture rt = new RenderTexture(resWidth, resHeight, 32, UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            screenShot.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            byte[] bytes = screenShot.EncodeToPNG();
            string path = folderPath + item.name + ".png";
            System.IO.File.WriteAllBytes(path, bytes);
            mdd.Texture = Resources.Load<Texture2D>(folderPath + item.name);
            Destroy(item);
        }

        float GetFieldOfView(Vector3 objectPosition, float objectHeight)
        {
            Vector3 diff = objectPosition - cam.transform.position;
            float distance = Vector3.Dot(diff, cam.transform.forward);
            float angle = Mathf.Atan((objectHeight * 0.9f) / distance);
            return angle * 2f * Mathf.Rad2Deg;
        }
    }

    public void CreateBaseResourcesData()
    {
        // Data.Data.Clear();

        string prevPath = "MapAssets/previews/";
        foreach (var go in Deco)
            if (!DodgeFromLight.Databases.ResourcesData.HasMapDeco(go.name))
                DodgeFromLight.Databases.ResourcesData.Data.Add(new MapDecoData()
                {
                    BaseScale = 1,
                    ID = go.name,
                    Prefab = go,
                    Scalable = true,
                    Texture = Resources.Load<Texture2D>(prevPath + go.name),
                    UnWalkableCells = new List<MapDecoCellPos>() { new MapDecoCellPos(0, 0) }
                });
    }
}