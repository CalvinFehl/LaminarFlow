using UnityEngine;

public class SimpleReversePendulumAnimationSystem : MonoBehaviour, ISimpleAnimateable
{
    #region Variables and Properties
    [Header("Runtime Variables")]
    [SerializeField] private Vector3 localForce;
    [SerializeField] private Vector3 dampedLeftoverMomentum;

    [SerializeField] private Vector3 returnForce;

    [SerializeField] private Vector3 localMovement;
    [SerializeField] public Vector3 LocalAcceleration;

    [Header("Settings")]
    [SerializeField] private Transform referenceTransform;

    [Tooltip("How much the Mass will be accelerated")]
    [SerializeField] private Vector3 currentPositionalImpulseGain = new Vector3(1f,1f,1f);

    [Header("Rest Position and Rotation")]

    [SerializeField] private Vector3 restPosition;
    [Tooltip("If set to true, the Mass will snap to the given Rest Position on start, however the borders stay unchanged")]
    [SerializeField] private bool setRestPositionInInspector = true;

    [SerializeField] private Quaternion restRotation;
    [Tooltip("If set to true, the Mass will snap to the given Rest Rotation on start, however the borders stay unchanged")]
    [SerializeField] private bool setRestRotation = true;

    [SerializeField] private bool setPositionalImpulseGainInInspector = true;

    [Tooltip("How much the moving Mass will be slowed through joint friction by direction")]
    [SerializeField] private Vector3 positiveTimeTillFullyDampened;
    [SerializeField] private Vector3 negativeTimeTillFullyDampened;
    [SerializeField] private bool setPositionalMomentumDampingInInspector = true;


    [Header("Stiffness Function Keypoints")]
    [Header("Positive Axes")]
    [SerializeField] private Vector3 positiveAxesBorders = new Vector3(0.5f, 0.5f, 0.5f);
    [Tooltip("(Forward, Right, Up): How much Returning Force is added to the impulse at the Borders")]
    [SerializeField] private Vector3 positiveAxesReturnForcesAtBorder;

    [Tooltip("Positions of the Keypoints (need to be smaller than the borders)")]
    [SerializeField] private Vector3 positiveAxesKeyPoints;
    [Tooltip("(Forward, Right, Up):  How much Returning Force is added to the impulse at the Keypoints")]
    [SerializeField] private Vector3 positiveAxesReturnForcesAtKeyPoints;
    
    [Header("Negative Axes")]
    [SerializeField] private Vector3 negativeAxesBorders = new Vector3(-0.5f, -0.5f, -0.5f);
    [Tooltip("(Backward, Left, Down): How much Returning Force is added to the impulse at the Borders")]
    [SerializeField] private Vector3 negativeAxesReturnForcesAtBorder;

    [Tooltip("Position of the Keypoints (need to be smaller than the borders)")]
    [SerializeField] private Vector3 negativeAxesKeyPoints;
    [Tooltip("(Backward, Left, Down): How much Returning Force is added to the impulse at the Keypoints")]
    [SerializeField] private Vector3 negativeAxesReturnForcesAtKeyPoints;


    // Properties derived from the ISimpleAnimateable interface
    public float ImpulseGain { get; set; }
    public Vector3 PositiveRelativeStiffness { get; set; }
    public Vector3 NegativeRelativeStiffness { get; set; }
    public Vector3 LocalMomentum { get; set; }
    public Vector3 LocalAngularMomentum { get; set; }

    #endregion

    // Initalize
    private void OnEnable()
    {
        // Set the initial values based on the inspector settings

        restPosition = setRestPositionInInspector ? transform.localPosition : Vector3.zero;
        restRotation = setRestRotation ? transform.localRotation : Quaternion.identity;

        LocalMomentum = Vector3.zero;
        LocalAngularMomentum = Vector3.zero;
    }


    public void AddImpulse(in Transform referenceTransform, Vector3 impulse = default, float deltaTime = 0)
    {
        if (deltaTime == 0f) return;
       
        localForce = - referenceTransform.InverseTransformDirection(impulse) / deltaTime; // Inverse transform to local space by m/s

        localForce.x *= currentPositionalImpulseGain.x;
        localForce.y *= currentPositionalImpulseGain.y;
        localForce.z *= currentPositionalImpulseGain.z;
    }

