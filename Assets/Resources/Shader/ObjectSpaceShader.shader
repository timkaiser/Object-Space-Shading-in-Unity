Shader "Custom/ObjectSpaceShader"
{
	Properties
	{
		_ID("ID", Int) = 0
		_TextureSize("TextureSize", Int) = 4096
		_Texture("_Texture", 2D) = "white" {}
		_TextureAtlas("_TextureAtlas", 2D) = "" {}
	}

		SubShader
	{

		Pass //First Pass
		{
			name "firspass"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _FIRST_PASS _READ_BACK

			#include "UnityCG.cginc"

		// FIRST PASS ############################################################################################################################
		// INPUT / OUTPUT STRUCTS _________________________________________________________________________________
		struct v2f {   //vert to frag
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
			float3 worldPos : COLOR0;
		};

		struct fOut {//Output of fragment shader
			#if defined(_FIRST_PASS)
			int2 idAndMip : SV_Target1;
			float2 uv : SV_Target0;
			float2 worldPos : SV_Target2;
			#else
			float4 color : SV_Target0;
			#endif
		};
		// SHADER _________________________________________________________________________________

		int _ID;
		int _TextureSize;
		sampler2D _TextureAtlas;

		//vertex shader
		v2f vert(appdata_base v) {
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
			o.uv.y = 1 - o.uv.y; //why do I need this
			o.worldPos = mul(unity_ObjectToWorld, v.vertex);
			return o;
		};

		//fragment shader
		fOut frag(v2f i) {
			float2 dx = abs(ddx(i.uv));
			float2 dy = abs(ddy(i.uv));
			uint mipLevel = log2(max(max(dx.x, dx.y), max(dy.x, dy.y)) * _TextureSize);
			
			#if defined(_FIRST_PASS) //FIRST PASS
			fOut o;
			o.idAndMip.x = _ID;
			o.uv = i.uv;
			o.worldPos = i.worldPos;
			
			o.idAndMip.y = mipLevel;

			return o;

			#else	//READ BACK
			uint powMipLevel = pow(2, mipLevel);

			float2 atlasOffset = float2(1.0 - (1.0 / powMipLevel), 0);		//offset in texture atlas (for mipmap) in uv coordinates (btw. 0 and 1)
			float2 uv = i.uv / powMipLevel;
			uv.x /= 2.0;  //because the atlas is twice the width of the texture


			fOut o = { tex2D(_TextureAtlas, atlasOffset + uv)};
			return o;
			#endif
		}
		ENDCG

	}
	}
}