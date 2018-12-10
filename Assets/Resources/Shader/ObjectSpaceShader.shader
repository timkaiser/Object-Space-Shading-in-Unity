Shader "Custom/ObjectSpaceShader"
{
	Properties
	{
		_ID("ID", Int) = 0
		_TextureSize("TextureSize", Int) = 512
		_TextureArray("_TextureArray", 2DArray) = "" {}
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
				float3 worldPos : COLOR0 ;
			};
			
			struct fOut{//Output of fragment shader
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
			UNITY_DECLARE_TEX2DARRAY(_TextureArray);

			//vertex shader
			v2f vert(appdata_base v){
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				o.uv.y = 1 - o.uv.y; //why do I need this
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			};

			//fragment shader
			fOut frag(v2f i){
				float2 dx = abs(ddx(i.uv));
				float2 dy = abs(ddy(i.uv));
				int mipLevel = log2(max(max(dx.x, dx.y), max(dy.x, dy.y)) * _TextureSize);

				#if defined(_FIRST_PASS) //FIRST PASS
				fOut o;
				o.idAndMip.x = _ID;
				o.uv = i.uv;
				o.worldPos = i.worldPos;
				o.idAndMip.y = mipLevel;

				return o;

				#else	//READ BACK
				int powMipLevel = pow(2, mipLevel);
				float2 uv = i.uv / powMipLevel;

				return (fOut) UNITY_SAMPLE_TEX2DARRAY(_TextureArray, float3(uv, mipLevel));
				#endif
			}
			ENDCG

		}
	}
}
