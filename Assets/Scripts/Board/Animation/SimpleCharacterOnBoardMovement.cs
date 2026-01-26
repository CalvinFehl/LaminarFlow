using UnityEngine;

public class SimpleCharacterOnBoardMovement : MonoBehaviour, ISimulateable
{
    [SerializeField] private bool autoUpdate = false;

    [Header("Runtime Variables")]
    [Header("Location Tracking")]
    //private LocationAsLocalMovementEventData locationAsLocalMovementEventData = new LocationAsLocalMovementEventData();
    [SerializeField] private Vector3 localMovement;
    [SerializeField] private float localMovementGain = 1f;
    [SerializeField] private Vector3 localAcceleration;
    [SerializeField] private float localAccelerationGain = 1f;
    [SerializeField] private Vector3 accumulatedLocalMovement;
    [SerializeField] private Vector3 accumulatedLocalMovementTime;

    [Header("Rotation Tracking")]
    //private PitchYawRollEventData pitchYawRollEventData = new PitchYawRollEventData();
    [SerializeField] private Vector3 localAngularMovement;
    [SerializeField] private float localAngularMovementGain = 1f;
    [SerializeField] private Vector3 localAngularAcceleration;
    [SerializeField] private float localAngularAccelerationGain = 1f;
    [SerializeField] private Vector3 accumulatedLocalAngularMovement;
    [SerializeField] private Vector3 accumulatedLocalAngularMovementTime;

    [SerializeField] bool isDirty = false;

    // Wird noch geändert
    [Header("Set-Up")]
    [SerializeField] private int rotate90DegreesAroundY = 0;
    [SerializeField] private Vector3 startPositionPelvis = Vector3.zero;

    [Header("Components")]
    [SerializeField] private Transform referenceTransform;

    [Header("Pelvis and Chest")]
    [SerializeField] private SimpleReversePendulumAnimationSystem pelvis;
    [SerializeField] private SimpleReversePendulumAnimationSystem chest;
    [SerializeField] private Transform headTransform;

    [Header("Hands")]
    [SerializeField] private SimpleReversePendulumAnimationSystem leftHand;
    [SerializeField] private SimpleReversePendulumAnimationSystem rightHand;
    [SerializeField] private float spreadArmsWhenFalling;
    [SerializeField] private float spreadArmsWhenYawing;

    [Header("Feet")]
    [SerializeField] private Transform leftFootTransform;
    [SerializeField] private Transform rightFootTransform;


    [Header("Settings")]
    [SerializeField] private LocationAsLocalMovementInterpreter locationAsLocalMovementInterpreter;
    [SerializeField] private RotationAsPitchYawRollInterpreter rotationAsPitchYawRollInterpreter;


    #region Initialization and Setter Methods
    private void OnEnable()
    {
        if (locationAsLocalMovementInterpreter != null)
        {
            locationAsLocalMovementInterpreter.OnRelativeLocationUpdated += ReceiveLocationTracking;
        }
        if (rotationAsPitchYawRollInterpreter != null)
        {
            rotationAsPitchYawRollInterpreter.OnPitchYawRollUpdated += ReceiveRotationTracking;
        }
    }

