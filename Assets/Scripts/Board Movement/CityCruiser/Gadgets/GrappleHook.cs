using Assets.Scripts.scrible.Input;
using UnityEngine;
using SmallHedge.SoundManager;
using System;

public class GrappleHook : BaseGadget
{
    [SerializeField] string shootSound = "GrappleShoot", hitSound = "GrappleHit", tightenSound = "GrappleTighten", releaseSound = "GrappleRelease";
    [SerializeField] float shootVolume = 0.6f, hitVolume = 0.6f, tightenVolume = 0.6f, releaseVolume = 0.5f;

    public int assignButton = 1;
    public bool hooked = false;
    public float maxRange = 300f;

    [SerializeField] bool usesAnalog = false;
    [SerializeField] GamepadInputReader gamepadInputReader;
    [SerializeField] float defaultShootDirectionFrontUp = 0.5f;

    GameObject parentObject;
    CitycruseController parentScript;
    private Rigidbody parentRigidbody;

    private Vector3 hookPoint;
    private SpringJoint rope;
    [SerializeField] private LineRenderer ropeRenderer;
    private Vector3 currentRopePosition;

    public GameObject hookObject;
    public float grapplePullSpeed;
    public float distanceFromPoint;

    [SerializeField] float ropeMinLength;
    [SerializeField] float ropeMaxLength;
    [SerializeField] float ropeSpring;    
    [SerializeField] float ropeDamper;    
    [SerializeField] float ropeMassScale;

    [SerializeField] bool usesBattery = true;
    [SerializeField] OldBattery oldBattery;
    [SerializeField] float energyConsumptionPerSecond = 0.2f;
    [SerializeField] float minimumEnergyForStartSwing = 0.5f;
    [SerializeField] float stopSwingTresholdEnergy = 0.01f;
    [SerializeField] bool canKeepHangingWithoutEnergy = false;

    private void Start()
    {
        if (ropeRenderer == null)
        {
            ropeRenderer = GetComponent<LineRenderer>();
        }

        if (oldBattery == null)
        {
            oldBattery = GetComponentInParent<OldBattery>();
        }

        if (usesAnalog && gamepadInputReader == null)
        {
            gamepadInputReader = GetComponentInParent<GamepadInputReader>();
        }
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

    void StartSwing()
    {
        if (usesAnalog && gamepadInputReader != null) 
        {
            defaultShootDirectionFrontUp = 0.5f + gamepadInputReader.GetRightStick().y / 2f;
        }

        PlaySound(shootSound, null, shootVolume);

        if (Physics.Raycast(transform.position, Vector3.Lerp(parentObject.transform.forward, parentObject.transform.up, defaultShootDirectionFrontUp), out RaycastHit hook, maxRange))
        {
            PlaySound(hitSound, null, hitVolume);
            PlaySound(tightenSound, null, tightenVolume);

            hooked = true;
            hookPoint = hook.point;
            //Instantiate(hookObject, hookPoint, Quaternion.identity);
            rope = parentObject.AddComponent<SpringJoint>();
            rope.connectedAnchor = hookPoint;            

            distanceFromPoint = Vector3.Distance(transform.position, hookPoint);

            rope.minDistance = distanceFromPoint * ropeMinLength;
            rope.maxDistance = distanceFromPoint * ropeMaxLength;

            rope.spring = ropeSpring;
            rope.damper = ropeDamper;
            rope.massScale = ropeMassScale;
            
            ropeRenderer.positionCount = 2;
            currentRopePosition = parentObject.transform.position;
        }
    }
    void DrawRope()
    {
        if (!rope) return;

        currentRopePosition = Vector3.Lerp(currentRopePosition, hookPoint, Time.deltaTime * 4f);

        ropeRenderer.SetPosition(0, parentObject.transform.position);
        ropeRenderer.SetPosition(1, currentRopePosition);
    }

    void StopSwing()
    {
        SoundManager.PlaySound(SoundType.GrappleRelease, null, releaseVolume);
        ropeRenderer.positionCount = 0;
        hooked = false;
        Destroy(rope);
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        if (hooked)
        {
            Vector3 distance = hookPoint - parentObject.transform.position;

            if (usesAnalog && gamepadInputReader != null)
            {
                parentRigidbody.AddForce(distance * grapplePullSpeed * gamepadInputReader.GetRightStick().y * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
            else 
            {
                float angle = Vector3.Angle(distance, parentObject.transform.up);
                float angleFactor = angle > 180 ? 360f - angle : angle;

                parentRigidbody.AddForce(distance * grapplePullSpeed * (0.8f - angleFactor / 360f) / Mathf.Clamp(distanceFromPoint, 1f, maxRange), ForceMode.Acceleration);
            }
        }
    }

    void Update()
    {
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

        if (wasPressed != buttonPressed)
        {
            if (buttonPressed && !hooked) 
            {
                if (usesBattery) 
                {
                    if (oldBattery != null)
                    {
                        if (oldBattery.GetCurrentEnergy() > minimumEnergyForStartSwing)
                        {
                            StartSwing();
                        }
                    }
                }
                else 
                {
                    StartSwing();
                }
            }

            if (!buttonPressed && hooked)
            {
                StopSwing();
            }

            wasPressed = buttonPressed;
        }

        if (wasPressed && hooked)
        {
            if (usesBattery)
            {
                if (oldBattery != null)
                {
                    if (!canKeepHangingWithoutEnergy)
                    {
                        if (oldBattery.GetCurrentEnergy() < stopSwingTresholdEnergy)
                        {
                            StopSwing();
                        }
                    }

                    //Debug.Log("Grapple is Consuming Energy: " + energyConsumptionPerSecond * Time.deltaTime);
                    oldBattery.UseEnergy(energyConsumptionPerSecond * Time.deltaTime);
                }
            }
        }
    }
}
