using UnityEngine;
using UnityEngine.InputSystem;

public class PelvisMovementWithoutPhysics : MonoBehaviour
{
    private Gamepad gamepad;
    private Vector2 leftStickInput;
    private Vector2 rightStickInput;
    [SerializeField] private bool usesLeftTrigger = true;
    [SerializeField] private Vector3 turnSpeeds = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private Vector3 maxAngles = new Vector3(20f, 70f, 30f);
    [SerializeField] private float returnSpeed = 0.5f;

    public bool calculateLegLengthByViewport = true;
    [SerializeField] private float legLength = 0.8f;
    [SerializeField] private float minimumHeightOfPelvis = 0.05f;

    [SerializeField] public GameObject CenterOfMass;
    [SerializeField] public int simulationLevel = 3;
    [SerializeField] private float maxRelativePelvisSpeed = 45f;

    //transforms in order from few to most functionality
    [SerializeField] private Transform activeLeaning;
    [SerializeField] private Transform passiveLeaning;
    [SerializeField] private Transform crouching;
    [SerializeField] private Transform clampedJoints;
    [SerializeField] private Transform rotationGoal;

    //feet and middle between feet
    [SerializeField] private Transform middleBetweenFeet;
    [SerializeField] private Transform leftFootPlacement;
    [SerializeField] private Transform rightFootPlacement;

    //default relaxed stance
    [SerializeField] private Vector3 relativeRelaxedCenterOfMassPosition;

    [SerializeField] private float motionDampingFactor = 0.5f;
    [SerializeField] private float pelvisDragFactor = 0.5f;

    private Vector3 previousPosition;
    private Vector3 previousSpeed;

    private void Start()
    {
        gamepad = Gamepad.current;

        if (activeLeaning == null) { activeLeaning = transform.Find("1 ActiveLeaning").transform; }
        if (passiveLeaning == null) { passiveLeaning = transform.Find("2 PassiveLeaning").transform; }
        if (crouching == null) { crouching = transform.Find("3 Crouching").transform; }
        if (clampedJoints == null) { clampedJoints = transform.Find("4 ClampedJoints").transform; }

        if (rotationGoal == null) { rotationGoal = transform.Find("5 RotationGoal" +
            "").transform; }

        if (middleBetweenFeet == null) { middleBetweenFeet = transform.Find("MiddleBetweenFeet").transform; }

        if (calculateLegLengthByViewport)
        {
            CalculatLegLength();
        }

        CalculateRelaxedStance();
    }

