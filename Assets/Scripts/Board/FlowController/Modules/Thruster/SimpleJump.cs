using UnityEngine;

public class SimpleJump : MonoBehaviour, IReferenceRigidbody, IHandleInput, IHandleGroundData, ISimulateable
{
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private Transform referenceTransform;
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();

    [Header("Runtime Variables")]
    [SerializeField] private bool buttonPressed = false;
    [SerializeField] private int airborneJumpsTaken = 0;
    

    [Header("Settings")]

    [Tooltip("1 = Left Shoulder, 2 = Right Shoulder, 3 = Left Stick Button, 4 = Right Stick Button\r\n " +
        "5 = A Button, 6 = B Button, 7 = X Button, 8 = Y Button\r\n " +
        "9 = North Button, 10 = East Button, 11 = South Button, 12 = West Button\r\n " +
        "13 = Start Button, 14 = Select Button\r\n " +
        "15 = Left Trigger, 16 = Right Trigger\r\n " +
        "17 = Left Stick X, 18 = Left Stick Y, 19 = Right Stick X, 20 = Right Stick Y")]
    [SerializeField] private int activationButton = 0;

    [Tooltip("Set to -1 for infinite jumps")]
    [SerializeField] private int maximumJumps = 1;
    [SerializeField] float coolDown = 0.5f;
    [SerializeField] float jumpStrength = 5f;

    [Tooltip("Jump can recharge when below heightTreshold (* heightFactor)")]
    [SerializeField] float heightTreshold = 1f;


    [Header("Effects")]
    [SerializeField] private ParticleSystem jumpEffect = null;
    [SerializeField] private FloatFlexibleSound soundSystem = null;

    private bool wasPressed = false;
    private float timeSinceJump = 0f;
    private float heightFactor;

    private void Awake()
    {
        if (referenceTransform == null)
        {
            referenceTransform = transform;
        }

        timeSinceJump = coolDown;
    }

    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        buttonPressed = inputLogic.CheckButton(activationButton, input);
    }

    public void HandleGroundData(in GroundData groundData, float targetFlightHeight, float deltaTime)
    {
        heightFactor = groundData.Hit ? groundData.HitDistance / targetFlightHeight : heightTreshold +1f;

        if (groundData.Hit && timeSinceJump >= coolDown && airborneJumpsTaken > 0f && heightFactor <= heightTreshold)
        {
            airborneJumpsTaken = 0;
        }
    }

    public void Simulate(float deltaTime)
    {
        if (timeSinceJump < coolDown)
        {
            timeSinceJump = timeSinceJump + deltaTime;
        }

        if (wasPressed != buttonPressed)
        {
            if (buttonPressed)  // On button pressed
            {
                if ((airborneJumpsTaken < maximumJumps || maximumJumps == -1f) && timeSinceJump >= coolDown)
                {                    
                    if (heightFactor > heightTreshold)  // when mid-air, count airborne Jumps
                    {
                        airborneJumpsTaken++;
                    }

                    //Debug.Log("Wahoo");
                    PhysicsRigidbody.AddForceAtPosition(referenceTransform.up * jumpStrength, referenceTransform.position);

                    // Play Sound Effect
                    if (soundSystem != null)
                    {
                        soundSystem.HandleSound(1, 1);
                    }

                    timeSinceJump = 0f;
                }
            }

            wasPressed = buttonPressed;
        }
    }
}
