using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering; 

namespace FluidFrenzy
{
	public class ShaderStripper : IPreprocessShaders
	{
		const string kWaterShadername = "FluidFrenzy/Water";
		const int kWaterHDRPSubShaderIndex = 0;
		const int kWaterURPSubShaderIndex = 1;
		const int kWaterBRPSubShaderIndex = 2;

		const string kLavaShadername = "FluidFrenzy/Lava";
		const int kLavaURPSubShaderIndex = 0;
		const int kLavaBRPSubShaderIndex = 1;

		const string kTerraformTerrainSingleLayerShadername = "FluidFrenzy/TerraformTerrain(Single Layer)";
		const int kTerraformTerrainSingleLayerURPSubShaderIndex = 0;
		const int kTerraformTerrainSingleLayerBRPSubShaderIndex = 1;
		
		const string kTerraformTerrainShadername = "FluidFrenzy/TerraformTerrain";
		const int kTerraformTerrainURPSubShaderIndex = 0;
		const int kTerraformTerrainBRPSubShaderIndex = 1;

		private static readonly ShaderKeyword[] kVariantsToSkip = new ShaderKeyword[]
		{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
#if UNITY_2022_1_OR_NEWER
			new ShaderKeyword( "_CLUSTERED_RENDERING" ),
#else
			
			new ShaderKeyword( "_FORWARD_PLUS" ),
			new ShaderKeyword( "_SHADOWS_SOFT_LOW" ),
			new ShaderKeyword( "_SHADOWS_SOFT_MEDIUM" ),
			new ShaderKeyword( "_SHADOWS_SOFT_HIGH" ),
#endif
#endif
		};
		//

		private static readonly ShaderKeyword kEnviroSimpleFog = new ShaderKeyword("ENVIRO_SIMPLEFOG");
		private static readonly ShaderKeyword kFogLinear= new ShaderKeyword("FOG_LINEAR");
		private static readonly ShaderKeyword kFogExp = new ShaderKeyword("FOG_EXP");
		private static readonly ShaderKeyword kFogExp2 = new ShaderKeyword("FOG_EXP2");

		private static readonly ShaderKeyword kUnityInstancing = new ShaderKeyword("INSTANCING_ON");
		private static readonly ShaderKeyword kFluidFrenzyInstancing = new ShaderKeyword("_FLUIDFRENZY_INSTANCING");


		public int callbackOrder { get { return 0; } }

