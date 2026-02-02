using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines.Interpolators;

public class SimpleBoardCameraManager : MonoBehaviour, IHandleInput, IHandleRigidbodyData
{
    [Header("Components")]
    [SerializeField] private CinemachineCamera followCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();

    [Header("Settings")]
    [Tooltip("1 = Left Stick X, 2 = Left Stick Y, 3 = Right Stick X, 4 = Right Stick Y")]
    [SerializeField] int inputAxis = 3;
    [Tooltip("How far the Camera shifts side to side in Follow Mode\r\n" +
       "Compare with Follow-Distance for 45 Degrees")]
    [SerializeField] private float orbitRadius = 6f;
    [SerializeField] private float orbitingSpeed = 0.3f;
    [SerializeField] private float orbitReturnSpeed = 0.4f;

    [SerializeField] private float whileRidingMaxAngle = 45f;
    [Tooltip("After how many Seconds standing still 360 Orbiting is unlocked")]
    [SerializeField] private float standingTimeTreshold = 2f;
    private float standingTime = 0f;
    private float currentMaxAngle = 360f;

    [Header("Runtime Variables")]
    [SerializeField] bool canOrbit360 = false;
    [SerializeField] float orbitAngle = 0f;
    private float orbitMovement = 0f;
    private Vector2 orbitPosition = Vector2.zero;
    [SerializeField] CameraMode currentCameraMode = CameraMode.Follow;

    public enum CameraMode
    {
        Follow,
        Aim
    }

    public void HandleInput(in GamepadInput input, float deltaTime) 
    {
        orbitMovement = inputLogic.CheckAxis(inputAxis, input);
    }

    public void HandleRigidbodyData(in RigidbodyData rigidbodyData, float deltaTime)
    {
        if (rigidbodyData.Velocity.magnitude < 0.1f)
        {
            if (standingTime >= standingTimeTreshold)
            {
                canOrbit360 = true;
                currentMaxAngle = 360f;
            }
            else
            {
                standingTime += deltaTime;
            }
        }
        else if (standingTime >= 0f)
        {
            canOrbit360 = false;
            standingTime = 0f;
        }
    }


    private void LateUpdate()
    {
        // Handle Orbiting Movement
        if (Mathf.Abs(orbitMovement) > 0.1f)
        {
            orbitAngle -= orbitMovement * orbitingSpeed * 360f * Time.deltaTime;
        }
        else
        {
            if (Mathf.Abs(orbitAngle) < 0.1f || Mathf.Abs(orbitAngle - 360f) < 0.1f)
            {
                orbitAngle = orbitAngle > 180f ? 360f : 0f;
            }
            else if (!canOrbit360)
            {
                // Smoothly return to center
                orbitAngle = Mathf.Lerp(orbitAngle, orbitAngle > 180f ? 360f : 0f, Time.deltaTime * orbitReturnSpeed);
            }
        }

        if (orbitAngle >= 360f) orbitAngle -= 360f;
        else if (orbitAngle < 0f) orbitAngle += 360f;

        // Clamp angle if 360 Orbiting is not allowed, also, during orbitmovement-Input
        if (!canOrbit360)
        {
            if (currentMaxAngle > whileRidingMaxAngle + 0.1f)
            {
                currentMaxAngle = Mathf.MoveTowards(currentMaxAngle, whileRidingMaxAngle, Time.deltaTime * orbitReturnSpeed * 360f);
                Debug.Log("Current Max Angle: " + currentMaxAngle);     
            }
            orbitAngle = orbitAngle > 180 ? Mathf.Clamp(orbitAngle, 360f - currentMaxAngle, 360f) : Mathf.Clamp(orbitAngle, 0f, currentMaxAngle);
        }

        float radians = orbitAngle * Mathf.Deg2Rad;
        orbitPosition = new Vector2(Mathf.Sin(radians), 1 - Mathf.Cos(radians)) * orbitRadius;
   

        var offset = followCamera.GetComponent<CinemachineCameraOffset>();
        if (offset != null)
        {
            offset.Offset = new Vector3(orbitPosition.x, 0f, orbitPosition.y);
        }
    }
}
