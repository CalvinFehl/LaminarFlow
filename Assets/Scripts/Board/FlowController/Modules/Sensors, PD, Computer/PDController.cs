using UnityEngine;

[System.Serializable]
public class PDController
{
    [Header("Runtime Variables")]
    [SerializeField] private float feedback;
    [SerializeField] private float pFactor, dFactor;

    private float _lastError;
    public PDController(float pFactor, float dFactor)
    {
        this.pFactor = pFactor;
        this.dFactor = dFactor;
    }

    public float GetFeedback(float target, float current, float deltaTime = 1)
    {
        if (deltaTime <= 0)
        {
            return 0;
        }

        float error = target - current;
        float derivative = (error - _lastError) / deltaTime;

        _lastError = error;

        feedback = error * pFactor + derivative * dFactor;
        return feedback;
    }
}
