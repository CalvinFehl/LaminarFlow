using SmallHedge.SoundManager;
using System;
using UnityEngine;

public class PitchableDoubleSourceSound : FloatFlexibleSound
{
    [Header("Looping Audio Sources - Clean and Noise")]
    [SerializeField] AudioSource thrusterClean;
    [SerializeField] AudioSource thrusterNoise;

    [Tooltip("Should be the same as the Thrusters Max Throttle")]
    [SerializeField] float maxThrottle = 1f;

    [Header("Clean Settings")]
    [SerializeField] float minCleanPitch = 0.2f;
    [SerializeField] float maxCleanPitch = 2f;
    [SerializeField] float cleanVolume = 0.2f, cleanEaseInTime = 1f, cleanEaseOutTime = 1f;
    [Tooltip("Pitch = Power * Pitch Bias / Max Throttle + Pitch Offset")]
    [SerializeField] float cleanPitchBias = 1f, cleanPitchOffset = 0f;

    [Header("Noise Settings")]
    [SerializeField] float minNoisePitch = 0.2f;
    [SerializeField] float maxNoisePitch = 2f, noiseVolume = 0.2f, noiseEaseInTime = 1f, noiseEaseOutTime = 1f;
    [Tooltip("Pitch = Power * Pitch Bias / Max Throttle + Pitch Offset")]
    [SerializeField] float noisePitchBias = 1f, noisePitchOffset = 0f;

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
            if (soundOn)
            {
                if (playsEndSound && shutDownSound != null)
                {
                    PlaySound(shutDownSound, null, shutDownVolume);
                }
                soundOn = false;
            }

            // Target values when thrust is off
            float targetCleanPitch = minCleanPitch;
            float targetNoisePitch = minNoisePitch;
            float targetCleanVolume = 0f;
            float targetNoiseVolume = 0f;

            // Update clean thruster
            if (thrusterClean != null)
            {
                // Smoothly adjust pitch and volume towards targets
                thrusterClean.pitch = cleanEaseOutTime > 0 ? Mathf.MoveTowards(thrusterClean.pitch, targetCleanPitch, deltaTime / cleanEaseOutTime) : targetCleanPitch;
                thrusterClean.volume = cleanEaseOutTime > 0 ? Mathf.MoveTowards(thrusterClean.volume, targetCleanVolume, (cleanVolume * deltaTime) / cleanEaseOutTime) : targetCleanVolume;

                if (thrusterClean.volume <= 0f && thrusterClean.isPlaying)
                {
                    thrusterClean.Stop();
                }
            }

            // Update noise thruster
            if (thrusterNoise != null)
            {
                thrusterNoise.pitch = noiseEaseOutTime > 0 ? Mathf.MoveTowards(thrusterNoise.pitch, targetNoisePitch, deltaTime / noiseEaseOutTime) : targetNoisePitch;
                thrusterNoise.volume = noiseEaseOutTime > 0 ? Mathf.MoveTowards(thrusterNoise.volume, targetNoiseVolume, (noiseVolume * deltaTime) / noiseEaseOutTime) : targetNoiseVolume;

                if (thrusterNoise.volume <= 0f && thrusterNoise.isPlaying)
                {
                    thrusterNoise.Stop();
                }
            }
        }
        else // Thrust On
        {
            if (!soundOn)
            {
                if (playsStartSound && startUpSound != null)
                {
                    PlaySound(startUpSound, null, startUpVolume);
                }
                soundOn = true;

                // Start playing sounds if not already
                if (thrusterClean != null && !thrusterClean.isPlaying) thrusterClean.Play();
                if (thrusterNoise != null && !thrusterNoise.isPlaying) thrusterNoise.Play();
            }

            // Calculate target pitches based on current power
            if (maxThrottle <= 0f)
            {
                maxThrottle = 1f;
            }
            float targetCleanPitch = Mathf.Lerp(minCleanPitch, maxCleanPitch, (power * cleanPitchBias / maxThrottle) + cleanPitchOffset);
            float targetNoisePitch = Mathf.Lerp(minNoisePitch, maxNoisePitch, (power * noisePitchBias / maxThrottle) + noisePitchOffset);
            float targetCleanVolume = cleanVolume;
            float targetNoiseVolume = noiseVolume;

            // Update clean thruster
            if (thrusterClean != null)
            {
                // Smoothly adjust pitch and volume towards targets
                thrusterClean.pitch = cleanEaseInTime > 0 ? Mathf.MoveTowards(thrusterClean.pitch, targetCleanPitch, deltaTime / cleanEaseInTime) : targetCleanPitch;
                thrusterClean.volume = cleanEaseInTime > 0 ? Mathf.MoveTowards(thrusterClean.volume, targetCleanVolume, (cleanVolume * deltaTime) / cleanEaseInTime) : targetCleanVolume;
            }

            // Update noise thruster
            if (thrusterNoise != null)
            {
                thrusterNoise.pitch = noiseEaseInTime > 0 ? Mathf.MoveTowards(thrusterNoise.pitch, targetNoisePitch, deltaTime / noiseEaseInTime) : targetNoisePitch;
                thrusterNoise.volume = noiseEaseInTime > 0 ? Mathf.MoveTowards(thrusterNoise.volume, targetNoiseVolume, (noiseVolume * deltaTime) / noiseEaseInTime) : targetNoiseVolume;
            }
        }
    }
}