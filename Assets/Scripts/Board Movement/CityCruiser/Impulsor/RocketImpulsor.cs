using UnityEngine;
using SmallHedge.SoundManager;
using System;

namespace Assets.Scripts.scrible.Impulsor
{
    public class RocketImpulsor: BaseImpulsor
    {
        [SerializeField] AudioSource thrusterClean, thrusterNoise;
        [SerializeField] string startUpSound = "RocketEngage", shutDownSound = "RocketDisengage";
        [SerializeField] float startUpVolume = 0.2f, shutDownVolume = 0.2f;
        [SerializeField] float minCleanPitch = 0.2f, maxCleanPitch = 2f, cleanVolume = 0.2f, cleanEaseInTime = 1f, cleanEaseOutTime = 1f, cleanPitchBias = 1f, cleanPitchOffset = 0f;
        [SerializeField] float minNoisePitch = 0.2f, maxNoisePitch = 2f, noiseVolume = 0.2f, noiseEaseInTime = 1f, noiseEaseOutTime = 1f, noisePitchBias = 1f, noisePitchOffset = 0f;
        private float targetCleanPitch, targetNoisePitch, targetCleanVolume, targetNoiseVolume;
        private bool soundOn, cleanPitchSet = false, noisePitchSet = false, cleanVolumeSet = false, noiseVolumeSet = false;

        private float cleanPitchEaseFactor, noisePitchEaseFactor;

        [SerializeField] bool playsStartSound = false, playsEndSound = false;

        public float power = 7f;
        private Rigidbody parentRigidbody;
        public ParticleSystem rocketParticles;
        private ParticleSystem.EmissionModule emissionModule;
        [SerializeField] float emissionFactor;
        [SerializeField] float emissionSpeedFactor;

        public bool isPIDControlled = false;
        public int isAssignedToLeftTrigger = 0;
        public int isAssignedToRightTrigger = 0;

        public int isAirborneAffected = 0;
        public float airborneTresholdFactor = 1f;
        public float airborneMultiplyer = 1f;
        public float airborneAffectedMultiplyerMinimum = 0f;
        public float airborneAffectedMultiplyer = 1f;

        public float activeFactor = 1f;
        public Vector3 ThrottleVector;

        public float Throttle;

        public bool clampThrottle = false;
        public float minThrottle = 0f;
        public float maxThrottle = 0f;
        public float ClampedThrottle;

        public void Clamp(float value, float min, float max)
        {
            ClampedThrottle = Mathf.Clamp(value, min, max);
        }

        private void Awake()
        {
            if (rocketParticles != null)
            {
                emissionModule = rocketParticles.emission;
            }

            if (parentRigidbody == null)
            {
                parentRigidbody = transform.parent?.GetComponent<Rigidbody>();
            }

            if (thrusterClean != null)
            {
                thrusterClean.volume = 0f;
                thrusterClean.pitch = minCleanPitch;
            }

            if (thrusterNoise != null)
            {
                thrusterNoise.volume = 0f;
                thrusterNoise.pitch = minNoisePitch;
            }

            cleanPitchEaseFactor = maxCleanPitch - minCleanPitch;
            noisePitchEaseFactor = maxNoisePitch - minNoisePitch;
        }

        void PlaySound(string sound, AudioSource audioSource = null, float volume = 1f)
        {
            if (sound == null) return;
            SoundType soundType;
            if (Enum.TryParse(sound, true, out soundType))
            {
                SoundManager.PlaySound(soundType, audioSource, volume);
            }
        }

        public void Update()
        {
            if (parentRigidbody == null)
            {
                parentRigidbody = transform.parent?.GetComponent<Rigidbody>();
            }

            ThrottleVector = transform.up * ClampedThrottle;
        }

        public void changeParticleDirection()
        {
            if (isPIDControlled && rocketParticles != null)
            {
                rocketParticles.gravityModifier = rocketParticles.gravityModifier * -1f;                
            }
        }

        private void shootParticles(float throttle)
        {
            if(rocketParticles != null)
            {

                emissionModule.rateOverTime = throttle * emissionFactor;
                rocketParticles.startSpeed = throttle * emissionSpeedFactor;
            }
        }
        
