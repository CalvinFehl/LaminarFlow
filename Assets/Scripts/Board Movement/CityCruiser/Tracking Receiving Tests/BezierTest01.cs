using System.Collections;
using UnityEngine;

public class BezierTest01 : MonoBehaviour
{
    [SerializeField] private Transform StartPoint;
    [SerializeField] private Transform EndPoint;
    public float EaseIn = 0.0f;
    public float EaseOut = 0.0f;

    [SerializeField] private float duration = 1f;

    Vector3 StartBezier = new Vector3(0, 0, 0);
    Vector3 EndBezier = new Vector3(10, 0, 0);

    [SerializeField] private float _overShoot;
    [SerializeField] private float _overShotStart;

    [SerializeField] private bool usesDebugging = false;
    [SerializeField] private Vector3 DebugTranslation = new Vector3(0f, -10f, 0f);

    public void MoveViaBezier(Vector3 start, Vector3 end, float duration = 0f, float easeIn = 0f, float easeOut = 0f, Transform StartTransform = null, Transform EndTransform = null, float overShoot = 0f, float overShotStart = 0f, Vector3 momentum = default, float momentumDecellerationDuration = 0f)
    {
        StartCoroutine(OldMoveWithBezierEaseInOut(start, end, duration, easeIn, easeOut, StartTransform, EndTransform, overShoot, overShotStart, momentum, momentumDecellerationDuration));
    }

    public void MoveViaBezierWithOverShoot(Vector3 start, Vector3 end, float duration = 0f, float easeIn = 0f, float easeOut = 0f, Transform StartTransform = null, Transform EndTransform = null, float overShoot = 0f, float overShotStart = 0f, Vector3 momentum = default, float momentumDecellerationDuration = 0f)
    {
        StartCoroutine(MoveViaBezierEaseInOutWithOverShoot(start, end, duration, easeIn, easeOut, StartTransform, EndTransform, overShoot, overShotStart, momentum, momentumDecellerationDuration));
    }

    public IEnumerator MoveViaBezierEaseInOutWithOverShoot(Vector3 start, Vector3 end, float duration = 0f, float easeIn = 0f, float easeOut = 0f, Transform StartTransform = null, Transform EndTransform = null, float overShoot = 0f, float overShotStart = 0f, Vector3 momentum = default, float momentumDecellerationDuration = 0f)
    {
        yield return StartCoroutine(OldMoveWithBezierEaseInOut(start, end, duration / (1 + overShoot), easeIn, 1f, StartTransform, EndTransform, overShoot, overShotStart, momentum, momentumDecellerationDuration));
        yield return StartCoroutine(OldMoveWithBezierEaseInOut(end, start, duration * overShoot, 1f, easeOut, EndTransform, StartTransform, 0f, overShoot, default, 0f));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(StartPoint != null && EndPoint != null)
            {
                StartBezier = StartPoint.position;
                EndBezier = EndPoint.position;
            }

            if (_overShoot == 0f)
            { MoveViaBezier(StartBezier, EndBezier, duration, EaseIn, EaseOut, StartPoint, EndPoint, 0f, 0f, default, 0f); }
            else MoveViaBezierWithOverShoot(StartBezier, EndBezier, duration, EaseIn, EaseOut, StartPoint, EndPoint, _overShoot, _overShotStart, default, 0f);
        }
    }

    Vector3 EaseInOut(Vector3 start, Vector3 end, float easeIn, float easeOut, float easeInDelay, float easeOutDelay, float t, float timeStretchFactor)
    {
        float prog;
        float timeStretchedT = t * timeStretchFactor;

        if (timeStretchedT < 0.5f)
        {
            prog = ((1f - easeIn) * timeStretchedT + easeIn * timeStretchedT * timeStretchedT);
        }
        else if (timeStretchedT > timeStretchFactor - 0.5f)
        {
            prog = (((1f - easeOut) * (timeStretchedT - (timeStretchFactor - 1)) ) + easeOut * (1f - Mathf.Pow((timeStretchedT - timeStretchFactor), 2f)));
        }
        else
        {
            prog = timeStretchedT - easeInDelay;
        }

        return Vector3.Lerp(start, end, prog);
    }


    public IEnumerator OldMoveWithBezierEaseInOut(Vector3 start, Vector3 end, float duration = 0f, float easeIn = 0f, float easeOut = 0f, Transform StartTransform = null, Transform EndTransform = null, float overShoot = 0f, float overShotStart = 0f, Vector3 momentum = default, float momentumDecellerationDuration = 0f)
    {
        Vector3 startPosition = start;
        Vector3 endPosition = end;
        float elapsedTime = 0f;
        float easeInDelay = 0.25f * easeIn;
        float easeOutDelay = 0.25f * easeOut;
        float timeStretchFactor = 1f;

        //account for overshoot
        if (StartTransform == null)
        { startPosition = start + overShotStart * (start - end); }
        if (EndTransform == null)
        { endPosition = end + overShoot * (end - start); }

        //Debug.Log("OverShoot: " + overShoot);
        //Debug.Log("OverShotStart: " + overShotStart);


        if (duration == 0f)
        {
            if (momentum != default)
            {
                Vector3 travelPath = endPosition - startPosition;
                float relativemomentum = Vector3.Dot(travelPath, momentum);
                duration = travelPath.magnitude / relativemomentum;
            }
            else
            {
                transform.position = end;
            }
        }

        //Debug.Log("Duration: " + duration); 

        if (duration > 0f) 
        {
            //timeStretchFactor is between 1 and 1.5, depending on the easeIn and easeOut values
            timeStretchFactor += (easeInDelay + easeOutDelay);

            while (elapsedTime < duration)
            {
                //between 0 and 1
                float t = elapsedTime / duration;

                if(momentum != default)
                {
                    float momentumFactor = Mathf.Clamp01(1f - elapsedTime / momentumDecellerationDuration);
                    start += momentum * momentumFactor;
                }

                //Update Start- and End-Position
                if (StartTransform != null) 
                {
                    startPosition = StartTransform.position;
                    if (overShotStart != 0f) { startPosition += overShotStart * (startPosition - endPosition); }
                }
                if (EndTransform != null) 
                {
                    endPosition = EndTransform.position;
                    if (overShoot != 0f) { endPosition += overShoot * (endPosition - startPosition); }
                }

                transform.position = EaseInOut(startPosition, endPosition, easeIn, easeOut, easeInDelay, easeOutDelay, t, timeStretchFactor);


                if (usesDebugging)  //For Debugging
                {
                    transform.position += DebugTranslation * elapsedTime;
                    Vector3 randomColor = new Vector3(1f - easeIn, 1f, 1f - easeOut);
                    SpawnBall(randomColor);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        transform.position = end;

        if(StartPoint != null && EndPoint != null)
        {
            Transform StorePoint = StartPoint;
            StartPoint = EndPoint;
            EndPoint = StorePoint;
        }
    }

    // For Debugging
    void SpawnBall(Vector3 color)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.GetComponent<Renderer>().material.color = new Color(color.x, color.y, color.z);
        ball.transform.position = transform.position;
        ball.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }

}
