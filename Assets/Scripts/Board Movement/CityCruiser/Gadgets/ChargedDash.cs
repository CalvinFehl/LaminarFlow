using Assets.Scripts.scrible.Input;
using System;
using UnityEngine;
using SmallHedge.SoundManager;
using Unity.VisualScripting;

public class ChargedDash : BaseGadget
{
    public int assignButton = 0;

    [SerializeField] AudioSource boardAudioSource;
    [SerializeField] string dashSound = "Dash";
    [SerializeField] float maxPitch = 2f, minPitch = 1f, chargeVolume = 0.2f, dashVolume = 0.2f;

    //[SerializeField] int maximumDashes = 1;
    //public int airBorneJumpsTaken = 0;
    [SerializeField] float coolDown = 0.5f;
    [SerializeField] float particleDuration = 0.2f;
    [SerializeField] float buttonPressedTime = 0f;
    private float buttonPressedAmount = 0f;
    [SerializeField] float maxButtonPressedTime = 2f;
    [SerializeField] OldBattery oldBattery;
    [SerializeField] float energyConsumption = 1f;
    private float timeSinceDash = 0f;
    [SerializeField] float dashMaxStrength = 15f;
    [SerializeField] float heightTreshold = 1f;

    GameObject parentObject;
    CitycruseController parentScript;
    private Rigidbody parentRigidbody;
    private Transform dashTransform;

    [SerializeField] bool usesAuxiliaryHoldButton = false;
    [SerializeField] GamepadInputReader auxiliaryButton;
    [SerializeField] bool usesRightTrigger = false;
    private bool auxiliaryButtonPressed = false;
    private bool relevantButtonPressed = false;

    public ParticleSystem dashParticles;
    private ParticleSystem.EmissionModule emissionModule;
    [SerializeField] float burstAmount = 1000f;

    public event Action<string> OnAimingCameraUpdated;
    [SerializeField] float cameraSwitchTreshold = 0.5f;
    private bool switchedCamera = false;

    private void Awake()
    {
        timeSinceDash = coolDown;
        if (dashParticles != null)
        {
            emissionModule = dashParticles.emission;
        }
        if (oldBattery == null)
        {
            oldBattery = GetComponentInParent<OldBattery>();
        }
        if (dashTransform == null) 
        {
            dashTransform = GetComponentInChildren<Dash>().transform;
        }
        if (auxiliaryButton == null && usesAuxiliaryHoldButton)
        {
            auxiliaryButton = GetComponentInParent<GamepadInputReader>();
            if (auxiliaryButton == null) Debug.LogWarning("No auxiliary button assigned to ChargedDash");
        }

        if (boardAudioSource == null)
        {
            boardAudioSource = GetComponentInParent<AudioSource>();
        }
    }

