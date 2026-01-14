#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
#endif
using UnityEngine.Rendering;

#if UNITY_EDITOR
namespace FluidFrenzy
{
	class ExternalCompatibilityGenerate
	{
		[UnityEditor.Callbacks.DidReloadScripts, MenuItem("Edit/Fluid Frenzy/Generate External Shader Compatibility")]
		private static void OnScriptsReloaded()
		{
			EditorApplication.delayCall += () => { GenerateCompatibilityHeader(); UpdateDefineSymbols(); };
		}

		private static void GenerateCompatibilityHeader([CallerFilePath] string sourcePath = null)
		{
			string basePath = Path.GetDirectoryName(sourcePath);
			string compatilbityHeaderPath = sourcePath + ".hlsl";

			List<string> externals = new List<string>(Enum.GetNames(typeof(FluidFrenzy_ExternalCompatibility)));
			if(AssetsContainsCurvedWorld())
			{
				externals.Add("CURVEDWORLD");
			}

			StringBuilder builder = new StringBuilder();
			builder.AppendLine("// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Fluid Frenzy > Generate External Shader Compatibility ] instead");
			builder.AppendLine("#ifndef FLUIDEXTERNALCOMPATIBILITY_CS_HLSL");
			builder.AppendLine("#define FLUIDEXTERNALCOMPATIBILITY_CS_HLSL");
			builder.AppendLine();
			foreach (string define in externals)
			{
				builder.AppendLine($"#define FLUIDFRENZY_EXTERNALCOMPATIBILITY_{define.ToUpper()}");
			}

			builder.AppendLine();
			builder.AppendLine("#endif // FLUIDEXTERNALCOMPATIBILITY_CS_HLSL");

			File.WriteAllText(compatilbityHeaderPath, builder.ToString());

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			WriteURPThirdPartyHeader(basePath);
#endif
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			WriteHDRPThirdPartyHeader(basePath);
#endif
			WriteBRPThirdPartyHeader(basePath);

			AssetDatabase.Refresh();
		}

		private static void WriteThirdPartyHeaderStart(StringBuilder builder, string pipeline)
		{
			builder.AppendLine("// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Fluid Frenzy > Generate External Shader Compatibility ] instead");
			builder.AppendLine($"#ifndef FLUID_THIRDPARTY_HEADERS_{pipeline}_HLSL");
			builder.AppendLine($"#define FLUID_THIRDPARTY_HEADERS_{pipeline}_HLSL");
			builder.AppendLine();
			builder.AppendLine($@"#include ""Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidExternalCompatibility.cs.hlsl""");
			builder.AppendLine();
		}

		private static void WriteThirdPartyHeaderEnd(StringBuilder builder, string pipeline)
		{
			builder.AppendLine();
			builder.AppendLine($"#endif // FLUID_THIRDPARTY_HEADERS_{pipeline}_HLSL");
		}

		private static void WriteURPThirdPartyHeader(string basePath)
		{
#if UNITY_2021_1_OR_NEWER
			string thirdPartheaderPath = Path.Join(basePath, "FluidThirdPartyHeadersURP.hlsl");
#else
			string thirdPartheaderPath = basePath + @"\FluidThirdPartyHeadersURP.hlsl";
#endif
			StringBuilder builder = new StringBuilder();
			WriteThirdPartyHeaderStart(builder, "URP");
#if ENVIRO_3
			string enviro3Path = FindAssetPath("FogIncludeHLSL");
			if (String.IsNullOrEmpty(enviro3Path))
				enviro3Path = "Assets/Enviro 3 - Sky and Weather/Resources/Shader/Includes/FogIncludeHLSL.hlsl";

			WriteEnviroThirdPartyHeader(builder, enviro3Path);
#endif

			WriteCozyWeatherThirdPartyHeader(builder);
			WriteCurvedWorldThirdPartyheader(builder);
			WriteThirdPartyHeaderEnd(builder, "URP");

			File.WriteAllText(thirdPartheaderPath, builder.ToString());
		}

		private static void WriteHDRPThirdPartyHeader(string basePath)
		{
#if UNITY_2021_1_OR_NEWER
			string thirdPartheaderPath = Path.Join(basePath, "FluidThirdPartyHeadersHDRP.hlsl");
#else
			string thirdPartheaderPath = basePath + @"\FluidThirdPartyHeadersHDRP.hlsl";
#endif
			StringBuilder builder = new StringBuilder();
			WriteThirdPartyHeaderStart(builder, "HDRP");
#if ENVIRO_3
			string enviro3Path = FindAssetPath("FogIncludeHLSL");
			if (String.IsNullOrEmpty(enviro3Path))
				enviro3Path = "Assets/Enviro 3 - Sky and Weather/Resources/Shader/Includes/FogIncludeHLSL.hlsl";

			WriteEnviroThirdPartyHeader(builder, enviro3Path);
#endif

			WriteCozyWeatherThirdPartyHeader(builder);
			WriteCurvedWorldThirdPartyheader(builder);
			WriteThirdPartyHeaderEnd(builder, "HDRP");

			File.WriteAllText(thirdPartheaderPath, builder.ToString());
		}

