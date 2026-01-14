Shader "Hidden/FluidFrenzy/InitSimulation"
{
    Properties
    {
		_HeightScale("Scale", Float) = 1.0
		_WaterHeight("Water level height", Float) = 0.0
    }
    SubShader
    {
        // No culling or depth

        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE

		#define USETEXTURE2D
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		struct v2f
		{
			float4 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		SamplerState my_linear_clamp_sampler;
		SamplerState my_point_clamp_sampler;
		float _HeightScale;
		float _WaterHeight;
		float _CaptureHeight;

		float _OffsetUVScale;

		int _SampleCountX;
		int _SampleCountY;

		float4 _TransformScale;

		Texture2D<float4> _Obstacles;


		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);;
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid);
			o.uv.zw = GetQuadTexCoord(vid) * _BlitScaleBias.xy + _BlitScaleBias.zw;;
			return o;
		}

		ENDHLSL

		Pass
		{
			Name "UpsampleHeightmap"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Utils/Shaders/FluidSamplingUtils.hlsl"

			float4 frag (v2f i) : SV_Target
			{
				float4 col = float4(0,0,0,0);
				float2 uv = i.uv.zw - _TerrainHeightField_TexelSize.xy * 0.5f * _OffsetUVScale;
				float4 obstacles = _Obstacles.Sample(my_linear_clamp_sampler, i.uv.xy);
				col = SampleCubicBSpline(_TerrainHeightField, my_point_clamp_sampler, uv, _TerrainHeightField_TexelSize.xy) * _HeightScale;
				col.r = max(obstacles.x, col.r);
				return col;
			}
			ENDHLSL
		}

        Pass
        {
			Name "CopyHeightmap"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {
				float4 col = float4(0,0,0,0);
				float2 uv = i.uv.zw - _TerrainHeightField_TexelSize.xy * 0.5f * _OffsetUVScale;
				float4 obstacles = _Obstacles.Sample(my_linear_clamp_sampler, TransformObstacleToPadded(i.uv.xy));
				col = _TerrainHeightField.Sample(my_point_clamp_sampler, uv) * _HeightScale;
				col.r = max(obstacles.x, col.r);
                return col;
            }
			ENDHLSL
        }

		Pass
		{
			Name "CopyHeightmapCombine"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			float4 frag(v2f i) : SV_Target
			{
				float4 col = float4(0,0,0,0);
				float2 uv = i.uv.zw - _TerrainHeightField_TexelSize.xy * 0.5f * _OffsetUVScale;
				float4 obstacles = _Obstacles.Sample(my_linear_clamp_sampler, TransformObstacleToPadded(i.uv.xy));
				col.r = dot(_TerrainHeightField.Sample(my_point_clamp_sampler, uv).rgba, (1.0f).xxxx) * _HeightScale;
				col.r = max(obstacles.x, col.r);
				return col;
			}
			ENDHLSL
		}

		Pass
		{
			Name "CopyFromDepth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			float UnpackDepth(float value)
			{
				const float near = 0.01f;
				const float far = _CaptureHeight;

				return _CaptureHeight - ((far-near)*(1-value)+near);
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 col = float4(0,0,0,0);
				float2 uv = i.uv.zw - _TerrainHeightField_TexelSize.xy * 0.5f * _OffsetUVScale;
				float4 obstacles = _Obstacles.Sample(my_linear_clamp_sampler, TransformObstacleToPadded(i.uv.xy));
				col.r = UnpackDepth(_TerrainHeightField.Sample(my_point_clamp_sampler, uv).r);
				col.r = max(obstacles.x, col.r);
				return col.r ;
			}
			ENDHLSL
		}

		Pass
		{
			Name "CopyUnityTerrain"
			ColorMask RG
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ _DOWN_SAMPLE_2X2 _DOWN_SAMPLE_1X2 _DOWN_SAMPLE_2X1 _DOWN_SAMPLE_NXN

			float4 frag(v2f i) : SV_Target
			{
				float4 col = float4(0,0,0,0);

				float2 heightUV = i.uv.zw * _TransformScale.xy + _TransformScale.zw;
				float2 obstacleUV = TransformObstacleToPadded(i.uv.xy);
				float h = UnpackHeightmap(_TerrainHeightField.Sample(my_point_clamp_sampler, heightUV)) * _HeightScale;
				float4 obstacles = _Obstacles.Sample(my_point_clamp_sampler, obstacleUV) / _TerrainHeightScale;


			#if _DOWN_SAMPLE_NXN 
				const int samplesX = _SampleCountX;
				const int samplesY = _SampleCountY;
			#elif _DOWN_SAMPLE_2X2
				const int samplesX = 1;
				const int samplesY = 1;
			#elif _DOWN_SAMPLE_1X2
				const int samplesX = 0;
				const int samplesY = 1;
			#elif _DOWN_SAMPLE_2X1
				const int samplesX = 1;
				const int samplesY = 0;
			#else 
				const int samplesX = 0;
				const int samplesY = 0;
			#endif

				
				if(i.uv.z > _TerrainHeightField_TexelSize.x && i.uv.z < (1-_TerrainHeightField_TexelSize.x)
					&& i.uv.w > _TerrainHeightField_TexelSize.y && i.uv.w < (1-_TerrainHeightField_TexelSize.y))
				{
					[loop]
					for(int y = -samplesY; y <= samplesY; y++)
					{
						[loop]
						for(int x = -samplesX; x <= samplesX; x++)
						{
							if(x == 0 && y == 0)continue;
							float2 uvOffset = _TerrainHeightField_TexelSize.xy * float2(x,y);
				
							heightUV = (i.uv.zw + uvOffset) * _TransformScale.xy + _TransformScale.zw;
							obstacleUV = TransformObstacleToPadded(i.uv.xy) + uvOffset;
                            h = max(h, UnpackHeightmap(_TerrainHeightField.SampleLevel(my_point_clamp_sampler, heightUV, 0)) * _HeightScale);
                            obstacles = max(obstacles, _Obstacles.SampleLevel(my_point_clamp_sampler, obstacleUV, 0) / _TerrainHeightScale);
						}
					}
				}
				
				col.r = max(obstacles.x,h);
				return PackHeightmap(col.r);
			}
			ENDHLSL
		}

		Pass
		{
			Name "FluidHeight"
			ColorMask RG
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 frag(v2f i) : SV_Target
			{
				float4 col = float4(0,0,0,0);
				float fluidHeight = tex2D(_MainTex, i.uv.xy).r * _HeightScale;
				fluidHeight = max(fluidHeight, _WaterHeight);
				col.r = dot(_TerrainHeightField.Sample(my_point_clamp_sampler, i.uv.zw).rg, (1.0f).xx)* 1;
				col.r = max(0, fluidHeight - col.r);
				col.g = 0;
				return col;
			}
			ENDHLSL
		}

		Pass
		{
			Name "FluidHeightUnityTerrain"
			ColorMask RG
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 frag(v2f i) : SV_Target
			{
				float4 col = float4(0,0,0,0);
				float4 fluidHeight = tex2D(_MainTex, i.uv.xy) * _TerrainHeightScale;
				fluidHeight.r = max(fluidHeight.r, _WaterHeight);
				col.r = UnpackHeightmap(_TerrainHeightField.Sample(my_point_clamp_sampler, i.uv.zw)) * _TerrainHeightScale;
				col.r = max(0, fluidHeight.r - col.r);
				col.g = max(0, fluidHeight.g - col.r);
				return col;
			}
			ENDHLSL
		}

		Pass
        {
			Name "SaveHeightmap"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            float4 frag (v2f i) : SV_Target
            {
				float4 col = _TerrainHeightField.Sample(my_point_clamp_sampler, i.uv.xy) * _HeightScale;
                return col;
            }
			ENDHLSL
        }
    }
}
