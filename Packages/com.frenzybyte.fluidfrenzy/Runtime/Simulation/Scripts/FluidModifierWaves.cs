using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// A specialized <see cref="FluidModifier"/> that generates procedural wave forces to simulate wind-driven water surfaces.
	/// </summary>
	/// <remarks>
	/// This component stacks multiple layers of waves (octaves) with varying properties to create complex, non-repetitive surface motion. 
	/// It generates forces that displace the fluid, creating the visual and physical appearance of waves.
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_modifiers/#fluid-modifier-waves")]
	public class FluidModifierWaves : FluidModifier
	{
		static int kWavesRT = Shader.PropertyToID("_WavesRT");

		/// <summary>
		/// Represents the parameters of a single wave component (octave) within the stacked wave simulation.
		/// </summary>
		[Serializable]
		public struct Wave
		{
			/// <summary> The spatial frequency of the wave (inverse of wavelength). </summary>
			public float frequency;
			/// <summary> The vertical displacement strength (height) of the wave. </summary>
			public float amplitude;
			/// <summary> The rate at which the wave phase shifts (travel speed). </summary>
			public float phase;
			/// <summary> The normalized direction vector describing the wave's propagation. </summary>
			public Vector2 direction;
		}

		private static int s_Property_Wave_frequency = Shader.PropertyToID("_Wave_frequency");
		private static int s_Property_Wave_amplitude = Shader.PropertyToID("_Wave_amplitude");
		private static int s_Property_Wave_phase = Shader.PropertyToID("_Wave_phase");
		private static int s_Property_Wave_direction = Shader.PropertyToID("_Wave_direction");

		private static int s_Property_PerlinNoiseScale = Shader.PropertyToID("_PerlinNoiseScale");
		private static int s_Property_NoiseAmplitude = Shader.PropertyToID("_NoiseAmplitude");
		private static int s_Property_Time = Shader.PropertyToID("_Time");

		/// <summary>
		/// A global multiplier applied to the total force calculated from all wave octaves.
		/// </summary>
		public float strength = 1.0f;

		/// <summary>
		/// The number of individual wave layers (octaves) to generate and stack.
		/// </summary>
		/// <remarks>
		/// Each octave is randomly generated based on the ranges defined below. 
		/// Increasing this count adds more detail and complexity to the surface but increases the computational cost.
		/// </remarks>
		public int octaveCount = 4;

		/// <summary>
		/// Defines the minimum and maximum wavelength (physical size) for the generated octaves.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>X (Min)</term>
		/// 		<description>The smallest allowed wavelength (tight ripples).</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Y (Max)</term>
		/// 		<description>The largest allowed wavelength (broad swells).</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public Vector2 waveLengthRange = new Vector2(3, 4);

		/// <summary>
		/// Defines the angular range (in degrees) for the propagation direction of the waves.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>X (Min)</term>
		/// 		<description>The minimum angle in degrees.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Y (Max)</term>
		/// 		<description>The maximum angle in degrees.</description>
		/// 	</item>
		/// </list>
		/// Use this to restrict waves to a specific wind direction or allow them to move chaotically in all directions.
		/// </remarks>
		public Vector2 directionRange = new Vector2(0, 0);

		/// <summary>
		/// Defines the minimum and maximum height intensity for the generated octaves.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>X (Min)</term>
		/// 		<description>The lowest possible amplitude for an octave.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Y (Max)</term>
		/// 		<description>The highest possible amplitude for an octave.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public Vector2 amplitudeRange = new Vector2(0.3f, 0.7f);

		/// <summary>
		/// Defines the minimum and maximum phase speed (travel speed) for the generated octaves.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>X (Min)</term>
		/// 		<description>The slowest speed a wave can travel.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Y (Max)</term>
		/// 		<description>The fastest speed a wave can travel.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public Vector2 speedRange = new Vector2(0.07f, 0.12f);

		/// <summary>
		/// Controls the intensity of the secondary Perlin noise layer.
		/// </summary>
		/// <remarks>
		/// A noise layer is applied on top of the wave octaves to break up mathematical patterns and add organic irregularity to the surface.
		/// Higher values result in a more chaotic surface.
		/// </remarks>
		public float noiseAmplitude = 0.61f;

		/// <summary>
		/// Controls the spatial frequency (tiling) of the secondary Perlin noise layer.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>High Values</term>
		/// 		<description>Creates high-frequency noise, resulting in small, detailed surface disturbances.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Low Values</term>
		/// 		<description>Creates low-frequency noise, resulting in large, broad variations.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public float perlinNoiseScale = 25.0f;

		Material m_perlinNoiseMaterial;
		Material m_externalWavesMaterial;
		MaterialPropertyBlock m_externalWavesProperties;

		RenderTexture m_perlinNoise = null;

		float m_time = 0;

		Wave CreateWave(float wavelength, float amplitude, float speed, float direction)
		{
			Wave w = new Wave();
			w.frequency = 2.0f / wavelength;
			w.amplitude = amplitude;
			w.phase = speed * Mathf.Sqrt(9.8f * 2.0f * Mathf.PI / wavelength);
			w.direction = new Vector2(Mathf.Cos(Mathf.Deg2Rad * direction), Mathf.Sin(Mathf.Deg2Rad * direction)).normalized;
			return w;
		}

		void Start()
		{
			m_perlinNoiseMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/PerlinNoise"));
			m_externalWavesMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/Waves"));

#if UNITY_2023_2_OR_NEWER
			GraphicsFormat perlinFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SNorm, GraphicsFormatUsage.Render) ? GraphicsFormat.R8_SNorm : GraphicsFormat.R16_SFloat;
#else
			GraphicsFormat perlinFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SNorm, FormatUsage.Render) ? GraphicsFormat.R8_SNorm : GraphicsFormat.R16_SFloat;
