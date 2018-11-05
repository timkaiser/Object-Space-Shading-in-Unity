Shader "Custom/CombinedShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_ID("ID", Int) = 0
		_IsCube("IsCube", Range(0,1)) = 0
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
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			/*===============================================================================================
				INPUT / OUTPUT STRUCTS
			  ===============================================================================================*/

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uvCube : TEXCOORD1;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uvCube : TEXTCOORD1;
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 worldPos : POSITON;
			};

			//Output of fragment shader
			struct fragOut {
				//int id;
				float4 UVandID : SV_Target;
				float3 worldPos : SV_Target1;
				float3 normal : SV_Target2;
			};
			/*===============================================================================================
				SHADER
			  ===============================================================================================*/

			sampler2D _MainTex;
			float4 _MainTex_ST;
			int _ID;
			int _IsCube;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvCube = TRANSFORM_TEX(v.uvCube, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.normal = normalize(UnityObjectToViewPos(v.normal));;
				return o;
			};

			fragOut frag(v2f i)
			{
				fragOut o;
				//textrue: fixed4 col = tex2D(_MainTex, i.uv);
				o.UVandID = fixed4((_IsCube == 0)?i.uv : i.uvCube,_ID/256.0f,1);
				o.worldPos = i.worldPos;
				o.normal = i.normal;
				return o;
			}
			ENDCG
		}
	}
}
