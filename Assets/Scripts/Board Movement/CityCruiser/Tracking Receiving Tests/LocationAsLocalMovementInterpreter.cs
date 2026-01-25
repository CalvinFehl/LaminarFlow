using System;
using UnityEngine;

public class LocationAsLocalMovementEventData
{
    public Vector3 DeltaLocation;
    public Vector3 LocationAcceleration;
    public Vector3 AccumulatedLocation;
    public Vector3 AccumulatedLocationTime;

    public LocationAsLocalMovementEventData()
    {
        DeltaLocation = Vector3.zero;
        LocationAcceleration = Vector3.zero;
        AccumulatedLocation = Vector3.zero;
        AccumulatedLocationTime = Vector3.zero;
    }

    public LocationAsLocalMovementEventData(Vector3 deltaLocation, Vector3 deltaLocationAcceleration, Vector3 accumulatedLocation, Vector3 accumulatedLocationTime)
    {
        this.DeltaLocation = deltaLocation;
        this.LocationAcceleration = deltaLocationAcceleration;
        this.AccumulatedLocation = accumulatedLocation;
        this.AccumulatedLocationTime = accumulatedLocationTime;
    }
}
public class LocationAsLocalMovementInterpreter : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    [SerializeField] private float resetTreshold = 5f;

    public event Action<LocationAsLocalMovementEventData> OnRelativeLocationUpdated;
    private LocationAsLocalMovementEventData locationAsLocalMovementEventData = new LocationAsLocalMovementEventData();

    private Vector3 lastLocation;
    private Vector3 lastDeltaLocation;
    private Vector3 lastLocalDeltaLocation;

    private void OnEnable()
    {
        BoardMovementTracker tracker = FindObjectOfType<BoardMovementTracker>();
        if (tracker != null)
        {
            tracker.OnPositionTrackingUpdated += ReceiveLocationTracking;
        }
    }

    private void OnDisable()
    {
        BoardMovementTracker tracker = FindObjectOfType<BoardMovementTracker>();
        if (tracker != null)
        {
            tracker.OnPositionTrackingUpdated -= ReceiveLocationTracking;
        }
    }

    void Start()
    {
        if (_transform == null)
        {
            _transform = transform;
        }
    }

    public void ReceiveLocationTracking(Vector3[] trackingData)
    {
        Vector3 currentLocation = trackingData[0];
        int stepsBetweenTrackings = trackingData.Length;

        if (stepsBetweenTrackings > 1)
        {
            lastLocation = trackingData[1];

            if (stepsBetweenTrackings > 2)
            {
                lastDeltaLocation = lastLocation - trackingData[2];
                Vector3 previousLocalDeltaLocation = LocationAsLocalMovementAccumulationSort(_transform, lastDeltaLocation, Time.fixedDeltaTime)[0];
                lastLocalDeltaLocation = previousLocalDeltaLocation;
            }
        }

        Vector3 deltaLocation = currentLocation - lastLocation;
        Vector3 deltaLocationAcceleration = deltaLocation - lastDeltaLocation;

        //values[0] = localDeltaLocation, values[1] = localLocationAcceleration values[2] = accumulatedLocation, values[3] = accumulatedLocationTiming
        Vector3[] values = LocationAsLocalMovementAccumulationSort(_transform, deltaLocation, stepsBetweenTrackings * Time.fixedDeltaTime, lastLocalDeltaLocation, locationAsLocalMovementEventData.AccumulatedLocation, locationAsLocalMovementEventData.AccumulatedLocationTime, stepsBetweenTrackings);

        locationAsLocalMovementEventData.DeltaLocation = values[0];
        locationAsLocalMovementEventData.LocationAcceleration = values[1];
        locationAsLocalMovementEventData.AccumulatedLocation = values[2];
        locationAsLocalMovementEventData.AccumulatedLocationTime = values[3];

        OnRelativeLocationUpdated?.Invoke(locationAsLocalMovementEventData);

        lastLocation = currentLocation;
        lastDeltaLocation = deltaLocation;
    }

    public Vector3[] LocationAsLocalMovementAccumulationSort(Transform transform, Vector3 currentDeltaLocation, float timeBetweenTrackings = 0f, Vector3? latestLocalDeltaLocation = null, Vector3? accumulatedLocationSoFar = null, Vector3? accumulatedLocationTimingSoFar = null, int stepsBetweenTrackings = 1)
    {
        bool gotLatestLocalDeltaLocation = latestLocalDeltaLocation.HasValue;
        bool gotAccumulatedLocationSoFar = accumulatedLocationSoFar.HasValue;
        bool gotAccumulatedLocationTimingSoFar = accumulatedLocationTimingSoFar.HasValue;

        Vector3[] values = new Vector3[4];

        //turn Vector3s into float arrays for int-iteration
        Vector3 localDeltaLocationVector = transform.InverseTransformDirection(currentDeltaLocation);
        float[] localDeltaLocation = Vector3ToArray(localDeltaLocationVector);
        float[] previousLocalDeltaLocation = new float[3];
        float[] accumulatedLocation = new float[3];
        float[] accumulatedLocationTiming = new float[3];

        if (gotLatestLocalDeltaLocation) { previousLocalDeltaLocation = Vector3ToArray(latestLocalDeltaLocation.Value); }
        if (gotAccumulatedLocationSoFar) { accumulatedLocation = Vector3ToArray(accumulatedLocationSoFar.Value); }
        if (gotAccumulatedLocationTimingSoFar) { accumulatedLocationTiming = Vector3ToArray(accumulatedLocationTimingSoFar.Value); }

        for (int i = 0; i < 3; i++)
        {
            float referenceValue = 0f;
            if (gotAccumulatedLocationSoFar) { referenceValue = accumulatedLocation[i]; }
            else if (gotLatestLocalDeltaLocation) { referenceValue = previousLocalDeltaLocation[i]; }

            bool sameDirection = (localDeltaLocation[i] >= 0f && referenceValue >= 0f) || (localDeltaLocation[i] <= 0f && referenceValue <= 0f);

            if (gotAccumulatedLocationSoFar)
            {
                accumulatedLocation[i] = sameDirection ? accumulatedLocation[i] + localDeltaLocation[i] : localDeltaLocation[i];
            }

            if (gotAccumulatedLocationTimingSoFar && timeBetweenTrackings != 0f)
            {
                if (!sameDirection)
                {
                    accumulatedLocationTiming[i] = 0f;
                }
                if (localDeltaLocation[i] != 0f || accumulatedLocationTiming[i] != 0f)
                {
                    accumulatedLocationTiming[i] += timeBetweenTrackings;
                }
            }
        }

        values[0] = localDeltaLocationVector;

        if (gotLatestLocalDeltaLocation) 
        {
            values[1] = values[0] - latestLocalDeltaLocation.Value;
        }
        if (gotAccumulatedLocationSoFar)
        {
            values[2] = new Vector3(accumulatedLocation[0], accumulatedLocation[1], accumulatedLocation[2]);
        }
        if (gotAccumulatedLocationTimingSoFar)
        {
            values[3] = new Vector3(accumulatedLocationTiming[0], accumulatedLocationTiming[1], accumulatedLocationTiming[2]);
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
                locationAsLocalMovementEventData.AccumulatedLocation.x = 0f;
                locationAsLocalMovementEventData.AccumulatedLocationTime.x = 0f;
            }
            if (Mathf.Abs(deltaValues.Value.y) > resetTreshold)
            {
                locationAsLocalMovementEventData.AccumulatedLocation.y = 0f;
                locationAsLocalMovementEventData.AccumulatedLocationTime.y = 0f;
            }
            if (Mathf.Abs(deltaValues.Value.z) > resetTreshold)
            {
                locationAsLocalMovementEventData.AccumulatedLocation.z = 0f;
                locationAsLocalMovementEventData.AccumulatedLocationTime.z = 0f;
            }
        }
        else if (!deltaValue.HasValue || Mathf.Abs(deltaValue.Value) > resetTreshold)
        {
            locationAsLocalMovementEventData.AccumulatedLocation = Vector3.zero;
            locationAsLocalMovementEventData.AccumulatedLocationTime = Vector3.zero;
        }
    }
}
