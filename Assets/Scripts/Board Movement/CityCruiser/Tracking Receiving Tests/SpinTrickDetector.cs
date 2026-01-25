using UnityEngine;

public class SpinTrickDetector : MonoBehaviour
{
    private void OnEnable()
    {
        RotationAsPitchYawRollInterpreter tracker = FindObjectOfType<RotationAsPitchYawRollInterpreter>();
        if (tracker != null)
        {
            tracker.OnPitchYawRollUpdated += Kickflip;
        }
    }
    private void OnDisable()
    {
        RotationAsPitchYawRollInterpreter tracker = FindObjectOfType<RotationAsPitchYawRollInterpreter>();
        if (tracker != null)
        {
            tracker.OnPitchYawRollUpdated -= Kickflip;
        }
    }

    public void Kickflip(PitchYawRollEventData pitchYawRollEventData)
    {
        //Debug.Log("Accumulated pitch yaw roll: " + pitchYawRollEventData.AccumulatedPitchYawRollTime);
        if(Mathf.Abs(pitchYawRollEventData.AccumulatedPitchYawRoll.x / 360f) > 0.9f)
        {
            //Debug.Log("Kickflip detected");
        }
    }
}
