using UnityEngine;

public class SimpleRotater : BaseInteractiveModule, IReferenceRigidbody, IHandleInput, ISimulateable
{
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();

    [Header("Runtime Variables")]
    [SerializeField] private Vector3 RotThrottle = Vector3.zero;


    [Header("Settings")]
    [SerializeField] private float pitchSpeed = 0f;
    [SerializeField] private float yawSpeed = 0f;
    [SerializeField] private float rollSpeed = 0f;

    [Tooltip("1 -> left.x, 2 -> left.y, 3 -> right.x, 4 -> right.y")]
    [SerializeField] private int PitchStickAxis = 2,
        YawStickAxis = 3, RollStickAxis = 1;


    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        RotThrottle = new Vector3(inputLogic.CheckAxis(PitchStickAxis, input) * pitchSpeed,
            inputLogic.CheckAxis(YawStickAxis, input) * yawSpeed,
            inputLogic.CheckAxis(RollStickAxis, input) * rollSpeed);
    }
    public void Simulate(float deltaTime)
    {
        PhysicsRigidbody.AddRelativeTorque(RotThrottle * deltaTime, ForceMode.Impulse);
    }
}
