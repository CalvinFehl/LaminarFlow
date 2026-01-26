using System;
using UnityEngine;

public class SimpleAdaptiveAerodynamics : MonoBehaviour, IReferenceRigidbody, IHandleRigidbodyData, IHandleInput, ISimulateable
{
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private Transform referenceTransform;
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();

    [Header("Runtime Variables")]
    [SerializeField] private bool buttonPressed = false;
    [SerializeField] private float auxiliaryButtonPressed;
    [SerializeField] private Vector3 dragBoost = new Vector3(1, 1, 1);

    [SerializeField] private float liftForce;
    [SerializeField] private float dragForce;

    [SerializeField] private Vector3 localLiftVector;
    [SerializeField] private Vector3 localDragVector;

    [Header("Wind Conditions")]
    public float AirDensity = 1.1f;
    public Vector3 WindVector;

    [SerializeField] private float angleOfAttack;
    [SerializeField] private float angleOnDragArea;

    [Header("Settings")]
    [Tooltip("Multiply the drag factor by up to Max Drag Boost when the button is pressed")]
    [SerializeField] private bool usesActiveDragBoost = false;

    [Tooltip("1 = Left Shoulder, 2 = Right Shoulder, 3 = Left Stick Button, 4 = Right Stick Button\r\n " +
        "5 = A Button, 6 = B Button, 7 = X Button, 8 = Y Button\r\n " +
        "9 = North Button, 10 = East Button, 11 = South Button, 12 = West Button\r\n " +
        "13 = Start Button, 14 = Select Button\r\n " +
        "15 = Left Trigger, 16 = Right Trigger\r\n " +
        "17 = Left Stick X, 18 = Left Stick Y, 19 = Right Stick X, 20 = Right Stick Y")]
    [SerializeField] private int activationButton = 0;

    [Tooltip("Multiply the drag factor by Trigger Amount * Max Drag Boost when the button is pressed")]
    [SerializeField] private bool usesAuxiliarTriggerForDragBoost = false;
    [SerializeField] private bool usesLeftTrigger = false;

    [SerializeField] private Vector3 minDragBoost = new Vector3(1, 1, 1);
    [SerializeField] private Vector3 maxDragBoost = new Vector3(1, 5, 1);

    [Header("Board Settings")]
    [SerializeField] private Vector3 boardDimensions;

    [SerializeField] private float stallPoint = 15f;

    [SerializeField] private float minFloat = 0.25f;
    [SerializeField] private float maxFloat = 1f;
    [SerializeField] private float floatFactor = 0.1f;

    [SerializeField] private float minDrag = 0.05f;
    [SerializeField] private float maxDrag = 30f;
    [SerializeField] private float dragFactor = 0.5f;

    RigidbodyData rigidbodyData;

    private Vector3 liftVector;
    private Vector3 dragVector;

    private float boardSurfaceArea;

    private float liftCoefficient;
    private float dragCoefficient;


    float currentVelocityTimesAirdensityTimesSurfaceArea;
    private void Awake()
    {
        boardSurfaceArea = boardDimensions.x * boardDimensions.z;
    }

    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        if (usesActiveDragBoost)
        {
            buttonPressed = inputLogic.CheckButton(activationButton, input);

            if (buttonPressed && usesAuxiliarTriggerForDragBoost)
            {
                auxiliaryButtonPressed = inputLogic.CheckTrigger(usesLeftTrigger, input);
                dragBoost = Vector3.Lerp(minDragBoost, maxDragBoost, auxiliaryButtonPressed);
            }
            else
            {
                dragBoost = buttonPressed ? maxDragBoost : minDragBoost;
            }   

            localLiftVector = referenceTransform.InverseTransformPoint(liftVector);

            localDragVector = referenceTransform.InverseTransformPoint(dragVector);

            localDragVector.x = Mathf.Clamp(localDragVector.x * dragBoost.x, -maxDrag, maxDrag);
            localDragVector.y = Mathf.Clamp(localDragVector.y * dragBoost.y, -maxDrag, maxDrag);
            localDragVector.z = Mathf.Clamp(localDragVector.z * dragBoost.z, -maxDrag, maxDrag);

            dragVector = referenceTransform.TransformPoint(localDragVector);
        }
    }

    public void HandleRigidbodyData(in RigidbodyData _rigidbodyData, float deltaTime) 
    {
        UpdateCoefficients(_rigidbodyData);

        liftForce = liftCoefficient * currentVelocityTimesAirdensityTimesSurfaceArea;
        dragForce = dragCoefficient * currentVelocityTimesAirdensityTimesSurfaceArea;
        //torqueForce = torqueCoefficient * currentVelocityTimesAirdensityTimesSurfaceArea * boardDimensions.z;

        liftVector = CalculateNormalVector(rigidbodyData.Velocity, referenceTransform.right * -1) * liftForce;
        dragVector = dragForce * _rigidbodyData.Velocity * -1f;
    }

    public void Simulate(float deltaTime)
    {
        PhysicsRigidbody.AddForce((liftVector + dragVector) * deltaTime, ForceMode.Impulse);
    }

    private void UpdateCoefficients(RigidbodyData _rigidbodyData)
    {
        rigidbodyData = _rigidbodyData; // BackUp the data

        // Angle of attack
        angleOfAttack = Vector3.Angle(_rigidbodyData.Velocity - WindVector, referenceTransform.forward * -1);
        float angleOfAttackInRadian = Mathf.Deg2Rad * angleOfAttack;

        // Lift and Drag Coefficients
        liftCoefficient = MathF.Abs(angleOfAttack) < stallPoint * 2f ? 
            (Mathf.Sin(angleOfAttackInRadian * 2f) + (Mathf.Sin(angleOfAttackInRadian * 6f))) * floatFactor + minFloat : 
            Mathf.Sin(angleOfAttackInRadian * 2f) * floatFactor + minFloat;

        angleOnDragArea = Vector3.Angle(_rigidbodyData.Velocity - WindVector, referenceTransform.up);

        dragCoefficient = (1f + Mathf.Cos(angleOnDragArea * 2f * Mathf.Deg2Rad)) * dragFactor + minDrag;

        Vector3 relativeVelocity = _rigidbodyData.Velocity + WindVector;

        currentVelocityTimesAirdensityTimesSurfaceArea = 0.5f * AirDensity * relativeVelocity.magnitude * relativeVelocity.magnitude * boardSurfaceArea;
    }

    private Vector3 CalculateNormalVector(Vector3 vectorA, Vector3 vectorB)
    {
        Vector3 normalVector = Vector3.Cross(vectorA, vectorB);

        normalVector.Normalize();

        return normalVector;
    }

}