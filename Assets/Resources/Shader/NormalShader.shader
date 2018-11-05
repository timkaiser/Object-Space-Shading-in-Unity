Shader "Custom/NormalShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 normalView : NORMAL;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			//parts taken from: https://forum.unity.com/threads/shader-convert-normals-to-view-space-normals.285262/
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normalView = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));;
				return o;
			};
			
			fixed4 frag (v2f i) : SV_Target
			{
				
				//textrue: fixed4 col = tex2D(_MainTex, i.uv);
				//uv: fixed4 col = fixed4(i.uv,0,1);
				//world position:
				fixed4 col = fixed4(i.normalView,1);
				return col;
			}
			ENDCG
		}
	}
}