#endif
			m_perlinNoise = new RenderTexture(256, 256, 0, perlinFormat)
			{
				wrapMode = TextureWrapMode.Repeat
			};

			m_perlinNoiseMaterial.SetFloat(s_Property_PerlinNoiseScale, perlinNoiseScale);
			Graphics.Blit(null, m_perlinNoise, m_perlinNoiseMaterial);

			SetWaveParameters();
		}

		protected override void OnDestroy()
		{
			GraphicsHelpers.ReleaseSimulationRT(m_perlinNoise);
			Destroy(m_perlinNoiseMaterial);
			Destroy(m_externalWavesMaterial);
			base.OnDestroy();
		}

		private void OnValidate()
		{
			if (!Application.isPlaying) return;
			SetWaveParameters();
		}

		public void SetWaveParameters()
		{
			m_externalWavesProperties = new MaterialPropertyBlock();
			float[] frequencies = new float[4];
			float[] amplitudes = new float[4];
			float[] phases = new float[4];
			Vector4[] directions = new Vector4[4];

			for (int i = 0; i < octaveCount; ++i)
			{
				float l = (float)i / octaveCount;
				float wavelength = Mathf.Lerp(waveLengthRange.x, waveLengthRange.y, l) * 0.01f;
				float amplitude = Mathf.Lerp(amplitudeRange.x, amplitudeRange.y, l);
				float ampOverLen = amplitude / wavelength;
				float direction = Mathf.Lerp(directionRange.x, directionRange.y, l);
				float speed = Mathf.Lerp(speedRange.x, speedRange.y, l);

				Wave wave = CreateWave(wavelength, wavelength * ampOverLen, speed, direction);

				frequencies[i] = wave.frequency;
				amplitudes[i] = wave.amplitude;
				phases[i] = wave.phase;
				directions[i] = wave.direction;
			}

			m_externalWavesProperties.SetFloatArray(s_Property_Wave_frequency, frequencies);
			m_externalWavesProperties.SetFloatArray(s_Property_Wave_amplitude, amplitudes);
			m_externalWavesProperties.SetFloatArray(s_Property_Wave_phase, phases);
			m_externalWavesProperties.SetVectorArray(s_Property_Wave_direction, directions);
			m_externalWavesProperties.SetFloat(s_Property_NoiseAmplitude, noiseAmplitude);
		}

		public override void Process(FluidSimulation fluidSim, float dt)
		{

			GraphicsFormat wavesFormat = fluidSim.simulationType == FluidSimulation.FluidSimulationType.Flux ? GraphicsFormat.R16_SFloat : GraphicsFormat.R16G16_SFloat;
			int pass = fluidSim.simulationType == FluidSimulation.FluidSimulationType.Flux ? 0 : 1;
			fluidSim.GetTemporaryRT(kWavesRT, 256, 256, wavesFormat);
			m_externalWavesProperties.SetVector(s_Property_Time, new Vector4(m_time / 20, m_time, m_time * 2, m_time * 3));
			fluidSim.BlitQuadExternal(m_perlinNoise, kWavesRT, m_externalWavesMaterial, m_externalWavesProperties, pass);
			fluidSim.ApplyForce(kWavesRT, strength, dt);

			fluidSim.ReleaseTemporaryRT(kWavesRT);
			m_time += dt;
		}
	}
}