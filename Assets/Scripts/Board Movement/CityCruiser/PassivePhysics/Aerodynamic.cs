using System;
using UnityEngine;

public class Aerodynamic : MonoBehaviour
{
    private float liftForce;
    private float dragForce;
    //private float torqueForce;

    public Vector3 LiftAndDrag;

    private float angleOfAttack;
    private float angleOnDragArea;
    [SerializeField] private float stallPoint = 15f;
    [SerializeField] private float minimumFloat = 0.25f;
    [SerializeField] private float floatFactor = 0.1f;

    [SerializeField] private float minimumDrag = 0.05f;
    [SerializeField] private float dragFactor = 0.5f;

    [SerializeField] private float liftCoefficient;
    [SerializeField] private float dragCoefficient;
    [SerializeField] private float torqueCoefficient;

    [SerializeField] private float airDensity = 1.1f;
    private Vector3 windVector;

    public Vector3 boardDimensions;
    private float boardSurfaceArea;

    private Rigidbody boardRigidbody;
    float currentVelocityTimesAirdensityTimesSurfaceArea;

    void Start()
    {
        boardSurfaceArea = boardDimensions.x * boardDimensions.z;


        if (boardRigidbody == null)
        {
            boardRigidbody = GetComponent<Rigidbody>();
        }
    }

    void UpdateCoefficients()
    {
        angleOfAttack =  Vector3.Angle(boardRigidbody.linearVelocity - windVector, transform.forward * -1);
        float angleOfAttackInRadian = Mathf.Deg2Rad * angleOfAttack;
        liftCoefficient = MathF.Abs(angleOfAttack) < stallPoint * 2f ? (Mathf.Sin(angleOfAttackInRadian * 2f) + (Mathf.Sin(angleOfAttackInRadian * 6f))) * floatFactor + minimumFloat : Mathf.Sin(angleOfAttackInRadian * 2f) * floatFactor + minimumFloat;

        angleOnDragArea = Vector3.Angle(boardRigidbody.linearVelocity - windVector, transform.up);
        dragCoefficient = (1f + Mathf.Cos(angleOnDragArea * 2f * Mathf.Deg2Rad)) * dragFactor + minimumDrag;


            
        Vector3 relativeVelocity = boardRigidbody.linearVelocity + windVector;
        currentVelocityTimesAirdensityTimesSurfaceArea = 0.5f * airDensity * relativeVelocity.magnitude * relativeVelocity.magnitude * boardSurfaceArea;
    }

    //float CurrentTorqueForce() { return torqueForce; }

    public static Vector3 CalculateNormalVector(Vector3 vectorA, Vector3 vectorB)
    {
        Vector3 normalVector = Vector3.Cross(vectorA, vectorB);

        normalVector.Normalize();

        return normalVector;
    }


    public void FixedUpdate()
    {
        if(boardRigidbody != null)
        {
            UpdateCoefficients();
            liftForce = liftCoefficient * currentVelocityTimesAirdensityTimesSurfaceArea;
            dragForce = dragCoefficient * currentVelocityTimesAirdensityTimesSurfaceArea;
            //torqueForce = torqueCoefficient * currentVelocityTimesAirdensityTimesSurfaceArea * boardDimensions.z;
            LiftAndDrag = CalculateNormalVector(boardRigidbody.linearVelocity, transform.right) * liftForce + dragForce * boardRigidbody.linearVelocity * -1f;
            boardRigidbody.AddForce(LiftAndDrag);
        }
    }
}