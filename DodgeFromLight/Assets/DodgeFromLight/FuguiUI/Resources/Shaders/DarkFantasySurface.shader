Shader "Dodge From Light/Fugui/Dark Fantasy Surface"
{
    Properties
    {
        [MainTexture] _Texture("Texture", 2D) = "white" {}
        _BaseColor("Iron", Color) = (0.07, 0.06, 0.05, 1.0)
        _DeepColor("Abyss", Color) = (0.01, 0.01, 0.01, 1.0)
        _Opacity("Opacity", Range(0.0, 1.0)) = 0.96
        _Interaction("Interaction", Range(-0.5, 1.0)) = 0.0
        _SurfaceMode("Surface Mode", Range(0.0, 3.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "DARK FANTASY FUGUI"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DarkFantasyVertex
            #pragma fragment DarkFantasyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                float4 _BaseColor;
                float4 _DeepColor;
                float _TextureIsAlpha;
                float _Opacity;
                float _Interaction;
                float _SurfaceMode;
            CBUFFER_END

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            struct DarkFantasyAttributes
            {
                float2 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint color : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DarkFantasyVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// Converts the packed Fugui vertex color into the active Unity color space.
            half4 UnpackDarkFantasyColor(uint packedColor)
            {
                half4 color = half4(
                    packedColor & 0xff,
                    (packedColor >> 8) & 0xff,
                    (packedColor >> 16) & 0xff,
                    (packedColor >> 24) & 0xff) / 255.0h;

                #ifndef UNITY_COLORSPACE_GAMMA
                    color.rgb = FastSRGBToLinear(color.rgb);
                #endif
                return color;
            }

            /// Transforms Fugui overlay vertices while preserving normalized quad UVs.
            DarkFantasyVaryings DarkFantasyVertex(
                DarkFantasyAttributes input)
            {
                DarkFantasyVaryings output = (DarkFantasyVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS =
                    TransformObjectToHClip(float3(input.positionOS, 0.0));
                output.uv = float2(input.uv.x, 1.0 - input.uv.y);
                output.color = UnpackDarkFantasyColor(input.color);
                return output;
            }

            /// Produces a stable pseudo-random value from two-dimensional coordinates.
            float Hash21(float2 coordinates)
            {
                coordinates =
                    frac(coordinates * float2(123.34, 456.21));
                coordinates +=
                    dot(coordinates, coordinates + 45.32);
                return frac(coordinates.x * coordinates.y);
            }

            /// Produces smooth value noise for smoke, forged metal, and worn edges.
            float ValueNoise(float2 coordinates)
            {
                float2 cell = floor(coordinates);
                float2 local = frac(coordinates);
                local = local * local * (3.0 - 2.0 * local);

                float lower = lerp(
                    Hash21(cell),
                    Hash21(cell + float2(1.0, 0.0)),
                    local.x);
                float upper = lerp(
                    Hash21(cell + float2(0.0, 1.0)),
                    Hash21(cell + 1.0),
                    local.x);
                return lerp(lower, upper, local.y);
            }

            /// Layers three noise octaves into a low-cost smoke field.
            float FractalNoise(float2 coordinates)
            {
                float noise = ValueNoise(coordinates) * 0.56;
                noise +=
                    ValueNoise(coordinates * 2.03 + 7.1) * 0.29;
                noise +=
                    ValueNoise(coordinates * 4.11 + 19.7) * 0.15;
                return noise;
            }

            /// Draws a subtle circular sigil that breaks up large flat surfaces.
            float RitualSigil(float2 uv)
            {
                float2 centered = uv - 0.5;
                centered.x *= 1.7;
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x);
                float ring = 1.0 -
                    smoothstep(0.010, 0.026, abs(radius - 0.265));
                float spokes = pow(
                    saturate(cos(angle * 8.0) * 0.5 + 0.5),
                    18.0);
                spokes *= 1.0 -
                    smoothstep(0.02, 0.055, abs(radius - 0.18));
                float innerRing = 1.0 -
                    smoothstep(0.008, 0.02, abs(radius - 0.12));
                return saturate(ring + spokes * 0.5 + innerRing * 0.45);
            }

            /// Renders smoke, worn iron, aged gold highlights, and restrained embers.
            half4 DarkFantasyFragment(DarkFantasyVaryings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                half4 sampled =
                    SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.uv);
                if (_TextureIsAlpha > 0.5h)
                {
                    sampled = half4(1.0h, 1.0h, 1.0h, sampled.a);
                }

                float time = _Time.y;
                float edgeDistance = min(
                    min(uv.x, 1.0 - uv.x),
                    min(uv.y, 1.0 - uv.y));
                float outerRim =
                    1.0 - smoothstep(0.0, 0.045, edgeDistance);
                float innerBevel = smoothstep(0.018, 0.075, edgeDistance) *
                    (1.0 - smoothstep(0.075, 0.16, edgeDistance));
                float grain = FractalNoise(
                    uv * float2(15.0, 8.0) + float2(time * 0.018, 0.0));
                float scratches = pow(
                    saturate(sin(
                        uv.y * 730.0 +
                        ValueNoise(uv * 17.0) * 4.0) * 0.5 + 0.5),
                    18.0);
                float sigil = RitualSigil(uv);

                half3 accent = max(input.color.rgb, 0.001h);
                half3 color = lerp(_DeepColor.rgb, _BaseColor.rgb, 0.58 + grain * 0.28);
                color += _BaseColor.rgb * scratches * 0.10;
                color += accent * innerBevel * (0.035 + max(_Interaction, 0.0) * 0.08);
                color += accent * outerRim * (0.055 + max(_Interaction, 0.0) * 0.14);
                color += accent * sigil * (0.012 + max(_Interaction, 0.0) * 0.025);

                if (_SurfaceMode > 0.5 && _SurfaceMode < 1.5)
                {
                    color *= 0.72;
                    color += accent * outerRim * 0.025;
                }
                else if (_SurfaceMode > 1.5 && _SurfaceMode < 2.5)
                {
                    float directional =
                        saturate((1.0 - uv.x) * 0.26 + (1.0 - uv.y) * 0.12);
                    color += accent * directional *
                        (0.035 + max(_Interaction, 0.0) * 0.12);
                    color += accent * sigil * 0.018;
                }
                else if (_SurfaceMode > 2.5)
                {
                    float2 smokeUv = uv * float2(4.2, 2.8);
                    smokeUv += float2(time * 0.022, -time * 0.013);
                    float smoke = FractalNoise(smokeUv);
                    float vignette = saturate(
                        1.0 - length((uv - 0.5) * float2(1.12, 0.92)));
                    float2 emberGrid = floor(
                        uv * float2(88.0, 48.0) +
                        float2(0.0, -time * 2.2));
                    float emberSeed = Hash21(emberGrid);
                    float ember = step(0.989, emberSeed) *
                        pow(saturate(
                            1.0 - length(
                                frac(uv * float2(88.0, 48.0) +
                                float2(0.0, -time * 2.2)) - 0.5) * 2.0),
                            5.0);

                    color = lerp(_DeepColor.rgb, _BaseColor.rgb, smoke * 0.72);
                    color += accent * smoke * 0.045;
                    color += accent * ember * (0.6 + sin(time * 5.0 + emberSeed * 13.0) * 0.25);
                    color *= lerp(0.6, 1.0, vignette);
                    outerRim = 0.0;
                    innerBevel = 0.0;
                    sigil *= 0.22;
                    color += accent * sigil * 0.02;
                }

                float disabledFactor = saturate(_Interaction + 0.35);
                half luminance =
                    dot(color, half3(0.299h, 0.587h, 0.114h));
                color = lerp(
                    luminance.xxx,
                    color,
                    disabledFactor);
                half alpha = sampled.a * input.color.a * _Opacity;
                return half4(color * sampled.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
