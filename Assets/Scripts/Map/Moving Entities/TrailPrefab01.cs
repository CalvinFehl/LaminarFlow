using UnityEngine;

public class TrailPrefab01 : MonoBehaviour
{
    public float lifeTime = 2.0f;
    public float decayTime = 1.0f;
    private float lifeTimer = 0.0f, lifeTimePlusDecay;
    [SerializeField] private Vector3 shrinkingFactor;

    private void Awake()
    {
        lifeTimePlusDecay = lifeTime + decayTime;
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;

        if (lifeTimer >= lifeTime)
        {
            gameObject.transform.localScale -= shrinkingFactor * decayTime * Time.deltaTime;

            if (lifeTimer >= lifeTimePlusDecay)
            {
                Destroy(gameObject);
            }
        }
    }
}
