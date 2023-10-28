using DFLCommonNetwork.GameEngine;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class Utils
{

}

public static class Extensions
{
    public static void SetStatic(this GameObject go, bool val)
    {
        foreach (Transform t in go.transform)
            t.gameObject.isStatic = val;
    }

    public static void SetLayer(this GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            t.gameObject.SetLayer(layer);
    }

    public static void LookAtY(this Transform t1, Transform t2)
    {
        Vector3 euler = t1.eulerAngles;
        t1.LookAt(t2);
        euler.y  = t1.eulerAngles.y;
        t1.eulerAngles = euler;
    }

    public static string ToStr(this Guid id)
    {
        return id.ToString().Replace("-", "");
    }

    public static void Shuffle<T>(this System.Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }

    public static void SetAndStretchParent(this RectTransform _mRect, RectTransform _parent)
    {
        _mRect.anchoredPosition = _parent.position;
        _mRect.anchorMin = new Vector2(0, 0);
        _mRect.anchorMax = new Vector2(1, 1);
        _mRect.pivot = new Vector2(0.5f, 0.5f);
        _mRect.sizeDelta = _parent.rect.size;
        _mRect.transform.SetParent(_parent);
    }

    //public static Vector3 GetCenterPoint(this GameObject go)
    //{
    //    Vector3 center = Vector3.zero;
    //    int nb = 0;
    //    var renders = go.GetComponentsInChildren<Renderer>();
    //    foreach (var render in renders)
    //        center += render.bounds.center;
    //    nb += renders.Length;
    //    return center / nb;
    //}

    //public static Bounds GetMaxBounds(this GameObject go)
    //{
    //    Bounds bounds = new Bounds();
    //    var renders = go.GetComponentsInChildren<Renderer>();
    //    foreach (var render in renders)
    //        bounds.Encapsulate(render.bounds);

    //    return bounds;
    //}

