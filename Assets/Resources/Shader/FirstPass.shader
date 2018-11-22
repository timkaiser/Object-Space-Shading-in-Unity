Shader "Custom/FirstPass"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
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

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float3 normal : NORMAL;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
					float3 normal : NORMAL;
					float3 worldPos : POSITON;
				};

				//Output of fragment shader
				struct fragOut {
					int id : SV_Target0;
					float2 uv : SV_Target1;
					float4 worldPos : SV_Target2;
					float3 normal : SV_Target3;
				};
				/*===============================================================================================
					SHADER
				  ===============================================================================================*/

				sampler2D _MainTex;
				float4 _MainTex_ST;
				int _ID;
				int _TextureSize;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;//TRANSFORM_TEX(v.uv, _MainTex);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex);
					o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));;

					return o;
				};

				fragOut frag(v2f i)
				{
					fragOut o;
					o.id = _ID;
					o.uv = i.uv;
					o.worldPos.xyz = i.worldPos;
					o.normal = i.normal;

					float2 dx = ddx(i.uv);
					float2 dy = ddy(i.uv);
					float mipLevel = log2(max(max(dx.x, dx.y), max(dy.x, dy.y)) * _TextureSize);
					o.worldPos.w = mipLevel;

					//o.uv = float2(mipLevel / 10, 0);
					return o;
				}
				ENDCG
			}
		}
}
