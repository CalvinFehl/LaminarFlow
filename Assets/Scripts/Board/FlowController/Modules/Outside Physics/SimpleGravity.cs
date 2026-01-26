using UnityEngine;

public class SimpleGravity : BaseInteractiveModule, IReferenceRigidbody, IHandleInput, ISimulateable
{
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();

    [Header("Runtime Variables")]
    public bool IsOn;
    public Vector3 gravityVector;

    [Header("Settings")]
    [Tooltip("1 = Left Shoulder, 2 = Right Shoulder, 3 = Left Stick Button, 4 = Right Stick Button\r\n" +
        "    /// 5 = A Button, 6 = B Button, 7 = X Button, 8 = Y Button\r\n" +
        "    /// 9 = North Button, 10 = East Button, 11 = South Button, 12 = West Button\r\n" +
        "    /// 13 = Start Button, 14 = Select Button\r\n" +
        "    /// 15 = Left Trigger, 16 = Right Trigger\r\n" +
        "    /// 17 = Left Stick X, 18 = Left Stick Y, 19 = Right Stick X, 20 = Right Stick Y")]
    [SerializeField] private int turnOffButton;

    [SerializeField] private Vector3 gravityDirection = Vector3.down;
    [SerializeField] private float gravityForce = 9.81f;


    private void Awake()
    {
        gravityVector = gravityDirection * gravityForce;
    }

    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        if (turnOffButton > 0)
        {
            IsOn = !inputLogic.CheckButton(turnOffButton, input);
        }
    }

    public void Simulate(float deltaTime)
    {
        if (!IsOn) return;
        PhysicsRigidbody.AddForce(gravityVector * deltaTime, ForceMode.Impulse);
    }
}