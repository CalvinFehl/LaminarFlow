using UnityEngine;

public class SimpleThruster : MonoBehaviour, IReferenceRigidbody, IHandleInput, IHandleGroundData, ISimulateable, IReconsileFloat
{
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();

    [Header("Runtime Variables")]

    [Tooltip("The force is applied in the direction of the Thruster Transform's Y axis")]
    public float TriggerValue = 0f;
    public float Thrust = 0f;
    public float HeightFactor = 1f;
    public float AirborneFactor = 0f;


    [Header("Settings")]
    [SerializeField] private bool usesLeftTrigger = true;
    [SerializeField] private float thrusterPower = 240f;
    [SerializeField] private float maxThrust = 240f;
    [SerializeField] private float airborneTresholdFactor = 3f;
    [SerializeField] private float airborneMultiplyer = 2f;


    [Header("Effects")]
    [SerializeField] private ParticleSystem thrusterEffect = null;
    [SerializeField] private FloatFlexibleSound soundSystem = null;


    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        TriggerValue = inputLogic.CheckTrigger(usesLeftTrigger, input);
    }

    public void HandleGroundData(in GroundData groundData, float targetFlightHeight, float deltaTime)
    {
        HeightFactor = groundData.Hit? groundData.HitDistance / targetFlightHeight : airborneTresholdFactor;

        AirborneFactor = HeightFactor < airborneTresholdFactor ? 
            Mathf.Clamp(airborneMultiplyer * (1 - HeightFactor / airborneTresholdFactor) + 1f, 0f, 1f) : 0f;

        Thrust = Mathf.Clamp(TriggerValue * thrusterPower * AirborneFactor, 0f, maxThrust);
    }

    // ISimulateable Method
    public void Simulate(float deltaTime)
    {
        if (Thrust != 0f)
        {
            PhysicsRigidbody.AddForce(Thrust * transform.up * deltaTime, ForceMode.Impulse);            
        }

        if (soundSystem != null)
        {
            soundSystem.HandleSound(Thrust, deltaTime);
        }
    }

    // IReconsileFloat Methods
    public float GetValue()
    {
        return Thrust;
    }

    public void Reconcile(float value, uint tick = 0)
    {
        Thrust = value; 
    }

}
