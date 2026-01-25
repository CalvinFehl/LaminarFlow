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

		private UnderwaterEffect m_underWaterEffect = null;
		private bool m_isUnderwaterEffectActive = false;

		protected override void Start()
		{
			base.Start();

			// Prevent initialization in Edit Mode
			if (!Application.isPlaying) return;

			if (underWaterEnabled)
			{
				InitializeEffect();
				if (m_underWaterEffect != null)
				{
					m_underWaterEffect.OnEnable();
					m_isUnderwaterEffectActive = true;
				}
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			// Clean up effect if it exists (works in both play and edit mode for safety)
			if (m_underWaterEffect != null)
			{
				m_underWaterEffect.OnDisable();
				m_isUnderwaterEffectActive = false;
			}
		}

		private void InitializeEffect()
		{
			// Only initialize during Play Mode
			if (!Application.isPlaying) return;
			if (m_underWaterEffect != null) return;
			if (simulation == null) return;

			UnderwaterEffect.Desc desc = new UnderwaterEffect.Desc()
			{
				settings = underWaterSettings,
				surface = this
			};

			m_underWaterEffect = new UnderwaterEffect(desc);
		}

		public void SetUnderwaterActive(bool active)
		{
			underWaterEnabled = active;

			// Don't run logic in Edit Mode
			if (!Application.isPlaying) return;

			if (active)
			{
				InitializeEffect();
				if (m_underWaterEffect != null && !m_isUnderwaterEffectActive)
				{
					m_underWaterEffect.OnEnable();
					m_isUnderwaterEffectActive = true;
				}
			}
			else
			{
				if (m_isUnderwaterEffectActive)
				{
					m_underWaterEffect?.OnDisable();
					m_isUnderwaterEffectActive = false;
				}
			}
		}

		public void OnUnderwaterChanged()
		{
			// Update logic, but only trigger effect lifecycle if playing
			if (Application.isPlaying)
			{
				if (underWaterEnabled) InitializeEffect();
				SetUnderwaterActive(underWaterEnabled);
			}
		}

		protected override void PreCull(Camera camera)
		{
			base.PreCull(camera);
			if (!Application.isPlaying || camera.cameraType == CameraType.Preview) return;

			if (underWaterEnabled)
			{
				m_underWaterEffect?.AddCommandBuffers(camera);
			}
		}

		protected override void PostRender(Camera camera)
		{
			base.PostRender(camera);
			if (!Application.isPlaying || camera.cameraType == CameraType.Preview) return;

			m_underWaterEffect?.RemoveCommandBuffers(camera);
		}

		protected override void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			base.OnBeginCameraRendering(context, camera);
			if (!Application.isPlaying || camera.cameraType == CameraType.Preview) return;

			m_underWaterEffect?.RenderSRP(context, camera);
		}

		protected override void Update()
		{
			base.Update();
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
		}
	}
}