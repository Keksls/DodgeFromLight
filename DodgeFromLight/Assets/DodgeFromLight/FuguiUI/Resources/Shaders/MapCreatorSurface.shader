Shader "Dodge From Light/Fugui/Creator Surface"
{
    Properties
    {
        [MainTexture] _Texture("Texture", 2D) = "white" {}
        _State("Surface state", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif
            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                float _State;
                float _TextureIsAlpha;
            CBUFFER_END
            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);
            struct Attributes
            {
                float2 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint color : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            /// <summary>
            /// Projects Fugui vertices and decodes the packed vertex alpha.
            /// </summary>
            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(float3(input.positionOS, 0.0));
                output.uv = input.uv;
                output.color = half4(input.color & 255, (input.color >> 8) & 255, (input.color >> 16) & 255, (input.color >> 24) & 255) / 255.0h;
                return output;
            }
            /// <summary>
            /// Produces stable fine grain without animation or expensive noise octaves.
            /// </summary>
            float Grain(float2 coordinates)
            {
                return frac(sin(dot(coordinates, float2(12.9898, 78.233))) * 43758.5453);
            }
            /// <summary>
            /// Shades quiet dark leather and forged bronze, keeping texture away from text contrast.
            /// </summary>
            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float noise = Grain(floor(input.positionCS.xy * 0.7));
                float grain = 0.93 + noise * 0.14;
                half3 dark = half3(0.035, 0.030, 0.027);
                half3 light = half3(0.105, 0.088, 0.069);
                if (_State > 1.5 && _State < 2.5)
                {
                    dark = half3(0.10, 0.065, 0.031);
                    light = half3(0.24, 0.165, 0.078);
                }
                else if (_State > 2.5 && _State < 3.5)
                    light *= 1.40;
                else if (_State > 3.5)
                    light *= 0.60;
                float vignette = saturate(1.0 - length((uv - 0.5) * 1.1));
                half3 color = lerp(dark, light, (0.32 + 0.35 * vignette) * grain);
                color += half3(0.04, 0.031, 0.019) * pow(1.0 - uv.y, 8.0);
                #ifndef UNITY_COLORSPACE_GAMMA
                    color = FastSRGBToLinear(color);
                #endif
                return half4(color, input.color.a);
            }
            ENDHLSL
        }
    }
}
