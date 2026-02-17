using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// WaterSurface is an extension of the <see cref="FluidRenderer"/> component that renders all things water like <see cref="FoamLayer">foam</see>, <see cref="UnderwaterEffect">underwater</see> visuals, absorption, and scattering.
	/// It does this by assigning the active rendering layers to its surface material and using the underwater settings.
	/// </summary>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_rendering_components/#water-surface")]
	public class WaterSurface : FluidRenderer
	{
		/// <summary>
		/// A FoamLayer component that provides the dynamically generated foam mask texture for water rendering effects.
		/// </summary>
		/// <remarks>
		/// The component's primary role is to update and supply the dynamic foam mask texture, ensuring foam is applied
		/// accurately to the water material. It also handles necessary adjustments to the mask's texture coordinates (UVs)
		/// to maintain alignment across different rendering setups.
		/// </remarks>
		public FoamLayer foamLayer;

		#region Underwater Effects
		/// <summary>
		/// Controls whether the <see cref="UnderwaterEffect">underwater visual effect</see> is currently enabled.
		/// </summary>
		[SerializeField]
		private bool underWaterEnabled = false;
		public bool IsUnderwaterEnabled => underWaterEnabled;

		/// <summary>
		/// Settings  for all configurable visual parameters of the <see cref="UnderwaterEffect"/>.
		/// This class defines how light interacts with the water volume, including absorption rates, scattering colors, and the appearance of the surface meniscus.
		/// </summary>
		public UnderwaterEffect.UnderwaterSettings underWaterSettings = new UnderwaterEffect.UnderwaterSettings();
		#endregion

		#region Caustics
		/// <summary>
		/// Controls whether the <see cref="CausticsEffect"/> is currently enabled.
		/// </summary>
		[SerializeField]
		private bool causticsEnabled = false;
		public bool IsCausticsEnabled => causticsEnabled;

		/// <summary>
		/// Settings for the <see cref="CausticsEffect"/>, which renders animated light patterns projected onto the scene geometry underwater.
		/// </summary>
		public CausticsEffect.CausticsSettings causticsSettings = new CausticsEffect.CausticsSettings();
		#endregion

		#region Reflections
		/// <summary>
		/// Controls whether real-time planar reflections are generated for this water surface.
		/// </summary>
		[SerializeField]
		private bool reflectionsEnabled = true;
		public bool IsReflectionsEnabled => reflectionsEnabled;

		/// <summary>
		/// Settings for the <see cref="FluidFrenzy.SurfaceReflections"/> module (Planar Reflections).
		/// </summary>
		public SurfaceReflections.Settings reflectionSettings = new SurfaceReflections.Settings();
		public SurfaceReflections SurfaceReflections { get; private set; }
		#endregion

		protected override void Start()
		{
			base.Start();
			if (!Application.isPlaying) return;

			// Initialize submodule logic
			SurfaceReflections = new SurfaceReflections(this, reflectionSettings);
			if (reflectionsEnabled) SurfaceReflections.Enable();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (Application.isPlaying)
			{
				FluidRenderPipeline.Register(this);
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			if (Application.isPlaying)
			{
				FluidRenderPipeline.Deregister(this);
				SurfaceReflections?.Disable();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			SurfaceReflections?.Cleanup();
		}

		protected override void Update()
		{
			base.Update();

			// Standard Surface logic (Foam params)
			if (foamLayer)
			{
				m_renderMaterial.EnableKeyword("_FOAMMASK_ON");
				m_renderMaterial.SetTexture(FluidShaderProperties._FluidFoamField, foamLayer.activeLayer);
				m_renderMaterial.SetVector(FluidShaderProperties._FluidFoamField_ST, foamLayer.textureST);
			}
			else
			{
				m_renderMaterial.DisableKeyword("_FOAMMASK_ON");
			}

			// Sync submodule settings
			if (SurfaceReflections != null && reflectionsEnabled)
			{
				// Only update if dirty ideally, but for now:
				SurfaceReflections.UpdateSettings(reflectionSettings);
			}
		}

		public new ISurfaceRenderer surfaceRenderer => base.surfaceRenderer;
	}
}