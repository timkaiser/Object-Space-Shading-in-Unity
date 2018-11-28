Shader "Custom/UVRenderer"
{
	Properties
	{
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
			#pragma geometry geom
			#pragma target 4.0

			#include "UnityCG.cginc"

			// Structs ================================================================
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint vertexId : SV_VertexID;
			};

			struct v2g
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				uint vertexId : TEXCOORD1;
			};


			struct g2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				uint3 vertexIds : TEXCOORD1;
				float2 baycent: TEXCOORD2;
			};

			struct fOut
			{
				float3 baycent : SV_Target0;
				uint3 vertexIds : SV_Target1;
			};

			// Shader =================================================================
			
			v2g vert (appdata v)
			{
				v2g o;
				v.vertex = float4(v.uv.xy-0.5, 0, 1.0);	
				v.vertex = mul(UNITY_MATRIX_V, v.vertex);
				v.vertex = mul(UNITY_MATRIX_P, v.vertex);
				o.vertex = float4(v.vertex.xy, 0, 510/512.0); //HOW CAN I AVOID THIS SCALE FACTOR?
				o.uv = v.uv;
				o.vertexId = v.vertexId;
				return o;
			}
			
			[maxvertexcount(3)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> tristream) {
				uint3 ids = uint3(input[0].vertexId, input[1].vertexId, input[2].vertexId);
				
				//vertex 0
				g2f o;
				o.uv = input[0].uv;
				o.vertex = input[0].vertex;
				o.vertexIds = ids;
				o.baycent = float2(0, 0);
				tristream.Append(o);

				//vertex 1
				o.uv = input[1].uv;
				o.vertex = input[1].vertex;
				o.vertexIds = ids;
				o.baycent = float2(1, 0);
				tristream.Append(o);

				//vertex 2
				o.uv = input[2].uv;
				o.vertex = input[2].vertex;
				o.vertexIds = ids;
				o.baycent = float2(0, 1);
				tristream.Append(o);

			}

			fOut frag (g2f i)
			{
				fOut o;
				o.baycent = float3(1 - i.baycent[0] - i.baycent[1], i.baycent);
				o.vertexIds = i.vertexIds;
				
				return o;
			}
			ENDCG
		}
	}
}
