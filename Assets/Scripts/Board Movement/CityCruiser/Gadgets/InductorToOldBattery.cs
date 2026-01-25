using UnityEngine;

public class InductorToOldBattery : MonoBehaviour
{
    [SerializeField] private CitycruseController citycruseController;
    [SerializeField] private OldBattery oldBattery;
    [SerializeField] private bool canInductEnergy = false;

    [SerializeField] private bool isHeightDependant;
    [SerializeField] private float minHeightForMaxEnergy, noEnergyHeight;
    private float energyHeightDropOffFactor, startEnergyAtZeroHeight;

    [SerializeField] private bool isSpeedDependant;
    [SerializeField] private float maxSpeedDependandFactor, energyPerMpSPerSecond;


    [SerializeField] private float heightFactor = 1f, speedFactor = 1f;
    [SerializeField] private float height, speed;

    private float currentSpeed;

    private void OnEnable()
    {
        if (isSpeedDependant)
        {
            LocationAsWorldMovementInterpreter locationAsWorldMovementInterpreter = FindObjectOfType<LocationAsWorldMovementInterpreter>();
            if (locationAsWorldMovementInterpreter != null) { locationAsWorldMovementInterpreter.OnLocationUpdated += UpdateSpeed; }
        }
    }

    private void OnDisable()
    {
        if (isSpeedDependant)
        {
            LocationAsWorldMovementInterpreter locationAsWorldMovementInterpreter = FindObjectOfType<LocationAsWorldMovementInterpreter>();
            if (locationAsWorldMovementInterpreter != null) { locationAsWorldMovementInterpreter.OnLocationUpdated -= UpdateSpeed; }
        }
    }

    void Start()
    {
        if (citycruseController == null)
        {
            citycruseController = GetComponentInParent<CitycruseController>();
        }

        if (oldBattery == null)
        {
            oldBattery = GetComponentInParent<OldBattery>();
        }

        energyHeightDropOffFactor = 1 / (noEnergyHeight - minHeightForMaxEnergy);
        startEnergyAtZeroHeight = 1f + minHeightForMaxEnergy * energyHeightDropOffFactor;
    }

    void Update()
    {
        if (citycruseController != null)
        {
            if (citycruseController.hitTag == "Inductive" || citycruseController.hitTag == "Magnetic")
            {
                canInductEnergy = true;
            }
            else
            {
                canInductEnergy = false;
            }

            if (canInductEnergy)
            {
                if (isHeightDependant)
                {
                    float currentHeight = citycruseController.distanceToTarget;

                    if (currentHeight < minHeightForMaxEnergy)
                    { heightFactor = 1f; }
                    else if (currentHeight > noEnergyHeight)
                    { heightFactor = 0f; }
                    else
                    { heightFactor = Mathf.Clamp01(startEnergyAtZeroHeight - currentHeight * energyHeightDropOffFactor); }
                }

                if (isSpeedDependant)
                { speedFactor = Mathf.Clamp(currentSpeed / Time.fixedDeltaTime, 0f, maxSpeedDependandFactor); }

                if (oldBattery != null)
                {
                    float energyChargedThisFrame = energyPerMpSPerSecond * speedFactor * heightFactor * Time.deltaTime;
                    oldBattery.ChargeEnergy(energyChargedThisFrame);                    
                }
            } 
        }
    }

    private void UpdateSpeed(LocationAsWorldMovementEventData eventData)
    {
        currentSpeed = eventData.DeltaLocation.magnitude;
    }
}
