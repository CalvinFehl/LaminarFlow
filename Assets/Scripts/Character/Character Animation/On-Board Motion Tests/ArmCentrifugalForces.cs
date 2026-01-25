using UnityEngine.InputSystem;
using UnityEngine;

public class ArmCentrifugalForces : MonoBehaviour
{
    private Gamepad gamepad;
    private Vector2 leftStickInput;
    private Vector2 rightStickInput;

    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float maxScaleSpeed = 0.2f;
    [SerializeField] private Vector3 scaleByPitchYawRoll = new Vector3(0.5f, 1f, 0f);
    [SerializeField] private float tallnessNormValue = 1f;
    private float currentScale = 1f;
    [SerializeField] private float maxYawRotation = 65f;
    [SerializeField] private float maxYawRotationSpeed = 1f;
    [SerializeField] private Vector3 rotateByPitchYawRoll = new Vector3(0f, -1f, 0f);
    private float currentYawRotation = 0f;
    [SerializeField] private float minHeight = 0.5f;
    [SerializeField] private float maxHeight = 1.5f;
    [SerializeField] private float maxHeightSpeed = 0.2f;
    [SerializeField] private Vector3 heightByPitchYawRoll = new Vector3(1f, 0.7f, 0.5f);
    private float currentHeight = 0f;

    public Transform centrifugalDisc;

    void Start()
    {
        gamepad = Gamepad.current;

        if(centrifugalDisc == null)
        {
            centrifugalDisc = transform;
        }
    }

    void FixedUpdate()
    {
        if (gamepad == null)
        {
            gamepad = Gamepad.current;
        }
        else
        {
            leftStickInput = gamepad.leftStick.ReadValue();
            rightStickInput = gamepad.rightStick.ReadValue();
        }

        float potentialScale = 0f;
        float potentialYawRotation = 0f;
        float potentialHeight = 0f;

        if (leftStickInput != Vector2.zero || rightStickInput != Vector2.zero)
        {
            potentialScale = Mathf.Abs(leftStickInput.y) * scaleByPitchYawRoll.x 
                           + Mathf.Abs(rightStickInput.x) * scaleByPitchYawRoll.y 
                           + Mathf.Abs(leftStickInput.x) * scaleByPitchYawRoll.z;
            potentialYawRotation = (Mathf.Abs(leftStickInput.y) * rotateByPitchYawRoll.x 
                                 + rightStickInput.x * rotateByPitchYawRoll.y 
                                 + Mathf.Abs(leftStickInput.x) * rotateByPitchYawRoll.z) * maxYawRotation;
            potentialHeight = Mathf.Abs(leftStickInput.y) * heightByPitchYawRoll.x 
                            + Mathf.Abs(rightStickInput.x) * heightByPitchYawRoll.y 
                            + Mathf.Abs(leftStickInput.x) * heightByPitchYawRoll.z;
        }

        if (potentialScale > currentScale)
        {
            if(potentialScale - currentScale > maxScaleSpeed * Time.fixedDeltaTime)
            {
                currentScale += maxScaleSpeed * Time.fixedDeltaTime;
            }
            else
            {
                currentScale = potentialScale;
            }
        }
        else if (potentialScale < currentScale)
        {
            if (currentScale - potentialScale > maxScaleSpeed * Time.fixedDeltaTime)
            {
                currentScale -= maxScaleSpeed * Time.fixedDeltaTime;
            }
            else
            {
                currentScale = potentialScale;
            }
        }

        if (potentialYawRotation > currentYawRotation)
        {
            if (potentialYawRotation - currentYawRotation > maxYawRotationSpeed * Time.fixedDeltaTime)
            {
                currentYawRotation += maxYawRotationSpeed * Time.fixedDeltaTime;
            }
            else
            {
                currentYawRotation = potentialYawRotation;
            }
        }
        else if (potentialYawRotation < currentYawRotation)
        {
            if (currentYawRotation - potentialYawRotation > maxYawRotationSpeed * Time.fixedDeltaTime)
            {
                currentYawRotation -= maxYawRotationSpeed * Time.fixedDeltaTime;
            }
            else
            {
                currentYawRotation = potentialYawRotation;
            }
        }

        if (potentialHeight > currentHeight)
        {
            if (potentialHeight - currentHeight > maxHeightSpeed * Time.fixedDeltaTime)
            {
                currentHeight += maxHeightSpeed * Time.fixedDeltaTime;
            }
            else
            {
                currentHeight = potentialHeight;
            }
        }
        else if (potentialHeight < currentHeight)
        {
            if (currentHeight - potentialHeight > maxHeightSpeed * Time.fixedDeltaTime)
            {
                currentHeight -= maxHeightSpeed * Time.fixedDeltaTime;
            }
            else
            {
                currentHeight = potentialHeight;
            }
        }

        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
        currentYawRotation = Mathf.Clamp(currentYawRotation, -maxYawRotation, maxYawRotation);
        currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);

        centrifugalDisc.localScale = new Vector3(currentScale, tallnessNormValue, currentScale);
        centrifugalDisc.localRotation = Quaternion.Euler(0f, currentYawRotation, 0f);
        centrifugalDisc.localPosition = new Vector3(0f, currentHeight, 0f);
    }
}