    private void OnDisable()
    {
        if (locationAsLocalMovementInterpreter != null)
        {
            locationAsLocalMovementInterpreter.OnRelativeLocationUpdated -= ReceiveLocationTracking;
        }
        if (rotationAsPitchYawRollInterpreter != null)
        {
            rotationAsPitchYawRollInterpreter.OnPitchYawRollUpdated -= ReceiveRotationTracking;
        }
    }
    private void ReceiveLocationTracking(LocationAsLocalMovementEventData data)
    {
        localMovement = data.DeltaLocation;

        int rotationSteps = rotate90DegreesAroundY % 4;
        if (rotationSteps == 0)
        {
            localAcceleration = data.LocationAcceleration;
        }
        else if (rotationSteps == 1)
        {
            localAcceleration = new Vector3(data.LocationAcceleration.z, data.LocationAcceleration.y, -data.LocationAcceleration.x);
        }
        else if (rotationSteps == 2)
        {
            localAcceleration = new Vector3(-data.LocationAcceleration.x, data.LocationAcceleration.y, -data.LocationAcceleration.z);
        }
        else if (rotationSteps == 3)
        {
            localAcceleration = new Vector3(-data.LocationAcceleration.z, data.LocationAcceleration.y, data.LocationAcceleration.x);
        }

        accumulatedLocalMovement = data.AccumulatedLocation;
        accumulatedLocalMovementTime = data.AccumulatedLocationTime;
    }
    private void ReceiveRotationTracking(PitchYawRollEventData data)
    {
        localAngularMovement = data.DeltaPitchYawRoll;
        localAngularAcceleration = data.PitchYawRollAcceleration;
        accumulatedLocalAngularMovement = data.AccumulatedPitchYawRoll;
        accumulatedLocalAngularMovementTime = data.AccumulatedPitchYawRollTime;
    }
    public void ResetDirtyFlag()
    {
        isDirty = false;
    }

    private void Start()
    {
        if (referenceTransform == null)
        {
            referenceTransform = transform;
        }
        startPositionPelvis = referenceTransform.position;
    }
    #endregion

    #region Runtime Methods

    public void Simulate(float deltaTime) 
    {
        if (!autoUpdate)
        {
            Animate(deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (autoUpdate || isDirty)
        {
            Animate(Time.deltaTime);
            ResetDirtyFlag();
        }
    }

    private void Animate(float deltaTime = 0f)
    {   
        if (pelvis == null) return;
        if (deltaTime == 0f) deltaTime = Time.deltaTime;



        // Extra Impulse for Pelvis
        Vector3 tilt = localAngularMovement.x * referenceTransform.right + localAngularMovement.z * referenceTransform.forward;

        pelvis.AddImpulse(referenceTransform, (localAcceleration * localAccelerationGain) + tilt * localAngularMovementGain * deltaTime, deltaTime);
        pelvis.Animate(deltaTime);

        if (chest == null) return;
        chest.AddImpulse(referenceTransform, localAcceleration, deltaTime);
        chest.Animate(deltaTime);

        // Extra Impulses for the Hands
        float _spreadArmsWhenFalling = localMovement.y * deltaTime * spreadArmsWhenFalling;
        float _spreadArmsWhenYawing = Mathf.Abs(localAngularMovement.y) * -spreadArmsWhenYawing * deltaTime;
        Vector3 rightArmSpread = referenceTransform.right * (_spreadArmsWhenFalling + _spreadArmsWhenYawing) + referenceTransform.up * _spreadArmsWhenYawing * 0.3f;

        if (leftHand != null) 
        {
            leftHand.AddImpulse(referenceTransform, localAcceleration - rightArmSpread, deltaTime);
            leftHand.Animate(deltaTime);            
        }

        if (rightHand != null)
        {
            rightHand.AddImpulse(referenceTransform, localAcceleration + rightArmSpread, deltaTime);
            rightHand.Animate(deltaTime);
        }
    }
    #endregion
}


public interface ISimpleAnimateable
{
    public float ImpulseGain { get; set; }
    public Vector3 PositiveRelativeStiffness { get; set; }
    public Vector3 NegativeRelativeStiffness { get; set; }
    public Vector3 LocalMomentum { get; set; }
    public Vector3 LocalAngularMomentum { get; set; }

    public void AddImpulse(in Transform referenceTransform, Vector3 impulse = default, float deltaTime = 0f);
    public bool SetGoalPosition(in Transform referenceTransform, Vector3 position = default, float deltaTime = 0f);
    public bool SetGoalRotation(in Transform referenceTransform, Quaternion rotation = default, float deltaTime = 0f);
    public void SetStiffness(float stiffness = 0, Vector3 positiveRelativeStiffness = default, Vector3 negativeRelativeStiffness = default);
    public void Animate(float deltaTime = 0f);
}
