using UnityEngine;

public class MoveByPath : MonoBehaviour
{
    [SerializeField] private Transform[] path;
    [SerializeField] private int currentGoalAtPath;
    [SerializeField] private bool loops = true;
    [SerializeField] private Transform startPoint, endPoint;
    [SerializeField] private float travelDuration = 5.0f;
    private float timePassed = 0.0f;

    private void Awake()
    {
        currentGoalAtPath = 1;
        startPoint = path[0];
        endPoint = path[1];
    }

    void FixedUpdate()
    {
        if (timePassed < travelDuration)
        {
            timePassed += Time.fixedDeltaTime;
            float progress = timePassed / travelDuration;
            Vector3 _position = Vector3.Lerp(startPoint.position, endPoint.position, progress);
            Quaternion _rotation = Quaternion.Lerp(startPoint.rotation, endPoint.rotation, progress);

            transform.position = _position;
            transform.rotation = _rotation;
        }
        else
        {
            if (currentGoalAtPath <= path.Length - 1)
            {
                currentGoalAtPath ++;

                if (currentGoalAtPath == path.Length)
                {
                    if (loops) currentGoalAtPath = 0;
                    else return;
                }

                if (currentGoalAtPath == 0) startPoint = path[path.Length - 1];
                else startPoint = path[currentGoalAtPath -1];
            }

            endPoint = path[currentGoalAtPath];
            timePassed = 0.0f;
        }
    }
}
