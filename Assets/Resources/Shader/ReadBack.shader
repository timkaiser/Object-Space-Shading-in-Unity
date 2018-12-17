﻿//This shader takes the tilemap with the world position and projects it back onto the object
Shader "Custom/ReadBack"
{
	Properties
	{
		[PerRendererData]_TextureAtlas("_TextureAtlas", 2D) = "" {}
		_TextureSize("TextureSize", Int) = 8192
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		

		Pass
		{
			name "readback"

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

			sampler2D _TextureAtlas;
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
				uint mipLevel = log2(max(max(dx.x, dx.y), max(dy.x, dy.y)) * _TextureSize);
				uint powMipLevel = pow(2, mipLevel);

				float2 atlasOffset = float2(1.0-(1.0/ powMipLevel),0);		//offset in texture atlas (for mipmap) in uv coordinates (btw. 0 and 1)
				float2 uv = i.uv / powMipLevel;
				uv.x /= 2.0;  //because the atlas is twice the width of the texture

				return float4(1, 0, 0, 1);// tex2D(_TextureAtlas, atlasOffset + uv);
			}
		ENDCG
		}
	}
}
