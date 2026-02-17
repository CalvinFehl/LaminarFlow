using Obi;
using UnityEngine;

public class SimpleObiGrapple : MonoBehaviour, IReferenceRigidbody, IHandleInput
{
    public Rigidbody PhysicsRigidbody { get; set; }
    [Header("Grapple")]
    public Transform targetTransform;
    public float maxDistance = 30f;
    public LayerMask grappleMask;

    [Header("Obi")]
    public ObiSolver solver;
    public ObiRope ropePrefab;
    public ObiRopeBlueprint ropeBlueprint;

    [Header("Rope Length Control")]
    public float ropeAdjustSpeed = 5f;

    private ObiRope ropeInstance;
    private ObiRopeCursor cursor;

    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        if (Input.GetMouseButtonDown(1))
            ShootGrapple();

        if (ropeInstance != null)
            HandleRopeLength();
    }

    void ShootGrapple()
    {
        RaycastHit hit;

        Vector3 origin = targetTransform.position;
        Vector3 direction = targetTransform.up;

        if (Physics.Raycast(origin, direction, out hit, maxDistance, grappleMask))
        {
            SpawnRope(hit);
        }
    }

    void SpawnRope(RaycastHit hit)
    {
        ropeInstance = Instantiate(ropePrefab, solver.transform);
        ropeInstance.ropeBlueprint = ropeBlueprint;
        ropeInstance.transform.position = Vector3.zero;

        cursor = ropeInstance.GetComponent<ObiRopeCursor>();
        cursor.ChangeLength(Vector3.Distance(targetTransform.position, hit.point));

        var attachments = ropeInstance.GetComponents<ObiParticleAttachment>();

        // Attachment 0 ? Player / überliegender Rigidbody
        attachments[0].target = targetTransform.GetComponentInParent<Rigidbody>().transform;
        attachments[0].attachmentType = ObiParticleAttachment.AttachmentType.Static;

        // Attachment 1 ? Hit point oder Rigidbody
        if (hit.rigidbody != null)
        {
            attachments[1].target = hit.rigidbody.transform;
            attachments[1].attachmentType = ObiParticleAttachment.AttachmentType.Dynamic;
        }
        else
        {
            attachments[1].target = null;
            attachments[1].transform.position = hit.point;
            attachments[1].attachmentType = ObiParticleAttachment.AttachmentType.Static;
        }
    }

    void HandleRopeLength()
    {
        float input = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(input) > 0.01f)
        {
            //float newLength = cursor. - input * ropeAdjustSpeed;

            //cursor.ChangeLength(newLength);
        }
    }
}
