using UnityEngine;
using UnityEngine.UI;
using HSVPicker;
using System;

public class UI_EnvironmentSettings : MonoBehaviour
{
    public static UI_EnvironmentSettings Instance;
    public GameObject EnvironmentSettingsPanel;
    public ColorPicker ColorPicker;
    // particles
    public Slider ParticleSize;
    public Slider ParticleDensity;
    //  water
    public Slider FoamScale;
    public Button FoamColorButton;
    public Button DeepDethColorButton;
    public Button DepthShallowColorButton;
    // lights
    public Button SunColorButton;
    public Slider SunIntensity;
    public Button SkyIlluminationButton;
    public Button EquatorIlluminationButton;
    public Button GroundIlluminationButton;
    // skybox
    public Slider Seed;
    public Slider Scale;
    public Slider Contrast;
    public Slider AnimationSpeed;
    public Slider Dust;

    public Slider SunSize;
    public Button SkyboxSunColor;
    public Slider SkyboxSunIntensity;
    public Slider SunUp;
    public Slider SunHorizontal;

    public Slider Nebular1Strenght;
    public Button Nebular1Color1;
    public Button Nebular1Color2;

    public Slider Nebular2Strenght;
    public Button Nebular2Color1;
    public Button Nebular2Color2;
    // on color changed
    Action<Color> OnColorChanged;

    public void Awake()
    {
        Instance = this;

        // ============================= PARTICLES
        // Particle Size
        ParticleSize.onValueChanged.AddListener((particleSize) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.ParticleSize = particleSize;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
            DodgeFromLight.EnvironmentController.RestartPS();
        });

