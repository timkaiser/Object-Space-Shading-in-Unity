Shader "Custom/FirstPass"
{
	Properties
	{
		_ID("ID", Int) = 0
		_TextureSize("TextureSize", Int) = 512
	}
		SubShader
		{

			Pass //First Pass
			{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				/*===============================================================================================
					INPUT / OUTPUT STRUCTS
				  ===============================================================================================*/

				struct appdata  //vert in
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f   //vert to frag
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				//Output of fragment shader
				struct fOut {
					int2 idAndMip : SV_Target0;
					float2 uv : SV_Target1;
				};
				/*===============================================================================================
					SHADER
				  ===============================================================================================*/

				int _ID;
				int _TextureSize;

				//vertex shader
				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;

					return o;
				};

				//fragment shader
				fOut frag(v2f i)
				{
					fOut o;
					o.idAndMip.x = _ID;
					o.uv = i.uv;

					float2 dx = abs(ddx(i.uv));
					float2 dy = abs(ddy(i.uv));
					int mipLevel = log2(max(max(dx.x, dx.y), max(dy.x, dy.y)) * _TextureSize);
					o.idAndMip.y = mipLevel;

					return o;
				}
				ENDCG
			}
		}
}