    public static Vector3 GetCenterPoint(this GameObject go)
    {
        List<Mesh> meshes = go.GetMeshes();
        int nb = 0;
        Vector3 cumul = Vector3.zero;
        foreach (Mesh mesh in meshes)
        {
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                nb++;
                cumul += vertices[i];
            }
        }
        return cumul / nb;
    }

    public static Bounds GetBounds(this GameObject go)
    {
        List<Mesh> meshes = go.GetMeshes();
        bool getCorrect = true;
        foreach (var mesh in meshes)
            if (!mesh.isReadable)
                getCorrect = false;
        if (getCorrect)
            return go.GetCorrectBounds();


        Bounds bounds = new Bounds();
        foreach (var render in meshes)
            bounds.Encapsulate(render.bounds);
        bounds.max = bounds.max.Mult(go.transform.localScale);
        bounds.min = bounds.min.Mult(go.transform.localScale);
        bounds.center = bounds.center.Mult(go.transform.localScale);
        return bounds;
    }

    public static Vector3 Mult(this Vector3 v1, Vector3 v2)
    {
        Vector3 v3 = v1;
        v3.x *= v2.x;
        v3.y *= v2.y;
        v3.z *= v2.z;
        return v3;
    }

    public static GameObject GetUnskined(this GameObject go)
    {
        SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
            return null;

        GameObject g = new GameObject();
        g.name = "Unskinned_" + go.name;
        g.transform.SetParent(go.transform.parent);
        g.transform.localPosition = go.transform.localPosition;
        g.transform.localRotation = go.transform.localRotation;
        var mf = g.AddComponent<MeshFilter>();
        mf.sharedMesh = smr.sharedMesh;
        var mr = g.AddComponent<MeshRenderer>();
        mr.sharedMaterials = smr.sharedMaterials;
        return g;
    }

    public static Bounds GetCorrectBounds(this GameObject go)
    {
        List<Mesh> meshes = go.GetMeshes();
        Bounds bounds = new Bounds();
        Vector3 Center = Vector3.zero;
        int nb = 0;
        float minX = float.MaxValue,
            minY = float.MaxValue,
            minZ = float.MaxValue,
            maxX = float.MinValue,
            maxY = float.MinValue,
            maxZ = float.MinValue;

        foreach (Mesh mesh in meshes)
        {
            using (var dataArray = Mesh.AcquireReadOnlyMeshData(mesh))
            {
                var data = dataArray[0];
                var gotVertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.TempJob);
                data.GetVertices(gotVertices);
                int size = gotVertices.Length;
                Vector3 vert = Vector3.zero;
                for (int i = 0; i < size; i++)
                {
                    vert = gotVertices[i];
                    if (vert.x < minX)
                        minX = vert.x;
                    if (vert.y < minY)
                        minY = vert.y;
                    if (vert.z < minZ)
                        minZ = vert.z;

                    if (vert.x > maxX)
                        maxX = vert.x;
                    if (vert.y > maxY)
                        maxY = vert.y;
                    if (vert.z > maxZ)
                        maxZ = vert.z;
                    Center += vert;
                    nb++;
                }
                gotVertices.Dispose();
            }
        }

        Center = Center / nb;
        Center.x *= go.transform.localScale.x;
        Center.y *= go.transform.localScale.y;
        Center.z *= go.transform.localScale.z;
        bounds.center = Center;
        bounds.SetMinMax(new Vector3(minX * go.transform.localScale.x, minY * go.transform.localScale.y, minZ * go.transform.localScale.z),
            new Vector3(maxX * go.transform.localScale.x, maxY * go.transform.localScale.y, maxZ * go.transform.localScale.z));
        return bounds;
    }

    public static List<Mesh> GetMeshes(this GameObject go)
    {
        List<Mesh> meshes = new List<Mesh>();
        var filters = go.GetComponentsInChildren<MeshFilter>();
        foreach (var filter in filters)
            meshes.Add(filter.mesh);

        var renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var renderer in renderers)
            meshes.Add(renderer.sharedMesh);

        return meshes;
    }

    //public static Vector3 GetCenterPoint(this GameObject go)
    //{
    //    Vector3 center = Vector3.zero;
    //    int nb = 0;
    //    var renders = go.GetComponentsInChildren<MeshFilter>();
    //    foreach (var render in renders)
    //        center += render.sharedMesh.bounds.center;
    //    nb += renders.Length;
    //    var renders2 = go.GetComponentsInChildren<SkinnedMeshRenderer>();
    //    foreach (var render in renders2)
    //        center += render.sharedMesh.bounds.center;
    //    nb += renders2.Length;
    //    return center / nb;
    //}

    //public static Bounds GetMaxBounds(this GameObject go)
    //{
    //    Bounds bounds = new Bounds();
    //    var renders = go.GetComponentsInChildren<MeshFilter>();
    //    foreach (var render in renders)
    //        bounds.Encapsulate(render.sharedMesh.bounds);

    //    var renders2 = go.GetComponentsInChildren<SkinnedMeshRenderer>();
    //    foreach (var render in renders2)
    //        bounds.Encapsulate(render.sharedMesh.bounds);

    //    return bounds;
    //}

    public static bool Equals(this CellPos pos, Cell cell)
    {
        return cell.X == pos.X && cell.Y == pos.Y;
    }

    public static Vector3 ToVector3(this CellPos pos, float y)
    {
        return new Vector3(pos.X, y, pos.Y);
    }

    public static Vector2Int ToVector2Int(this CellPos pos)
    {
        return new Vector2Int(pos.X, pos.Y);
    }

    public static float Distance(this CellPos pos, CellPos cell)
    {
        return Vector2Int.Distance(pos.ToVector2Int(), cell.ToVector2Int());
    }

    /// <summary>
    /// Rounds a Vector3 to world axis or relative to another transform
    /// </summary>
    /// <param name="vector">Vector to round</param>
    /// <param name="relativeTo">Optional relative transform space axis</param>
    /// <returns></returns>
    public static Vector3 AxisRound(this Vector3 vector, Transform relativeTo = null)
    {
        if (relativeTo)
        {
            vector = relativeTo.InverseTransformDirection(vector);
        }
        int largestIndex = 0;
        for (int i = 1; i < 3; i++)
        {
            largestIndex = Mathf.Abs(vector[i]) > Mathf.Abs(vector[largestIndex]) ? i : largestIndex;
        }
        float newLargest = vector[largestIndex] > 0 ? 1 : -1;
        vector = Vector3.zero;
        vector[largestIndex] = newLargest;
        if (relativeTo)
        {
            vector = relativeTo.TransformDirection(vector);
        }
        return vector;
    }
}