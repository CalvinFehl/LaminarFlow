using System;
using UnityEngine;

public class PitchYawRollEventData
{
    public Vector3 DeltaPitchYawRoll;
    public Vector3 PitchYawRollAcceleration;
    public Vector3 AccumulatedPitchYawRoll;
    public Vector3 AccumulatedPitchYawRollTime;

    public PitchYawRollEventData()
    {
        DeltaPitchYawRoll = Vector3.zero;
        PitchYawRollAcceleration = Vector3.zero;
        AccumulatedPitchYawRoll = Vector3.zero;
        AccumulatedPitchYawRollTime = Vector3.zero;
    }

    public PitchYawRollEventData(Vector3 deltaPitchYawRoll, Vector3 deltaPitchYawRollAcceleration, Vector3 accumulatedPitchYawRoll, Vector3 accumulatedPitchYawRollTime)
    {
        this.DeltaPitchYawRoll = deltaPitchYawRoll;
        this.PitchYawRollAcceleration = deltaPitchYawRollAcceleration;
        this.AccumulatedPitchYawRoll = accumulatedPitchYawRoll;
        this.AccumulatedPitchYawRollTime = accumulatedPitchYawRollTime;
    }
}

public class RotationAsPitchYawRollInterpreter : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    [SerializeField] private float resetTreshold = 5f;

    public event Action<PitchYawRollEventData> OnPitchYawRollUpdated;
    private PitchYawRollEventData pitchYawRollEventData = new PitchYawRollEventData();

    private Quaternion lastRotation;
    private Quaternion lastDeltaRotation;
    private Vector3 lastDeltaPitchYawRoll;

    private void OnEnable()
    {
        BoardMovementTracker tracker = FindObjectOfType<BoardMovementTracker>();
        if (tracker != null)
        {
            tracker.OnRotationTrackingUpdated += ReceiveRotationTracking;
        }
    }

    private void OnDisable()
    {
        BoardMovementTracker tracker = FindObjectOfType<BoardMovementTracker>();
        if (tracker != null)
        {
            tracker.OnRotationTrackingUpdated -= ReceiveRotationTracking;
        }
    }

    void Start()
    {
        if (_transform == null)
        {
            _transform = transform;
        }
    }

    public void ReceiveRotationTracking(Quaternion[] trackingData)
    {
        Quaternion currentRotation = trackingData[0];
        int stepsBetweenTrackings = trackingData.Length;

        if (stepsBetweenTrackings > 1)
        {
            lastRotation = trackingData[1];

            if (stepsBetweenTrackings > 2)
            {
                lastDeltaRotation = trackingData[1] * Quaternion.Inverse(trackingData[2]);
                Vector3 previousDeltaPitchYawRoll = PitchYawRollAccumulationSort(_transform, lastDeltaRotation, Time.fixedDeltaTime)[0];
                lastDeltaPitchYawRoll = previousDeltaPitchYawRoll;
            }
        }
    
        Quaternion currentDeltaRotation = currentRotation * Quaternion.Inverse(lastRotation);

        //values[0] = DeltaPitchYawRoll, values[1] = PitchYawRollAcceleration, values[2] = AccumulatedPitchYawRoll, values[3] = AccumulatedPitchYawRollTime
        Vector3[] values = PitchYawRollAccumulationSort(_transform, currentDeltaRotation, stepsBetweenTrackings * Time.fixedDeltaTime, lastDeltaPitchYawRoll, pitchYawRollEventData.AccumulatedPitchYawRoll, pitchYawRollEventData.AccumulatedPitchYawRollTime, stepsBetweenTrackings);

        pitchYawRollEventData.DeltaPitchYawRoll = values[0];
        pitchYawRollEventData.PitchYawRollAcceleration = values[1];
        pitchYawRollEventData.AccumulatedPitchYawRoll = values[2];
        pitchYawRollEventData.AccumulatedPitchYawRollTime = values[3];

        OnPitchYawRollUpdated?.Invoke(pitchYawRollEventData);

        lastDeltaRotation = currentDeltaRotation;
        lastRotation = currentRotation;
        lastDeltaPitchYawRoll = values[0];
    }

    private float AxisRotation(Vector3 referenceAxis, float rotationAngle, Vector3 rotationAxis, int stepsBetweenTrackings = 1)
    {
        return rotationAngle * Vector3.Dot(rotationAxis, referenceAxis) * stepsBetweenTrackings;
    }

    public Vector3[] PitchYawRollAccumulationSort(Transform transform, Quaternion currentDeltaRotation, float timeBetweenTrackings = 0f, Vector3? latestDeltaPitchYawRoll = null, Vector3? accumulatedPitchYawRollSoFar = null, Vector3? accumulatedPitchYawRollTimingSoFar = null, int stepsBetweenTrackings = 1)
    {
        bool gotLatestDeltaPitchYawRoll = latestDeltaPitchYawRoll.HasValue;
        bool gotAccumulatedPitchYawRollSoFar = accumulatedPitchYawRollSoFar.HasValue;
        bool gotAccumulatedPitchYawRollTimingSoFar = accumulatedPitchYawRollTimingSoFar.HasValue;


        //get the angle and axis of the rotation
        currentDeltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        axis.Normalize();

        //turn Vector3s into float arrays for int-iteration
        float[] deltaPitchYawRoll = new float[3];
        float[] previousDeltaPitchYawRoll = new float[3];
        float[] accumulatedPitchYawRoll = new float[3];
        float[] accumulatedPitchYawRollTiming = new float[3];

        if (gotLatestDeltaPitchYawRoll) { previousDeltaPitchYawRoll = Vector3ToArray(latestDeltaPitchYawRoll.Value); }
        if (gotAccumulatedPitchYawRollSoFar) { accumulatedPitchYawRoll = Vector3ToArray(accumulatedPitchYawRollSoFar.Value); }
        if (gotAccumulatedPitchYawRollTimingSoFar) { accumulatedPitchYawRollTiming = Vector3ToArray(accumulatedPitchYawRollTimingSoFar.Value); }

        for (int i = 0; i < 3; i++)
        {
            float deltaPitchOrYawOrRoll = 0.0f;
            Vector3 referenceAxis = Vector3.zero;
            if (i == 0) { referenceAxis = transform.right; }
            else if (i == 1) { referenceAxis = transform.up; }
            else if (i == 2) { referenceAxis = transform.forward; }

            //calculate the delta pitch, yaw or roll
            deltaPitchOrYawOrRoll = AxisRotation(referenceAxis, angle, axis, stepsBetweenTrackings);
            deltaPitchYawRoll[i] = deltaPitchOrYawOrRoll;

            //determine if the delta pitch, yaw or roll is in the same direction as the previous one (reference value)
            float referenceValue = 0f;
            if (gotAccumulatedPitchYawRollSoFar) { referenceValue = accumulatedPitchYawRoll[i]; }
            else if (gotLatestDeltaPitchYawRoll) { referenceValue = previousDeltaPitchYawRoll[i]; }

            bool sameDirection = deltaPitchOrYawOrRoll >= 0f && referenceValue >= 0f || deltaPitchOrYawOrRoll <= 0f && referenceValue <= 0f;

            if (gotAccumulatedPitchYawRollSoFar)
            {
                accumulatedPitchYawRoll[i] = sameDirection ? accumulatedPitchYawRoll[i] + deltaPitchOrYawOrRoll : deltaPitchOrYawOrRoll;
            }

            if (gotAccumulatedPitchYawRollTimingSoFar && timeBetweenTrackings != 0f)
            {
                if (!sameDirection)
                {
                    accumulatedPitchYawRollTiming[i] = 0f;
                }
                if (deltaPitchOrYawOrRoll != 0f || accumulatedPitchYawRollTiming[i] != 0f)
                {
                    accumulatedPitchYawRollTiming[i] += timeBetweenTrackings;
                }
            }
        }

        Vector3[] values = new Vector3[4];

        values[0] = new Vector3(deltaPitchYawRoll[0], deltaPitchYawRoll[1], deltaPitchYawRoll[2]);

        if (gotLatestDeltaPitchYawRoll)
        {
            values[1] = values[0] - latestDeltaPitchYawRoll.Value;
        }
        if (gotAccumulatedPitchYawRollSoFar)
        {
            values[2] = new Vector3(accumulatedPitchYawRoll[0], accumulatedPitchYawRoll[1], accumulatedPitchYawRoll[2]);
        }
        if (gotAccumulatedPitchYawRollTimingSoFar)
        {
            values[3] = new Vector3(accumulatedPitchYawRollTiming[0], accumulatedPitchYawRollTiming[1], accumulatedPitchYawRollTiming[2]);
        }

        return values;
    }

    private float[] Vector3ToArray(Vector3 vector)
    {
        return new float[] { vector.x, vector.y, vector.z };
    }

    public void ResetCombo(float? deltaValue = 0f, Vector3? deltaValues = default)
    {
        if (deltaValues != null)
        {
            if (Mathf.Abs(deltaValues.Value.x) > resetTreshold)
            {
                pitchYawRollEventData.AccumulatedPitchYawRoll.x = 0f;
                pitchYawRollEventData.AccumulatedPitchYawRollTime.x = 0f;            
            }
            if (Mathf.Abs(deltaValues.Value.y) > resetTreshold)
            {
                pitchYawRollEventData.AccumulatedPitchYawRoll.y = 0f;
                pitchYawRollEventData.AccumulatedPitchYawRollTime.y = 0f;            
            }
            if (Mathf.Abs(deltaValues.Value.z) > resetTreshold)
            {
                pitchYawRollEventData.AccumulatedPitchYawRoll.z = 0f;
                pitchYawRollEventData.AccumulatedPitchYawRollTime.z = 0f;            
            }
        }
        else if (!deltaValue.HasValue || Mathf.Abs(deltaValue.Value) > resetTreshold)
        {
            pitchYawRollEventData.AccumulatedPitchYawRoll = Vector3.zero;
            pitchYawRollEventData.AccumulatedPitchYawRollTime = Vector3.zero;
        }
    }
}
