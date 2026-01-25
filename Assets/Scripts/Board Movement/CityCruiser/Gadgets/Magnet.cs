using UnityEngine;
using SmallHedge.SoundManager;

namespace Assets.Scripts.scrible.Gadgets
{
    public class Magnet : BaseGadget
    {
        [SerializeField] AudioSource boardAudioSource;
        [SerializeField] float volume = 0.2f;

        public bool usesPredictor = false;
        public int assignButton = 0;
        public bool magnetismEngaged = false;

        [SerializeField] bool usesBattery = true;
        [SerializeField] OldBattery oldBattery;
        [SerializeField] float energyConsumptionPerSecond = 0.2f;
        [SerializeField] float minimumEnergyForStartMagnet = 0.5f;
        [SerializeField] float stopMagnetTresholdEnergy = 0.01f;

        public ParticleSystem magnetParticles;
        private ParticleSystem.EmissionModule emissionModule;
        [SerializeField] float emissionRateOverTime = 300f;
        public bool magnetismSwitchable { get; set; } = true;
        public float activateTreshold = 3f;
        public float releaseTreshold = 6f;

        GameObject parentObject;
        CitycruseController parentScript;

        //v3.x --> P, v3.y -->I, v3,z --> D
        public Vector3 magnetizedPIDFactor = new Vector3(1f, 1f, 2f);
        public Vector3 magnetizedRotateXPIDFactor = new Vector3(1f, 1f, 1f);
        public Vector3 magnetizedRotateYPIDFactor = new Vector3(0f, 0f, 0f);
        public Vector3 magnetizedRotateZPIDFactor = new Vector3(1f, 1f, 1f);

        public void Awake()
        {
            if (oldBattery == null)
            {
                oldBattery = GetComponentInParent<OldBattery>();
            }

            if (parentObject == null || parentScript == null)
            {
                parentObject = transform.parent.gameObject;
                parentScript = parentObject?.GetComponent<CitycruseController>();

                if (parentScript == null)
                {
                    Debug.LogWarning("No CitycruseController assigned to Magnet");
                    parentScript = parentObject?.GetComponentInChildren<CitycruseController>();
                }
            }

            if(magnetParticles != null)
            {
                emissionModule = magnetParticles.emission;
            }

            if (boardAudioSource == null)
            {
                boardAudioSource = GetComponentInParent<AudioSource>();
            }
        }

        public void ChangeMagnetState()
        {
            if (parentScript != null)
            {
                parentScript.magnetismEngaged = magnetismEngaged;
                parentScript.ChangePIDValues(magnetizedPIDFactor, magnetizedRotateXPIDFactor, magnetizedRotateYPIDFactor, magnetizedRotateZPIDFactor);

                //Debug.Log("Magnetism " + parentScript.magnetismEngaged);

                if (parentScript.magnetismEngaged == true) SoundManager.PlaySound(SoundType.MagnetEngage, null, volume);
                else SoundManager.PlaySound(SoundType.MagnetRelease, null, volume);
                
                if (boardAudioSource != null)
                {
                    if (magnetismEngaged && !boardAudioSource.isPlaying) boardAudioSource.Play();
                    else if (!magnetismEngaged && boardAudioSource.isPlaying) boardAudioSource.Stop();
                }

                if (magnetParticles != null)
                {
                    if (magnetismEngaged)
                    {
                        emissionModule.rateOverTime = emissionRateOverTime;
                    }
                    else
                    {
                        emissionModule.rateOverTime = 0f;
                    }
                }
                parentScript.UpdateParticles();
            }
        }

        public void Update()
        {
            if (assignedButton != assignButton)
            {
                assignedButton = assignButton;
            }

            if (wasPressed != buttonPressed)
            {
                if (buttonPressed && magnetismSwitchable)
                {
                    //Debug.Log("Magnet " + magnetismEngaged);

                    if (usesBattery && oldBattery != null)
                    {
                        if (!magnetismEngaged)
                        {
                            if (oldBattery.GetCurrentEnergy() > minimumEnergyForStartMagnet)
                            {
                                magnetismEngaged = true;
                                ChangeMagnetState();
                            }
                        }
                        else
                        {
                            magnetismEngaged = false;
                            ChangeMagnetState();
                        }
                    }
                    else
                    {
                        magnetismEngaged = !magnetismEngaged;
                        ChangeMagnetState();
                    }

                    if (magnetismEngaged && magnetParticles != null)
                    {
                        magnetParticles.Play();
                    }
                }

                magnetismSwitchable = !buttonPressed;
                wasPressed = buttonPressed;
            }

            if(parentScript.heightFactor > releaseTreshold && magnetismEngaged)
            {
                magnetismEngaged = false;
                ChangeMagnetState();
            }

            if (magnetismEngaged && usesBattery && oldBattery != null)
            {
                //Debug.Log("Magnet is Consuming Energy: " + energyConsumptionPerSecond * Time.deltaTime);
                oldBattery.UseEnergy(energyConsumptionPerSecond * Time.deltaTime);

                if (oldBattery.GetCurrentEnergy() < stopMagnetTresholdEnergy)
                {
                    magnetismEngaged = false;
                    ChangeMagnetState();
                }
            }
        }
    }
}
