using SmallHedge.SoundManager;
using System;
using UnityEngine;

public class PitchableSingleSourceSound : FloatFlexibleSound
{
    [Header("Looping Audio Source")]
    [SerializeField] AudioSource thrusterSound;

    [Tooltip("Should be the same as the Thrusters Max Throttle")]
    [SerializeField] float maxThrottle = 1f;

    [Header("Sound Settings")]
    [SerializeField] float minPitch = 0.2f;
    [SerializeField] float maxPitch = 2f;
    [SerializeField] float volume = 0.2f, easeInTime = 1f, easeOutTime = 1f;
    [Tooltip("Pitch = Power * Pitch Bias / Max Throttle + Pitch Offset")]
    [SerializeField] float pitchBias = 1f, pitchOffset = 0f;

    [Header("StartUp and ShutDown Settings")]
    [SerializeField] bool playsStartSound = false;
    [SerializeField] bool playsEndSound = false;
    [SerializeField] string startUpSound = "RocketEngage", shutDownSound = "RocketDisengage";
    [SerializeField] float startUpVolume = 0.2f, shutDownVolume = 0.2f;

    private bool soundOn;
    void PlaySound(string sound, AudioSource audioSource = null, float volume = 1f)
    {
        if (sound == null) return;

        SoundType soundType;

        if (Enum.TryParse(sound, true, out soundType))
        {
            SoundManager.PlaySound(soundType, audioSource, volume);
        }
    }

    public override void HandleSound(float power, float deltaTime)
    {
        if (power <= 0.001f) // Thrust Off
        {
            if (soundOn) // Shuts Down
            {
                if (playsEndSound && shutDownSound != null)
                {
                    PlaySound(shutDownSound, null, shutDownVolume);
                }
                soundOn = false;
            }

            // Target values when thrust is off
            float targetPitch = minPitch;
            float targetVolume = 0f;

            // Update clean thruster
            if (thrusterSound != null)
            {
                // Smoothly adjust pitch and volume towards targets
                thrusterSound.pitch = easeOutTime > 0 ? Mathf.MoveTowards(thrusterSound.pitch, targetPitch, deltaTime / easeOutTime) : targetPitch;
                thrusterSound.volume = easeOutTime > 0 ? Mathf.MoveTowards(thrusterSound.volume, targetVolume, (volume * deltaTime) / easeOutTime) : targetVolume;

                if (thrusterSound.volume <= 0f && thrusterSound.isPlaying)
                {
                    thrusterSound.Stop();
                }
            }
        }
        else // Thrust On
        {
            if (!soundOn) // Starts Up
            {
                if (playsStartSound && startUpSound != null)
                {
                    PlaySound(startUpSound, null, startUpVolume);
                }
                soundOn = true;

                // Start playing sounds if not already
                if (thrusterSound != null && !thrusterSound.isPlaying)
                {
                    thrusterSound.Play();
                }
            }

            // Calculate target pitches based on current power
            if (maxThrottle <= 0f)
            {
                maxThrottle = 1f;
            }
            float targetPitch = Mathf.Lerp(minPitch, maxPitch, (power * pitchBias / maxThrottle) + pitchOffset);
            float targetVolume = volume;

            // Update clean thruster
            if (thrusterSound != null)
            {
                // Smoothly adjust pitch and volume towards targets
                thrusterSound.pitch = easeInTime > 0 ? Mathf.MoveTowards(thrusterSound.pitch, targetPitch, deltaTime / easeInTime) : targetPitch;
                thrusterSound.volume = easeInTime > 0 ? Mathf.MoveTowards(thrusterSound.volume, targetVolume, (volume * deltaTime) / easeInTime) : targetVolume;
            }
        }
    }
}
