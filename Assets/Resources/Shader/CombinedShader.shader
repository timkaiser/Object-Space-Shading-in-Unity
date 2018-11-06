Shader "Custom/CombinedShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_ID("ID", Int) = 0
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
				float2 uv : SV_Target0;
				float3 worldPos : SV_Target1;
				float3 normal : SV_Target2;
				float id : SV_Target3;
			};
			/*===============================================================================================
				SHADER
			  ===============================================================================================*/

			sampler2D _MainTex;
			float4 _MainTex_ST;
			int _ID;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));;

				return o;
			};

			fragOut frag(v2f i)
			{
				fragOut o;
				o.id = _ID/256.0f;
				o.uv = i.uv;
				o.worldPos = i.worldPos;
				o.normal = i.normal;
				return o;
			}
			ENDCG
		}
	}
}