        // Particle Density
        ParticleSize.onValueChanged.AddListener((ParticleDensity) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.ParticleDensity = ParticleDensity;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
            DodgeFromLight.EnvironmentController.RestartPS();
        });

        // ============================= WATER
        // Foam Intensity
        FoamScale.onValueChanged.AddListener((foamScale) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.FoamScale = foamScale;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Foam Color
        FoamColorButton.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Foam Color");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.FoamColor = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().FoamColor.ToColor();
        });

        // Deep deth Color
        DeepDethColorButton.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Deep Depth Color");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.DepthDeep = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().DepthDeep.ToColor();
        });

        // Deep shallow Color
        DepthShallowColorButton.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Dept Shallow Color");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.DepthShallow = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().DepthShallow.ToColor();
        });

        // ============================= LIGHTING
        // Sun Color
        SunColorButton.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Sun Color");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.SunColor = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().SunColor.ToColor();
        });

        // Sun Intensity
        SunIntensity.onValueChanged.AddListener((SunIntensity) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.SunIntensity = SunIntensity;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Sky Illumination
        SkyIlluminationButton.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Sky Light Color");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.SkyColor = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().SkyColor.ToColor();
        });

        // Equator Illumination
        EquatorIlluminationButton.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Equator Light Color");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.EquatorColor = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().EquatorColor.ToColor();
        });

        // Ground Illumination
        GroundIlluminationButton.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("ground Light Color");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.GroundColor = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().GroundColor.ToColor();
        });

        // ============================= SKYBOX
        // Seed
        Seed.onValueChanged.AddListener((seed) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.Seed = seed;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Scale
        Scale.onValueChanged.AddListener((scale) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.Scale = scale;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Contrast
        Contrast.onValueChanged.AddListener((contrast) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.Contrast = contrast;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Animation Speed
        AnimationSpeed.onValueChanged.AddListener((speed) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.AnimationSpeed = speed;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Dust Amounth
        Dust.onValueChanged.AddListener((dust) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.DustAmount = dust;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Nebular 1
        Nebular1Strenght.onValueChanged.AddListener((nebular1) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.Nebular1Strength = nebular1;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Nebular 1 Color 1
        Nebular1Color1.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Nebular 1 Color 2");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.Nebular1ColorMain = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().Nebular1ColorMain.ToColor();
        });

        // Nebular 1 Color 2
        Nebular1Color2.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Nebular 1 Color 2");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.Nebular1ColorMid = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().Nebular1ColorMid.ToColor();
        });

        // Nebular 2
        Nebular2Strenght.onValueChanged.AddListener((nebular2) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.Nebular2Strength= nebular2;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Nebular 2 Color 1
        Nebular2Color1.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Nebular 2 Color 1");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.Nebular2Color1 = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().Nebular2Color1.ToColor();
        });

        // Nebular 2 Color 2
        Nebular2Color2.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Nebular 2 Color 2");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.Nebular2Color2 = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().Nebular2Color2.ToColor();
        });

        // Sun Size
        SunSize.onValueChanged.AddListener((sunSize) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.SunSize = sunSize;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Sun Intensity
        SkyboxSunIntensity.onValueChanged.AddListener((sunIntensity) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.SunSkyIntensity = sunIntensity;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Sun Color
        SkyboxSunColor.onClick.AddListener(() =>
        {
            ColorPicker.gameObject.SetActive(true);
            ColorPicker.SetTitle("Sun Color");
            OnColorChanged = (color) =>
            {
                EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
                env.SunSkyColor = new Serializable_Vector4(color);
                DodgeFromLight.CurrentMap.Grid.Environment = env;
                DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
                BindEnvironment(env);
            };
            ColorPicker.CurrentColor = DodgeFromLight.CurrentMap.Grid.GetEnvironment().SunSkyColor.ToColor();
        });

        // Sun Up
        SunUp.onValueChanged.AddListener((sunUp) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.SunUp = sunUp;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        // Sun Horizontal
        SunHorizontal.onValueChanged.AddListener((sunHorizontal) =>
        {
            EnvironmentSettings env = DodgeFromLight.CurrentMap.Grid.GetEnvironment();
            env.SunHorizontal = sunHorizontal;
            DodgeFromLight.CurrentMap.Grid.Environment = env;
            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        });

        ColorPicker.gameObject.SetActive(false);
    }

    public void BindEnvironment(EnvironmentSettings env)
    {
        // particle size
        ParticleSize.minValue = 0.1f;
        ParticleSize.maxValue = 2f;
        ParticleSize.value = env.ParticleSize;
        // particle density
        ParticleDensity.minValue = 0.1f;
        ParticleDensity.maxValue = 2f;
        ParticleDensity.value = env.ParticleDensity;

        // Foam scale
        FoamScale.minValue = 0.01f;
        FoamScale.maxValue = 10f;
        FoamScale.value = env.FoamScale;
        // Foam Color
        FoamColorButton.GetComponentInChildren<Image>().color = env.FoamColor.ToColor();
        // Deep deth
        DeepDethColorButton.GetComponentInChildren<Image>().color = env.DepthDeep.ToColor();
        // Deep shallow
        DepthShallowColorButton.GetComponentInChildren<Image>().color = env.DepthShallow.ToColor();

        // Sun Color
        SunColorButton.GetComponentInChildren<Image>().color = env.SunColor.ToColor();
        // Sky Color
        SkyIlluminationButton.GetComponentInChildren<Image>().color = env.SkyColor.ToColor();
        // Equator Color
        EquatorIlluminationButton.GetComponentInChildren<Image>().color = env.EquatorColor.ToColor();
        // Ground Colorr
        GroundIlluminationButton.GetComponentInChildren<Image>().color = env.GroundColor.ToColor();
        // Sun Intensity
        SunIntensity.minValue = 0f;
        SunIntensity.maxValue = 2f;
        SunIntensity.value = env.SunIntensity;

        // Seed
        Seed.minValue = 0f;
        Seed.maxValue = 1000f;
        Seed.value = env.Seed;
        // Contrast
        Contrast.minValue = 0.001f;
        Contrast.maxValue = 1f;
        Contrast.value = env.Contrast;
        // AnimationSpeed
        AnimationSpeed.value = env.AnimationSpeed;
        AnimationSpeed.minValue = 0f;
        AnimationSpeed.maxValue = 0.25f;
        // Scale
        Scale.minValue = 0.001f;
        Scale.maxValue = 10f;
        Scale.value = env.Seed;
        // Dust
        Dust.minValue = 0f;
        Dust.maxValue = 1f;
        Dust.value = env.DustAmount;
        // Nebular1Strenght
        Nebular1Strenght.minValue = 0f;
        Nebular1Strenght.maxValue = 1f;
        Nebular1Strenght.value = env.Nebular1Strength;
        // Nebular2Strenght
        Nebular2Strenght.minValue = 0f;
        Nebular2Strenght.maxValue = 1f;
        Nebular2Strenght.value = env.Nebular2Strength;
        // SunSize
        SunSize.minValue = 0f;
        SunSize.maxValue = 1f;
        SunSize.value = env.SunSize;
        // SunIntensity
        SkyboxSunIntensity.minValue = 0.3f;
        SkyboxSunIntensity.maxValue = 25f;
        SkyboxSunIntensity.value = env.SunSkyIntensity;
        // SunHorizontal
        SunHorizontal.minValue = 0f;
        SunHorizontal.maxValue = 1f;
        SunHorizontal.value = env.SunHorizontal;
        // SunSize
        SunUp.minValue = -1f;
        SunUp.maxValue = 1f;
        SunUp.value = env.SunUp;
        // skybox colors
        Nebular1Color1.GetComponentInChildren<Image>().color = env.Nebular1ColorMain.ToColor();
        Nebular1Color2.GetComponentInChildren<Image>().color = env.Nebular1ColorMid.ToColor();
        Nebular2Color1.GetComponentInChildren<Image>().color = env.Nebular2Color1.ToColor();
        Nebular2Color2.GetComponentInChildren<Image>().color = env.Nebular2Color2.ToColor();
        SkyboxSunColor.GetComponentInChildren<Image>().color = env.SunSkyColor.ToColor();
    }

    public void OnColorPickedBValueChanged(Color color)
    {
        OnColorChanged?.Invoke(color);
    }

    void DrawEnvironment()
    {
        //if (ImGui.CollapsingHeader("Skybox", TreeNodeFlags.DefaultOpen | TreeNodeFlags.Bullet))
        //{
        //    ImGui.BeginChild("SkyboxContainer", new Vector2(0, 384f), true);
        //    Vector4 ColorA = env.ColorA.ToVector4();
        //    if (ImGui.ColorEdit4("Color A", ref ColorA))
        //    {
        //        env.ColorA = new Serializable_Vector4(ColorA);
        //        DodgeFromLight.CurrentMap.Grid.Environment = env;
        //        DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        //    }

        //    float IntensityA = env.IntensityA;
        //    if (ImGui.DragFloat("Intensity A", ref IntensityA, .01f, 0f, 2f, (IntensityA * 100f) + "%%"))
        //    {
        //        env.IntensityA = IntensityA;
        //        DodgeFromLight.CurrentMap.Grid.Environment = env;
        //        DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        //    }

        //    Serializable_Vector4 DirA = env.DirA;
        //    if (DrawVector4("Direction A", ref DirA, 0f, 359f))
        //    {
        //        env.DirA = DirA;
        //        DodgeFromLight.CurrentMap.Grid.Environment = env;
        //        DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        //    }

        //    Vector4 ColorB = env.ColorB.ToVector4();
        //    if (ImGui.ColorEdit4("Color B", ref ColorB))
        //    {
        //        env.ColorB = new Serializable_Vector4(ColorB);
        //        DodgeFromLight.CurrentMap.Grid.Environment = env;
        //        DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        //    }

        //    float IntensityB = env.IntensityB;
        //    if (ImGui.DragFloat("Intensity B", ref IntensityB, .01f, 0f, 2f, (IntensityB * 100f) + "%%"))
        //    {
        //        env.IntensityB = IntensityB;
        //        DodgeFromLight.CurrentMap.Grid.Environment = env;
        //        DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        //    }

        //    Serializable_Vector4 DirB = env.DirB;
        //    if (DrawVector4("Direction B", ref DirB, 0f, 359f))
        //    {
        //        env.DirB = DirB;
        //        DodgeFromLight.CurrentMap.Grid.Environment = env;
        //        DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        //    }
        //    ImGui.EndChild();
        //}

        //if (ImGui.CollapsingHeader("BuildIn Environments", TreeNodeFlags.DefaultOpen | TreeNodeFlags.Bullet))
        //{
        //    Array envts = Enum.GetValues(typeof(BuildInEnvironments));
        //    ImGui.BeginChild("bdenvContainer", new Vector2(0, envts.Length * 32f), true);
        //    foreach (BuildInEnvironments e in envts)
        //    {
        //        if (ImGui.Button(e.ToString()))
        //        {
        //            DodgeFromLight.CurrentMap.Grid.Environment = DodgeFromLight.EnvironmentController.GetBuildinEnvironment(e);
        //            DodgeFromLight.SetEnvironment(DodgeFromLight.CurrentMap);
        //        }
        //    }
        //    ImGui.EndChild();
        //}
    }
}