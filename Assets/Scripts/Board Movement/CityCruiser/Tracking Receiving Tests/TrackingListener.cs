using UnityEngine;

public class TrackingListener : MonoBehaviour
{
    private void OnEnable()
    {
        // Registriere dich auf die Events des Trackers
        BoardMovementTracker tracker = FindObjectOfType<BoardMovementTracker>();
        if (tracker != null)
        {
            tracker.OnPositionTrackingUpdated += ReceivePositionTracking;
            tracker.OnRotationTrackingUpdated += ReceiveRotationTracking;
        }
    }

    private void OnDisable()
    {
        // Deregistriere dich von den Events
        BoardMovementTracker tracker = FindObjectOfType<BoardMovementTracker>();
        if (tracker != null)
        {
            tracker.OnPositionTrackingUpdated -= ReceivePositionTracking;
            tracker.OnRotationTrackingUpdated -= ReceiveRotationTracking;
        }
    }

    public void ReceiveTracking(Vector3[] trackingData)
    {
        // Implementiere spezifische Logik hier, falls notwendig
    }

    public void ReceivePositionTracking(Vector3[] trackingData)
    {
        Debug.Log("Position tracking received: " + trackingData[0]);
    }

    public void ReceiveRotationTracking(Quaternion[] trackingData)
    {
        Debug.Log("Rotation tracking received: " + trackingData[0]);
    }
}
