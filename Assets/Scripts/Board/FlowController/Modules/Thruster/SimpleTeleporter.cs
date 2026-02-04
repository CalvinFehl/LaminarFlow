using Unity.Cinemachine;
using UnityEngine;

public class SimpleTeleporter : MonoBehaviour, IHandleInput, IHandleRigidbodyData, IReferenceRigidbody
{
    [Header("Components")]
    [SerializeField] private Transform referenceTransform;
    public Rigidbody PhysicsRigidbody { get; set; }
    [SerializeField] private SimpleBoardCameraManager cameraManager;
    private Transform cameraTransform;
    [SerializeField] private GetGamepadParameter inputLogic = new GetGamepadParameter();

    [Header("Runtime Variables")]
    [SerializeField] private bool buttonPressed = false;
    private bool buttonWasPressed = false, buttonHadBeenPressed = false;
    public Vector3 spawnPositionOffset = Vector3.zero;
    public Quaternion spawnRotationOffset = Quaternion.identity;

    public Vector3 cameraPosition = Vector3.zero;
    public Quaternion cameraRotation = Quaternion.identity;

    public RigidbodyData checkPointData = new();

    private float buttonHoldTime = 0f;
    private double lastButtonPressTime = -10f;
    private bool isTeleporting = false;
    private bool isSettingCheckpoint = false;


    [Header("Settings")]
    [Tooltip("1 = Left Shoulder, 2 = Right Shoulder, 3 = Left Stick Button, 4 = Right Stick Button\r\n " +
        "5 = A Button, 6 = B Button, 7 = X Button, 8 = Y Button\r\n " +
        "9 = North Button, 10 = East Button, 11 = South Button, 12 = West Button\r\n " +
        "13 = Start Button, 14 = Select Button\r\n " +
        "15 = Left Trigger, 16 = Right Trigger\r\n " +
        "17 = Left Stick X, 18 = Left Stick Y, 19 = Right Stick X, 20 = Right Stick Y")]
    [SerializeField] private int teleportButton = 11;
    [SerializeField] private float holdTimeToTeleport = 0.7f;
    [SerializeField] private float doublePressTimeToSetCheckpoint = 0.5f;


    public void HandleInput(in GamepadInput input, float deltaTime)
    {
        buttonPressed = inputLogic.CheckButton(teleportButton, input);

        if (buttonHadBeenPressed)
        {
            lastButtonPressTime += deltaTime;
        }

        if (buttonPressed)
        {
            buttonHoldTime += deltaTime;
        }
        else
        {
            // On release Button
            if (buttonWasPressed)
            {
                // On Double Press -> Set Checkpoint
                if (lastButtonPressTime < doublePressTimeToSetCheckpoint && buttonHadBeenPressed)
                {
                    // Set Checkpoint
                    Debug.Log("Setting Checkpoint");
                    isSettingCheckpoint = true;
                    buttonHadBeenPressed = false;
                    lastButtonPressTime = 0f;
                }
                else { buttonHadBeenPressed = true; }

                lastButtonPressTime = 0f;

                // Release after holding long enough to reset-teleport
                if (buttonHoldTime >= holdTimeToTeleport)
                {
                    Debug.Log("Reset-Teleporting to Checkpoint");
                    TeleportToCheckpoint(true, true);
                    buttonHadBeenPressed = false;
                }

                buttonHoldTime = 0f;

            }
            // On single press -> after doublePressTime runs out -> Teleport
            else if (lastButtonPressTime > doublePressTimeToSetCheckpoint && buttonHadBeenPressed)
            {
                Debug.Log("Teleporting to Checkpoint");
                TeleportToCheckpoint(false, false);
                buttonHadBeenPressed = false;
                lastButtonPressTime = 0f;
            }

        }
        buttonWasPressed = buttonPressed;
    }

    public void HandleRigidbodyData(in RigidbodyData rigidbodyData, float deltaTime)
    {
        if (isSettingCheckpoint)
        {
            SetCheckpoint(rigidbodyData);
            isSettingCheckpoint = false;
        }
    }

    private void Awake()
    {
        if (referenceTransform == null)
        {
            referenceTransform = transform;
        }

        if (cameraManager == null)
        {
            cameraManager = GetComponentInParent<SimpleBoardCameraManager>();

            cameraTransform = cameraManager != null ? cameraManager.followCamera.transform : null;
        }
    }

    public void SetCheckpoint(RigidbodyData rigidbodyData)
    {
        checkPointData = rigidbodyData;
    }

    private void TeleportToCheckpoint(bool isResettingVelocity = true, bool isResettingOffset = false)
    {
        referenceTransform.position = checkPointData.Position;
        referenceTransform.rotation = checkPointData.Rotation.normalized;

        if (PhysicsRigidbody != null)
        {
            if (!isResettingVelocity)
            {
                PhysicsRigidbody.position = checkPointData.Position;
                PhysicsRigidbody.rotation = checkPointData.Rotation.normalized;
            }
            else
            {
                PhysicsRigidbody.linearVelocity = Vector3.zero;
                PhysicsRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
}
