using UnityEngine;

public class SimplePDStabilizer : BaseInteractiveModule, IReferenceRigidbody, IHandleGroundData, ISimulateable, IAccessibleTriplePDController
{
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private PDRotationController pdRotationController;
    [SerializeField] private Transform referenceTransform;

    [Header("Runtime Variables")]
    [SerializeField] private Vector3 RotThrottle = Vector3.zero;

    [Header("Power")]
    [SerializeField] private Vector3 powerValues = new Vector3(1f, 0f, 1f);

    [Header("PD Settings")]
    [Tooltip("Limit the angle of attack at which stabilization happens\r\n" +
        "When Changing Values, make sure to set Dirty to Reload the PD Controller")]
    [SerializeField] Vector3 angleTresholdValues = new Vector3(60f, 60f, 60f);
    [Tooltip("When Changing Values, make sure to set Dirty to Reload the PD Controller")]
    [SerializeField] public Vector3 PValues = new Vector3(1f, 0f, 0.6f), DValues = new Vector3(0f, 0f, 0.03f);

    [SerializeField] public bool PDControllerIsDirty;  // Set to true when changing PD values in the inspector

    [Header("Effects")]
    [SerializeField] private ParticleSystem thrusterEffect = null;
    [SerializeField] private FloatFlexibleSound soundSystem = null;


    private void Awake()
    {
        if (referenceTransform == null)
        {
            referenceTransform = transform;
        }
        pdRotationController = new PDRotationController(referenceTransform, PValues, DValues, angleTresholdValues);
    }

    public void HandleGroundData(in GroundData groundData, float targetFlightHeight, float deltaTime)
    {
        if (groundData.Hit)
        {
            if (pdRotationController == null)
            {
                return; 
            }

            RotThrottle = pdRotationController.GetFeedback(groundData.HitNormal, deltaTime);

            RotThrottle.x *= powerValues.x;
            RotThrottle.y *= powerValues.y;
            RotThrottle.z *= powerValues.z;
        }
        else
        {
            RotThrottle = Vector3.zero;
        }
    }

    public void Simulate(float deltaTime)
    {
        if (PDControllerIsDirty)
        {
            ReloadPDController();
            PDControllerIsDirty = false;
        }

        if (soundSystem != null)
        {
            soundSystem.HandleSound(RotThrottle.magnitude, deltaTime);
        }

        if (RotThrottle != Vector3.zero)
        {
            PhysicsRigidbody.AddRelativeTorque(RotThrottle * deltaTime, ForceMode.Impulse);
        }        
    }

    public void ReloadPDController(Vector3 _pValues, Vector3 _dValues, Vector3 _angleTresholdValues, Transform _referenceTransform = null)
    {
        if (_referenceTransform == null)
        {
            _referenceTransform = referenceTransform;
        }
        pdRotationController = new PDRotationController(_referenceTransform, _pValues, _dValues, _angleTresholdValues);
    }

    public void ReloadPDController()
    {
        pdRotationController = new PDRotationController(referenceTransform, PValues, DValues, angleTresholdValues);
    }

    public void BoostPDValues(Vector3 _pFactorBoost, Vector3 _dFactorBoost)
    {
        Vector3 _pValues = PValues;
        _pValues.x *= _pFactorBoost.x;
        _pValues.y *= _pFactorBoost.y;
        _pValues.z *= _pFactorBoost.z;

        Vector3 _dValues = DValues;
        _dValues.x *= _dFactorBoost.x;
        _dValues.y *= _dFactorBoost.y;
        _dValues.z *= _dFactorBoost.z;

        pdRotationController = new PDRotationController(referenceTransform, _pValues, DValues, angleTresholdValues);
    }
}