    private void FixedUpdate()
    {
        Vector3 speed = (transform.position - previousPosition) / Time.fixedDeltaTime;
        Vector3 acceleration = speed - previousSpeed;
        if(acceleration.magnitude < 0.001f) { acceleration = Vector3.zero; }

        if (gamepad != null)
        {
            float triggerInput = 0f;
            if (usesLeftTrigger)
            {
                triggerInput = gamepad.leftTrigger.ReadValue();
            }
            else
            {
                triggerInput = gamepad.rightTrigger.ReadValue();
            }

            leftStickInput = gamepad.leftStick.ReadValue();
            rightStickInput = gamepad.rightStick.ReadValue();

            if (middleBetweenFeet != null)
            {
                Vector3 relaxedCenterOfMassPosition = transform.TransformPoint(relativeRelaxedCenterOfMassPosition);


                //if, Input, move pelvis by turnSpeeds, else return to relaxed position
                Vector3 balancingVector = leftStickInput.magnitude > 0.1f ? relaxedCenterOfMassPosition + middleBetweenFeet.right * leftStickInput.x * turnSpeeds.z + middleBetweenFeet.forward * leftStickInput.y * turnSpeeds.x : relaxedCenterOfMassPosition;
                if (activeLeaning != null) { activeLeaning.position = balancingVector; }

                //add acceleration
                Vector3 leaningGoal = balancingVector - acceleration * pelvisDragFactor;
                if (passiveLeaning != null) { passiveLeaning.position = leaningGoal; }

                float distance = Vector3.Distance(middleBetweenFeet.transform.position, leaningGoal);

                //add crouching and clamp between minimumHeightOfPelvis and legLength
                Vector3 crouchGoal = Vector3.Lerp(middleBetweenFeet.transform.position, leaningGoal, Mathf.Clamp(1 - triggerInput, Mathf.Clamp01(minimumHeightOfPelvis / distance), Mathf.Clamp01(legLength / distance)));
                if (crouching != null) { crouching.position = crouchGoal; }

                Vector3 relativeCrouchGoal = transform.InverseTransformPoint(crouchGoal);

                Vector3 relativeCrouchGoalWithY1 = relativeCrouchGoal / relativeCrouchGoal.y;
                float pitchAngle = Mathf.Rad2Deg * Mathf.Atan(relativeCrouchGoalWithY1.z);
                float rollAngle = Mathf.Rad2Deg * Mathf.Atan(relativeCrouchGoalWithY1.x);

                if(Mathf.Abs(pitchAngle) > maxAngles.x)
                {
                    relativeCrouchGoalWithY1.z = Mathf.Tan(Mathf.Deg2Rad * maxAngles.x);
                    if(relativeCrouchGoal.z < 0) { relativeCrouchGoalWithY1.z = -relativeCrouchGoalWithY1.z; } 
                    pitchAngle = maxAngles.x;
                }
                if(Mathf.Abs(rollAngle) > maxAngles.z)
                {
                    relativeCrouchGoalWithY1.x = Mathf.Tan(Mathf.Deg2Rad * maxAngles.z);
                    if (relativeCrouchGoal.x < 0) { relativeCrouchGoalWithY1.x = -relativeCrouchGoalWithY1.x; }
                    rollAngle = maxAngles.z;
                }

                Vector3 clampedLeanGoal = transform.TransformPoint(relativeCrouchGoalWithY1 * relativeCrouchGoal.y);
                if (clampedJoints != null) { clampedJoints.position = clampedLeanGoal; }

                if (rotationGoal != null)
                {
                    rotationGoal.position = clampedLeanGoal;
                    float yawAngle = Mathf.Clamp(rightStickInput.x * turnSpeeds.y, -maxAngles.y, maxAngles.y);
                    rotationGoal.rotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);
                }

                if(CenterOfMass != null)
                {
                    Transform simulationTarget = null;
                    switch (simulationLevel)
                    {
                        case 1:
                            simulationTarget = activeLeaning;
                            break;
                        case 2:
                            simulationTarget = passiveLeaning;
                            break;
                        case 3:
                            simulationTarget = crouching;
                            break;
                        case 4:
                            simulationTarget = clampedJoints;
                            break;
                        case 5:
                            simulationTarget = rotationGoal;
                            break;
                    }
                    if(simulationTarget != null)
                    {
                        Vector3 distanceToSimulationTarget = simulationTarget.position - CenterOfMass.transform.position;

                        if(distanceToSimulationTarget.magnitude > maxRelativePelvisSpeed * Time.fixedDeltaTime)
                        {
                            CenterOfMass.transform.position = CenterOfMass.transform.position + distanceToSimulationTarget.normalized * maxRelativePelvisSpeed * Time.fixedDeltaTime;
                        }
                        else
                        {
                            CenterOfMass.transform.position = simulationTarget.position;
                        }
                    }
                }
            }
        }

        previousPosition = transform.position;
        previousSpeed = speed;
    }

    private void CalculatLegLength()
    {
        legLength = (Vector3.Distance(leftFootPlacement.position, rotationGoal.position) + Vector3.Distance(rightFootPlacement.position, rotationGoal.position)) / 2f;
    }

    private void CalculateRelaxedStance()
    {
        // Calculate the middle between the feet
        Vector3 leftFootToMiddle = (rightFootPlacement.position - leftFootPlacement.position) / 2;

        if(middleBetweenFeet == null) { middleBetweenFeet = new GameObject().transform; }
        middleBetweenFeet.position = leftFootPlacement.position + leftFootToMiddle;
        middleBetweenFeet.rotation = transform.rotation;

        // Calculate the relaxed center of mass position
        float halfLengthBetweenFeet = leftFootToMiddle.magnitude;
        relativeRelaxedCenterOfMassPosition = transform.InverseTransformPoint(leftFootPlacement.position + leftFootToMiddle + rotationGoal.transform.up * Mathf.Sqrt(legLength * legLength - halfLengthBetweenFeet * halfLengthBetweenFeet));
        if(rotationGoal != null) { rotationGoal.localPosition = relativeRelaxedCenterOfMassPosition; }
    }
}
