using UnityEngine;
using UnityEngine.Rendering;
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif

namespace FluidFrenzy
{
	/// <summary>
	/// Abstract base class for all effects and surfaces.
	/// </summary>
	public abstract class RenderModule
    {
		public abstract void OnEnable();

		public abstract void OnDisable();

		public abstract void AddCommandBuffers(Camera camera);

		public abstract void RemoveCommandBuffers(Camera camera);

		public abstract void RenderSRP(ScriptableRenderContext context, Camera camera);
    }
}