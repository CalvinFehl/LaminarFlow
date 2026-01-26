using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void GamepadInputDelegate(in GamepadInput input, float deltaTime);
public delegate void RigidbodyDataDelegate(in RigidbodyData rigidbodyData, float deltaTime);
public delegate void GroundDataDelegate(in GroundData groundData, float targetFlightHeight, float deltaTime);

public class FlowController : MonoBehaviour, IReposition, IReferenceRigidbody
{
    [Header("Settings")]
    [SerializeField] private float targetFlightHeight = 10f;

    [Header("Components")]
    [SerializeField] public GameObject PhysicsObject;
    [SerializeField] public GameObject GraphicObject;

    public Rigidbody PhysicsRigidbody { get; set; }
    public GameObject CameraSystem;

    [Header("Processors")]
    [Tooltip("Processors are activated in this order")]
    [SerializeField] private BaseGamepadInputProcessor inputProcessor;
    [SerializeField] private BaseTracker tracker;
    [SerializeField] private BaseFSensor sensor;
    [SerializeField] private BaseGroundProcessor groundProcessor;

    [Header("Modules")]
    [SerializeField] public List<GameObject> allModules;

    // structs
    public RigidbodyData CurrentRigidbodyData = default;
    public GroundData CurrentGround = default;

    // events
    GamepadInputDelegate OnNewInput;
    RigidbodyDataDelegate OnRigidbodyDataUpdated;
    GroundDataDelegate OnGroundDataUpdated;
    public Action<float> OnSimulate;


    #region Initialize
    public void Initialize(bool offline = false)
    {        
        SetRigidbody(PhysicsObject);
        
        foreach (var module in allModules)
        {
            // Set Rigidbody in the module
            var physicModuleInterface = module.GetComponent<IReferenceRigidbody>();
            if (PhysicsRigidbody != null && physicModuleInterface != null)
            {
                physicModuleInterface.PhysicsRigidbody = PhysicsRigidbody;
            }

            // Subsribe to events
            var newInputInterface = module.GetComponent<IHandleInput>();
            if (newInputInterface != null)
            {
                OnNewInput += newInputInterface.HandleInput;
            }

            var rigidbodyDataInterface = module.GetComponent<IHandleRigidbodyData>();
            if (rigidbodyDataInterface != null)
            {
                OnRigidbodyDataUpdated += rigidbodyDataInterface.HandleRigidbodyData;
            }

            var groundDataInterface = module.GetComponent<IHandleGroundData>();
            if (groundDataInterface != null)
            {
                OnGroundDataUpdated += groundDataInterface.HandleGroundData;
            }

            var moduleInterface = module.GetComponent<ISimulateable>();
            if (moduleInterface != null)
            {
                OnSimulate += moduleInterface.Simulate;
            }
        }
    }

    public void DeInitialize()
    {
        foreach (var module in allModules)
        {
            // Unsubsribe from events
            var newInputInterface = module.GetComponent<IHandleInput>();
            if (newInputInterface != null)
            {
                OnNewInput -= newInputInterface.HandleInput;
            }

            var rigidbodyDataInterface = module.GetComponent<IHandleRigidbodyData>();
            if (rigidbodyDataInterface != null)
            {
                OnRigidbodyDataUpdated -= rigidbodyDataInterface.HandleRigidbodyData;
            }

            var groundDataInterface = module.GetComponent<IHandleGroundData>();
            if (groundDataInterface != null)
            {
                OnGroundDataUpdated -= groundDataInterface.HandleGroundData;
            }

            var moduleInterface = module.GetComponent<ISimulateable>();
            if (moduleInterface != null)
            {
                OnSimulate -= moduleInterface.Simulate;
            }
        }
    }


    public void SetRigidbody(GameObject physicsObject)
    {
        var rb = physicsObject?.GetComponent<Rigidbody>();

        if (rb != null) { PhysicsRigidbody = rb; }
    }
    #endregion


    // IReposition Method
    public void Reposition(RigidbodyData rigidbodyData)
    {
        if (PhysicsRigidbody == null)
        {
            Debug.LogError("PhysicsRigidbody is null");
            return;
        }

        // Reposition the Rigidbody based on the provided data
        PhysicsRigidbody.position = rigidbodyData.Position;
        PhysicsRigidbody.rotation = rigidbodyData.Rotation;
        PhysicsRigidbody.linearVelocity = rigidbodyData.Velocity;
        PhysicsRigidbody.angularVelocity = rigidbodyData.AngularVelocity;

        // Optionally, update the visual representation
        if (GraphicObject != null)
        {
            GraphicObject.transform.position = rigidbodyData.Position;
            GraphicObject.transform.rotation = rigidbodyData.Rotation;
        }
    }


    // ISwitchToController Methods
    public void SwitchToController(bool isOwner = false)
    {
        if (CameraSystem != null && isOwner)
        {
            CameraSystem.transform.SetParent(null);
            CameraSystem.SetActive(true);
        }
    }

    public void SwitchFromController(bool isOwner = false)
    {
        if (CameraSystem != null && isOwner)
        {         
            CameraSystem.transform.SetParent(this.transform);
            CameraSystem.SetActive(false);
        }
    }

    public void Simulate(float deltaTime, in GamepadInput input)
    {
        /*  1. Input
         *  2. RigidbodyData
         *  3. GroundData
         *  4. Simulate
         */

        if (PhysicsRigidbody == null)
        {
            Debug.LogError("PhysicsRigidbody is null");
            return;
        }

        // Process Input
        inputProcessor?.ProcessGamepadInput(input, deltaTime);

        // Write and Process Tracking Data
        CurrentRigidbodyData = new RigidbodyData
        {
            Position = PhysicsRigidbody.position,
            Rotation = PhysicsRigidbody.rotation,
            Velocity = PhysicsRigidbody.linearVelocity,
            AngularVelocity = PhysicsRigidbody.angularVelocity,
        };
        tracker?.Track(CurrentRigidbodyData, deltaTime);

        // Update Sensor and Process Ground Data
        CurrentGround = sensor.ScanCurrentGround();
        groundProcessor?.ProcessGroundData(CurrentGround, deltaTime);

        #region Module Processing

        // Simulate
        OnNewInput?.Invoke(input, deltaTime);
        OnRigidbodyDataUpdated?.Invoke(CurrentRigidbodyData, deltaTime);
        OnGroundDataUpdated?.Invoke(CurrentGround, targetFlightHeight, deltaTime);
        OnSimulate?.Invoke(deltaTime);

        #endregion
    }
}