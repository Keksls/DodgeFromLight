using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Applies a reversible neutral work light only while the editor camera renders.
/// </summary>
public sealed class MapCreatorSceneView : IDisposable
{
    private readonly Camera _camera;
    private readonly List<Light> _lights = new List<Light>();
    private readonly List<Renderer> _effects = new List<Renderer>();
    private readonly List<bool> _lightStates = new List<bool>();
    private readonly List<bool> _effectStates = new List<bool>();
    private readonly List<MapCreatorMaterialOverride> _materialOverrides = new List<MapCreatorMaterialOverride>();
    private readonly Dictionary<Material, Material> _workMaterials = new Dictionary<Material, Material>();
    private Light _sun;
    private Color _sunColor;
    private float _sunIntensity;
    private bool _sunUseTemperature;
    private Quaternion _sunRotation;
    private LightShadows _sunShadows;
    private float _sunShadowStrength;
    private float _sunShadowBias;
    private float _sunNormalBias;
    private UniversalRenderPipelineAsset _pipeline;
    private float _shadowDistance;
    private bool _renderShadows;
    private AmbientMode _ambientMode;
    private Color _skyColor;
    private Color _equatorColor;
    private Color _groundColor;
    private float _reflectionIntensity;
    private float _ambientIntensity;
    private SphericalHarmonicsL2 _ambientProbe;
    private bool _fog;
    private CameraClearFlags _clearFlags;
    private Color _background;
    private UniversalAdditionalCameraData _cameraData;
    private bool _postProcessing;
    private bool _applied;
    private bool _refresh = true;

    public bool ShowEnvironment { get; private set; }

    /// <summary>
    /// Subscribes the existing camera to scoped render overrides without creating a camera or light.
    /// </summary>
    public MapCreatorSceneView(Camera camera)
    {
        _camera = camera;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    /// <summary>
    /// Switches between work lighting and the map's unchanged authored atmosphere.
    /// </summary>
    public void SetEnvironmentPreview(bool show)
    {
        // This is a viewport preference, never an edit to Grid.Environment or document history.
        Restore();
        ShowEnvironment = show;
        _refresh = true;
    }

    /// <summary>
    /// Invalidates cached scene references after a board rebuild or pool mutation.
    /// </summary>
    public void Refresh()
    {
        _refresh = true;
    }

    /// <summary>
    /// Collects lights and atmospheric renderers belonging to the editor scene only.
    /// </summary>
    private void RefreshReferences()
    {
        ReleaseWorkMaterials();
        _lights.Clear();
        _effects.Clear();
        foreach (GameObject root in _camera.gameObject.scene.GetRootGameObjects())
        {
            _lights.AddRange(root.GetComponentsInChildren<Light>(true));
            _effects.AddRange(root.GetComponentsInChildren<ParticleSystemRenderer>(true));
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Material[] authored = renderer.sharedMaterials;
                Material[] work = null;
                for (int i = 0; i < authored.Length; i++)
                {
                    Material source = authored[i];
                    if (source == null || !source.IsKeywordEnabled("_EMISSION") || !source.HasProperty("_EmissionColor")) continue;
                    if (!_workMaterials.TryGetValue(source, out Material neutral))
                    {
                        neutral = new Material(source) { name = source.name + " (creator work light)", hideFlags = HideFlags.HideAndDontSave };
                        neutral.DisableKeyword("_EMISSION");
                        neutral.SetColor("_EmissionColor", Color.black);
                        _workMaterials.Add(source, neutral);
                    }
                    if (work == null) work = (Material[])authored.Clone();
                    work[i] = neutral;
                }
                if (work != null) _materialOverrides.Add(new MapCreatorMaterialOverride(renderer, authored, work));
            }
        }
        if (GameContext.GridController != null && GameContext.GridController.Fog != null)
            foreach (Renderer renderer in GameContext.GridController.Fog.GetComponentsInChildren<Renderer>(true))
                if (!_effects.Contains(renderer)) _effects.Add(renderer);
        _cameraData = _camera.GetComponent<UniversalAdditionalCameraData>();
        _refresh = false;
    }

