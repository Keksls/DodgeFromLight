Shader "Custom/UIDisolve"
{
  Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)

        _MainTex  ("Texture", 2D) = "white" {}
		_Scale ("Scale", Range (0.00001, 1)) = 0.01
		[HDR]_EdgeColour1 ("Edge colour 1", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR]_EdgeColour2 ("Edge colour 2", Color) = (1.0, 1.0, 1.0, 1.0)
		_Level ("Dissolution level", Range (0.0, 1.0)) = 0.1
		_Edges ("Edge width", Range (0.0, 1.0)) = 0.1
		_StartDisolve ("Start Disolve", Float) = 100
		_StartDisolveWidth ("Start Disolve Width", Float) = 100
        
		_SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)
		_SurfaceDistortion("Surface Distortion", 2D) = "white" {}	
		_SurfaceDistortionAmount("Surface Distortion Amount", Range(0, 1)) = 0.27
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
        CGPROGRAM
			#define SMOOTHSTEP_AA 0.01
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
			float4 _EdgeColour1;
			float4 _EdgeColour2;
			float _Level;
			float _Scale;
			float _Edges;
            float _StartDisolve;
            float _StartDisolveWidth;

			sampler2D _SurfaceDistortion;
			float4 _SurfaceDistortion_ST;
			float _SurfaceDistortionAmount;
			float2 _SurfaceNoiseScroll;

			float4 alphaBlend(float4 top, float4 bottom)
			{
				float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
				float alpha = top.a + bottom.a * (1 - top.a);

				return float4(color, alpha);
			}

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);  
                OUT.screenPos = ComputeScreenPos(OUT.vertex) * _ScreenParams.x / 100;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                _Scale /= 100;
				float2 distortSample = (tex2D(_SurfaceDistortion, (IN.screenPos.xy + IN.screenPos.z) * _Scale).xy * 2 - 1) * _SurfaceDistortionAmount;

				float2 noiseUV = float2((IN.screenPos.x * _Scale + _Time.y * _SurfaceNoiseScroll.x) + distortSample.x, 
				(IN.screenPos.y * _Scale + _Time.y * _SurfaceNoiseScroll.y) + distortSample.y);                
                _StartDisolve = _ScreenParams.x - _StartDisolve;
                half4 color = IN.color;
                float x = (1 / ((IN.screenPos.x - _StartDisolve) / _StartDisolveWidth));
                float cutout = lerp(1, tex2D(_MainTex , noiseUV).r, x);
                float alpha = 1;

                if (cutout <= _Level) {
				    alpha = 0;
			    }
                else {
                    if(cutout < alpha && cutout < _Level + _Edges){ 
                        color = lerp(_EdgeColour1, _EdgeColour2, (cutout - _Level) / _Edges);
                    }
                }

                float surfaceNoise = smoothstep(_Level - SMOOTHSTEP_AA, _Level + SMOOTHSTEP_AA, 1);
                alpha *= lerp(1, surfaceNoise, x);

                clip (alpha - _Level);

                return color;
            }
        ENDCG
        }
    }
}