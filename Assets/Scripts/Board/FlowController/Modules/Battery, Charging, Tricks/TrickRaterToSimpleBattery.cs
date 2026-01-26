using System;
using UnityEngine;

public class TrickRaterToSimpleBattery : MonoBehaviour
{
    [SerializeField] private SimpleBattery battery;
    [SerializeField] RotationAsPitchYawRollInterpreter rotationTracker;

    [Header("Runtime Variables")]
    public int[] halfTurnStreak = new int[3];

    [Header("Settings")]
    [SerializeField] Vector3 pointsFor180s = new Vector3(0.7f, 0.3f, 0.5f);
    [SerializeField] Vector3 overchargeDelayFor180s = new Vector3(0.7f, 0.3f, 0.5f);

    private float[] pointsFor180sArray;
    private float[] overchargeDelayFor180sArray;

    private void Start()
    {
        pointsFor180sArray = new float[3] { pointsFor180s.x, pointsFor180s.y, pointsFor180s.z };
        overchargeDelayFor180sArray = new float[3] { overchargeDelayFor180s.x, overchargeDelayFor180s.y, overchargeDelayFor180s.z };        
    }

    private void OnEnable()
    {
        if (rotationTracker != null) { rotationTracker.OnPitchYawRollUpdated += GiveMadRespect; }
    }

    private void OnDisable()
    {
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

                if (battery != null)
                {
                    battery.ChargeEnergy(pointsFor180sArray[i]);
                    battery.DelayOvercharge(overchargeDelayFor180sArray[i]);
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
