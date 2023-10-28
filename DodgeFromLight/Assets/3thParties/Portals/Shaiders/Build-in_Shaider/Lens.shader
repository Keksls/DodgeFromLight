Shader "SimuranStudio/Lens"
{
	Properties
	{
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_Power("Power", Float) = 0
		_Speed("Speed", Vector) = (0,0,0,0)
		_Main("Main Texture", 2D) = "white" {}
		_Main_2("Additional Texture", 2D) = "white" {}
		_Tiling("Tiling", Vector) = (0,0,0,0)
		[HDR]_Color0("Color", Color) = (1,1,1,0)
		_Mask_Power1("Mask_Power", Float) = 2
		_Mask_intens1("Mask_intensity", Float) = 3.89

	}


	Category 
	{
		SubShader
		{
		LOD 0

			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask RGB
			Cull Front
			Lighting Off 
			ZWrite Off
			ZTest LEqual
			GrabPass{ }

			Pass {
			
				CGPROGRAM
				#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
				#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
				#else
				#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
				#endif

				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
				#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
				#endif
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_instancing
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_FRAG_COLOR


				#include "UnityCG.cginc"

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					
				};

				struct v2f 
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					float4 ase_texcoord3 : TEXCOORD3;
				};
				
				
				#if UNITY_VERSION >= 560
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				#else
				uniform sampler2D_float _CameraDepthTexture;
				#endif

				//Don't delete this comment
				// uniform sampler2D_float _CameraDepthTexture;

				uniform sampler2D _MainTex;
				uniform fixed4 _TintColor;
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform float4 _Color0;
				ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
				uniform sampler2D _Main;
				uniform float2 _Speed;
				uniform float4 _Tiling;
				uniform sampler2D _Main_2;
				uniform float _Power;
				uniform float _Mask_Power1;
				uniform float _Mask_intens1;
				inline float4 ASE_ComputeGrabScreenPos( float4 pos )
				{
					#if UNITY_UV_STARTS_AT_TOP
					float scale = -1.0;
					#else
					float scale = 1.0;
					#endif
					float4 o = pos;
					o.y = pos.w * 0.5f;
					o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
					return o;
				}
				


				v2f vert ( appdata_t v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
					float4 screenPos = ComputeScreenPos(ase_clipPos);
					o.ase_texcoord3 = screenPos;
					

					v.vertex.xyz +=  float3( 0, 0, 0 ) ;
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos (o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag ( v2f i  ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( i );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );

					#ifdef SOFTPARTICLES_ON
						float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate (_InvFade * (sceneZ-partZ));
						i.color.a *= fade;
					#endif

					float4 screenPos = i.ase_texcoord3;
					float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
					float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
					float2 appendResult23 = (float2(_Speed.x , _Speed.y));
					float2 appendResult27 = (float2(_Tiling.x , _Tiling.y));
					float2 texCoord20 = i.texcoord.xy * appendResult27 + float2( 0,0 );
					float2 panner18 = ( 1.0 * _Time.y * appendResult23 + texCoord20);
					float2 appendResult28 = (float2(_Tiling.z , _Tiling.w));
					float2 texCoord25 = i.texcoord.xy * appendResult28 + float2( 0,0 );
					float4 screenColor9 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( ase_grabScreenPosNorm + ( ( tex2D( _Main, panner18 ) * tex2D( _Main_2, texCoord25 ) ) * _Power ) ).xy);
					float2 texCoord30 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					

					fixed4 col = ( i.color * _Color0 * screenColor9 * ( pow( texCoord30.y , _Mask_Power1 ) * _Mask_intens1 ) );
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG 
			}
		}	
	}
	CustomEditor "ASEMaterialInspector"
	
	
}