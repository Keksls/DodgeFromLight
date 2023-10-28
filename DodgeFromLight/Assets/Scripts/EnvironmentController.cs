using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public class EnvironmentController : MonoBehaviour
{
    public Light Sun;
    public ParticleSystem Particles;
    public Material FogMaterial;
    public Material FoamMaterial;
    public Material SkyboxMaterial;
    public float minSize;
    public float maxSize;
    public int nbParticle;

    private void Awake()
    {
        DodgeFromLight.EnvironmentController = this;
    }

    public void SetEnvironment(EnvironmentSettings env)
    {
        try
        {
            SetSunColor(env.SunColor.ToColor());
            SetSunIntensity(env.SunIntensity);
            SetParticlesDensity(env.ParticleDensity);
            SetPartcilesSize(env.ParticleSize);
            SetFogSettings(env);
            SetSkybox(env);

            if (env.SkyColor != null)
            {
                RenderSettings.ambientSkyColor = env.SkyColor.ToColor();
                RenderSettings.ambientEquatorColor = env.EquatorColor.ToColor();
                RenderSettings.ambientGroundColor = env.GroundColor.ToColor();
            }
        }
        catch {
            env = GetBuildinEnvironment(BuildInEnvironments.Default);
            SetSunColor(env.SunColor.ToColor());
            SetSunIntensity(env.SunIntensity);
            SetParticlesDensity(env.ParticleDensity);
            SetPartcilesSize(env.ParticleSize);
            SetFogSettings(env);
            SetSkybox(env);

            if (env.SkyColor != null)
            {
                RenderSettings.ambientSkyColor = env.SkyColor.ToColor();
                RenderSettings.ambientEquatorColor = env.EquatorColor.ToColor();
                RenderSettings.ambientGroundColor = env.GroundColor.ToColor();
            }
        }
    }

    public void SetFromMap(FullMap map)
    {
        var env = map.Grid.GetEnvironment();
        SetEnvironment(env);
    }

    public void SetFogSettings(EnvironmentSettings env)
    {
        if (DodgeFromLight.GridController == null || DodgeFromLight.GridController.Fog == null)
            return;
        Material fogMat = Instantiate(FogMaterial);
        fogMat.SetColor("_DepthGradientShallow", env.DepthShallow.ToColor());
        fogMat.SetColor("_DepthGradientDeep", env.DepthDeep.ToColor());

        var renderer = DodgeFromLight.GridController.Fog.gameObject.GetComponent<Renderer>();
        renderer.material = fogMat;

        Material foamMat = Instantiate(FoamMaterial);
        foamMat.SetColor("_FoamColor", env.FoamColor.ToColor());
        foamMat.SetFloat("_SurfaceNoiseScale", env.FoamScale);
        var rendererFoam = DodgeFromLight.GridController.Fog.transform.GetChild(1).GetComponent<Renderer>();
        rendererFoam.material = foamMat;
    }

    public void SetFogSettings(EnvironmentSettings env, GameObject fog, GameObject foam)
    {
        Material fogMat = Instantiate(FogMaterial);
        fogMat.SetColor("_DepthGradientShallow", env.DepthShallow.ToColor());
        fogMat.SetColor("_DepthGradientDeep", env.DepthDeep.ToColor());
        var renderer = fog.gameObject.GetComponent<Renderer>();
        renderer.material = fogMat;

        Material foamMat = Instantiate(FoamMaterial);
        foamMat.SetColor("_FoamColor", env.FoamColor.ToColor());
        foamMat.SetFloat("_SurfaceNoiseScale", env.FoamScale);
        var rendererFoam = foam.GetComponent<Renderer>();
        rendererFoam.material = foamMat;
    }

    public void SetSkybox(EnvironmentSettings env)
    {
        Material skyMat = RenderSettings.skybox;

        skyMat.SetFloat("_Seed", env.Seed);
        skyMat.SetFloat("_Scale", env.Scale);
        skyMat.SetFloat("_Contrast", env.Contrast);
        skyMat.SetFloat("_AnimationSpeed", env.AnimationSpeed);
        skyMat.SetFloat("_DustAmount", env.DustAmount);

        skyMat.SetFloat("_Nebular1Strength", env.Nebular1Strength);
        skyMat.SetColor("_Nebular1ColorMain", env.Nebular1ColorMain.ToColor());
        skyMat.SetColor("_Nebular1ColorMid", env.Nebular1ColorMid.ToColor());

        skyMat.SetFloat("_Nebular2Strength", env.Nebular2Strength);
        skyMat.SetColor("_Nebular2Color1", env.Nebular2Color1.ToColor());
        skyMat.SetColor("_Nebular2Color2", env.Nebular2Color2.ToColor());

        skyMat.SetFloat("_Sunsize", env.SunSize);
        skyMat.SetFloat("_SunIntensity", env.SunSkyIntensity);
        skyMat.SetColor("_SunColor", env.SunSkyColor.ToColor());
        skyMat.SetFloat("_SunUp", env.SunUp);
        skyMat.SetFloat("_SunHorizontal", env.SunHorizontal);

        RenderSettings.skybox = skyMat;
    }

    public void SetSunColor(Color col)
    {
        Sun.color = col;
    }

    public void SetSunIntensity(float intensity)
    {
        Sun.intensity = intensity;
    }

    public void RestartPS()
    {
        Particles.Stop();
        Particles.Clear();
        Particles.Play();
    }

    public void SetPartcilesSize(float percent)
    {
        var main = Particles.main;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize * percent, maxSize * percent);
    }

    public void SetParticlesDensity(float percent)
    {
        var main = Particles.emission;
        main.rateOverTime = new ParticleSystem.MinMaxCurve(nbParticle * percent, nbParticle * percent);
    }

    public void SetParticleEmissionSize(Grid grid)
    {
        float size = grid.Width;
        Particles.gameObject.transform.position = new Vector3(size * 3f, -50, size * 3f);
        var ps = Particles.shape;
        ps.radius = size * 12f;
    }

    public EnvironmentSettings GetBuildinEnvironment(BuildInEnvironments env)
    {
        string json = File.ReadAllText(Application.streamingAssetsPath + @"\Environments\" + env.ToString() + ".json");
        EnvironmentSettings e = JsonConvert.DeserializeObject<EnvironmentSettings>(json);
        return e;
    }
}

