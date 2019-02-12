// This shader does two passes, controlled by the '_FIRST_PASS' and '_READ_BACK' tag
// First pass: calculates the uv coordinate, the mip level and the id of every object 
// Second pass: (read back): renderers objects with already shaded texture (_TextureAtlas)
// 
// Input:	_ID:			int, Id of current object
//			_TextureSize:	int, size of the texture of this object
//			_Texture:		texture, normal, unshaded texture of the object
//			_TextureAtlas:	texture, shaded texture on diffrent mip levels
//			_VertexIDs:		texture, contains ids of the corresponding vertices for every pixel of the texture
//			_BaycentCoords:	texture, contains baycentric coords for every pixel of the texture
//			_TileMask:		texture, contains mask that shows which part of the texture is shaded (not needed for that shader, but stored here)

Shader "Custom/ObjectSpaceShader"
{
	Properties
	{
		_ID("_ID", Int) = 0
		_TextureSize("_TextureSize", Int) = 4096
		_Texture("_Texture", 2D) = "white" {}
		_TextureAtlas("_TextureAtlas", 2D) = "" {}
		_VertexIDs("_VertexIDs", 2D) = "" {}
		_BaycentCoords("_BaycentCoords", 2D) = "" {}
		_TileMask("_TileMask", 2D) = "" {}
	}

		SubShader
	{

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _FIRST_PASS _READ_BACK

			#include "UnityCG.cginc"

		// INPUT / OUTPUT STRUCTS _________________________________________________________________________________
		struct v2f {   //vert to frag for both passes
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
			float3 worldPos : COLOR0;
		};

		struct fOut {//Output of fragment shader
			#if defined(_FIRST_PASS)		//output for first pass
			int2 idAndMip : SV_Target1;
			float2 uv : SV_Target0;
			float2 worldPos : SV_Target2;
			#else							//output for second pass
			float4 color : SV_Target0;		
			#endif
		};
		// SHADER __________________________________________________________________________________________________

		int _ID;
		int _TextureSize;
		sampler2D _TextureAtlas;
		sampler2D _Texture;

		//vertex shader (same for both passes)
		v2f vert(appdata_base v) {
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
			o.uv.y = 1 - o.uv.y; //why do I need this?
			o.worldPos = mul(unity_ObjectToWorld, v.vertex);
			return o;
		};

		//fragment shader
		fOut frag(v2f i) {
			//calculate mip level (for both passes)
			float2 dx = abs(ddx(i.uv));
			float2 dy = abs(ddy(i.uv));
			uint mipLevel = log2(max(max(dx.x, dx.y), max(dy.x, dy.y)) * _TextureSize);
			
			#if defined(_FIRST_PASS) //FIRST PASS
			fOut o;
			o.idAndMip.x = _ID;
			o.idAndMip.y = mipLevel;
			o.uv = i.uv;

			//o.uv = tex2D(_Texture,i.uv);
			return o;

			#else	//READ BACK
			uint powMipLevel = pow(2, mipLevel);

			float2 atlasOffset = float2(1.0 - (1.0 / powMipLevel), 0);		//offset in texture atlas (for mipmap) in uv coordinates (btw. 0 and 1)
			float2 uv = i.uv / powMipLevel;
			uv.x /= 2.0;  //because the atlas is twice the width of the texture

			fOut o = { tex2D(_TextureAtlas, atlasOffset + uv) };
			return o;
			#endif
		}
		ENDCG

	}
	}
}