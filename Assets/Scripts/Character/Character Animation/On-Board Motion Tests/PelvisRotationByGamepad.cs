using UnityEngine.InputSystem;
using UnityEngine;
using Assets.Scripts.scrible.Impulsor;

public class PelvisRotationByGamepad : MonoBehaviour
{
    private Gamepad gamepad;
    private Vector2 leftStickInput;
    private Vector2 rightStickInput;
    private float thrustInput;

    public GameObject activeLeanPivot;

    [SerializeField] private RocketImpulsor rocketImpulsor;
    [SerializeField] private float kneesBias = 30f;
    [SerializeField] private Vector3 maxLeanSpeed = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private Vector3 maxAngles = new Vector3(20f, 70f, 30f);
    [SerializeField] private float thrustCompensationMaxAngle = 30f;
    [SerializeField] private float thrustCompensationFactor = 0.5f;

    void Start()
    {
        gamepad = Gamepad.current;

        if(rocketImpulsor == null)
        {
            rocketImpulsor = GetComponentInParent<RocketImpulsor>();
        }
    }

    void Update()
    {
        if (gamepad == null)
        {
            gamepad = Gamepad.current;
        }
        else
        {
            leftStickInput = gamepad.leftStick.ReadValue();
            rightStickInput = gamepad.rightStick.ReadValue();
            thrustInput = gamepad.rightTrigger.ReadValue();
        }

        if(rocketImpulsor != null)
        {
            thrustInput = rocketImpulsor.ClampedThrottle;
        }

        activeLeanPivot.transform.localEulerAngles = new Vector3(
            Mathf.Clamp(leftStickInput.y * maxAngles.x + thrustCompensationMaxAngle * thrustInput * thrustCompensationFactor, -maxAngles.x, maxAngles.x),
            Mathf.Clamp(rightStickInput.x * maxAngles.y, -maxAngles.y, maxAngles.y),
            Mathf.Clamp(-leftStickInput.x * maxAngles.z, -maxAngles.z - kneesBias, maxAngles.z - kneesBias));


        /*    activeLeanPivot.transform.localEulerAngles = new Vector3(
            Mathf.Clamp(activeLeanPivot.transform.localEulerAngles.x + Mathf.Clamp((leftStickInput.y * maxAngles.x - activeLeanPivot.transform.localEulerAngles.x), -maxLeanSpeed.x, maxLeanSpeed.x), -maxAngles.x, maxAngles.x),
            Mathf.Clamp(activeLeanPivot.transform.localEulerAngles.z + Mathf.Clamp((leftStickInput.x * maxAngles.z - activeLeanPivot.transform.localEulerAngles.z), -maxLeanSpeed.z, maxLeanSpeed.z), -maxAngles.z, maxAngles.z),
            Mathf.Clamp(activeLeanPivot.transform.localEulerAngles.y + Mathf.Clamp((rightStickInput.x * maxAngles.y - activeLeanPivot.transform.localEulerAngles.y), -maxLeanSpeed.y, maxLeanSpeed.y), -maxAngles.y, maxAngles.y));
        */
    }
}