    void Update()
    {
        if (auxiliaryButton != null)
        {
            auxiliaryButtonPressed = usesRightTrigger ? auxiliaryButton.GetRightTrigger() > 0.01f : auxiliaryButton.GetLeftTrigger() > 0.01f;
        }

        if (assignedButton != assignButton)
        {
            assignedButton = assignButton;
        }

        if (parentObject == null)
        {
            parentObject = transform.parent.gameObject;
            parentScript = parentObject.GetComponent<CitycruseController>();
        }

        if (parentRigidbody == null)
        {
            parentRigidbody = transform.parent?.GetComponent<Rigidbody>();
        }

        if(timeSinceDash < coolDown)
        {
            timeSinceDash = timeSinceDash + Time.deltaTime;

            if (dashParticles != null && timeSinceDash < particleDuration)
            {
                emissionModule.rateOverTime = burstAmount;
            }
        }

        if (dashParticles != null && timeSinceDash >= particleDuration)
        {
            emissionModule.rateOverTime = 0f;
        }

        if (!usesAuxiliaryHoldButton)
        {
            if (wasPressed != buttonPressed)
            {
                if (buttonPressed) 
                { 
                    wasPressed = buttonPressed;
                }
                else
                {
                    if (oldBattery != null && timeSinceDash >= coolDown)
                    {
                        float availableEnergy = oldBattery.UseEnergy(energyConsumption * buttonPressedAmount) / energyConsumption; // an available Energy of 1 translates to a dash with 1 dashMaxStrength

                        parentRigidbody.AddForceAtPosition(dashTransform.up * dashMaxStrength * availableEnergy, transform.position);

                        if (boardAudioSource != null) 
                        {
                            if (boardAudioSource.isPlaying) boardAudioSource.Stop();
                            if (dashSound != null) 
                            {
                                SoundType soundType;
                                if (Enum.TryParse(dashSound, true, out soundType))
                                {
                                    SoundManager.PlaySound(soundType, null, dashVolume);
                                }
                            }
                        }

                        buttonPressedTime = 0f;
                        timeSinceDash = 0f;
                    }
                    wasPressed = buttonPressed;
                }

                wasPressed = buttonPressed;
            }
            else if (buttonPressed && wasPressed)
            {
                if (buttonPressedTime < maxButtonPressedTime)
                {
                    if (boardAudioSource != null)
                    {
                        if (!boardAudioSource.isPlaying) boardAudioSource.Play();
                    }

                    buttonPressedTime = buttonPressedTime + Time.deltaTime;
                    buttonPressedAmount = buttonPressedTime / maxButtonPressedTime;
                }
                else if (buttonPressedTime > maxButtonPressedTime)
                {
                    buttonPressedTime = maxButtonPressedTime;
                }
                if (boardAudioSource != null) 
                {
                    if (boardAudioSource.isPlaying) boardAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, buttonPressedAmount);
                }
            }
        }
        else 
        {
            if (buttonPressed && !wasPressed) 
            {
                relevantButtonPressed = true;
                wasPressed = buttonPressed;
            }

            if (relevantButtonPressed) 
            {
                if (!buttonPressed && !auxiliaryButtonPressed)
                {
                    relevantButtonPressed = false;

                    if (oldBattery != null && timeSinceDash >= coolDown)
                    {
                        float availableEnergy = oldBattery.UseEnergy(energyConsumption * buttonPressedAmount) / energyConsumption; // an available Energy of 1 translates to a dash with 1 dashMaxStrength

                        parentRigidbody.AddForceAtPosition(dashTransform.up * dashMaxStrength * availableEnergy, transform.position);

                        if (boardAudioSource != null)
                        {
                            if (boardAudioSource.isPlaying) boardAudioSource.Stop();
                            if (buttonPressedAmount > 0.1f) 
                            {
                                if (dashSound != null)
                                {
                                    SoundType soundType;
                                    if (Enum.TryParse(dashSound, true, out soundType))
                                    {
                                        SoundManager.PlaySound(soundType, null, dashVolume);
                                    }
                                }
                            }
                        }

                        buttonPressedTime = 0f;
                        timeSinceDash = 0f;
                        if(switchedCamera)
                        {
                            OnAimingCameraUpdated?.Invoke("Follow");
                            switchedCamera = false;
                        }
                    }
                    wasPressed = buttonPressed;
                }
                else if (buttonPressed)
                {
                    if (buttonPressedTime < maxButtonPressedTime)
                    {
                        buttonPressedTime = buttonPressedTime + Time.deltaTime;
                        buttonPressedAmount = buttonPressedTime / maxButtonPressedTime;

                        if (boardAudioSource != null)
                        {
                            if (!boardAudioSource.isPlaying) boardAudioSource.Play();
                            if (boardAudioSource.isPlaying) boardAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, buttonPressedAmount);
                        }

                    }
                    else if (buttonPressedTime > maxButtonPressedTime)
                    {
                        buttonPressedTime = maxButtonPressedTime;
                    }

                    if (buttonPressedTime > cameraSwitchTreshold && !switchedCamera && auxiliaryButtonPressed)
                    {
                        OnAimingCameraUpdated?.Invoke("Aim");
                        switchedCamera = true;
                    }
                }
            }
        }
    }
}
