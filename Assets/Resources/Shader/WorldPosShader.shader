Shader "Custom/WorldPosShader"
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

			//parts taken from: https://answers.unity.com/questions/1272511/getting-the-world-position-of-the-pixel-in-the-fra.html
			struct v2f
			{
				//float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : POSITON;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				
				//textrue: fixed4 col = tex2D(_MainTex, i.uv);
				//uv: fixed4 col = fixed4(i.uv,0,1);
				//world position:
				fixed4 col = fixed4(i.worldPos,1);
				return col;
			}
			ENDCG
		}
	}
}
