using System.Collections.Generic;
using UnityEngine;

public class SimpleMagnet : MonoBehaviour, IReferenceRigidbody, IHandleInput, IHandleGroundData, ISimulateable, IReconsileBool, ISetTick
{
    public struct MagnetOnData
    {
        public bool IsEngaged;
        public uint Tick;
    }


    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();
    [SerializeField] private SimpleBattery battery;

    [Header("Runtime Variables")]
    [SerializeField] private bool buttonPressed = false;
    [SerializeField] private bool magnetismEngaged = false;
    [SerializeField] private Vector3 magnetVector = Vector3.zero;

    [Header("Settings")]

    [Tooltip("1 = Left Shoulder, 2 = Right Shoulder, 3 = Left Stick Button, 4 = Right Stick Button\r\n" +
        "    /// 5 = A Button, 6 = B Button, 7 = X Button, 8 = Y Button\r\n" +
        "    /// 9 = North Button, 10 = East Button, 11 = South Button, 12 = West Button\r\n" +
        "    /// 13 = Start Button, 14 = Select Button\r\n" +
        "    /// 15 = Left Trigger, 16 = Right Trigger\r\n" +
        "    /// 17 = Left Stick X, 18 = Left Stick Y, 19 = Right Stick X, 20 = Right Stick Y")]
    [SerializeField] private int activationButton;
    [SerializeField] private float heightTreshold = 2.4f;

    [SerializeField] private float magnetStrength = 100f;    
    [SerializeField] private float energyConsumptionPerSecond = 0.2f;
    [SerializeField] float minimumEnergyForStartMagnet = 1.01f;
    [SerializeField] float stopMagnetTresholdEnergy = 0.9f;



    [Header("PD Boost Settings")]

    [SerializeField] private float pFactorBoost = 1f;
    [SerializeField] private float dFactorBoost = 1f;
    [SerializeField] private Vector3 pFactorsBoost = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 dFactorsBoost = new Vector3(1f, 1f, 1f);

    [SerializeField] private List<GameObject> boostableObjects = new List<GameObject>();


    [Header("Effects")]
    [SerializeField] private ParticleSystem thrusterEffect = null;
    [SerializeField] private FloatFlexibleSound soundSystem = null;

    private uint lastReconsiledTick = 0;

    private bool magnetismShouldBeEngaged = false;
    private bool wasPressed = false;

    // IHandleInput Method
    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        buttonPressed = inputLogic.CheckButton(activationButton, input);

        if (buttonPressed) 
        {
            if (!wasPressed) // On Button Down
            {
                Debug.Log("Magnet Button Down");
                if (magnetismEngaged)
                {
                    magnetismShouldBeEngaged = false;
                }
                else
                {
                    magnetismShouldBeEngaged = true;
                }
            }            
        }
        
        wasPressed = buttonPressed;

