using UnityEngine;

public class Dash : BaseGadget
{
    public int assignButton = 0;
    [SerializeField] int maximumDashes = 1;
    public int airBorneJumpsTaken = 0;
    [SerializeField] float coolDown = 0.5f;
    [SerializeField] float particleDuration = 0.2f;
    private float timeSinceDash = 0f;
    [SerializeField] float jumpStrength = 15f;
    [SerializeField] float heightTreshold = 1f;
    GameObject parentObject;
    CitycruseController parentScript;
    private Rigidbody parentRigidbody;
    public ParticleSystem dashParticles;
    private ParticleSystem.EmissionModule emissionModule;
    [SerializeField] float burstAmount = 1000f;

    private void Awake()
    {
        timeSinceDash = coolDown;
        if(dashParticles != null)
        {
            emissionModule = dashParticles.emission;
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

        if (timeSinceDash >= coolDown && airBorneJumpsTaken > 0f && parentScript.heightFactor <= heightTreshold)
        {
            airBorneJumpsTaken = 0;
        }

        if (wasPressed != buttonPressed)
        {
            if (buttonPressed) 
            { 
            
                if(parentScript.heightFactor <= heightTreshold && timeSinceDash >= coolDown)
                {
                    airBorneJumpsTaken = 0;
                }

                else if(parentScript.heightFactor > heightTreshold)
                {
                    airBorneJumpsTaken ++;
                }

                if ((airBorneJumpsTaken < maximumDashes || maximumDashes == -1f) && timeSinceDash >= coolDown)
                {
                    //Debug.Log("Wahoo");
                    parentRigidbody.AddForceAtPosition(GetComponentInChildren<Dash>().transform.up * jumpStrength, transform.position);


                    timeSinceDash = 0f;                    
                }
            }

            wasPressed = buttonPressed;
        }
    }
}
