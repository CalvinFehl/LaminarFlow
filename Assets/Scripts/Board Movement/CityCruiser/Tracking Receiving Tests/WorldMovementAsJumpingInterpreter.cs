using Assets.Scripts.scrible.Impulsor;
using System;
using UnityEngine;

public class WorldMovementAsJumpingInterpreter : MonoBehaviour
{
    [SerializeField] private Gravitation gravitation;
    [SerializeField] private float threshold = 2.5f;
    [SerializeField] private Aerodynamic aerodynamic;
    [SerializeField] private Rigidbody boardRigidbody;
    [SerializeField] private int framesOfSkepsis = 5;
    private int currentSkepsisFrame = 0;
    private bool isMaybeJumping = false;
    public bool IsJumping = false;

    public event Action<bool> OnIsJumpingUpdated;

    private void OnEnable()
    {
        LocationAsWorldMovementInterpreter locationTracker = FindObjectOfType<LocationAsWorldMovementInterpreter>();
        if (locationTracker != null) { locationTracker.OnLocationUpdated += UpdateJumping; }
    }

    private void OnDisable()
    {
        LocationAsWorldMovementInterpreter locationTracker = FindObjectOfType<LocationAsWorldMovementInterpreter>();
        if (locationTracker != null) { locationTracker.OnLocationUpdated -= UpdateJumping; }
    }

    void Start()
    {
        if (gravitation == null) gravitation = FindObjectOfType<Gravitation>();
        if (aerodynamic == null) aerodynamic = GetComponent<Aerodynamic>();
        if (boardRigidbody == null) boardRigidbody = GetComponent<Rigidbody>();
    }

    public void UpdateJumping(LocationAsWorldMovementEventData eventData)
    {
        if (eventData == null || gravitation == null) return;

        float currentFallingValue = eventData.LocationAcceleration.y / Time.fixedDeltaTime;
        float referenceFallingValue = gravitation.gravityVector.y;

        //Debug.Log($"CurrentFallingSimple: {currentFallingValue}");

        if (aerodynamic != null && boardRigidbody != null)
        {
            currentFallingValue -= aerodynamic.LiftAndDrag.y / boardRigidbody.mass;
            //Debug.Log($"CurrentFallingWithAerodynamics: {currentFallingValue}");
        }

        bool isWithinThreshold = Mathf.Abs(referenceFallingValue - currentFallingValue) <= threshold;

        if (isWithinThreshold != IsJumping) //potential jump detected
        {
            if (isWithinThreshold == isMaybeJumping) 
            {
                if (currentSkepsisFrame < framesOfSkepsis) currentSkepsisFrame++;
                else
                {
                    IsJumping = isWithinThreshold;
                    isMaybeJumping = isWithinThreshold;
                    currentSkepsisFrame = 0;
                    //Debug.Log(IsJumping ? "Jumping" : "Not Jumping");
                    OnIsJumpingUpdated?.Invoke(IsJumping);
                }
            }
            else
            {
                isMaybeJumping = isWithinThreshold;
                currentSkepsisFrame = 0;
            }
        }
        //Debug.Log("Jumping: " + IsJumping);
    }
}