public enum BuildInEnvironments
{
    Clair,
    Default,
    Fairy,
    Hellish,
    Holly,
    Night,
    Poison,
    Sunshine
}

public class EnvironmentSettings
{
    // Sun
    public float SunIntensity { get; set; }
    public Serializable_Vector4 SunColor { get; set; }
    // Particles
    public float ParticleSize { get; set; }
    public float ParticleDensity { get; set; }
    // Water
    public Serializable_Vector4 DepthShallow { get; set; }
    public Serializable_Vector4 DepthDeep { get; set; }
    public Serializable_Vector4 FoamColor { get; set; }
    public float FoamScale { get; set; }
    // Sky lighting
    public Serializable_Vector4 SkyColor { get; set; }
    public Serializable_Vector4 EquatorColor { get; set; }
    public Serializable_Vector4 GroundColor { get; set; }
    // Skybox Shader
    public float Seed { get; set; }
    public float Scale { get; set; }
    public float Contrast { get; set; }
    public float AnimationSpeed { get; set; }
    public float DustAmount { get; set; }

    public float Nebular1Strength { get; set; }
    public Serializable_Vector4 Nebular1ColorMain { get; set; }
    public Serializable_Vector4 Nebular1ColorMid { get; set; }

    public float Nebular2Strength { get; set; }
    public Serializable_Vector4 Nebular2Color1 { get; set; }
    public Serializable_Vector4 Nebular2Color2 { get; set; }

