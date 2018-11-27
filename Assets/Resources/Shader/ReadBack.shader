Shader "Custom/ReadBack"
{
	Properties
	{
		_TextureArray("_TextureArray", 2DArray) = "" {}
		_TextureSize("TextureSize", Int) = 512
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma require 2darray

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			UNITY_DECLARE_TEX2DARRAY(_TextureArray);
			int _TextureSize;


			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}


			float4 frag(v2f i) : SV_Target
			{
				float2 dx = abs(ddx(i.uv));
				float2 dy = abs(ddy(i.uv));
				int mipLevel = log2(max(max(dx.x, dx.y), max(dy.x, dy.y)) * _TextureSize);

				
				int powMipLevel = pow(2, mipLevel);
				float2 uv = i.uv / powMipLevel;
				return UNITY_SAMPLE_TEX2DARRAY(_TextureArray, float3(uv, mipLevel));
			}
		ENDCG
		}
	}
}
