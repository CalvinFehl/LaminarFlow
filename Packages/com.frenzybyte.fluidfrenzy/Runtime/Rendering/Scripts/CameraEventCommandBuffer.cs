using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	internal class CameraEventCommandBuffer
	{
		public Camera camera;
        public CommandBuffer commandBuffer;
        public bool attached;

		static Dictionary<(Camera, CameraEvent), CameraEventCommandBuffer> s_AllBuffers = new Dictionary<(Camera, CameraEvent), CameraEventCommandBuffer>();
		static List<(Camera, CameraEvent)> s_Cleanup = new List<(Camera, CameraEvent)>();

        internal CameraEventCommandBuffer(Camera cam, string name)
        {
            camera = cam;
            commandBuffer = new CommandBuffer();
			commandBuffer.name = name;
			attached = false;
        }

        public static implicit operator CommandBuffer(CameraEventCommandBuffer handle)
        {
            return handle.commandBuffer;
        }

        /// <summary>
        /// Gets an existing or Creates a new commandbuffer for the specified camera and camera event.
        /// </summary>
        /// <param name="camera">Camera for which the CameraEventCommandBuffer is needed.</param>
        /// <param name="CameraEvent">Camera event.</param>
        /// <returns>The CameraEventCommandBuffer for the camera and the specified event containing the commandBuffer and camera.</returns>
        internal static CameraEventCommandBuffer GetOrCreate(Camera camera, CameraEvent cameraEvent, string name = "")
        {
            if (!s_AllBuffers.TryGetValue((camera, cameraEvent), out CameraEventCommandBuffer cmd))
            {
                cmd = new CameraEventCommandBuffer(camera, name);
                s_AllBuffers.Add((camera, cameraEvent), cmd);
            }

            return cmd;
        }

        /// <summary>
        /// Gets an existing or Creates a new commandbuffer for the specified camera and camera event and attached it to the camera if it is not yet attached.
        /// </summary>
        /// <param name="camera">Camera for which the CameraEventCommandBuffer is needed.</param>
        /// <param name="CameraEvent">Camera event.</param>
        /// <returns>The CameraEventCommandBuffer for the camera and the specified event containing the commandBuffer and camera.</returns>
        internal static CameraEventCommandBuffer GetOrCreateAndAttach(Camera camera, CameraEvent cameraEvent, string name = "")
        {
            if (!s_AllBuffers.TryGetValue((camera, cameraEvent), out CameraEventCommandBuffer cmd))
            {
                cmd = new CameraEventCommandBuffer(camera, name);
                s_AllBuffers.Add((camera, cameraEvent), cmd);

            }
            if (!cmd.attached)
                camera.AddCommandBuffer(cameraEvent, cmd.commandBuffer);
            cmd.attached = true;
            return cmd;
        }

        /// <summary>
        /// Detaches the commandbuffer from the Camera.
        /// </summary>
        /// <param name="camera">Camera for which the CameraEventCommandBuffer is needed.</param>
        /// <param name="CameraEvent">Camera event.</param>
        internal static void Detach(Camera camera, CameraEvent cameraEvent)
        {
            if (!s_AllBuffers.TryGetValue((camera, cameraEvent), out CameraEventCommandBuffer cmd))
            {
                return;
            }
            if (cmd.attached)
                camera.RemoveCommandBuffer(cameraEvent, cmd.commandBuffer);

            cmd.commandBuffer.Clear();
            cmd.attached = false;
            return;
        }

        /// <summary>
        /// Removes any unused CameraEventCommandBuffers if they are no longer active or their camera has been destroyed.
        /// </summary>
        public static void CleanupUnused()
        {
            foreach (var key in s_AllBuffers.Keys)
            {
                var camera = s_AllBuffers[key];

                // Unfortunately, the scene view camera is always isActiveAndEnabled==false so we can't rely on this. For this reason we never release it (which should be fine in the editor)
                if (camera.camera != null && camera.camera.cameraType == CameraType.SceneView)
                    continue;

                if (camera.camera == null || (!camera.camera.isActiveAndEnabled && camera.camera.cameraType != CameraType.Preview && camera.camera.cameraType != CameraType.Game))
                    s_Cleanup.Add(key);
            }

            foreach (var cam in s_Cleanup)
            {
                //s_AllBuffers[cam].Dispose();
                s_AllBuffers.Remove(cam);
            }

            s_Cleanup.Clear();
        }
	}		
}