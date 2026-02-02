using Unity.Cinemachine;
using UnityEngine;

public class SimpleBoardCameraManager : MonoBehaviour, IHandleInput
{
    [Header("Components")]
    [SerializeField] private CinemachineCamera followCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();

    [Header("Settings")]
    [Tooltip("How far the camera shifts side to side in Follow Mode\r\n" +
       "Compare with Follow-Distance for 45 Degrees")]
    [SerializeField] private float cornerDistance = 6f;

    [Header("Runtime Variables")]
    [SerializeField] int orbitTiming;

    public enum CameraMode
    {
        Follow,
        Aim
    }

    public void HandleInput(in GamepadInput input, float deltaTime) 
    {

    }
}
