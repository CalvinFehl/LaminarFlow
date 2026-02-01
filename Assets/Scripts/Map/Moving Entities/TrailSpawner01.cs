using UnityEngine;

public class TrailSpawner01 : MonoBehaviour
{
    public GameObject trailPrefab;
    [SerializeField] private Vector3 trailPrefabDimensions;

    [SerializeField] private Vector3 lastPosition;
    [SerializeField] private Quaternion lastRotation = Quaternion.identity;
    private float accumulatedOvershoot = 0.0f;

    void FixedUpdate()
    {
        if (lastPosition != transform.position) 
        { 
            SpawnBetweenTwoPoints(lastPosition, transform.position, lastRotation, transform.rotation); 

            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
    }

    public void SpawnBetweenTwoPoints(Vector3 startPosition, Vector3 endPosition, Quaternion startRotation = default, Quaternion endRotation = default, float distanceBetweenSegments = 0f)
    {
        if (distanceBetweenSegments == 0f) distanceBetweenSegments = trailPrefabDimensions.z;

        float distance = Vector3.Distance(startPosition, endPosition);
        float overshoot = distance % distanceBetweenSegments;

        accumulatedOvershoot += overshoot;
        int additionalSegments = 0;

        if (accumulatedOvershoot > distanceBetweenSegments)
        {
            additionalSegments = Mathf.FloorToInt(accumulatedOvershoot / distanceBetweenSegments);
            accumulatedOvershoot -= distanceBetweenSegments * additionalSegments;
        }

        int numberOfPrefabsToSpawn = Mathf.FloorToInt((distance - overshoot) + accumulatedOvershoot / distanceBetweenSegments) + additionalSegments;

        for (int i = 0; i < numberOfPrefabsToSpawn; i++)
        {
            Vector3 _position = Vector3.Lerp(startPosition, endPosition, (1.0f / numberOfPrefabsToSpawn) * i) + (endPosition - startPosition).normalized * overshoot;
            Quaternion _rotation;

            if (startRotation == default || endRotation == default)
            {
                _rotation = Quaternion.LookRotation(endPosition - startPosition);
            }
            else
            {
                _rotation = Quaternion.Lerp(startRotation, endRotation, (1.0f / numberOfPrefabsToSpawn) * i);
            }
            GameObject _trail = Instantiate(trailPrefab, _position, _rotation);
        }
    }
}
