using UnityEngine;

public class SinglePDModule : MonoBehaviour
{
    [SerializeField] private BaseGroundProcessor groundProcessor;

    [Header("Settings")]

    [SerializeField] private float target;
    [SerializeField] private float feedbackMin = 0f;
    [SerializeField] private float feedbackMax = 30000f;

    [Header("PD Settings")]

    [SerializeField] private float pValue = 1f, dValue = 0.13f;

    [Header("Feedback")]
    public float Response;

    private PDController controller;

    #region Subscribe/Unsubscribe
    private void OnEnable()
    {
        if (groundProcessor != null)
        {
            groundProcessor.OnHitDistanceUpdated += Calculate;
        }
    }

    private void OnDisable()
    {
        if (groundProcessor != null)
        {
            groundProcessor.OnHitDistanceUpdated -= Calculate;
        }
    }
    #endregion

    public void Start()
    {
        controller = new PDController(pValue, dValue);
    }

    public void Calculate(float current, float deltaTime)
    {
        Response = Mathf.Clamp(controller.GetFeedback(target, current, deltaTime), feedbackMin, feedbackMax);
    }
}