    /// <summary>
    /// Captures authored render state and installs a white studio light for the work camera alone.
    /// </summary>
    private void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != _camera || ShowEnvironment)
            return;
        Restore();
        if (_refresh)
            RefreshReferences();
        _ambientMode = RenderSettings.ambientMode;
        _skyColor = RenderSettings.ambientSkyColor;
        _equatorColor = RenderSettings.ambientEquatorColor;
        _groundColor = RenderSettings.ambientGroundColor;
        _reflectionIntensity = RenderSettings.reflectionIntensity;
        _ambientIntensity = RenderSettings.ambientIntensity;
        _ambientProbe = RenderSettings.ambientProbe;
        _fog = RenderSettings.fog;
        _clearFlags = camera.clearFlags;
        _background = camera.backgroundColor;
        _postProcessing = _cameraData != null && _cameraData.renderPostProcessing;
        _renderShadows = _cameraData != null && _cameraData.renderShadows;
        _pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (_pipeline != null)
        {
            _shadowDistance = _pipeline.shadowDistance;
        }
        _sun = GameContext.EnvironmentController != null ? GameContext.EnvironmentController.Sun : null;
        if (_sun != null)
        {
            _sunColor = _sun.color;
            _sunIntensity = _sun.intensity;
            _sunUseTemperature = _sun.useColorTemperature;
            _sunRotation = _sun.transform.rotation;
            _sunShadows = _sun.shadows;
            _sunShadowStrength = _sun.shadowStrength;
            _sunShadowBias = _sun.shadowBias;
            _sunNormalBias = _sun.shadowNormalBias;
        }
        _lightStates.Clear();
        _effectStates.Clear();
        _applied = true;
        foreach (MapCreatorMaterialOverride binding in _materialOverrides) binding.Apply();
        // Disable colored prop lights and particle overdraw, but preserve every original enabled state.
        foreach (Light light in _lights)
        {
            _lightStates.Add(light != null && light.enabled);
            if (light != null) light.enabled = light == _sun;
        }
        foreach (Renderer effect in _effects)
        {
            _effectStates.Add(effect != null && effect.enabled);
            if (effect != null) effect.enabled = false;
        }
        if (_sun != null)
        {
            _sun.color = Color.white;
            _sun.useColorTemperature = false;
            _sun.intensity = 1.15f;
            _sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.82f;
            _sun.shadowBias = 0.035f;
            _sun.shadowNormalBias = 0.18f;
        }
        // Keep distant board tiles inside the shadow range; restore the active profile immediately after this camera.
        if (_pipeline != null)
        {
            _pipeline.shadowDistance = Mathf.Max(_shadowDistance, Mathf.Min(180f, camera.transform.position.y * 2f + 20f));
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.75f, 0.80f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.54f, 0.57f);
        RenderSettings.ambientGroundColor = new Color(0.33f, 0.34f, 0.36f);
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.ambientIntensity = 1f;
        var neutralProbe = new SphericalHarmonicsL2();
        neutralProbe.AddAmbientLight(new Color(0.30f, 0.32f, 0.35f));
        RenderSettings.ambientProbe = neutralProbe;
        RenderSettings.fog = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.075f, 0.073f, 0.080f);
        if (_cameraData != null) _cameraData.renderPostProcessing = false;
        if (_cameraData != null) _cameraData.renderShadows = true;
    }

    /// <summary>
    /// Restores authored lighting before another camera, the UI overlay, or a preview capture renders.
    /// </summary>
    private void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera == _camera) Restore();
    }

    /// <summary>
    /// Restores every temporarily changed value, including originally disabled lights and effects.
    /// </summary>
    private void Restore()
    {
        if (!_applied) return;
        _applied = false;
        foreach (MapCreatorMaterialOverride binding in _materialOverrides) binding.Restore();
        for (int i = 0; i < _lightStates.Count; i++)
            if (_lights[i] != null) _lights[i].enabled = _lightStates[i];
        for (int i = 0; i < _effectStates.Count; i++)
            if (_effects[i] != null) _effects[i].enabled = _effectStates[i];
        if (_sun != null)
        {
            _sun.color = _sunColor;
            _sun.intensity = _sunIntensity;
            _sun.useColorTemperature = _sunUseTemperature;
            _sun.transform.rotation = _sunRotation;
            _sun.shadows = _sunShadows;
            _sun.shadowStrength = _sunShadowStrength;
            _sun.shadowBias = _sunShadowBias;
            _sun.shadowNormalBias = _sunNormalBias;
        }
        RenderSettings.ambientMode = _ambientMode;
        RenderSettings.ambientSkyColor = _skyColor;
        RenderSettings.ambientEquatorColor = _equatorColor;
        RenderSettings.ambientGroundColor = _groundColor;
        RenderSettings.reflectionIntensity = _reflectionIntensity;
        RenderSettings.ambientIntensity = _ambientIntensity;
        RenderSettings.ambientProbe = _ambientProbe;
        RenderSettings.fog = _fog;
        if (_camera != null)
        {
            _camera.clearFlags = _clearFlags;
            _camera.backgroundColor = _background;
        }
        if (_cameraData != null) _cameraData.renderPostProcessing = _postProcessing;
        if (_cameraData != null) _cameraData.renderShadows = _renderShadows;
        if (_pipeline != null)
        {
            _pipeline.shadowDistance = _shadowDistance;
        }
    }

    /// <summary>
    /// Releases render callbacks and restores the scene even when the creator is destroyed mid-frame.
    /// </summary>
    public void Dispose()
    {
        Restore();
        ReleaseWorkMaterials();
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
    }

    /// <summary>
    /// Destroys only cache-owned material copies after all renderer bindings have been restored.
    /// </summary>
    private void ReleaseWorkMaterials()
    {
        _materialOverrides.Clear();
        foreach (Material material in _workMaterials.Values)
            if (material != null) UnityEngine.Object.Destroy(material);
        _workMaterials.Clear();
    }
}