		private static void WriteBRPThirdPartyHeader(string basePath)
		{
#if UNITY_2021_1_OR_NEWER
			string thirdPartheaderPath = Path.Join(basePath, "FluidThirdPartyHeadersBRP.hlsl");
#else
			string thirdPartheaderPath = (basePath + @"\FluidThirdPartyHeadersBRP.hlsl");
#endif
			StringBuilder builder = new StringBuilder();
			WriteThirdPartyHeaderStart(builder, "BIRP");
#if ENVIRO_3
			string enviro3Path = FindAssetPath("FogInclude");
			if (String.IsNullOrEmpty(enviro3Path))
				enviro3Path = "Assets/Enviro 3 - Sky and Weather/Resources/Shader/Includes/FogInclude.cginc";

			WriteEnviroThirdPartyHeader(builder, enviro3Path);
#endif
			WriteCozyWeatherThirdPartyHeader(builder);
			WriteCurvedWorldThirdPartyheader(builder);
			WriteThirdPartyHeaderEnd(builder, "BIRP");

			File.WriteAllText(thirdPartheaderPath, builder.ToString());
		}

		private static void WriteEnviroThirdPartyHeader(StringBuilder builder, string enviro3Path)
		{
			builder.AppendLine($"#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_ENVIRO3");
			builder.AppendLine($@"#include_with_pragmas ""{enviro3Path}""");
			builder.AppendLine("#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_ENVIRO3");
		}

		private static void WriteCozyWeatherThirdPartyHeader(StringBuilder builder)
		{
#if COZY_WEATHER
			string cozyWeatherPath = FindAssetPath("StylizedFogIncludes");
			if (String.IsNullOrEmpty(cozyWeatherPath))
				cozyWeatherPath = "Assets/Distant Lands/Cozy Weather/Contents/Materials/Shaders/Includes/StylizedFogIncludes.cginc";

			builder.AppendLine($"#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_COZYWEATHER");
			builder.AppendLine($@"#include_with_pragmas ""{cozyWeatherPath}""");
			builder.AppendLine("#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_COZYWEATHER");
#endif
		}

		private static void WriteCurvedWorldThirdPartyheader(StringBuilder builder)
		{
			if (AssetsContainsCurvedWorld())
			{
				string curvedWorldPath = "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc";
				builder.AppendLine($"#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD");
				builder.AppendLine($@"#include ""{curvedWorldPath}""");
				builder.AppendLine("#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD");
			}
		}

		private static void UpdateDefineSymbols()
		{
			SetDefineSymbol("FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD", AssetsContainsCurvedWorld());
		}

		private static bool IsObsolete(NamedBuildTarget group)
		{
			var fieldInfo = typeof(NamedBuildTarget).GetField(group.ToString());
			if (fieldInfo == null)
				return false;

			var attr = fieldInfo.GetCustomAttribute<ObsoleteAttribute>();
			return attr != null;
		}

		private static void SetDefineSymbol(string symbol, bool condition)
		{
			var targets = Enum.GetValues(typeof(BuildTargetGroup));
			foreach (BuildTargetGroup targetGroup in targets)
			{
				NamedBuildTarget buildTarget;
				try
				{
					buildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
					if (buildTarget == NamedBuildTarget.Unknown || IsObsolete(buildTarget))
					{
						continue;
					}
				}
				catch
				{
					continue;
				}

				// Get the current scripting define symbols for the target group
				string definesString = PlayerSettings.GetScriptingDefineSymbols(buildTarget);

				var defineSymbols = new HashSet<string>(definesString.Split(';'));

				if (condition)
				{
					// Add the symbol if it doesn't exist
					if (!defineSymbols.Contains(symbol))
					{
						defineSymbols.Add(symbol);
					}
				}
				else
				{
					// Remove the symbol if it exists
					if (defineSymbols.Contains(symbol))
					{
						defineSymbols.Remove(symbol);
					}
				}

				// Set the updated define symbols
				PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.Join(";", defineSymbols));
			}
		}

		private static bool AssetsContainsCurvedWorld()
		{
			return !string.IsNullOrEmpty(FindAssetPath("AmazingAssets.CurvedWorld"));
		}

		private static string FindAssetPath(string asset)
		{
			string[] guids = AssetDatabase.FindAssets(asset);

			if (guids.Length > 0)
			{
				foreach (string guid in guids)
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					return path;
				}
			}
			return string.Empty;
		}

		[GenerateHLSL]
		enum FluidFrenzy_ExternalCompatibility
		{
#if ENVIRO_3
			Enviro3 = 1,
#endif

#if COZY_WEATHER
			CozyWeather
#endif
		}
	}
}
#endif
