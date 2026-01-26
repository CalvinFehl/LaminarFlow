using UnityEngine;

[System.Serializable]

public class PDRotationController
{
    [SerializeField] private Transform referenceTransform;

    [Header("Runtime Variables")]
    [SerializeField] private Vector3 errorEuler;
    [SerializeField] private Vector3 correction;
    [SerializeField] private Vector3 feedback;

    [Header("Settings")]
    [SerializeField] private Vector3 target;

    [SerializeField] Vector3 AngleTreshold = new Vector3(60f, 60f, 60f);

    [Header("PD Controllers")]

    [SerializeField] private PDController xController;
    [SerializeField] private PDController yController;
    [SerializeField] private PDController zController;


    public PDRotationController(Transform referenceTransform, Vector3 pValues, Vector3 dValues, Vector3 angleTresholdValues)
    {
        this.referenceTransform = referenceTransform;

        this.xController = new PDController(pValues.x, dValues.x);
        this.yController = new PDController(pValues.y, dValues.y);
        this.zController = new PDController(pValues.z, dValues.z);

        this.AngleTreshold.x = angleTresholdValues.y;
        this.AngleTreshold.y = angleTresholdValues.z;
        this.AngleTreshold.z = angleTresholdValues.x;

        this.target = new Vector3(0f, 1f, 0f);
    }

    public Vector3 GetFeedback(Vector3 groundNormal, float deltaTime)
    {
        if (referenceTransform == null)
        {
            return Vector3.zero;
        }

        Vector3 groundNormalLocal = referenceTransform.InverseTransformVector(groundNormal);
        errorEuler = Quaternion.FromToRotation(target, groundNormalLocal).eulerAngles;

        // Take in account negative angles
        correction = new Vector3(
            errorEuler.x > 180f ? errorEuler.x - 360f : errorEuler.x, 
            errorEuler.y > 180f ? errorEuler.y - 360f : errorEuler.y, 
            errorEuler.z > 180f ? errorEuler.z - 360f : errorEuler.z);

        // Calculate Response while limiting angle correction to the treshold
        feedback.x = Mathf.Abs(correction.x) < AngleTreshold.x ?
            xController.GetFeedback(correction.x, 0f, deltaTime) / 360f : 0f;
        
        feedback.y = Mathf.Abs(correction.y) < AngleTreshold.y ?
            yController.GetFeedback(correction.y, 0f, deltaTime) / 360f : 0f;

        feedback.z = Mathf.Abs(correction.z) < AngleTreshold.z ?
            zController.GetFeedback(correction.z, 0f, deltaTime) / 360f : 0f;

        return feedback;
    }
}