        //if (magnetismShouldBeEngaged != magnetismEngaged)
        //{
        //    Debug.Log($"Magnetism Engaged: {magnetismEngaged}, Should be Engaged: {magnetismShouldBeEngaged}, Button Pressed: {buttonPressed}, lastTick: ");            
        //}
    }

    // IHandleGroundData Method
    public void HandleGroundData(in GroundData groundData, float targetFlightHeight, float deltaTime)
    {
        if (!groundData.Hit || groundData.HitDistance > heightTreshold * targetFlightHeight)
        {
            magnetismShouldBeEngaged = false;
            magnetVector = Vector3.zero;  // only for Debugging purposes
        }
        else
        {
            magnetVector = - groundData.HitNormal * magnetStrength;
        }

    }

    // ISimulateable Method
    public void Simulate(float deltaTime)
    {
        if (battery != null)
        {
            if (magnetismEngaged)
            {
                if (battery.UseEnergy(energyConsumptionPerSecond * deltaTime) < stopMagnetTresholdEnergy)  // Use Energy from Battery
                {
                    magnetismShouldBeEngaged = false;
                }
            }
            else
            {
                if (battery.GetCurrentEnergy() < minimumEnergyForStartMagnet)
                {
                    magnetismShouldBeEngaged = false;
                }
            }
        }

        bool debugOutput = false;
        if (magnetismShouldBeEngaged != magnetismEngaged)
        {
            debugOutput = true;
        }


        if (magnetismShouldBeEngaged)
        {
            if (!magnetismEngaged)  // Switch on
            {
                magnetismEngaged = true;
                BoostDuringMagnetState(true);

                soundSystem?.HandleSound(1f, deltaTime);
            }
        }
        else
        {
            if (magnetismEngaged)  // Switch off
            {
                magnetismEngaged = false;
                BoostDuringMagnetState(false);

                soundSystem?.HandleSound(0f, deltaTime);
            }
        }

        //if (debugOutput)
        //{
        //    Debug.Log($"Magnetism Engaged: {magnetismEngaged}, Should be Engaged: {magnetismShouldBeEngaged}");
        //}


        if (magnetismEngaged)
        {
            PhysicsRigidbody.AddForce(magnetVector * deltaTime, ForceMode.Impulse);
        }
    }

    // ISetTick Method
    public void SetTick(uint tick)
    {
        if (tick <= lastReconsiledTick)
        {
            return;
        }
        lastReconsiledTick = tick;
        //Debug.Log($"SetTick Magnetism: {magnetismEngaged}, Tick: {tick}");
    }

    // IReconsileBool Method
    public bool GetValue()
    {
        //Debug.Log($"GetValue Magnetism: {magnetismEngaged}, lastreconsile: {lastReconsiledTick}");

        return magnetismEngaged;
    }

    public void Reconcile(bool isEngaged, uint tick)
    {
        //Debug.Log($"Reconcile Magnetism: {isEngaged}, Tick: {tick}");
        if (tick <= lastReconsiledTick)
        {
            return;
        }

        //Debug.Log($"actually reconsiling!! Magnetism: {isEngaged}");

        lastReconsiledTick = tick;
        //if (magnetismEngaged == isEngaged)
        //{
        //    return; // no change
        //}

        magnetismEngaged = isEngaged;
        magnetismShouldBeEngaged = isEngaged;
        BoostDuringMagnetState(isEngaged);
        soundSystem?.HandleSound(isEngaged ? 1f : 0f, 0f);
    }


    public void BoostDuringMagnetState(bool engagesMagnetism)
    {
        foreach (GameObject boostableObject in boostableObjects)
        {
            // Boost PD values (when engagesMagnetism is true) -> x * factor
            // or reset them (when engagesMagnetism is false) -> x / factor

            Vector3 _pFactorsBoost = engagesMagnetism? pFactorsBoost : new Vector3(Invert(pFactorsBoost.x), Invert(pFactorsBoost.y), Invert(pFactorsBoost.z));
            Vector3 _dFactorsBoost = engagesMagnetism ? dFactorsBoost : new Vector3(Invert(dFactorsBoost.x), Invert(dFactorsBoost.y), Invert(dFactorsBoost.z));

            boostableObject.GetComponent<IAccessibleSinglePDController>()?.BoostPDValues(
                engagesMagnetism? pFactorBoost : Invert(pFactorBoost), 
                engagesMagnetism ? dFactorBoost : Invert(dFactorBoost));
            boostableObject.GetComponent<IAccessibleTriplePDController>()?.BoostPDValues(_pFactorsBoost, _dFactorsBoost);

            // Toggle Gravity
            var gravity = boostableObject.GetComponent<SimpleGravity>();
            if (gravity != null)
            {
                gravity.IsOn = !engagesMagnetism;
            }
        }        
    }

    private float Invert(float value)
    {
        if ( value == 0)
        {
            value = 0.000001f;
        }
        return 1 / value;
    }
}