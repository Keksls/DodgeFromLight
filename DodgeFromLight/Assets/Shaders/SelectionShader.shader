Shader "DodgeFromLight/Selection"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
		_Color2("Color2", Color) = (1,1,1,1)
		[HDR]_Emission("Emission", Color) = (1,1,1,1)
		[HDR]_Emission2("Emission2", Color) = (1,1,1,1)
		_Blend("Blend", Range(0, 1)) = 0.5
		_Speed("Speed", Range(0, 10)) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha  
			ZWrite Off
			Cull Back
			Lighting Off

        CGPROGRAM 
        #pragma surface surf Lambert
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
			float blend;
			float3 worldPos;
			fixed facing : VFACE;
        };

		fixed4 _Color;
		fixed4 _Color2;
		fixed4 _Emission;
		fixed4 _Emission2;
		float _Blend;
		float _Speed;
		float _WindPower;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutput o)
		{
			_Blend = (1 + _SinTime.w * _Speed) / 2;

			o.Albedo = lerp(_Color, _Color2, _Blend);
			o.Emission = lerp(_Emission, _Emission2, _Blend);
        }

        ENDCG
    }
    FallBack "Diffuse"
}