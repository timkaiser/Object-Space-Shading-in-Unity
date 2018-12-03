Shader "Custom/UVRenderer"
{	
	//This shader outputs the baycentric coordinates and the vertex ids of the mesh of a single object 
	Properties{}
	SubShader
	{
		Pass
		{
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			#pragma target 4.0

			#include "UnityCG.cginc"

			// Structs for in-/output ================================================================
			struct appdata{  //vert in
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint vertexId : SV_VertexID;
			};

			struct v2g{  //vert to geom
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				uint vertexId : TEXCOORD1;
			};


			struct g2f{  //geom to frag
				float4 vertex : SV_POSITION;
				uint3 vertexIds : TEXCOORD1;
				float2 baycent: TEXCOORD2;
			};

			struct fOut{  //frag out
				float3 baycent : SV_Target0;
				uint3 vertexIds : SV_Target1;
			};

			// Shader =================================================================
			
			//vertex shader
			v2g vert (appdata v)
			{
				//placing the vertices according to their uv coordinate
				v2g o;
				o.vertex = float4(v.uv.xy * 2.0 - 1.0, 0.5, 1.0);
				o.uv = v.uv;
				o.vertexId = v.vertexId;
				return o;
			}
			
			//geometry shader
			[maxvertexcount(3)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> tristream) {
				uint3 ids = uint3(input[0].vertexId, input[1].vertexId, input[2].vertexId);
				
				//vertex 0
				g2f o;
				o.vertex = input[0].vertex;
				o.vertexIds = ids;
				o.baycent = float2(0, 0);
				tristream.Append(o);

				//vertex 1
				o.vertex = input[1].vertex;
				o.vertexIds = ids;
				o.baycent = float2(1, 0);
				tristream.Append(o);

				//vertex 2
				o.vertex = input[2].vertex;
				o.vertexIds = ids;
				o.baycent = float2(0, 1);
				tristream.Append(o);

			}

			//fragment shader
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
