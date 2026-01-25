using UnityEngine;
using SmallHedge.SoundManager;

public class Jump : BaseGadget
{
    [SerializeField] float volume = 0.2f;
    public int assignButton = 0;
    [SerializeField] int maximumJumps = 1;
    public int airBorneJumpsTaken = 0;
    [SerializeField] float coolDown = 0.5f;
    [SerializeField] float particleDuration = 0.2f;
    private float timeSinceJump = 0f;
    [SerializeField] float jumpStrength = 5f;
    [SerializeField] float heightTreshold = 1f;
    GameObject parentObject;
    CitycruseController parentScript;
    private Rigidbody parentRigidbody;
    public ParticleSystem jumpParticles;
    private ParticleSystem.EmissionModule emissionModule;
    [SerializeField] float burstAmount = 1000f;

    private void Awake()
    {
        timeSinceJump = coolDown;
        if(jumpParticles != null)
        {
            emissionModule = jumpParticles.emission;
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

        if(timeSinceJump < coolDown)
        {
            timeSinceJump = timeSinceJump + Time.deltaTime;

            if (jumpParticles != null && timeSinceJump < particleDuration)
            {
                emissionModule.rateOverTime = burstAmount;
            }
        }

        if (jumpParticles != null && timeSinceJump >= particleDuration)
        {
                emissionModule.rateOverTime = 0f;            
        }

        if (timeSinceJump >= coolDown && airBorneJumpsTaken > 0f && parentScript.heightFactor <= heightTreshold)
        {
            airBorneJumpsTaken = 0;
        }

        if (wasPressed != buttonPressed)
        {
            if (buttonPressed) 
            {           

                if(parentScript.heightFactor <= heightTreshold && timeSinceJump >= coolDown)
                {
                    airBorneJumpsTaken = 0;
                }

                else if(parentScript.heightFactor > heightTreshold)
                {
                    airBorneJumpsTaken ++;
                }

                if ((airBorneJumpsTaken < maximumJumps || maximumJumps == -1f) && timeSinceJump >= coolDown)
                {
                    //Debug.Log("Wahoo");
                    parentRigidbody.AddForceAtPosition(transform.up * jumpStrength, transform.position);
                    SoundManager.PlaySound(SoundType.Jump, null, volume);

                    timeSinceJump = 0f;                    
                }
            }

            wasPressed = buttonPressed;
        }
    }
}
