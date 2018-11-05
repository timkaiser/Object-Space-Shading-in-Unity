Shader "Custom/UVShader"
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
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			//Output of fragment shader
			struct fragOut {
				//int id;
				fixed4 color : SV_Target;
				//float uv;
				//float3 worldPos;
				//float3 normal;
			};

			fragOut frag (v2f i)
			{
				fragOut o;
				//textrue: fixed4 col = tex2D(_MainTex, i.uv);
				o.color = fixed4(i.uv,0,1);
				return o;
			}
			ENDCG
		}
	}
}