    public float SunSize { get; set; }
    public float SunSkyIntensity { get; set; }
    public Serializable_Vector4 SunSkyColor { get; set; }
    public float SunUp { get; set; }
    public float SunHorizontal { get; set; }

    public EnvironmentSettings()
    {

    }

    public void SetDefaultValues()
    {
        SunIntensity = 1f;
        SunColor = new Serializable_Vector4(new Color(1f, 0.7888583f, 0.7028302f, 1f));
        ParticleSize = 1f;
        ParticleDensity = 1f;

        SkyColor = new Serializable_Vector4(new Color(.212f, 0.227f, 0.259f, 1f));
        EquatorColor = new Serializable_Vector4(new Color(.133f, 0.114f, 0.125f, 1f));
        GroundColor = new Serializable_Vector4(new Color(0.047f, 0.043f, 0.035f, 1f));

        Material fogMat = DodgeFromLight.EnvironmentController.FogMaterial;
        DepthShallow = new Serializable_Vector4(fogMat.GetColor("_DepthGradientShallow"));
        DepthDeep = new Serializable_Vector4(fogMat.GetColor("_DepthGradientDeep"));

        Material foamMat = DodgeFromLight.EnvironmentController.FoamMaterial;
        FoamColor = new Serializable_Vector4(foamMat.GetColor("_FoamColor"));
        FoamScale = foamMat.GetFloat("_SurfaceNoiseScale");

        Material skyMat = DodgeFromLight.EnvironmentController.SkyboxMaterial;
        Seed = skyMat.GetFloat("_Seed");
        Scale = skyMat.GetFloat("_Scale");
        Contrast = skyMat.GetFloat("_Contrast");
        AnimationSpeed = skyMat.GetFloat("_AnimationSpeed");
        DustAmount = skyMat.GetFloat("_DustAmount");

        Nebular1Strength = skyMat.GetFloat("_Nebular1Strength");
        Nebular1ColorMain = new Serializable_Vector4(skyMat.GetColor("_Nebular1ColorMain"));
        Nebular1ColorMid = new Serializable_Vector4(skyMat.GetColor("_Nebular1ColorMid"));

        Nebular2Strength = skyMat.GetFloat("_Nebular2Strength");
        Nebular2Color1 = new Serializable_Vector4(skyMat.GetColor("_Nebular2Color1"));
        Nebular2Color2 = new Serializable_Vector4(skyMat.GetColor("_Nebular2Color2"));

        SunSize = skyMat.GetFloat("_Sunsize");
        SunSkyIntensity = skyMat.GetFloat("_SunIntensity");
        SunUp = skyMat.GetFloat("_SunUp");
        SunHorizontal = skyMat.GetFloat("_SunHorizontal");
        SunSkyColor = new Serializable_Vector4(skyMat.GetColor("_SunColor"));
    }
}

public class Serializable_Vector3
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }

    public Serializable_Vector3() { }

    public Serializable_Vector3(Vector3 v3)
    {
        x = v3.x;
        y = v3.y;
        z = v3.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

public class Serializable_Vector2
{
    public float x { get; set; }
    public float y { get; set; }

    public Serializable_Vector2() { }

    public Serializable_Vector2(Vector2 v2)
    {
        x = v2.x;
        y = v2.y;
    }
    public Serializable_Vector2(float _x, float _y)
    {
        x = _x;
        y = _y;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}

public class Serializable_Vector4
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public float w { get; set; }

    public Serializable_Vector4(Vector4 v4)
    {
        x = v4.x;
        y = v4.y;
        z = v4.z;
        w = v4.w;
    }

    public Serializable_Vector4(Color v4)
    {
        x = v4.r;
        y = v4.g;
        z = v4.b;
        w = v4.a;
    }

    public Serializable_Vector4() { }

    public Vector4 ToVector4()
    {
        return new Vector4(x, y, z, w);
    }

    public Vector4 ToColor()
    {
        return new Color(x, y, z, w);
    }
}