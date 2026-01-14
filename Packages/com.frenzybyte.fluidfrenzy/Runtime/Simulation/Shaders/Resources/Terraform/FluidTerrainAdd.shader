Shader "Hidden/FluidFrenzy/AddTerrain"
{
    Properties
    {
		_MainTex("Texture", 2D) = "black" {}
		_FluidHeightField("Texture", 2D) = "white" {}
    }
    SubShader
    {
		HLSLINCLUDE

		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidInputCommon.hlsl"


		float4 fragCircleSetHeight(v2f i, float4 texelSize) 
		{
			float4 screenPos = i.vertex;
			DiscardBoundary(screenPos.xy, texelSize);

			float dist = DistanceCircle(i.uv.xy);
			clip(1 - dist);

			float4 terrainHeight = SampleBase(screenPos.xy * _TerrainHeightField_TexelSize.xy);
			float4 base = 0;
			float4 result = CalculateStrength(dist, screenPos.xy, texelSize) - dot(terrainHeight, _BottomLayersMask);
			#if _BLEND_NOT_SUPPORTED
				base = terrainHeight;
				if(_FallbackBlendOp == MIX_MODE_MIN)
				{
					result = min(result, base);
				}
				else if(_FallbackBlendOp == MIX_MODE_MAX)
				{
					result = max(result, base);
				}
			#endif

			return max(0,result);
		}

		float4 fragSquareSetHeight(v2f i, float4 texelSize) 
		{
			float4 screenPos = i.vertex;
			DiscardBoundary(screenPos.xy, texelSize);

			float dist = DistanceBox(i.uv.xy);
			clip(1 - dist);

			float4 terrainHeight = SampleBase(screenPos.xy * _TerrainHeightField_TexelSize.xy);
			float4 base = 0;

			float4 result = CalculateStrength(dist, screenPos.xy, texelSize) - dot(terrainHeight, _BottomLayersMask);
			#if _BLEND_NOT_SUPPORTED
				base = terrainHeight;
				if(_FallbackBlendOp == MIX_MODE_MIN)
				{
					result = min(result, base);
				}
				else if(_FallbackBlendOp == MIX_MODE_MAX)
				{
					result = max(result, base);
				}
			#endif

			return max(0,result);
		}
		
		float4 fragTexSetHeight(float2 uv, float2 screenPos, float4 texelSize)
		{
			DiscardBoundary(screenPos, texelSize);

			float4 terrainHeight = SampleBase(screenPos.xy * _TerrainHeightField_TexelSize.xy);
			float4 base = 0;

			float4 result = (RemapNormalizedToRange(tex2D(_MainTex, uv).r, _RemapRange).xxxx * _IncreaseStrength * _LayerMask) - dot(terrainHeight, _BottomLayersMask);
			#if _BLEND_NOT_SUPPORTED
				base = terrainHeight;
				if(_FallbackBlendOp == MIX_MODE_MIN)
				{
					result = min(result, base);
				}
				else if(_FallbackBlendOp == MIX_MODE_MAX)
				{
					result = max(result, base);
				}
			#endif

			return max(0,result);
		}

		ENDHLSL
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		

		// 0
        Pass
        {	
			Name "AddTerrainCircle"
			Blend One One, One One
			ColorMask RGBA
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				return fragCircleAdd(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// 1
        Pass
        {	
			Name "AddTerrainSquare"
			Blend One One
			ColorMask RGBA
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				return fragSquareAdd(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// 2
		Pass
		{
			Name "AddTerrainTexture"
			Blend One One
			ColorMask RG
			HLSLPROGRAM
			#pragma vertex vertTex
			#pragma fragment frag

			float4 frag(v2f i) : SV_Target
			{
				return fragTexAdd(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
		}


		// max height
		// 3
		Pass
        {	
			Name "MixTerrainHeightCircle"
			BlendOp [_BlendOpTerrain]
			ColorMask [_ColorMaskTerrain]
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _BLEND_NOT_SUPPORTED

			float4 frag(v2f i) : SV_Target
			{
				return fragCircleSetHeight(i, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// 4
		Pass
        {	
			Name "MixTerrainHeightSquare"
			BlendOp [_BlendOpTerrain]
			ColorMask [_ColorMaskTerrain]
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _BLEND_NOT_SUPPORTED

			float4 frag(v2f i) : SV_Target
			{
				return fragSquareSetHeight(i, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// 5
		Pass
        {	
			Name "MixTerrainHeightTexture"
			BlendOp [_BlendOpTerrain]
			ColorMask [_ColorMaskTerrain]

			HLSLPROGRAM
            #pragma vertex vertTex
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _BLEND_NOT_SUPPORTED

			float4 frag(v2f i) : SV_Target
			{
				return fragTexSetHeight(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		//max depth
		// 6
		Pass
        {	
			Name "MixTerrainDepthCircle"
			BlendOp [_BlendOpTerrain]
			ColorMask [_ColorMaskTerrain]
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _BLEND_NOT_SUPPORTED

			float4 frag(v2f i) : SV_Target
			{
				return fragCircleSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }


		// 7
		Pass
        {	
			Name "MixTerrainDepthSquare"
			BlendOp [_BlendOpTerrain]
			ColorMask [_ColorMaskTerrain]
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _BLEND_NOT_SUPPORTED

			float4 frag(v2f i) : SV_Target
			{
				return fragSquareSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// 8
		Pass
        {	
			Name "MixTerrainDepthTexture"
			BlendOp [_BlendOpTerrain]
			ColorMask [_ColorMaskTerrain]
			HLSLPROGRAM
            #pragma vertex vertTex
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _BLEND_NOT_SUPPORTED

			float4 frag(v2f i) : SV_Target
			{
				return fragTexSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		/// Splatmap
		// 9
		Pass
		{
			Name "AddSplatmapCircle"
			Blend One One
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER


			float4 frag(v2f i) : SV_Target
			{
				return fragCircleAdd(i.uv, i.vertex.xy, _Simulation_TexelSize) * _SplatmapMask;
			}


			ENDHLSL
		}

		// 10
		Pass
		{
			Name "SubSplatmapCircle"
			Blend One One
			BlendOp RevSub
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				return fragCircleAdd(i.uv, i.vertex.xy, _Simulation_TexelSize) * (1 - _SplatmapMask);
			}

			ENDHLSL
		}	
		
		// 11
		Pass
		{
			Name "AddSplatmapSquare"
			Blend One One
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER


			float4 frag(v2f i) : SV_Target
			{
				return fragSquareAdd(i.uv, i.vertex.xy, _Simulation_TexelSize) * _SplatmapMask;
			}


			ENDHLSL
		}

		// 12
		Pass
		{
			Name "SubSplatmapSquare"
			Blend One One
			BlendOp RevSub
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				return fragSquareAdd(i.uv, i.vertex.xy, _Simulation_TexelSize) * (1 - _SplatmapMask);
			}

			ENDHLSL
		}	

		// 13
		Pass
		{
			Name "AddSplatmapTexture"
			Blend One One
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vertTex
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				return fragTexAdd(i.uv, i.vertex.xy, _Simulation_TexelSize) * _SplatmapMask;
			}


			ENDHLSL
		}

		// 14
		Pass
		{
			Name "SubSplatmapTexture"
			Blend One One
			BlendOp RevSub
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vertTex
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				return fragTexAdd(i.uv, i.vertex.xy, _Simulation_TexelSize) * (1 - _SplatmapMask);
			}

			ENDHLSL
		}

		// 15
		Pass
		{
			Name "SetSplatmapCircle"
			Blend One Zero
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER


			float4 frag(v2f i) : SV_Target
			{
				return fragCircleSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize) * _SplatmapMask;
			}


			ENDHLSL
		}
		
		// 16
		Pass
		{
			Name "SetSplatmapSquare"
			Blend One Zero
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER


			float4 frag(v2f i) : SV_Target
			{
				return fragSquareSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize) * _SplatmapMask;
			}


			ENDHLSL
		}

		// 17
		Pass
		{
			Name "SetSplatmapTexture"
			Blend One One
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vertTex
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				float4 value = fragTexSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize) * _SplatmapMask;
				return value;
			}


			ENDHLSL
		}

		// 18
		Pass
		{
			Name "SubSplatmapTexture"
			Blend One One
			BlendOp RevSub
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vertTex
			#pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				float4 value = fragTexSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize) * (1 - _SplatmapMask);;
				return value;
			}


			ENDHLSL
		}


		// 19
		Pass
		{
			Name "AddTerrainDynamic"
			ColorMask RGBA
			HLSLPROGRAM
			#pragma vertex vertTexDynamic
			#pragma fragment fragAddDynamic

			v2f vertTexDynamic(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * _BlitScaleBiasRt.xy + _BlitScaleBiasRt.zw;
			#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv.xy = GetQuadTexCoord(vid);
			#else
				o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
				o.uv.xy = GetQuadTexCoord(vid);
				o.uv.y = 1-o.uv.y;
			#endif
				return o;
			}

			float4 fragAddDynamic(v2f i) : SV_Target
			{
				return tex2D(_TerrainHeightField, i.uv.xy).rgba + tex2D(_MainTex, i.uv.xy).rgba;
			}

			ENDHLSL
		}
    }
}