		public void OnProcessShader( Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderVariants )
		{
			string shaderName = shader.name;
			bool isWaterShader = shaderName == kWaterShadername;
			bool isLavaShader = shaderName == kLavaShadername;
			bool isTerraformTerrainSingleLayerShader = shaderName == kTerraformTerrainSingleLayerShadername;
			bool isTerraformTerrainShader = shaderName == kTerraformTerrainShadername;

			int inputShaderVariantCount = shaderVariants.Count;

			if (isWaterShader)
			{
				switch (snippet.pass.SubshaderIndex)
				{
					case kWaterBRPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							shaderVariants.Clear();
#elif FLUIDFRENZY_EDITOR_HDRP_SUPPORT
							shaderVariants.Clear();
#else
							RemoveBRPVariants(ref shaderVariants);
#endif
							break;
						}					
					case kWaterURPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							RemoveURPVariants(ref shaderVariants);
#elif FLUIDFRENZY_EDITOR_HDRP_SUPPORT
							shaderVariants.Clear();
#else
							shaderVariants.Clear();
#endif
							break;
						}
					case kWaterHDRPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							shaderVariants.Clear();
#elif FLUIDFRENZY_EDITOR_HDRP_SUPPORT
#else
							shaderVariants.Clear();
#endif
							break;
						}
				}
			}
			else if(isLavaShader)
			{
				switch (snippet.pass.SubshaderIndex)
				{
					case kLavaBRPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							shaderVariants.Clear();
#elif FLUIDFRENZY_EDITOR_HDRP_SUPPORT
#else
							RemoveBRPVariants(ref shaderVariants);
#endif
							break;
						}
					case kLavaURPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							RemoveURPVariants(ref shaderVariants);
#else
							shaderVariants.Clear();
#endif
							break;
						}
				}
			}
			else if (isTerraformTerrainSingleLayerShader)
			{
				switch (snippet.pass.SubshaderIndex)
				{
					case kTerraformTerrainSingleLayerBRPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							shaderVariants.Clear();
#else
							RemoveBRPVariants(ref shaderVariants);
#endif
							break;
						}
					case kTerraformTerrainSingleLayerURPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							RemoveURPVariants(ref shaderVariants);
#else
							shaderVariants.Clear();
#endif
							break;
						}
				}
			}			
			else if (isTerraformTerrainShader)
			{
				switch (snippet.pass.SubshaderIndex)
				{
					case kTerraformTerrainBRPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							shaderVariants.Clear();
#else
							RemoveBRPVariants(ref shaderVariants);
#endif
							break;
						}
					case kTerraformTerrainURPSubShaderIndex:
						{
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
							RemoveURPVariants(ref shaderVariants);
#else
							shaderVariants.Clear();
#endif
							break;
						}
				}
			}

			if (shaderVariants.Count != inputShaderVariantCount)
			{
				float percentage = ((float)shaderVariants.Count / (float)inputShaderVariantCount * 100f);
				int shadersRemoved = inputShaderVariantCount - shaderVariants.Count;
				Debug.Log($"[FluidFrenzyShaderStripper] Stripped {shadersRemoved}({100 - percentage}%) variants from: {shader.name}({snippet.pass.SubshaderIndex})({snippet.passName}). {shaderVariants.Count}/{inputShaderVariantCount}({percentage}%) variants remain.");
			}
		}

		private void RemoveBRPVariants(ref IList<ShaderCompilerData> data)
		{
			for (int i = 0; i < data.Count; i++)
			{
				if (ShouldRemoveVariant(data[i].shaderKeywordSet))
				{
					data.RemoveAt(i);
					--i;
				}
			}
		}
		
		private void RemoveURPVariants(ref IList<ShaderCompilerData> data)
		{
			for (int i = 0; i < data.Count; i++)
			{
				if (ShouldRemoveVariant(data[i].shaderKeywordSet))
				{
					data.RemoveAt(i);
					--i;
				}
			}
		}

		private bool ShouldRemoveVariant(ShaderKeywordSet shaderKeywords)
		{
			bool removeShader = false;
			for (int j = 0; j < kVariantsToSkip.Length; j++)
			{
				removeShader |= shaderKeywords.IsEnabled(kVariantsToSkip[j]);
			}
			removeShader |= IsEnviroFogAndUnityFog(shaderKeywords);
			removeShader |= IsFluidInstancedAndUnityInstanced(shaderKeywords);

			BuildTarget currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;

			if (currentBuildTarget == BuildTarget.WebGL)
			{
				removeShader |= IsFluidInstanced(shaderKeywords);
			}
			return removeShader;
		}

		private bool IsEnviroFogAndUnityFog(ShaderKeywordSet keywordSet)
		{
			if(keywordSet.IsEnabled(kEnviroSimpleFog) || (
				keywordSet.IsEnabled(kFogLinear) ||
				keywordSet.IsEnabled(kFogExp) ||
				keywordSet.IsEnabled(kFogExp2)))
			{
				return true;
			}

			return false;
		}

		private bool IsFluidInstancedAndUnityInstanced(ShaderKeywordSet keywordSet)
		{
			return keywordSet.IsEnabled(kUnityInstancing) && keywordSet.IsEnabled(kFluidFrenzyInstancing);
		}		
		
		private bool IsFluidInstanced(ShaderKeywordSet keywordSet)
		{
			return keywordSet.IsEnabled(kFluidFrenzyInstancing);
		}
	}
}

