Shader "Hidden/FluidFrenzy/FluidSimulationSceneView"
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
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            // make fog work
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN

            #include "UnityCG.cginc"

            float3 _TerrainPosition;
            float3 _Position;
            float2 _GridDim;
            float3 _TerrainScale;

			float _FluidHeight;
			float4 _TransformScale;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD1;

            };

            sampler2D _Heightmap;
			float4 _Heightmap_TexelSize;
			float4x4 _WorldMatrix;

		    v2f vert(uint vid : SV_VertexID)
		    {
			    v2f o;

                int vertexID = vid % 4;
                uint topBit = vertexID >> 1;
                uint botBit = (vertexID & 1);
                float x = topBit;
                float y = 1 - (topBit + botBit) & 1; // produces 1 for indices 0,3 and 0 for 1,2

				float4 vertex = float4(x, 0, y, 1);

                o.uv = float2(x,y) * _TransformScale.xy + _TransformScale.zw;
                vertex.xz = (vertex.xz * 2 - 1);

				vertex.xyz = mul(_WorldMatrix, float4(vertex.xyz,1));
				vertex.y += _FluidHeight;



                o.vertex = UnityWorldToClipPos(vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

			     #if _FLUID_UNITY_TERRAIN
                    float height = UnpackHeightmap(tex2Dlod(_Heightmap, float4(i.uv.xy - _Heightmap_TexelSize.xy * 0.5f,0,0))) * _TerrainScale.y;
                #else
                    float height = dot(tex2Dlod(_Heightmap, float4(i.uv.xy- _Heightmap_TexelSize.xy * 0.5f,0,0)), float4(1,1,0,0)) * 75;
                #endif

				clip(_FluidHeight - height);	

				float alpha = _FluidHeight - height;
				//alpha = 1-smoothstep(0, _FluidHeight, height);
				alpha = log(saturate(_FluidHeight - height) + 1);
                return float4(0,0.25,0.3,alpha);
            }
            ENDHLSL
        }
    }
}
