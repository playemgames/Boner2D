// Upgrade NOTE: upgraded instancing buffer 'MyProperties' to new syntax.

Shader "Sprites/Skeleton-CutOut"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
		[PerRendererData] _Normal ("Normal", vector) = (0,0,-1, 0) // Normals set by script
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		_Cutoff ("Shadow alpha cutoff", Range(0,1)) = 0.5
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="TransparentCutOut" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting On
		ZWrite On
		Offset -1, -1
		// Fog { Mode On }
		Blend One OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert alpha:blend addshadow vertex:vert fullforwardshadows
		#pragma multi_compile DUMMY PIXELSNAP_ON
		#pragma multi_compile_instancing
		#pragma multi_compile_fog

		sampler2D _MainTex;
		// fixed4 _Color;
		// float3 _Normal;

		struct Input
		{
			float2 uv_MainTex;
			fixed4 color;

            UNITY_VERTEX_INPUT_INSTANCE_ID
		};
 
		UNITY_INSTANCING_BUFFER_START(MyProperties)
			UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
#define _Color_arr MyProperties
			UNITY_DEFINE_INSTANCED_PROP(float3, _Normal)
#define _Normal_arr MyProperties
		UNITY_INSTANCING_BUFFER_END(MyProperties)
		
		void vert (inout appdata_full v, out Input o)
		{
			#if defined(PIXELSNAP_ON) && !defined(SHADER_API_FLASH)
			v.vertex = UnityPixelSnap (v.vertex);
			#endif
			v.normal = UNITY_ACCESS_INSTANCED_PROP(_Normal_arr, _Normal);
			
			UNITY_INITIALIZE_OUTPUT(Input, o);
 
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_TRANSFER_INSTANCE_ID(v, o);
			o.color = v.color * UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color);
		}

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}

Fallback "Transparent/Cutout/VertexLit"
}