    public void Animate(float deltaTime = 0)
    {
        if (deltaTime == 0f) return;

        Vector3 localMomentum = LocalMomentum;
        
        localMomentum.x = DampenMomentumAxis(LocalMomentum.x, positiveTimeTillFullyDampened.x, negativeTimeTillFullyDampened.x, deltaTime);
        localMomentum.y = DampenMomentumAxis(LocalMomentum.y, positiveTimeTillFullyDampened.y, negativeTimeTillFullyDampened.y, deltaTime);
        localMomentum.z = DampenMomentumAxis(LocalMomentum.z, positiveTimeTillFullyDampened.z, negativeTimeTillFullyDampened.z, deltaTime);

        dampedLeftoverMomentum = localMomentum; // Store the damped momentum for debugging purposes


        Vector3 currentLocalPosition = transform.localPosition;


        // Apply a returning impulse depending on the local position

        returnForce.x = CalculateReturnForce(currentLocalPosition.x, restPosition.x, 
            positiveAxesKeyPoints.x, positiveAxesBorders.x, 
            positiveAxesReturnForcesAtKeyPoints.x, positiveAxesReturnForcesAtBorder.x, 
            negativeAxesReturnForcesAtKeyPoints.x, negativeAxesReturnForcesAtBorder.x);

        returnForce.y = CalculateReturnForce(currentLocalPosition.y, restPosition.y, 
            positiveAxesKeyPoints.y, positiveAxesBorders.y, 
            positiveAxesReturnForcesAtKeyPoints.y, positiveAxesReturnForcesAtBorder.y, 
            negativeAxesReturnForcesAtKeyPoints.y, negativeAxesReturnForcesAtBorder.y);

        returnForce.z = CalculateReturnForce(currentLocalPosition.z, restPosition.z,
            positiveAxesKeyPoints.z, positiveAxesBorders.z, 
            positiveAxesReturnForcesAtKeyPoints.z, positiveAxesReturnForcesAtBorder.z, 
            negativeAxesReturnForcesAtKeyPoints.z, negativeAxesReturnForcesAtBorder.z);


        localMomentum += (localForce + returnForce) * deltaTime; // Add the local force to the local momentum

        // Apply the local momentum to the local position
        Vector3 newLocalPosition = currentLocalPosition + localMomentum * deltaTime;

        // Clamp the new local position to the defined borders
        newLocalPosition.x = Mathf.Clamp(newLocalPosition.x, negativeAxesBorders.x, positiveAxesBorders.x);
        newLocalPosition.y = Mathf.Clamp(newLocalPosition.y, negativeAxesBorders.y, positiveAxesBorders.y);
        newLocalPosition.z = Mathf.Clamp(newLocalPosition.z, negativeAxesBorders.z, positiveAxesBorders.z);

        transform.localPosition = newLocalPosition;

        LocalAcceleration = localMovement; // Store the local movement

        localMovement = newLocalPosition - currentLocalPosition; // Calculate the local movement this Frame

        LocalAcceleration = (localMovement - LocalAcceleration) / deltaTime;

        LocalMomentum = localMovement; // Update the local momentum for the next frame

    }

    public bool SetGoalPosition(in Transform referenceTransform, Vector3 position = default, float deltaTime = 0)
    {
        throw new System.NotImplementedException();
    }

    public bool SetGoalRotation(in Transform referenceTransform, Quaternion rotation = default, float deltaTime = 0)
    {
        throw new System.NotImplementedException();
    }

    public void SetStiffness(float stiffness = 0, Vector3 positiveRelativeStiffness = default, Vector3 negativeRelativeStiffness = default)
    {
        if (positiveRelativeStiffness != default) positiveAxesReturnForcesAtKeyPoints = positiveRelativeStiffness;
        if (negativeRelativeStiffness != default) negativeAxesReturnForcesAtKeyPoints = negativeRelativeStiffness;
    }


    private float DampenMomentumAxis(float momentum, float positiveDampTime, float negativeDampTime, float deltaTime)
    {
        if (momentum > 0f)
        {
            if (positiveDampTime == 0f) return 0f;
            return Mathf.Lerp(momentum, 0f, deltaTime / positiveDampTime);
        }
        else if (momentum < 0f)
        {
            if (negativeDampTime == 0f) return 0f;
            return Mathf.Lerp(momentum, 0f, deltaTime / negativeDampTime);
        }
        return 0f;
    }


    /// <summary>
    /// Return Force function that determines how much Return Force to apply based on the local position on the axis and within the defined borders.
    /// Lerps betweeen these three Key points: Rest Position, Key Point, and Border.
    /// </summary>
    float CalculateReturnForce(
        float current, float rest, float keyPoint, float border,
        float forceAtKeyPoint, float forceAtBorder, float forceAtNegKeyPoint, float forceAtNegBorder)
    {
        if (current == rest) // At Rest Position
        {            
            return 0f;
        }
        else if (current > 0) // Positive Axis
        {        
            if (current > keyPoint)
            {
                if (current >= border)
                    return forceAtBorder;
                return Mathf.Lerp(forceAtKeyPoint, forceAtBorder, Mathf.InverseLerp(keyPoint, border, current));
            }
            return Mathf.Lerp(0f, forceAtKeyPoint, Mathf.InverseLerp(rest, keyPoint, current));
        }
        // Negative Axis
        else if (current < keyPoint)
        {
            if (current <= border)
                return forceAtNegBorder;
            return Mathf.Lerp(forceAtNegBorder, forceAtNegKeyPoint, Mathf.InverseLerp(border, keyPoint, current));
        }
        return Mathf.Lerp(forceAtNegKeyPoint, 0f, Mathf.InverseLerp(keyPoint, rest, current));
    }


}
