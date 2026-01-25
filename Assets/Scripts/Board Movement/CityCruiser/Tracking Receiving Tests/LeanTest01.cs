using UnityEngine;

public class LeanTest01 : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    private Vector3 lastPosition;
    private Vector3 lastMovement;
    [SerializeField] private float accelerationInaccuracy = 0.1f;

    private void OnEnable()
    {
        // Registriere dich auf die Events des Trackers
        BoardMovementTracker tracker = FindObjectOfType<BoardMovementTracker>();
        if (tracker != null)
        {
            tracker.OnPositionTrackingUpdated += ReceivePositionTracking;
        }
    }

    private void OnDisable()
    {
        // Deregistriere dich von den Events
        BoardMovementTracker tracker = FindObjectOfType<BoardMovementTracker>();
        if (tracker != null)
        {
            tracker.OnPositionTrackingUpdated -= ReceivePositionTracking;
        }
    }

    public void Awake()
    {
        if(pivot == null)
        {
            pivot = transform;
        }
    }

    public void ReceivePositionTracking(Vector3[] trackingData)
    {
        Vector3 currentPosition = trackingData[0];
        Vector3 currentMovement = Vector3.zero;

        if (trackingData.Length > 1)
        {
            lastPosition = trackingData[1];
        }

        if (lastPosition == Vector3.zero)
        {
            lastPosition = currentPosition;
        }
        else
        {
            currentMovement = currentPosition - lastPosition;
        }

        if (lastMovement == Vector3.zero)
        {
            lastMovement = currentMovement;
        }
        else
        {
            /*if (Vector3.Distance(currentMovement, lastMovement) > accelerationInaccuracy)
            {
                //Debug.Log("Acceleration detected: " + Vector3.Distance(currentMovement, lastMovement));
            }*/

            if (pivot != null)
            {
                // Transform movements into the local space of the pivot
                Vector3 localLastMovement = pivot.InverseTransformVector(lastMovement);
                Vector3 localCurrentMovement = pivot.InverseTransformVector(currentMovement);

                // Determine the difference in Z direction (forward/backward movement)
                float deltaZ = localCurrentMovement.z - localLastMovement.z;
                float tiltAngle = 0f;

                if (deltaZ > accelerationInaccuracy)
                {
                    //Debug.Log("Accelerating");
                    tiltAngle = 25f;
                }
                else if (deltaZ < -accelerationInaccuracy)
                {
                    //Debug.Log("Decelerating");
                    tiltAngle = -25f;
                }

                // Apply the rotation smoothly
                Quaternion targetRotation = Quaternion.Euler(tiltAngle, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 5f); // 5f ist der Lerp-Faktor, kannst du anpassen
            }
        }

        lastMovement = currentMovement;
        lastPosition = currentPosition;
    }
}
