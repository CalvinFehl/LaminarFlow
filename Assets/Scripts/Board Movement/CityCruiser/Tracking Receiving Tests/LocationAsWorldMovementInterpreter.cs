using System;
using UnityEngine;

public class LocationAsWorldMovementEventData
{
    public Vector3 DeltaLocation;
    public Vector3 LocationAcceleration;
    public Vector3 AccumulatedLocation;
    public Vector3 AccumulatedLocationTime;

    public LocationAsWorldMovementEventData()
    {
        DeltaLocation = Vector3.zero;
        LocationAcceleration = Vector3.zero;
        AccumulatedLocation = Vector3.zero;
        AccumulatedLocationTime = Vector3.zero;
    }

    public LocationAsWorldMovementEventData(Vector3 deltaLocation, Vector3 deltaLocationAcceleration, Vector3 accumulatedLocation, Vector3 accumulatedLocationTime)
    {
        this.DeltaLocation = deltaLocation;
        this.LocationAcceleration = deltaLocationAcceleration;
        this.AccumulatedLocation = accumulatedLocation;
        this.AccumulatedLocationTime = accumulatedLocationTime;
    }
}
public class LocationAsWorldMovementInterpreter : MonoBehaviour
{
    [SerializeField] private float resetTreshold = 5f;

    public event Action<LocationAsWorldMovementEventData> OnLocationUpdated;
    private LocationAsWorldMovementEventData locationAsWorldMovementEventData = new LocationAsWorldMovementEventData();

    private Vector3 lastLocation;
    private Vector3 lastDeltaLocation;

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
            }
        }

        Vector3 deltaLocation = currentLocation - lastLocation;
        Vector3 deltaLocationAcceleration = deltaLocation - lastDeltaLocation;

        //values[0] = accumulatedLocation, values[1] = accumulatedLocationTiming
        Vector3[] values = LocationAsWorldMovementAccumulationSort(deltaLocation, stepsBetweenTrackings * Time.fixedDeltaTime, lastDeltaLocation, locationAsWorldMovementEventData.AccumulatedLocation, locationAsWorldMovementEventData.AccumulatedLocationTime, stepsBetweenTrackings);

        locationAsWorldMovementEventData.DeltaLocation = deltaLocation;
        locationAsWorldMovementEventData.LocationAcceleration = deltaLocationAcceleration / Time.fixedDeltaTime;
        locationAsWorldMovementEventData.AccumulatedLocation = values[0];
        locationAsWorldMovementEventData.AccumulatedLocationTime = values[1];

        OnLocationUpdated?.Invoke(locationAsWorldMovementEventData);

        lastLocation = currentLocation;
        lastDeltaLocation = deltaLocation;
    }

    public Vector3[] LocationAsWorldMovementAccumulationSort(Vector3 currentDeltaLocation, float timeBetweenTrackings = 0f, Vector3? latestDeltaLocation = null, Vector3? accumulatedLocationSoFar = null, Vector3? accumulatedLocationTimingSoFar = null, int stepsBetweenTrackings = 1)
    {
        bool gotLatestDeltaLocation = latestDeltaLocation.HasValue;
        bool gotAccumulatedLocationSoFar = accumulatedLocationSoFar.HasValue;
        bool gotAccumulatedLocationTimingSoFar = accumulatedLocationTimingSoFar.HasValue;

        Vector3[] values = new Vector3[2];
        if (!gotAccumulatedLocationSoFar && !gotAccumulatedLocationTimingSoFar) 
        {
            values[0] = currentDeltaLocation;
            values[1] = new Vector3 (timeBetweenTrackings, timeBetweenTrackings, timeBetweenTrackings);

            return values; 
        }

        //turn Vector3s into float arrays for int-iteration
        float[] deltaLocation = Vector3ToArray(currentDeltaLocation);
        float[] previousDeltaLocation = new float[3];
        float[] accumulatedLocation = new float[3];
        float[] accumulatedLocationTiming = new float[3];

        if (gotLatestDeltaLocation) { previousDeltaLocation = Vector3ToArray(latestDeltaLocation.Value); }
        if (gotAccumulatedLocationSoFar) { accumulatedLocation = Vector3ToArray(accumulatedLocationSoFar.Value); }
        if (gotAccumulatedLocationTimingSoFar) { accumulatedLocationTiming = Vector3ToArray(accumulatedLocationTimingSoFar.Value); }

        for (int i = 0; i < 3; i++)
        {
            float referenceValue = 0f;
            if (gotAccumulatedLocationSoFar) { referenceValue = accumulatedLocation[i]; }
            else if (gotLatestDeltaLocation) { referenceValue = previousDeltaLocation[i]; }

            bool sameDirection = (deltaLocation[i] >= 0f && referenceValue >= 0f) || (deltaLocation[i] <= 0f && referenceValue <= 0f);

            if (gotAccumulatedLocationSoFar)
            {
                accumulatedLocation[i] = sameDirection ? accumulatedLocation[i] + deltaLocation[i] : deltaLocation[i];
            }

            if (gotAccumulatedLocationTimingSoFar && timeBetweenTrackings != 0f) 
            {
                if (!sameDirection)
                {
                    accumulatedLocationTiming[i] = 0f;
                }
                if (deltaLocation[i] != 0f || accumulatedLocationTiming[i] != 0f)
                {
                    accumulatedLocationTiming[i] += timeBetweenTrackings;
                }
            }
        }

        if (gotAccumulatedLocationSoFar)
        {
            values[0] = new Vector3(accumulatedLocation[0], accumulatedLocation[1], accumulatedLocation[2]);
        }
        if (gotAccumulatedLocationTimingSoFar)
        {
            values[1] = new Vector3(accumulatedLocationTiming[0], accumulatedLocationTiming[1], accumulatedLocationTiming[2]);
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
                locationAsWorldMovementEventData.AccumulatedLocation.x = 0f;
                locationAsWorldMovementEventData.AccumulatedLocationTime.x = 0f;
            }
            if (Mathf.Abs(deltaValues.Value.y) > resetTreshold)
            {
                locationAsWorldMovementEventData.AccumulatedLocation.y = 0f;
                locationAsWorldMovementEventData.AccumulatedLocationTime.y = 0f;
            }
            if (Mathf.Abs(deltaValues.Value.z) > resetTreshold)
            {
                locationAsWorldMovementEventData.AccumulatedLocation.z = 0f;
                locationAsWorldMovementEventData.AccumulatedLocationTime.z = 0f;
            }
        }
        else if (!deltaValue.HasValue || Mathf.Abs(deltaValue.Value) > resetTreshold)
        {
            locationAsWorldMovementEventData.AccumulatedLocation = Vector3.zero;
            locationAsWorldMovementEventData.AccumulatedLocationTime = Vector3.zero;
        }
    }
}
