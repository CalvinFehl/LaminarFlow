using Assets.Scripts.scrible.Feedback_moduls;
using Assets.Scripts.scrible.Gadgets;
using Assets.Scripts.scrible.Impulsor;
using Assets.Scripts.scrible.Input;
using Assets.Scripts.scrible.Sensors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CitycruseController : MonoBehaviour
{
    public List<InputReader> inputReaders = new List<InputReader>();
    public List<BaseSensor> sensors = new List<BaseSensor>();
    public List<BaseImpulsor> passiveImpulsors = new List<BaseImpulsor>();
    public List<BaseImpulsor> rotators = new List<BaseImpulsor>();
    public List<BaseImpulsor> activeImpulsors = new List<BaseImpulsor>();
    public List<BaseGadget> gadgets = new List<BaseGadget>();
    public List<int> assignedButtons;
    public List<Feedbackmodul> feedbackmoduls = new List<Feedbackmodul>();
    
    public float flightHeight = 1.0f;
    public float heightFactor = 1f;

    public float distanceToTarget;
    public Vector3 groundNormal;
    public float groundNormalDistance;
    public float groundNormalHeightFactor;
    public float groundRelatedTilt;
    public string hitTag;

    public float distanceToLanding;
    public Vector3 landingNormal;
    public string landingTag;

    public int gadgetCount = 0;

    public Vector3 gravityDefault;
    public Vector3 currentGravity;
    public bool magnetismEngaged = false;

    private Dictionary<int, System.Func<bool>> inputMethods;
    public void ChangePIDValues(Vector3 PIDfac, Vector3 xRotPIDfac, Vector3 yRotPIDfac, Vector3 zRotPIDfac)
    {
        if (feedbackmoduls != null)
        {
            foreach (var PIDFeed in feedbackmoduls)
            {
                if (PIDFeed is PIDRotationFeedbackmodul pIDRotationFeedbackmodul)
                {
                    pIDRotationFeedbackmodul.OverwritePIDValues(xRotPIDfac, yRotPIDfac, zRotPIDfac, magnetismEngaged);
                }
                else if (PIDFeed is PIDFeedbackmodul pIDFeedbackmodul)
                {
                    pIDFeedbackmodul.OverwritePIDValues(PIDfac, magnetismEngaged);
                }
            }
        }
    }
    public void RecalibrateButtonAssignments() 
    { 
        if(assignedButtons.Count != gadgets.Count)
        {
            if (assignedButtons.Count < gadgets.Count)
            {
                assignedButtons.Add(0);
            }

            else if (assignedButtons.Count > gadgets.Count)
            {
                assignedButtons.RemoveAt(0);
            }                    
        }

        Debug.Log("recalibrating ButtonAssignment");
        if(gadgets.Count != 0)
        {
            for (int i = 0; i < gadgets.Count; i++)
            {
                assignedButtons[i] = gadgets[i].AssignButton();
            }
        }

    }

    private void Start()
    {
        if(passiveImpulsors != null)
        {
            var gravitation = (Gravitation)passiveImpulsors[0];

            gravityDefault = gravitation.DefaultGravity();
            currentGravity = gravityDefault;
            foreach(var psensor in sensors)
            {
                if(psensor is Predictor predictor)
                {
                    predictor.ChangeGravity(currentGravity);
                }
            }
        }
    }

    private void Awake()
    {
        var inputRead = (GamepadInputReader)inputReaders[0];

        inputMethods = new Dictionary<int, System.Func<bool>>()
        {
            { 1, () => inputRead.GetLeftShoulder() },
            { 2, () => inputRead.GetRightShoulder() },
            { 3, () => inputRead.GetLeftStickButton() },
            { 4, () => inputRead.GetRightStickButton() },
            { 5, () => inputRead.GetAButton() },
            { 6, () => inputRead.GetBButton() },
            { 7, () => inputRead.GetXButton() },
            { 8, () => inputRead.GetYButton() },
            { 9, () => inputRead.GetNorthButton() },
            { 10, () => inputRead.GetEastButton() },
            { 11, () => inputRead.GetSouthButton() },
            { 12, () => inputRead.GetWestButton() },
            { 13, () => inputRead.GetStartButton() }
        };
    }

    public void GadgetsActivation()
    {
        var inputRead = (GamepadInputReader)inputReaders[0];

        if(gadgetCount != gadgets.Count)
        {
            RecalibrateButtonAssignments();
            gadgetCount = gadgets.Count;

            //Debug.Log("GadgetCount: " + gadgetCount);
        }

        for (int i = 0; i < gadgetCount; i++)
        {
            if (inputMethods.TryGetValue(assignedButtons[i], out var inputMethod))
            {
                gadgets[i].buttonPressed = inputMethod.Invoke();
                //Debug.Log("Assigned " + i + gadgets[i].buttonPressed);
            }
        }
    }

    public void UpdateParticles()
    {
        foreach (var impulsor in activeImpulsors)
        {
            if (impulsor is RocketImpulsor rocketImpulsor)
            {
                rocketImpulsor.changeParticleDirection();
            }
        }
    }


    private void Update()
    {
        var inputRead = (GamepadInputReader)inputReaders[0];

        if (Input.GetKeyDown(KeyCode.Space) || inputRead.GetStartButton() == true)
        {
            RecalibrateButtonAssignments();
        }

        GadgetsActivation();

    }

    void FixedUpdate()
    {
        var inputRead = (GamepadInputReader)inputReaders[0];

        float leftTriggerValue = inputRead.GetLeftTrigger();
        float rightTriggerValue = inputRead.GetRightTrigger();
        Vector4 StickValue = inputRead.RotInput;

        var PIDFeedbackmodul = (PIDFeedbackmodul)feedbackmoduls[0];
        var PIDRotFeedbackmodul = (PIDRotationFeedbackmodul)feedbackmoduls[1];

        if (sensors != null)
        {
            foreach (var sensor in sensors)
            {
                if (sensor is RaycastSensor tsensor)
                {

                    distanceToTarget = tsensor.hit ? tsensor.distance : 1000000f;
                    groundNormal = tsensor.groundNormal;
                    hitTag = tsensor.hit ? tsensor.hitTag : string.Empty;
                    groundRelatedTilt = tsensor.hit ? Vector3.Angle(groundNormal, transform.up) : 0f;
                    if (tsensor.hit)
                    {
                        if(Physics.Raycast(transform.position, groundNormal * -1, out RaycastHit groundNormalRay, distanceToTarget))
                        {
                            groundNormalDistance = groundNormalRay.distance;
                        }
                        else
                        {
                            groundNormalDistance = distanceToTarget;
                        }
                    }
                }

                if(sensor is Predictor predictor)
                {
                    distanceToLanding = predictor.hit ? predictor.distance : 1000000f;
                    landingNormal = predictor.groundNormal;
                    landingTag = predictor.hit ? predictor.hitTag : string.Empty;

                }
            }


            PIDFeedbackmodul.Target = flightHeight;
            PIDFeedbackmodul.Current = PIDFeedbackmodul.usesPredictor == true ? distanceToLanding : distanceToTarget;

            PIDRotFeedbackmodul.Target = PIDRotFeedbackmodul.usesPredictor == true ? landingNormal : groundNormal;
            //PIDRotFeedbackmodul.Current = transform.up;

            if (flightHeight != 0f && distanceToTarget > 0f)
            {
                heightFactor = distanceToTarget / flightHeight;     //heightfactor < 1, when below flightHeight
                groundNormalHeightFactor = groundNormalDistance / flightHeight;
            }

            foreach (var gadget in gadgets)
            {
                if (gadget is Magnet magnet)
                {
                    if (hitTag == "Magnetic" && !magnetismEngaged && magnet.activateTreshold > heightFactor)
                    {                
                        //Debug.Log("over magnetic grounds");

                        magnetismEngaged = true;
                        ChangePIDValues(magnet.magnetizedPIDFactor, magnet.magnetizedRotateXPIDFactor, magnet.magnetizedRotateYPIDFactor, magnet.magnetizedRotateZPIDFactor);
                        magnet.magnetismEngaged = magnetismEngaged;
                        UpdateParticles();
                    }
                }
            }

            if (passiveImpulsors != null)
            {
                foreach (var passiveImpulsors in passiveImpulsors)
                {
                    if (passiveImpulsors is Gravitation gravitation)
                    {
                        if (magnetismEngaged)
                        {
                            currentGravity = gravitation.MagnetGravity(groundNormal);
                            foreach (var psensor in sensors)
                            {
                                if (psensor is Predictor predictor)
                                {
                                    predictor.ChangeGravity(currentGravity);
                                }
                            }

                        }

                        else if (currentGravity != gravityDefault)
                        {
                            currentGravity = gravitation.DefaultGravity();
                            //Debug.Log("Gravity Reset");
                            foreach (var psensor in sensors)
                            {
                                if (psensor is Predictor predictor)
                                {
                                    predictor.ChangeGravity(currentGravity);
                                    predictor.PredictLanding();
                                }
                            }
                        }
                    }
                }
            }
        }

        if (rotators != null)
        {
            foreach (var rotater in rotators)
            {

                if (rotater is ActiveRotater rotator)
                {

                    float airborneFactor = 1f;
                    //               rotator.tiltCompensation = rotator.coneFactor * groundRelatedTilt / 90f;
                    float airborneTresholdFactor = rotator.airborneTresholdFactor /** (1 + rotator.tiltCompensation)*/;
                    float heightThreshold = heightFactor / airborneTresholdFactor;
                    //               rotator.newHeight = flightHeight * airborneTresholdFactor;


                    rotator.RotThrottle = rotator.isPIDControlled ? new Vector3(PIDRotFeedbackmodul.xResponse, PIDRotFeedbackmodul.yResponse, PIDRotFeedbackmodul.zResponse) : new Vector3(StickValue.x * rotator.pitchSpeed, StickValue.y * rotator.yawSpeed, StickValue.z * rotator.rollSpeed);

                    if (rotator.isAirborneAffected != 0 && sensors != null)
                    {
                        // 1 -> aM wenn über aTf sonst 1;     2 -> aM*  hT  +aMmin wenn unter aTf sonst 1;     3 -> aM*  hT²  +aMin wenn unter aTf sonst 1;     
                        //-1 -> aM wenn unter aTf sonst 0;   -2 -> aM*(1-hT)+aMmin wenn unter aTf sonst 0;    -3 -> aM*(1-hT²)+aMin wenn unter aTf sonst 0;     -4 -> aM wenn über aTf sonst 0;
                        switch (rotator.isAirborneAffected)
                        {
                            case 1:
                                airborneFactor = groundNormalHeightFactor > airborneTresholdFactor ? rotator.airborneMultiplyer : 1f;
                                break;
                            case 2:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? Mathf.Clamp(rotator.airborneMultiplyer * heightThreshold + rotator.airborneAffectedMultiplyerMinimum, 0f, 1f) : 1f;
                                break;
                            case 3:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? Mathf.Clamp(rotator.airborneMultiplyer * heightThreshold * heightThreshold + rotator.airborneAffectedMultiplyerMinimum, 0f, 1f) : 1f;
                                break;
                            case -1:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? rotator.airborneMultiplyer : 0f;
                                break;
                            case -2:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? Mathf.Clamp(rotator.airborneMultiplyer * (1 - heightThreshold) + rotator.airborneAffectedMultiplyerMinimum, 0f, 1f) : 0f;
                                break;
                            case -3:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? Mathf.Clamp(rotator.airborneMultiplyer * (1 - heightThreshold * heightThreshold) + rotator.airborneAffectedMultiplyerMinimum, 0f, 1f) : 0f;
                                break;
                            case -4:
                                airborneFactor = groundNormalHeightFactor > airborneTresholdFactor ? rotator.airborneMultiplyer : 0f;
                                break;
                        }
                    }

                    rotator.airborneAffectedMultiplyer = airborneFactor;
                }
                else if(rotater is FeetImpulseRotater feetRotator)
                {
                    if (feetRotator.isAirborneAffected != 0 && sensors != null)
                    {
                        float airborneFactor = 1f;
                        float airborneTresholdFactor = feetRotator.airborneTresholdFactor;
                        float heightThreshold = heightFactor / airborneTresholdFactor;

                        // 1 -> aM wenn über aTf sonst 1;     2 -> aM*  hT  +aMmin wenn unter aTf sonst 1;     3 -> aM*  hT²  +aMin wenn unter aTf sonst 1;     
                        //-1 -> aM wenn unter aTf sonst 0;   -2 -> aM*(1-hT)+aMmin wenn unter aTf sonst 0;    -3 -> aM*(1-hT²)+aMin wenn unter aTf sonst 0;     -4 -> aM wenn über aTf sonst 0;
                        switch (feetRotator.isAirborneAffected)
                        {
                            case 1:
                                airborneFactor = groundNormalHeightFactor > airborneTresholdFactor ? feetRotator.airborneMultiplyer : 1f;
                                break;
                            case 2:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? Mathf.Clamp(feetRotator.airborneMultiplyer * heightThreshold + feetRotator.airborneAffectedMultiplyerMinimum, 0f, 1f) : 1f;
                                break;
                            case 3:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? Mathf.Clamp(feetRotator.airborneMultiplyer * heightThreshold * heightThreshold + feetRotator.airborneAffectedMultiplyerMinimum, 0f, 1f) : 1f;
                                break;
                            case -1:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? feetRotator.airborneMultiplyer : 0f;
                                break;
                            case -2:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? Mathf.Clamp(feetRotator.airborneMultiplyer * (1 - heightThreshold) + feetRotator.airborneAffectedMultiplyerMinimum, 0f, 1f) : 0f;
                                break;
                            case -3:
                                airborneFactor = groundNormalHeightFactor < airborneTresholdFactor ? Mathf.Clamp(feetRotator.airborneMultiplyer * (1 - heightThreshold * heightThreshold) + feetRotator.airborneAffectedMultiplyerMinimum, 0f, 1f) : 0f;
                                break;
                            case -4:
                                airborneFactor = groundNormalHeightFactor > airborneTresholdFactor ? feetRotator.airborneMultiplyer : 0f;
                                break;
                        }

                        feetRotator.airborneAffectedMultiplyer = airborneFactor;
                    }

                    feetRotator.AxesInput = feetRotator.usesLeftTriggerToSquash ? new Vector4(StickValue.x, StickValue.y, StickValue.z, -inputRead.GetLeftTrigger()) : new Vector4(StickValue.x, StickValue.y, StickValue.z, StickValue.w);
                    if (feetRotator.gravity != currentGravity)
                    {
                        feetRotator.gravity = currentGravity;
                    }
                }
            }
        }

        if (activeImpulsors != null)
        {
            for (int i = 0; i < activeImpulsors.Count; i++)
            {
                var activeImpulsor = (RocketImpulsor)activeImpulsors[i];

                float factor = 1f;

                if (activeImpulsor.isAssignedToLeftTrigger != 0)
                {
                    switch (activeImpulsor.isAssignedToLeftTrigger)
                    {
                        case 1:
                            factor = leftTriggerValue;
                            break;
                        case -1:
                            factor = 1f - leftTriggerValue;
                            break;
                        case -2:
                            factor = 1f - leftTriggerValue * leftTriggerValue;
                            break;
                        case 2:
                            factor = leftTriggerValue * leftTriggerValue;
                            break;
                    }
                }

                if (activeImpulsor.isAssignedToRightTrigger != 0)
                {
                    switch (activeImpulsor.isAssignedToRightTrigger)
                    {
                        case 1:
                            factor = rightTriggerValue;
                            break;
                        case -1:
                            factor = 1f - rightTriggerValue;
                            break;
                        case -2:
                            factor = 1f - rightTriggerValue * rightTriggerValue;
                            break;
                        case 2:
                            factor = rightTriggerValue * rightTriggerValue;
                            break;
                    }
                }

                activeImpulsor.activeFactor = factor;

                float airborneFactor = 1f;
                float airborneTresholdFactor = activeImpulsor.airborneTresholdFactor;
                float heightThreshold = heightFactor / airborneTresholdFactor;

                if (activeImpulsor.isAirborneAffected != 0 && sensors != null)
                {
                    // 1 -> aM wenn über aTf sonst 1;     2 -> aM*  hT  +aMmin wenn unter aTf sonst 1;     3 -> aM*  hT²  +aMin wenn unter aTf sonst 1;
                    //-1 -> aM wenn unter aTf sonst 0;   -2 -> aM*(1-hT)+aMmin wenn unter aTf sonst 0;    -3 -> aM*(1-hT²)+aMin wenn unter aTf sonst 0;
                    switch (activeImpulsor.isAirborneAffected)
                    {
                        case 1:
                            airborneFactor = heightFactor > airborneTresholdFactor ? activeImpulsor.airborneMultiplyer : 1f;
                            break;
                        case 2:
                            airborneFactor = heightFactor < airborneTresholdFactor ? Mathf.Clamp(activeImpulsor.airborneMultiplyer * heightFactor + activeImpulsor.airborneAffectedMultiplyerMinimum, 0f, 1f) : 1f;
                            break;
                        case 3:
                            airborneFactor = heightFactor < airborneTresholdFactor ? Mathf.Clamp(activeImpulsor.airborneMultiplyer * heightFactor * heightFactor + activeImpulsor.airborneAffectedMultiplyerMinimum, 0f, 1f) : 1f;
                            break;
                        case -1:
                            airborneFactor = heightFactor < airborneTresholdFactor ? activeImpulsor.airborneMultiplyer : 0f;
                            break;
                        case -2:
                            airborneFactor = heightFactor < airborneTresholdFactor ? Mathf.Clamp(activeImpulsor.airborneMultiplyer * (1 - heightThreshold) + activeImpulsor.airborneAffectedMultiplyerMinimum, 0f, 1f) : 0f;
                            break;
                        case -3:
                            airborneFactor = heightFactor < airborneTresholdFactor ? Mathf.Clamp(activeImpulsor.airborneMultiplyer * (1 - heightThreshold * heightThreshold) + activeImpulsor.airborneAffectedMultiplyerMinimum, 0f, 1f) : 0f;
                            break;
                    }
                }

                activeImpulsor.airborneAffectedMultiplyer = airborneFactor;

                activeImpulsor.Throttle = activeImpulsor.isPIDControlled ? PIDFeedbackmodul.Response * airborneFactor * factor : activeImpulsor.power * airborneFactor * factor;

                if (activeImpulsor.clampThrottle)
                {
                    activeImpulsor.Clamp(activeImpulsor.Throttle, activeImpulsor.minThrottle, activeImpulsor.maxThrottle);
                }
                else
                {
                    activeImpulsor.ClampedThrottle = activeImpulsor.Throttle;
                }
            }
        }

    }
}
