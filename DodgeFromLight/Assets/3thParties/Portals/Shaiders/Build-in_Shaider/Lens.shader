Shader "SimuranStudio/Lens"
{
    Properties
    {
        _InvFade ("Soft Particles Factor", Range(0.01, 3.0)) = 1.0
        _Power ("Power", Float) = 0
        _Speed ("Speed", Vector) = (0, 0, 0, 0)
        _Main ("Main Texture", 2D) = "white" {}
        _Main_2 ("Additional Texture", 2D) = "white" {}
        _Tiling ("Tiling", Vector) = (0, 0, 0, 0)
        [HDR] _Color0 ("Color", Color) = (1, 1, 1, 0)
        _Mask_Power1 ("Mask_Power", Float) = 2
        _Mask_intens1 ("Mask_intensity", Float) = 3.89
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        Cull Front
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "PortalLens"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LensVertex
            #pragma fragment LensFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ SOFTPARTICLES_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_Main);
            SAMPLER(sampler_Main);
            TEXTURE2D(_Main_2);
            SAMPLER(sampler_Main_2);

            CBUFFER_START(UnityPerMaterial)
                float4 _Speed;
                float4 _Tiling;
                float4 _Color0;
                float _InvFade;
                float _Power;
                float _Mask_Power1;
                float _Mask_intens1;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 fogAndEyeDepth : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// <summary>
            /// Transforms portal geometry and forwards its particle tint, UVs, fog, and depth.
            /// </summary>
            Varyings LensVertex(Attributes input)
            {
                // Preserve the authored particle vertex colors and texture coordinates.
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.color = input.color;
                output.uv = input.uv;
                output.fogAndEyeDepth = float2(
                    ComputeFogFactor(positions.positionCS.z),
                    -positions.positionVS.z);
                return output;
            }

            /// <summary>
            /// Distorts the URP opaque texture while preserving the existing lens material parameters.
            /// </summary>
            half4 LensFragment(Varyings input) : SV_Target
            {
                // SRP supplies the scene texture explicitly; no GrabPass or render-thread copy is needed.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float2 scrollingUV = input.uv * _Tiling.xy + _Time.y * _Speed.xy;
                float2 maskUV = input.uv * _Tiling.zw;
                half4 distortion = SAMPLE_TEXTURE2D(_Main, sampler_Main, scrollingUV)
                    * SAMPLE_TEXTURE2D(_Main_2, sampler_Main_2, maskUV);
                half3 sceneColor = SampleSceneColor(saturate(screenUV + distortion.xy * _Power));
                float mask = pow(saturate(input.uv.y), _Mask_Power1) * _Mask_intens1;
                half4 color = input.color * _Color0 * half4(sceneColor, 1.0h) * mask;

                #if defined(SOFTPARTICLES_ON)
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                    color.a *= saturate(_InvFade * (sceneDepth - input.fogAndEyeDepth.y));
                #endif
                color.rgb = MixFog(color.rgb, input.fogAndEyeDepth.x);
                return color;
            }
            ENDHLSL
        }
    }
}
