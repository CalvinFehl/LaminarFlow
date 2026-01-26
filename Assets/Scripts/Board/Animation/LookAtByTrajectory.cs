using UnityEngine;

public class LookAtByTrajectory : MonoBehaviour
{
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Transform headPosition;

    [SerializeField] private Vector3 lastPosition;
    void Start()
    {
        if (lookAtTarget == null)
        {
            GameObject target = GetComponentInChildren<Transform>()?.gameObject;

            if (target != gameObject && target != null)
            {
                lookAtTarget = target.transform;
            }
        }

        if (headPosition == null)
        {
            GameObject headTarget = GameObject.Find("Head_target");

            if (headTarget != null)
            {
                headPosition = headTarget.transform;
            }
        }
    }

    void FixedUpdate()
    {
        if(lookAtTarget != null && lastPosition != null && headPosition != null)
        {   
            Vector3 lookAtDirection = (transform.position - lastPosition).normalized;
            Vector3 localLookAtDirection = headPosition.InverseTransformDirection(lookAtDirection);
            //Debug.Log(localLookAtDirection);
            Vector3 correctedLocalLookAtDirection = new Vector3(localLookAtDirection.x, localLookAtDirection.y, Mathf.Clamp01(localLookAtDirection.z));
            //Debug.Log("Corrected to: " + correctedLocalLookAtDirection);
            lookAtDirection = headPosition.TransformDirection(correctedLocalLookAtDirection.normalized);
            lookAtTarget.position = headPosition.position + lookAtDirection;

        }

        lastPosition = transform.position;
    }
}
