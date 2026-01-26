using UnityEngine;

public class SimpleBattery : MonoBehaviour, ISimulateable, IReconsileFloat, ISetTick
{
    [Header("Runtime Variables")]
    [SerializeField] private float currentEnergy = 0f;
    [SerializeField] private float overcharge = 0f;

    [Header("Settings")]
    [SerializeField] private float maxEnergy = 3000f;
    [SerializeField] private bool autoRegen = true;
    [SerializeField] private float energyRegen = 1f;
    [SerializeField] private float maxOverchargeFactor = 1.5f;
    [SerializeField] private float overchargeRegen = 1f;
    [SerializeField] private float overChargeRegenDelay = 0f;

    [Header("UI")]
    [SerializeField] GameObject[] fuelDiageticUI;
    [SerializeField] GameObject[] overchargeDiageticUI;
    [SerializeField] float easeCharge = 0f;
    [SerializeField] float easeOvercharge = 0f;

    private uint lastReconsiledTick = 0;


    // ISimulateable Method
    public void Simulate(float deltaTime)
    {
        if (autoRegen)
        {
            if (currentEnergy < maxEnergy)
            {
                overcharge = 0f;
                currentEnergy = Mathf.Clamp(currentEnergy + energyRegen * deltaTime, 0f, maxEnergy);
            }
            if (currentEnergy > maxEnergy)
            {
                overcharge = currentEnergy - maxEnergy;
                if (overChargeRegenDelay > 0)
                {
                    overChargeRegenDelay -= deltaTime;
                }
                else
                {
                    overChargeRegenDelay = 0f;
                    currentEnergy = Mathf.Max(currentEnergy - overchargeRegen * deltaTime, maxEnergy);
                }
            }
        }

        foreach (GameObject fuelDiageticUI in fuelDiageticUI)
        {
            if (fuelDiageticUI != null)
            {
                if (easeCharge > 0f)
                {
                    fuelDiageticUI.transform.localScale = new Vector3(Ease(deltaTime, fuelDiageticUI.transform.localScale.x, Mathf.Clamp01(currentEnergy / maxEnergy), easeCharge), 1f, 1f);
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
                    overchargeDiageticUI.transform.localScale = new Vector3(Ease(deltaTime, overchargeDiageticUI.transform.localScale.x, Mathf.Clamp01(overcharge / (maxOverchargeFactor - 1)), easeOvercharge), 1f, 1f);
                }
                else
                {
                    overchargeDiageticUI.transform.localScale = new Vector3(Mathf.Clamp01(overcharge / (maxOverchargeFactor - 1f)), 1f, 1f);
                }
            }
        }
    }

    // ISetTick Method
    public void SetTick(uint tick)
    {
        lastReconsiledTick = tick;
    }


    // IReconsileFloat Methods
    public float GetValue()
    {
        return GetCurrentEnergy();
    }

    public void Reconcile(float value, uint tick = 0)
    {
        if (tick < lastReconsiledTick)
        {
            return;
        }
        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy * maxOverchargeFactor);
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

    private float Ease(float deltaTime, float current, float target, float easingPerSecond = 0.5f, float minumum = 0.02f)
    {
        float difference = target - current;
        if (Mathf.Abs(difference) < minumum)
        {
            return target;
        }
        else
        {
            return current + difference * easingPerSecond * deltaTime;
        }
    }
}
