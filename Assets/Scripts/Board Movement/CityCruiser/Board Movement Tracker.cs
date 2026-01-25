using UnityEngine;
using System;

public class BoardMovementTracker : MonoBehaviour
{
    public event Action<Vector3[]> OnPositionTrackingUpdated;
    public event Action<Quaternion[]> OnRotationTrackingUpdated;

    [SerializeField] private Transform trackedObject;
    [SerializeField] private Rigidbody trackedRigidbody;
    [SerializeField] private int stepsBetweenSends = 30;

    private Vector3[] trackedPositions;
    private Quaternion[] trackedRotations;

    private int currentStep;

    private void Awake()
    {
        if (trackedObject == null)
        {
            trackedObject = GetComponentInParent<Transform>();
            if (trackedObject == null)
            {
                Debug.LogError("No Transform found to track");
            }
            else
            {
                trackedRigidbody = trackedObject.GetComponent<Rigidbody>();
                if(trackedRigidbody == null)
                {
                    Debug.LogError("No Rigidbody found to track");
                }
            }
        }

        trackedPositions = new Vector3[stepsBetweenSends];
        trackedRotations = new Quaternion[stepsBetweenSends];
        currentStep = 0;
    }

    private void UpdatePositionTracking()
    {
        Vector3 currentPosition = trackedRigidbody != null ? trackedRigidbody.transform.position : trackedObject.transform.position;
        trackedPositions[currentStep] = currentPosition;
    }

    private void UpdateRotationTracking()
    {
        Quaternion currentRotation = trackedRigidbody != null ? trackedRigidbody.transform.rotation : trackedObject.transform.rotation;
        trackedRotations[currentStep] = currentRotation;
    }

    private void FixedUpdate()
    {
        //Debug.Log(trackedPositions[currentStep]);

        UpdatePositionTracking();
        UpdateRotationTracking();

        currentStep++;

        if (currentStep >= stepsBetweenSends)
        {
            //Debug.Log("Sending tracking data");

            OnPositionTrackingUpdated?.Invoke(trackedPositions);
            OnRotationTrackingUpdated?.Invoke(trackedRotations);

            currentStep = 0;
        }
    }
}
