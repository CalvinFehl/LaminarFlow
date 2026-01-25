using System;
using UnityEngine;

public class TrickRaterToOldBattery : MonoBehaviour
{
    [SerializeField] private OldBattery oldBattery;
    public int[] halfTurnStreak = new int[3];

    [SerializeField] Vector3 pointsFor180s = new Vector3(0.7f, 0.3f, 0.5f);
    [SerializeField] Vector3 overchargeDelayFor180s = new Vector3(0.7f, 0.3f, 0.5f);
    private float[] pointsFor180sArray;
    private float[] overchargeDelayFor180sArray;

    private void Start()
    {
        if (oldBattery == null)
        {
            oldBattery = GetComponentInParent<OldBattery>();
            if (oldBattery == null) oldBattery = GetComponentInChildren<OldBattery>();
            if (oldBattery == null) Debug.LogWarning("No OldBattery assigned to TrickRaterToOldBattery");
        }
        pointsFor180sArray = new float[3] { pointsFor180s.x, pointsFor180s.y, pointsFor180s.z };
        overchargeDelayFor180sArray = new float[3] { overchargeDelayFor180s.x, overchargeDelayFor180s.y, overchargeDelayFor180s.z };
    }

    private void OnEnable()
    {
        RotationAsPitchYawRollInterpreter rotationTracker = FindObjectOfType<RotationAsPitchYawRollInterpreter>();
        if (rotationTracker != null) { rotationTracker.OnPitchYawRollUpdated += GiveMadRespect; }
    }

    private void OnDisable()
    {
        RotationAsPitchYawRollInterpreter rotationTracker = FindObjectOfType<RotationAsPitchYawRollInterpreter>();
        if (rotationTracker != null) { rotationTracker.OnPitchYawRollUpdated -= GiveMadRespect; }
    }

    void GiveMadRespect(PitchYawRollEventData pyrEventData) 
    {
        float[] accumulatedPitchYawRoll = new float[3] {pyrEventData.AccumulatedPitchYawRoll.x, pyrEventData.AccumulatedPitchYawRoll.y, pyrEventData.AccumulatedPitchYawRoll.z };

        for (int i = 0; i < 3; i++)
        {
            int flip = RoundToInt(accumulatedPitchYawRoll[i]);
            if (flip > halfTurnStreak[i])
            {
                halfTurnStreak[i] = flip;

                if (oldBattery != null)
                {
                    oldBattery.ChargeEnergy(pointsFor180sArray[i]);
                    oldBattery.DelayOvercharge(overchargeDelayFor180sArray[i]);
                }

                /*Debug.Log($"Charged {pointsFor180sArray[i]} for {(float)halfTurnStreak[i] / 2} flip");
                if (flip % 2 == 0) { Debug.Log("360!!"); }*/
            }
            if (flip < halfTurnStreak[i])
            {
                halfTurnStreak[i] = 0;
            }
        }
    }

    private int RoundToInt(float value)
    {
        return (int)Math.Floor(Mathf.Abs(value / 180));
    }
}
