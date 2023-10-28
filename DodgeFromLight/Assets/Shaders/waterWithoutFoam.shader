// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Slyvek/WaterFoamless"
{
    Properties
    {
		// What color the water will sample when the surface below is shallow.
		_DepthGradientShallow("Depth Gradient Shallow", Color) = (0.325, 0.807, 0.971, 0.725)

		// What color the water will sample when the surface below is at its deepest.
		[HDR]_DepthGradientDeep("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)

		// Maximum distance the surface below the water will affect the color gradient.
		_DepthMaxDistance("Depth Maximum Distance", Float) = 1
    }
    SubShader
    {
		Tags
		{
			"RenderType" = "Transparent" "Queue" = "Transparent"
		}

        Pass
        {
			// Transparent "normal" blending.
			Blend SrcAlpha OneMinusSrcAlpha  
			ZWrite Off
			Cull Back
			Lighting Off

            CGPROGRAM
			#define SMOOTHSTEP_AA 0.01

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			// Blends two colors using the same algorithm that our shader is using
			// to blend with the screen. This is usually called "normal blending",
			// and is similar to how software like Photoshop blends two layers.
			float4 alphaBlend(float4 top, float4 bottom)
			{
				float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
				float alpha = top.a + bottom.a * (1 - top.a);

				return float4(color, alpha);
			}

            struct appdata
            {
                float4 vertex : POSITION;
				float4 uv : TEXCOORD0;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;	
				float4 worldPosition : TEXCOORD2;
				float4 screenPos  : TEXCOORD3;
				float3 viewNormal : NORMAL;
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPosition = mul (unity_ObjectToWorld, v.vertex);// ComputeScreenPos(o.vertex);
				o.viewNormal = COMPUTE_VIEW_NORMAL;
				o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

			float4 _DepthGradientShallow;
			float4 _DepthGradientDeep;
			float4 _FoamColor;

			float _DepthMaxDistance;

			sampler2D _CameraDepthTexture;
			sampler2D _CameraNormalsTexture;

            float4 frag (v2f i) : SV_Target
            {
				// Retrieve the current depth value of the surface behind the
				// pixel we are currently rendering.
				float existingDepth01 = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
				// Convert the depth from non-linear 0...1 range to linear
				// depth, in Unity units.
				float existingDepthLinear = LinearEyeDepth(existingDepth01);

				//float existingDepthLinear = LinearEyeDepth(tex2Dlod(_CameraDepthTexture, i.screenPos).r);
				// Difference, in Unity units, between the water's surface and the object behind it.
				float depthDifference = existingDepthLinear - i.screenPos.w;

				// Calculate the color of the water based on the depth using our two gradient colors.
				float waterDepthDifference01 = saturate(_DepthMaxDistance / depthDifference);
				float4 waterColor = lerp(_DepthGradientShallow, _DepthGradientDeep, waterDepthDifference01);
				
				// Retrieve the view-space normal of the surface behind the
				// pixel we are currently rendering.
				float3 existingNormal = tex2Dproj(_CameraNormalsTexture, UNITY_PROJ_COORD(i.worldPosition));
				
				// Modulate the amount of foam we display based on the difference
				// between the normals of our water surface and the object behind it.
				// Larger differences allow for extra foam to attempt to keep the overall
				// amount consistent.
				

				// Use normal alpha blending to combine the foam with the surface.
				return waterColor;
            }
            ENDCG
        }
    }
}