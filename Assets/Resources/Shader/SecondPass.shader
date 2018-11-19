Shader "Custom/SecondPass"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			UNITY_DECLARE_TEX2DARRAY(_TextureArray);
			int _TextureSize;

			float4 _MainTex_ST;
			sampler2D _MainTex;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;// TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 dx = ddx(i.uv);
				float2 dy = ddy(i.uv);
				int mipLevel = log2(max(max(dx.x, dx.y), max(dy.x, dy.y)) * _TextureSize);

				int powMipLevel = pow(2, mipLevel);
				float2 uv = i.uv / powMipLevel;

				fixed4 result = fixed4(0,0,0,1);
				for (int j = 0; j < 7; j++) {
					int p = pow(2, j);
					result += UNITY_SAMPLE_TEX2DARRAY(_TextureArray, float3(i.uv / p, j));
				}
				result.w = 1;

				return result;

			}
		ENDCG
		}
	}
}
