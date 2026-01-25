using Assets.Scripts.CheckpointSystem.Configurators;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Translocator : BaseGadget
{
    public int assignButton = 0;
    public Vector3 spawnPosition;
    public Quaternion spawnRotation;
    public CheckPoint ChallengeStartCheckPoint, ChallengeLastCheckPoint;

    public Vector3 LastCheckPointRelativePosition, LastCheckPointVelocity;
    public Quaternion LastCheckPointRelativeRotation;

    public GameObject parentObject;
    private Rigidbody parentRigidbody;

    [SerializeField] float holdButtonTime = 0.4f;
    [SerializeField] float doubleTapButtonTime = 0.5f;
    private float timeSinceButtonDown = 0f;
    private bool wasDoubleTapped = false;

    public bool IsOnTrack = false;
    public BaseConfigurator Configurator;

    [SerializeField] private CameraManager01Knot cameraManager;

    private void Awake()
    {
        parentObject = transform.parent.gameObject;
        parentRigidbody = parentObject?.GetComponent<Rigidbody>();
    }


    void Update()
    {
        if (assignedButton != assignButton)
        {
            assignedButton = assignButton;
        }

        if (parentObject == null)
        {
            return;
        }

        if (timeSinceButtonDown < holdButtonTime || timeSinceButtonDown < doubleTapButtonTime)
        {
            timeSinceButtonDown = timeSinceButtonDown + Time.deltaTime;
        }

        if (wasPressed != buttonPressed)
        {
            if (buttonPressed)
            {
                if(timeSinceButtonDown < doubleTapButtonTime) // Double tap
                {
                    wasDoubleTapped = true;

                    if (IsOnTrack) // Teleport to last checkpoint with no momentum
                    {
                        if (ChallengeLastCheckPoint == null)
                        {
                            Debug.LogError("No Last Checkpoint assigned");
                            return;
                        }
                        Debug.Log("Teleporting");
                        if(parentRigidbody != null)
                        {
                            parentRigidbody.linearVelocity = Vector3.zero;
                        }
                        parentObject.transform.position = ChallengeLastCheckPoint.transform.position;
                        parentObject.transform.rotation = ChallengeLastCheckPoint.transform.rotation;


                        if (Configurator != null)
                        {
                            Configurator.ResetTimer(ChallengeLastCheckPoint, parentObject);
                        }

                        if (cameraManager != null)
                        {
                            cameraManager.SetCameraBehindPlayer(default, parentObject.transform);
                        }
                    }
                    else // set respawn point
                    {
                        Debug.Log("Respawnpoint set");
                        spawnPosition = parentObject.transform.position;
                        spawnRotation = parentObject.transform.rotation;
                    }
                }

                timeSinceButtonDown = 0f;
            }
            else if (!buttonPressed)
            {
                if (timeSinceButtonDown >= holdButtonTime) // Hold
                {
                    if (IsOnTrack) // Restart Challenge
                    {
                        if (ChallengeStartCheckPoint == null)
                        {
                            Debug.LogError("No Start Checkpoint assigned");
                            return;
                        }
                        parentObject.transform.position = ChallengeStartCheckPoint.transform.position;
                        parentObject.transform.rotation = ChallengeStartCheckPoint.transform.rotation;

                        ChallengeLastCheckPoint = ChallengeStartCheckPoint;

                        LastCheckPointRelativePosition = Vector3.zero;
                        LastCheckPointRelativeRotation = Quaternion.identity;
                        LastCheckPointVelocity = Vector3.zero;

                        if (parentRigidbody != null)
                        {
                            parentRigidbody.linearVelocity = Vector3.zero;
                        }

                        if (Configurator != null)
                        {
                            Configurator.ResetTimer(ChallengeStartCheckPoint, parentObject);
                            Configurator.RestartChallenge(parentObject);
                        }
                    }
                    else // Teleport to respawn point
                    {
                        Debug.Log("Teleporting");
                        parentObject.transform.position = spawnPosition;
                        parentObject.transform.rotation = spawnRotation;


                    }

                    if (cameraManager != null)
                    {
                        cameraManager.SetCameraBehindPlayer(default, parentObject.transform);
                    }
                }
                else // Tap
                {
                    if (wasDoubleTapped)
                    {
                        wasDoubleTapped = false;
                    }                    
                    else if (IsOnTrack) // Teleport to last checkpoint with momentum
                    {
                        parentObject.transform.position = ChallengeLastCheckPoint.transform.TransformPoint(LastCheckPointRelativePosition);
                        parentObject.transform.rotation = ChallengeLastCheckPoint.transform.rotation * LastCheckPointRelativeRotation;

                        if (parentRigidbody != null)
                        {
                            if (LastCheckPointVelocity != Vector3.zero)
                            {
                                parentRigidbody.linearVelocity = ChallengeLastCheckPoint.transform.TransformPoint(LastCheckPointVelocity);
                            }
                            else
                            {
                                parentRigidbody.linearVelocity = Vector3.zero;
                            }
                        }

                        CheckPoint checkPoint = ChallengeLastCheckPoint.GetComponent<CheckPoint>();
                        if (Configurator != null && checkPoint != null)
                        {
                            Configurator.ResetTimer(checkPoint, parentObject);
                        }

                        if (cameraManager != null)
                        {
                            cameraManager.SetCameraBehindPlayer(default, parentObject.transform);
                        }
                    }
                }
            }

            wasPressed = buttonPressed;
        }
    }
}
