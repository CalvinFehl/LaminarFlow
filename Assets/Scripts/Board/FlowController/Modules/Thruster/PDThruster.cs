using UnityEngine;

public class PDThruster : BaseInteractiveModule, IReferenceRigidbody, IHandleInput, IHandleGroundData, ISimulateable, IAccessibleSinglePDController, IReconsileFloat
{
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();
    [SerializeField] private PDController pdController;

    [Header("Runtime Variables")]
    [Tooltip("The force is applied in the direction of the Thruster Transform's Y axis")]
    public float TriggerValue = 0f;
    public float HeightFactor = 1f;
    public float AirborneFactor = 0f;
    public float Thrust = 0f;


    [Header("Settings")]
    [SerializeField] private bool usesLeftTrigger = true;
    [SerializeField] private float thrusterPower = 600f;
    [SerializeField] private float maxThrust = 900f;
    [SerializeField] private float airborneTresholdFactor = 3f;
    [SerializeField] private float airborneMultiplyer = 2f;

    [Header("Initial PD Settings")]
    [Tooltip("When Changing Values, make sure to set Dirty to Reload the PD Controller")]
    [SerializeField] public float PFactor = 1f;
    [Tooltip("When Changing Values, make sure to set Dirty to Reload the PD Controller")]
    [SerializeField] public float DFactor = 0.02f;

    [SerializeField] public bool PDControllerIsDirty;  // Set to true when changing PD values in the inspector

    [Header("Effects")]
    [SerializeField] private ParticleSystem thrusterEffect = null;
    [SerializeField] private FloatFlexibleSound soundSystem = null;


    private void Awake()
    {
        pdController = new PDController(PFactor, DFactor);
    }

    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        TriggerValue = inputLogic.CheckTrigger(usesLeftTrigger, input);
    }

    public void HandleGroundData(in GroundData groundData, float targetFlightHeight, float deltaTime)
    {
        HeightFactor = groundData.Hit ? groundData.HitDistance / targetFlightHeight : 1000f;

        AirborneFactor = HeightFactor < airborneTresholdFactor ? 1f : 0f;

        if (pdController != null)
        {
            Thrust = pdController.GetFeedback(targetFlightHeight, groundData.HitDistance, deltaTime);
            Thrust = Mathf.Clamp(Thrust * thrusterPower * (1 - TriggerValue) * AirborneFactor, 0f, maxThrust);
        }
        else
        {
            Thrust = 0f;
        }
    }

    // ISimulateable Method
    public void Simulate(float deltaTime)
    {
        if (PDControllerIsDirty)
        {
            ReloadPDController();
            PDControllerIsDirty = false;
        }

        if (Thrust != 0f)
        {
            PhysicsRigidbody.AddForce(Thrust * transform.up * deltaTime, ForceMode.Impulse);
        }

        if (soundSystem != null)
        {
            soundSystem.HandleSound(Thrust, deltaTime);
        }
    }

    public void ReloadPDController(float _pFactor, float _dFactor)
    {
        pdController = new PDController(_pFactor, _dFactor);
    }

    public void ReloadPDController()
    {
        pdController = new PDController(PFactor, DFactor);
    }

    public void BoostPDValues(float _pFactorBoost, float _dFactorBoost)
    {
        pdController = new PDController(PFactor * _pFactorBoost, DFactor * _dFactorBoost);
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
