using ECM2.Examples;
using System;
using UnityEngine;

public class AimedChargedDash : MonoBehaviour, IReferenceRigidbody, IHandleInput, ISimulateable
{    
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private Transform referenceTransform;
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();
    [SerializeField] private SimpleBattery battery;
    //[SerializeField] private CameraSystem cameraSystem;

    [Header("Runtime Variables")]
    [SerializeField] private bool buttonPressed = false;
    [SerializeField] private bool auxiliaryButtonPressed = false;


    [Header("Settings")]
    [SerializeField] bool usesAuxiliaryHoldButton = false;

    [Tooltip("1 = Left Shoulder, 2 = Right Shoulder, 3 = Left Stick Button, 4 = Right Stick Button\r\n " +
        "5 = A Button, 6 = B Button, 7 = X Button, 8 = Y Button\r\n " +
        "9 = North Button, 10 = East Button, 11 = South Button, 12 = West Button\r\n " +
        "13 = Start Button, 14 = Select Button\r\n " +
        "15 = Left Trigger, 16 = Right Trigger\r\n " +
        "17 = Left Stick X, 18 = Left Stick Y, 19 = Right Stick X, 20 = Right Stick Y")]
    [SerializeField] private int activationButton = 0, auxiliaryHoldButton;
    [SerializeField] float buttonPressedTime = 0f;
    [SerializeField] float maxButtonPressedTime = 2f;
    [SerializeField] float coolDown = 0.5f;
    [SerializeField] float energyConsumption = 1f;
    [SerializeField] float dashMaxStrength = 15f;
    [SerializeField] float heightTreshold = 1f;


    [Header("Camera Settings")]
    [SerializeField] float cameraSwitchTreshold = 0.5f;
    [SerializeField] private SimpleBoardCameraManager cameraManager;


    [Header("Effects")]
    [SerializeField] private ParticleSystem thrusterEffect = null;
    [SerializeField] private FloatFlexibleSound soundSystem = null;


    private bool relevantButtonPressed = false;
    private float buttonPressedAmount = 0f;
    private bool wasPressed = false;
    private float timeSinceDash = 0f;
    private bool switchedCamera = false;

    private void Awake()
    {
        if (referenceTransform == null)
        {
            referenceTransform = transform;
        }

        if (cameraManager != null)
        {
            cameraManager = FindFirstObjectByType<SimpleBoardCameraManager>();
        }

        timeSinceDash = coolDown;
    }

    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        buttonPressed = inputLogic.CheckButton(activationButton, input);

        if (usesAuxiliaryHoldButton)
        {
            auxiliaryButtonPressed = inputLogic.CheckButton(auxiliaryHoldButton, input);
        }
    }


    public void Simulate(float deltaTime)
    {
        if (timeSinceDash < coolDown)
        {
            timeSinceDash += deltaTime;
        }

        if (!usesAuxiliaryHoldButton)
        {
            SimulateWithoutAuxiliary(deltaTime);
        }
        else
        {
            SimulateWithAuxiliary(deltaTime);
        }
    }

    private void SimulateWithoutAuxiliary(float deltaTime)
    {
        if (wasPressed != buttonPressed)
        {
            if (!buttonPressed) // On Releasing the Button
            {
                if (timeSinceDash >= coolDown)
                {
                    // an available Energy of 1 translates to a dash with 1 dashMaxStrength
                    float availableEnergy = battery ? battery.UseEnergy(energyConsumption * buttonPressedAmount) / energyConsumption : 1f;

                    PhysicsRigidbody.AddForceAtPosition(referenceTransform.up * dashMaxStrength * availableEnergy, referenceTransform.position);

                    // Handle Sound
                    if (soundSystem != null)
                    {
                        soundSystem.HandleSound(0f, deltaTime);
                    }

                    buttonPressedTime = 0f;
                    timeSinceDash = 0f;
                }
            }
            wasPressed = buttonPressed;
        }
        else if (buttonPressed && wasPressed) // Holding Button
        {
            if (buttonPressedTime < maxButtonPressedTime)
            {
                buttonPressedTime = buttonPressedTime + deltaTime;
                buttonPressedAmount = buttonPressedTime / maxButtonPressedTime;
            }
            else if (buttonPressedTime > maxButtonPressedTime)
            {
                buttonPressedTime = maxButtonPressedTime;
            }

            // Handle Sound
            if (soundSystem != null)
            {
                soundSystem.HandleSound(buttonPressedAmount, deltaTime);
            }
        }
    }

    private void SimulateWithAuxiliary(float deltaTime)
    {
        if (buttonPressed && !wasPressed)  // On Primary Button Down
        {
            relevantButtonPressed = true;
            wasPressed = buttonPressed;
        }

        if (relevantButtonPressed)
        {
            if (!buttonPressed && !auxiliaryButtonPressed)  // When Releasing all Buttons
            {
                relevantButtonPressed = false;

                if (timeSinceDash >= coolDown)
                {
                    // an available Energy of 1 translates to a dash with 1 dashMaxStrength
                    float availableEnergy = battery ? battery.UseEnergy(energyConsumption * buttonPressedAmount) / energyConsumption : 1f;

                    PhysicsRigidbody.AddForceAtPosition(referenceTransform.up * dashMaxStrength * availableEnergy, referenceTransform.position);

                    if (soundSystem != null)
                    {
                        soundSystem.HandleSound(0f, deltaTime);
                    }

                    buttonPressedTime = 0f;
                    timeSinceDash = 0f;

                    if (switchedCamera && cameraManager != null)
                    {
                        cameraManager?.SwitchCameraMode(SimpleBoardCameraManager.CameraMode.Follow);
                        switchedCamera = false;
                    }
                }
                wasPressed = buttonPressed;
            }
            else if (buttonPressed)
            {
                if (buttonPressedTime < maxButtonPressedTime)
                {
                    buttonPressedTime = buttonPressedTime + deltaTime;
                    buttonPressedAmount = buttonPressedTime / maxButtonPressedTime;

                    // Handle Sound
                    if (soundSystem != null)
                    {
                        soundSystem.HandleSound(buttonPressedAmount, deltaTime);
                    }

                }
                else if (buttonPressedTime > maxButtonPressedTime)
                {
                    buttonPressedTime = maxButtonPressedTime;
                }

                if (buttonPressedTime > cameraSwitchTreshold && !switchedCamera && auxiliaryButtonPressed && cameraManager != null)
                {
                    cameraManager?.SwitchCameraMode(SimpleBoardCameraManager.CameraMode.Aim);
                    switchedCamera = true;
                }
            }
        }
    }
}
