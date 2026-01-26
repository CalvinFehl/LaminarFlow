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
public class LocationAsLocalMovementInterpreter : MonoBehaviour, IHandleRigidbodyData
{
    [SerializeField] private bool autoUpdate = false;

    [Header("Runtime Variables")]
    [SerializeField] private Vector3 lastLocation;
    [SerializeField] private Vector3 lastDeltaLocation;
    [SerializeField] private Vector3 lastLocalDeltaLocation;
    [SerializeField] private Vector3 localAcceleration;

    [Header("Settings")]
    [Tooltip("Can be Null, if you get Location from a different Source (e.g. Flow Controller)")]
    [SerializeField] private BaseTracker tracker;

    [Tooltip("Can be Null, if you want to use the Interpreter Object Transform or get the Location from somewhere else")]
    [SerializeField] private Transform _referenceTransform;

    [SerializeField] private float resetTreshold = 5f;

    public event Action<LocationAsLocalMovementEventData> OnRelativeLocationUpdated;
    private LocationAsLocalMovementEventData locationAsLocalMovementEventData = new LocationAsLocalMovementEventData();

    private void OnEnable()
    {
        if (tracker != null)
        {
            tracker.OnPositionUpdated += ReceiveLocationTracking;
        }
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.OnPositionUpdated -= ReceiveLocationTracking;
        }
    }

    void Start()
    {
        if (_referenceTransform == null)
        {
            _referenceTransform = transform;
        }
    }

    private void FixedUpdate()
    {
        if (autoUpdate)
        {
            ReceiveLocationTracking(_referenceTransform.position, Time.fixedDeltaTime);
        }
    }

    public void HandleRigidbodyData(in RigidbodyData rigidbodyData, float deltaTime)
    {
        if (autoUpdate) return;
        ReceiveLocationTracking(rigidbodyData.Position, deltaTime);
    }

    public void ReceiveLocationTracking(Vector3 currentLocation, float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        Vector3 deltaLocation = currentLocation - lastLocation;
        Vector3 localDeltaLocation = _referenceTransform.InverseTransformDirection(deltaLocation);

        // Korrekte Berechnung der lokalen Beschleunigung
        Vector3 localDeltaLocationPrev = lastLocalDeltaLocation;
        Vector3 localAccel = (localDeltaLocation - localDeltaLocationPrev) / deltaTime;

        // Werte für EventData berechnen
        Vector3[] values = LocationAsLocalMovementAccumulationSort(
            _referenceTransform,
            deltaLocation,
            deltaTime,
            lastLocalDeltaLocation,
            locationAsLocalMovementEventData.AccumulatedLocation,
            locationAsLocalMovementEventData.AccumulatedLocationTime
        );

        locationAsLocalMovementEventData.DeltaLocation = localDeltaLocation;
        locationAsLocalMovementEventData.LocationAcceleration = localAccel;
        locationAsLocalMovementEventData.AccumulatedLocation = values[2];
        locationAsLocalMovementEventData.AccumulatedLocationTime = values[3];

        OnRelativeLocationUpdated?.Invoke(locationAsLocalMovementEventData);

        lastLocation = currentLocation;
        lastDeltaLocation = deltaLocation;
        lastLocalDeltaLocation = localDeltaLocation;
        localAcceleration = localAccel;
    }

    public Vector3[] LocationAsLocalMovementAccumulationSort(
        Transform transform,
        Vector3 currentDeltaLocation,
        float deltaTime = 0f,
        Vector3? latestLocalDeltaLocation = null,
        Vector3? accumulatedLocationSoFar = null,
        Vector3? accumulatedLocationTimingSoFar = null,
        int stepsBetweenTrackings = 1)
    {
        bool gotLatestLocalDeltaLocation = latestLocalDeltaLocation.HasValue;
        bool gotAccumulatedLocationSoFar = accumulatedLocationSoFar.HasValue;
        bool gotAccumulatedLocationTimingSoFar = accumulatedLocationTimingSoFar.HasValue;

        Vector3[] values = new Vector3[4];

        Vector3 localDeltaLocationVector = transform.InverseTransformDirection(currentDeltaLocation);
        float[] localDeltaLocation = Vector3ToArray(localDeltaLocationVector);
        float[] previousLocalDeltaLocation = gotLatestLocalDeltaLocation ? Vector3ToArray(latestLocalDeltaLocation.Value) : new float[3];
        float[] accumulatedLocation = gotAccumulatedLocationSoFar ? Vector3ToArray(accumulatedLocationSoFar.Value) : new float[3];
        float[] accumulatedLocationTiming = gotAccumulatedLocationTimingSoFar ? Vector3ToArray(accumulatedLocationTimingSoFar.Value) : new float[3];

        for (int i = 0; i < 3; i++)
        {
            float referenceValue = gotAccumulatedLocationSoFar ? accumulatedLocation[i] : (gotLatestLocalDeltaLocation ? previousLocalDeltaLocation[i] : 0f);
            bool sameDirection = (localDeltaLocation[i] >= 0f && referenceValue >= 0f) || (localDeltaLocation[i] <= 0f && referenceValue <= 0f);

            if (gotAccumulatedLocationSoFar)
            {
                accumulatedLocation[i] = sameDirection ? accumulatedLocation[i] + localDeltaLocation[i] : localDeltaLocation[i];
            }

            if (gotAccumulatedLocationTimingSoFar && deltaTime != 0f)
            {
                if (!sameDirection)
                {
                    accumulatedLocationTiming[i] = 0f;
                }
                if (localDeltaLocation[i] != 0f || accumulatedLocationTiming[i] != 0f)
                {
                    accumulatedLocationTiming[i] += deltaTime;
                }
            }
        }

        values[0] = localDeltaLocationVector;
        values[1] = gotLatestLocalDeltaLocation && deltaTime != 0f
            ? (localDeltaLocationVector - latestLocalDeltaLocation.Value) / deltaTime
            : Vector3.zero;
        values[2] = new Vector3(accumulatedLocation[0], accumulatedLocation[1], accumulatedLocation[2]);
        values[3] = new Vector3(accumulatedLocationTiming[0], accumulatedLocationTiming[1], accumulatedLocationTiming[2]);

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
