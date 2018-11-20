// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//source: https://forum.unity.com/threads/unwrapping-mesh-geometry-in-a-shader.125455/
Shader "Custom/Unwrap" {
	SubShader{
	Tags{ "RenderType" = "Opaque" }
		Pass
		{	
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint vertexId : SV_VertexID;
			};

			struct v2f
			{
				float4  pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				v.vertex = float4(v.uv.xy, 0.0, 1.0);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				return float4(i.uv,0,1);
			}
		ENDCG
	}
}
FallBack Off
}