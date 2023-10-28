using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ColorHelpers
{
    public static IList<Color> GetDominantColour(Texture2D texture, int k)
    {
        List<Color> colors = new List<Color>(texture.width * texture.height);
        for (int x = 0; x < texture.width; x++)
            for (int y = 0; y < texture.height; y++)
                if (texture.GetPixel(x, y).a > 0.5f)
                    colors.Add(texture.GetPixel(x, y));

        KMeansClusteringCalculator clustering = new KMeansClusteringCalculator();
        return clustering.Calculate(k, colors, 5.0d);
    }

    public static Color32 AverageColorFromTexture(Texture2D tex)
    {
        Color32[] texColors = tex.GetPixels32();
        int total = texColors.Length;
        float r = 0;
        float g = 0;
        float b = 0;
        int nb = 0;

        for (int i = 0; i < total; i++)
        {
            if (texColors[i].a > 0)
            {
                r += texColors[i].r;
                g += texColors[i].g;
                b += texColors[i].b;
                nb++;
            }
        }

        return new Color32((byte)(r / nb), (byte)(g / nb), (byte)(b / nb), 255);
    }

    public static Color MultiplyIntensity(Color col, float intensity)
    {
        intensity = Mathf.Pow(2, intensity);
        return new Color(col.r * intensity, col.g * intensity, col.b * intensity, col.a);
    }

    public static Color SetSaturation(Color col, float satruration)
    {
        float H, S, V;
        Color.RGBToHSV(col, out H, out S, out V);
        S = satruration;
        return Color.HSVToRGB(H, S, V);
    }

    public static Color SetV(Color col, float satruration)
    {
        float H, S, V;
        Color.RGBToHSV(col, out H, out S, out V);
        V = satruration;
        return Color.HSVToRGB(H, S, V);
    }
}

/// <summary>
/// Calculates the K-Means Clusters for a set of colours
/// </summary>
public class KMeansClusteringCalculator
{

    /// <summary>
    /// Calculates the <paramref name="k"/> clusters for <paramref name="colours"/>. Iterations continues until clusters move by less than <paramref name="threshold"/>
    /// </summary>
    /// <param name="k">The number of clusters to calculate (eg. The number of results to return)</param>
    /// <param name="colours">The list of colours to calculate <paramref name="k"/> for</param>
    /// <param name="threshold">Threshold for iteration. A lower value should produce more correct results but requires more iterations and for some <paramref name="colours"/> may never produce a result</param>
    /// <returns>The <paramref name="k"/> colours for the image in descending order from most common to least common</returns>
    public IList<Color> Calculate(int k, IList<Color> colours, double threshold = 0.0d)
    {
        List<KCluster> clusters = new List<KCluster>();

        // 1. Initialisation.
        //   Create K clusters with a random data point from our input.
        //   We make sure not to use the same index twice for two inputs
        List<int> usedIndexes = new List<int>();
        while (clusters.Count < k)
        {
            int index = Random.Range(0, colours.Count);
            if (usedIndexes.Contains(index) == true)
            {
                continue;
            }

            usedIndexes.Add(index);
            KCluster cluster = new KCluster(colours[index]);
            clusters.Add(cluster);
        }

        bool updated = false;
        do
        {
            updated = false;
            // 2. For each colour in our input determine which cluster's centre point is the closest and add the colour to the cluster
            foreach (Color colour in colours)
            {
                double shortestDistance = float.MaxValue;
                KCluster closestCluster = null;

                foreach (KCluster cluster in clusters)
                {
                    double distance = cluster.DistanceFromCentre(colour);
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestCluster = cluster;
                    }
                }

                closestCluster.Add(colour);
            }

            // 3. Recalculate the clusters centre.
            foreach (KCluster cluster in clusters)
            {
                if (cluster.RecalculateCentre(threshold) == true)
                {
                    updated = true;
                }
            }

            // 4. If we updated any centre point this iteration then iterate again
        } while (updated == true);

        return clusters.OrderByDescending(c => c.PriorCount).Select(c => c.Centre).ToList();
    }
}
/// <summary>
/// Describes a cluster in a K-Means Clustering set
/// </summary>
public class KCluster
{
    private readonly List<Color> _colours;

    /// <summary>
    /// Creates a new K-Means Cluster set
    /// </summary>
    /// <param name="centre">The initial centre point for the set</param>
    public KCluster(Color centre)
    {
        Centre = centre;
        _colours = new List<Color>();
    }

    /// <summary>
    /// The current centre point of the cluster
    /// </summary>
    public Color Centre { get; set; }

    /// <summary>
    /// The number of points this cluster had before <seealso cref="RecalculateCentre"/> was called
    /// </summary>
    public int PriorCount { get; set; }

    /// <summary>
    /// Add <paramref name="colour"/> to the cluster. This means that the next time <seealso cref="RecalculateCentre"/> <paramref name="colour"/> will be considered in the centre calculation
    /// </summary>
    /// <param name="colour"></param>
    public void Add(Color colour)
    {
        _colours.Add(colour);
    }

    /// <summary>
    /// Based on all the items that have been <seealso cref="Add">Added</seealso> to this cluster calculates the centre.
    /// </summary>
    /// <param name="threshold">If the centre has moved by at least this value cluster has not yet converged and needs to be recalculated</param>
    /// <returns><c>true</c> if the recalculated centre's euclidean distance from the old centre is at least <paramref name="threshold"/>. <c>false</c> if it is less than this value</returns>
    public bool RecalculateCentre(double threshold = 0.0d)
    {
        Color updatedCentre;

        if (_colours.Count > 0)
        {
            float r = 0;
            float g = 0;
            float b = 0;

            foreach (Color color in _colours)
            {
                r += color.r;
                g += color.g;
                b += color.b;
            }

            updatedCentre = new Color((int)System.Math.Round(r / _colours.Count), (int)System.Math.Round(g / _colours.Count), (int)System.Math.Round(b / _colours.Count));
        }
        else
        {
            updatedCentre = new Color(0, 0, 0, 0);
        }

        double distance = EuclideanDistance(Centre, updatedCentre);
        Centre = updatedCentre;

        PriorCount = _colours.Count;
        _colours.Clear();

        return distance > threshold;
    }

    /// <summary>
    /// Returns the Euclidean distance of <paramref name="colour"/> from the current cluster centre point
    /// </summary>
    public double DistanceFromCentre(Color colour)
    {
        return EuclideanDistance(colour, Centre);
    }

    /// <summary>
    /// Calcultes the Euclidean distance between two colours, <paramref name="c1"/> and <paramref name="c2"/>
    /// </summary>
    public static double EuclideanDistance(Color c1, Color c2)
    {
        double distance = System.Math.Pow(c1.r - c2.r, 2) + System.Math.Pow(c1.g - c2.g, 2) + System.Math.Pow(c1.b - c2.b, 2);

        return System.Math.Sqrt(distance);
    }
}