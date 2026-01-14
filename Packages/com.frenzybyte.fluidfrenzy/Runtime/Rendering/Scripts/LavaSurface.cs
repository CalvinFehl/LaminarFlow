using UnityEngine;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="LavaSurface"/> is an extension of the <see cref="FluidRenderer"/> component that specifically deals with rendering lava-related elements of the fluid simulation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This component adds specific lava rendering features, such as heat and emissive color gradients, by generating and applying a custom **Heat Look-Up Texture (LUT)**.
	/// </para>
	/// <para>
	/// The LUT is procedurally generated from the <see cref="heat"/> gradient field and is assigned to the <see cref="FluidRenderer.fluidMaterial">Lava material</see>. This allows the lava's emissive color and heat visual effect to be determined dynamically by factors like the lava's velocity or age.
	/// </para>
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_rendering_components/#lava-surface")]
	public class LavaSurface : FluidRenderer
	{
		/// <summary> 
		/// If enabled, the <see cref="heat"/> gradient will be used to procedurally generate a **Heat LUT** that overrides the existing LUT on the <see cref="FluidRenderer.fluidMaterial">Fluid Material</see>.
		/// </summary>
		public bool generateHeatLut = false;

		/// <summary> 
		/// The <see cref="Gradient"/> used to define the heat/color transition for the lava. The color samples are mapped from Cold Lava (Left side of the gradient) to Hot Lava (Right side of the gradient).
		/// </summary>
		public Gradient heat;
		private Texture2D m_HeatLUT = null;

		public override void CopyFrom(FluidRenderer source)
		{
			base.CopyFrom(source);

			LavaSurface lavaSource = source as LavaSurface;
			heat = lavaSource.heat;
		}

		// Start is called before the first frame update
		protected override void Start()
		{
			base.Start();
			GenerateColorRamp();
		}

		protected override void Update()
		{
			base.Update();

			if(generateHeatLut)
				m_renderMaterial.SetTexture(FluidShaderProperties._HeatLUT, m_HeatLUT);
		}

		public void GenerateColorRamp()
		{
			if (!generateHeatLut)
				return;

			if (m_HeatLUT == null)
				m_HeatLUT = new Texture2D(128, 1, TextureFormat.ARGB32, false, false);
			m_HeatLUT.wrapMode = TextureWrapMode.Clamp;


			Color[] cols = new Color[128];
			for (int i = 0; i < 128; i++)
			{
				cols[i] = heat.Evaluate((float)i / 128f);
			}

			m_HeatLUT.SetPixels(cols);
			m_HeatLUT.Apply();
			m_renderMaterial.SetTexture(FluidShaderProperties._HeatLUT, m_HeatLUT);
		}


		public static Gradient DefaultHeatLUT()
        {
			return new Gradient()
			{
				colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(new Color(0, 0, 0, 1), 0.0f),
					new GradientColorKey(new Color(0.0754717f, 0.0754717f, 0.0754717f, 1), 0.05f),
					new GradientColorKey(new Color(1, 0, 0, 1), 0.578f),
					new GradientColorKey(new Color(1, 0.69943273f, 0, 1), 0.751f),
					new GradientColorKey(new Color(1, 0.96128434f, 0.5518868f, 1), 0.836f),
					new GradientColorKey(new Color(1, 1, 1, 1), 0.888f),
				}
			};
		}
	}
}