        public void FixedUpdate()
        {
            if (ThrottleVector == Vector3.zero) // Thrust Off
            {
                if (soundOn) // Called if sound switches off
                {
                    if (playsEndSound && shutDownSound != null)
                    {
                        PlaySound(shutDownSound, null, shutDownVolume);
                    }

                    targetCleanPitch = minCleanPitch;
                    targetNoisePitch = minNoisePitch;
                    targetCleanVolume = 0f;
                    targetNoiseVolume = 0f;

                    if (thrusterClean != null)
                    {
                        cleanPitchSet = !(thrusterClean.pitch > targetCleanPitch);
                        cleanVolumeSet = !(thrusterClean.volume > targetCleanVolume);
                        //if (thrusterClean.pitch > targetCleanPitch) { cleanPitchSet = false; }
                        //if (thrusterClean.volume > targetCleanVolume) { cleanVolumeSet = false; }
                    }

                    if (thrusterNoise != null)
                    {
                        noisePitchSet = !(thrusterNoise.pitch > targetNoisePitch);
                        noiseVolumeSet = !(thrusterNoise.volume > targetNoiseVolume);
                        //if (thrusterNoise.pitch > targetNoisePitch) { noisePitchSet = false; }
                        //if (thrusterNoise.volume > targetNoiseVolume) { noiseVolumeSet = false; }
                    }

                    soundOn = false;
                }

                if (thrusterClean != null)
                {
                    if (thrusterClean.isPlaying && thrusterClean.volume == 0f) // Stop sound if volume is 0
                    {
                        thrusterClean.Stop();
                        thrusterClean.pitch = minCleanPitch;
                        cleanPitchSet = true;
                        cleanVolumeSet = true;
                    }

                    if (cleanPitchSet == false) // Ease out pitch, can be interrupted before reaching minCleanPitch
                    {
                        float _thrusterCleanPitch = thrusterClean.pitch;
                        if (_thrusterCleanPitch > targetCleanPitch && cleanEaseOutTime != 0f)
                        { 
                            thrusterClean.pitch = Mathf.Clamp(_thrusterCleanPitch - cleanPitchEaseFactor * Time.fixedDeltaTime / cleanEaseOutTime, targetCleanPitch, _thrusterCleanPitch); 
                        }
                        else 
                        { 
                            thrusterClean.pitch = targetCleanPitch;
                            cleanPitchSet = true;
                        }
                    }

                    if (cleanVolumeSet == false) // Ease out volume, can be interrupted before reaching 0
                    {
                        float _thrusterCleanVolume = thrusterClean.volume;
                        if (_thrusterCleanVolume > targetCleanVolume && cleanEaseOutTime != 0f)
                        {
                            thrusterClean.volume = Mathf.Clamp(_thrusterCleanVolume - cleanVolume * Time.fixedDeltaTime / cleanEaseOutTime, targetCleanVolume, _thrusterCleanVolume); 
                        }
                        else
                        {
                            thrusterClean.volume = targetCleanVolume;
                            cleanVolumeSet = true;
                        }
                    }
                }

                if (thrusterNoise != null)
                {
                    if (thrusterNoise.isPlaying && thrusterNoise.volume == 0f) // Stop sound if volume is 0
                    {
                        thrusterNoise.Stop();
                        thrusterNoise.pitch = minNoisePitch;
                        noisePitchSet = true;
                        noiseVolumeSet = true;
                    }

                    if (noisePitchSet == false) // Ease out pitch, can be interrupted before reaching minNoisePitch
                    {
                        float _thrusterNoisePitch = thrusterNoise.pitch;
                        if (_thrusterNoisePitch > targetNoisePitch && noiseEaseOutTime != 0f)
                        {
                            thrusterNoise.pitch = Mathf.Clamp(_thrusterNoisePitch - noisePitchEaseFactor * Time.fixedDeltaTime / noiseEaseOutTime, targetNoisePitch, _thrusterNoisePitch);
                        }
                        else
                        {
                            thrusterNoise.pitch = targetNoisePitch;
                            noisePitchSet = true;
                        }
                    }

                    if (noiseVolumeSet == false) // Ease out volume, can be interrupted before reaching 0
                    {
                        float _thrusterNoiseVolume = thrusterNoise.volume;
                        if (_thrusterNoiseVolume > targetNoiseVolume && noiseEaseOutTime != 0f)
                        {
                            thrusterNoise.volume = Mathf.Clamp(_thrusterNoiseVolume - noiseVolume * Time.fixedDeltaTime / noiseEaseOutTime, targetNoiseVolume, _thrusterNoiseVolume);
                        }
                        else
                        {
                            thrusterNoise.volume = targetNoiseVolume;
                            noiseVolumeSet = true;
                        }
                    }
                }
                return;
            }
            else  // Thrust On
            {
                if (thrusterClean != null && maxThrottle != 0f)
                {
                    targetCleanPitch = Mathf.Lerp(minCleanPitch, maxCleanPitch, ClampedThrottle * cleanPitchBias / maxThrottle + cleanPitchOffset);
                }

                if (thrusterNoise != null && maxThrottle != 0f)
                {
                    targetNoisePitch = Mathf.Lerp(minNoisePitch, maxNoisePitch, ClampedThrottle * noisePitchBias / maxThrottle + noisePitchOffset);
                }

                if (!soundOn)
                {
                    soundOn = true;

                    if (playsStartSound && startUpSound != null)
                    {
                        PlaySound(startUpSound, null, startUpVolume);
                    }

                    targetCleanVolume = cleanVolume;
                    targetNoiseVolume = noiseVolume;

                    if (thrusterClean != null)
                    {
                        if (thrusterClean.isPlaying == false) { thrusterClean.Play(); }
                        cleanPitchSet = !(thrusterClean.pitch < targetCleanPitch);
                        cleanVolumeSet = !(thrusterClean.volume < targetCleanVolume);
                        //if (thrusterClean.pitch < targetCleanPitch) { cleanPitchSet = false; }
                        //if (thrusterClean.volume < targetCleanVolume) { cleanVolumeSet = false; }
                    }

                    if (thrusterNoise != null)
                    {
                        if (thrusterNoise.isPlaying == false) { thrusterNoise.Play(); }
                        noisePitchSet = !(thrusterNoise.pitch < targetNoisePitch);
                        noiseVolumeSet = !(thrusterNoise.volume < targetNoiseVolume);
                        //if (thrusterNoise.pitch < targetNoisePitch) { noisePitchSet = false; }
                        //if (thrusterNoise.volume < targetNoiseVolume) { noiseVolumeSet = false; }
                    }
                }

                // Update Pitches and Volumes with potential easeIn
                if (thrusterClean != null)
                {
                    if (cleanPitchSet == false)
                    {
                        float _thrusterCleanPitch = thrusterClean.pitch;
                        if (_thrusterCleanPitch < targetCleanPitch && cleanEaseInTime != 0f)
                        {
                            thrusterClean.pitch = Mathf.Clamp(_thrusterCleanPitch + cleanPitchEaseFactor * Time.fixedDeltaTime / cleanEaseInTime, _thrusterCleanPitch, targetCleanPitch);
                        }
                        else
                        {
                            thrusterClean.pitch = targetCleanPitch;
                            cleanPitchSet = true;
                        }
                    }
                    else if (thrusterClean.pitch != targetCleanPitch) thrusterClean.pitch = targetCleanPitch;

                    if (cleanVolumeSet == false)
                    {
                        float _thrusterCleanVolume = thrusterClean.volume;
                        if (_thrusterCleanVolume < targetCleanVolume && cleanEaseInTime != 0f)
                        {
                            thrusterClean.volume = Mathf.Clamp(_thrusterCleanVolume + cleanVolume * Time.fixedDeltaTime / cleanEaseInTime, _thrusterCleanVolume, targetCleanVolume);
                        }
                        else
                        {
                            thrusterClean.volume = targetCleanVolume;
                            cleanVolumeSet = true;
                        }
                    }

                }

                if (thrusterNoise != null)
                {
                    if (noisePitchSet == false)
                    {
                        float _thrusterNoisePitch = thrusterNoise.pitch;
                        if (_thrusterNoisePitch < targetNoisePitch && noiseEaseInTime != 0f)
                        {
                            thrusterNoise.pitch = Mathf.Clamp(_thrusterNoisePitch + noisePitchEaseFactor * Time.fixedDeltaTime / noiseEaseInTime, _thrusterNoisePitch, targetNoisePitch);
                        }
                        else
                        {
                            thrusterNoise.pitch = targetNoisePitch;
                            noisePitchSet = true;
                        }
                    }
                    else if (thrusterNoise.pitch != targetNoisePitch) thrusterNoise.pitch = targetNoisePitch;

                    if (noiseVolumeSet == false)
                    {
                        float _thrusterNoiseVolume = thrusterNoise.volume;
                        if (_thrusterNoiseVolume < targetNoiseVolume && noiseEaseInTime != 0f)
                        {
                            thrusterNoise.volume = Mathf.Clamp(_thrusterNoiseVolume + noiseVolume * Time.fixedDeltaTime / noiseEaseInTime, _thrusterNoiseVolume, targetNoiseVolume);
                        }
                        else
                        {
                            thrusterNoise.volume = targetNoiseVolume;
                            noiseVolumeSet = true;
                        }
                    }
                }
            }

            if (parentRigidbody != null)
            {
                parentRigidbody.AddForce(ThrottleVector, ForceMode.Impulse);
            }

            shootParticles(ClampedThrottle);
        }
    }
}
