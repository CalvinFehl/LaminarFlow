using UnityEngine;

public class OldBattery : MonoBehaviour
{
    [SerializeField] AudioSource boardAudioSource;
    [SerializeField] string sound;
    [SerializeField] float maxPitch = 2f, minPitch = -2f;
    [SerializeField] float minimumDifferenceForSound = 0.1f, easingPerSecondSound = 10f;

    [SerializeField] float maxEnergy = 3000f;
    [SerializeField] float currentEnergy = 0f;
    [SerializeField] float energyRegen = 1f;
    [SerializeField] bool autoRegen = true;
    [SerializeField] float overcharge = 0f;
    [SerializeField] float overChargeRegenDelay = 0f;
    [SerializeField] float overchargeRegen = 1f;
    [SerializeField] float maxOverchargeFactor = 1.5f;
    [SerializeField] GameObject[] fuelDiageticUI;
    [SerializeField] GameObject[] overchargeDiageticUI;
    [SerializeField] float easeCharge = 0f;
    [SerializeField] float easeOvercharge = 0f;
    private float overchargeEased;


    void Update()
    {
        if (autoRegen)
        {
            if (currentEnergy < maxEnergy)
            {
                overcharge = 0f;
                currentEnergy = Mathf.Clamp(currentEnergy + energyRegen * Time.deltaTime, 0f, maxEnergy);
            }
            if (currentEnergy > maxEnergy)
            {
                overcharge = currentEnergy - maxEnergy;
                if (overChargeRegenDelay > 0)
                {
                    overChargeRegenDelay -= Time.deltaTime;
                }
                else
                {
                    overChargeRegenDelay = 0f;
                    currentEnergy = Mathf.Max(currentEnergy - overchargeRegen * Time.deltaTime, maxEnergy);
                }
            }
        }

        foreach (GameObject fuelDiageticUI in fuelDiageticUI)
        {
            if (fuelDiageticUI != null)
            {
                if (easeCharge > 0f)
                {
                    fuelDiageticUI.transform.localScale = new Vector3(Ease(fuelDiageticUI.transform.localScale.x, Mathf.Clamp01(currentEnergy / maxEnergy), easeCharge), 1f, 1f);
                }
                else
                {
                    fuelDiageticUI.transform.localScale = new Vector3(Mathf.Clamp01(currentEnergy / maxEnergy), 1f, 1f);
                }
            }
        }

        foreach (GameObject overchargeDiageticUI in overchargeDiageticUI)
        {
            if (overchargeDiageticUI != null)
            {
                if (easeOvercharge > 0f)
                {
                    overchargeDiageticUI.transform.localScale = new Vector3(Ease(overchargeDiageticUI.transform.localScale.x, Mathf.Clamp01(overcharge / (maxOverchargeFactor - 1)), easeOvercharge), 1f, 1f);
                }
                else
                {
                    overchargeDiageticUI.transform.localScale = new Vector3(Mathf.Clamp01(overcharge / (maxOverchargeFactor - 1f)), 1f, 1f);
                }
            }
        }

        if (boardAudioSource != null)
        {
            if (overchargeEased != overcharge)
            {
                if (overchargeEased < overcharge - minimumDifferenceForSound)
                {
                    if (!boardAudioSource.isPlaying) boardAudioSource.Play();
                }

                if (overchargeEased < overcharge)
                {
                    overchargeEased = Ease(overchargeEased, overcharge, easingPerSecondSound, 0.02f);
                    boardAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, overchargeEased / maxOverchargeFactor);
                }
                else
                {
                    overchargeEased = overcharge;
                    if (boardAudioSource.isPlaying) boardAudioSource.Stop();
                }
            }
            else
            {
                if (boardAudioSource.isPlaying) boardAudioSource.Stop();
            }
        }
    }

    public float UseEnergy(float requestedEnergy)
    {
        float availableEnergy = Mathf.Min(requestedEnergy, currentEnergy);
        currentEnergy = Mathf.Clamp(currentEnergy - requestedEnergy, 0f, maxEnergy * maxOverchargeFactor);
        return availableEnergy;
    }

    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }

    public void DelayOvercharge(float delay)
    {
        overChargeRegenDelay += delay;
    }

    public void ChargeEnergy(float charge)
    {
        currentEnergy = Mathf.Min(currentEnergy + charge, maxEnergy * maxOverchargeFactor);
    }

    private float Ease(float current, float target, float easingPerSecond = 0.5f, float minumum = 0.02f)
    {
        float difference = target - current;
        if (Mathf.Abs(difference) < minumum)
        {
            return target;
        }
        else
        {
            return current + difference * easingPerSecond * Time.deltaTime;
        }
    }
}
