using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlowControllerNetworkAdapter : MonoBehaviour
{
    public FlowController flowController;
    [SerializeField] private bool autoUpdate = true;

    private uint _lastReplicateTick;
    private uint _lastReconsileTick;

    // Input state variables
    private bool leftShoulder;
    private bool rightShoulder;
    private bool leftStickButton;
    private bool rightStickButton;
    private bool aButton;
    private bool bButton;
    private bool xButton;
    private bool yButton;
    private bool northButton;
    private bool eastButton;
    private bool southButton;
    private bool westButton;
    private bool startButton;

    //private float leftTrigger;
    //private float rightTrigger;
    //private Vector2 leftStick;
    //private Vector2 rightStick;

    // Binding for the input actions
    private SwitchDualInput boardInputActions;

    #region Initialization

    private void Awake()
    {
        boardInputActions = new SwitchDualInput();               
        InitializeInputCallbacks();
    }


    private void OnEnable()
    {
        boardInputActions.Board.Enable();
        flowController?.Initialize();
    }

    private void OnDisable()
    {
        boardInputActions.Board.Disable();
        flowController?.DeInitialize();
    }

    // Register input callbacks
    private void InitializeInputCallbacks()
    {
        // Map buttons to their respective buffer indices
        boardInputActions.Board.ActionTrigger1.performed += _ => leftShoulder = true;
        boardInputActions.Board.ActionTrigger2.performed += _ => rightShoulder = true;
        boardInputActions.Board.ActionTrigger3.performed += _ => leftStickButton = true;
        boardInputActions.Board.ActionTrigger4.performed += _ => rightStickButton = true;
        boardInputActions.Board.ActionTrigger5.performed += _ => aButton = true;
        boardInputActions.Board.ActionTrigger6.performed += _ => bButton = true;
        boardInputActions.Board.ActionTrigger7.performed += _ => xButton = true;
        boardInputActions.Board.ActionTrigger8.performed += _ => yButton = true;
        boardInputActions.Board.ActionTrigger9.performed += _ => northButton = true;
        boardInputActions.Board.ActionTrigger10.performed += _ => eastButton = true;
        boardInputActions.Board.ActionTrigger11.performed += _ => southButton = true;
        boardInputActions.Board.ActionTrigger12.performed += _ => westButton = true;
        boardInputActions.Board.ActionTrigger13.performed += _ => startButton = true;

        boardInputActions.Board.ActionTrigger1.canceled += _ => leftShoulder = false;
        boardInputActions.Board.ActionTrigger2.canceled += _ => rightShoulder = false;
        boardInputActions.Board.ActionTrigger3.canceled += _ => leftStickButton = false;
        boardInputActions.Board.ActionTrigger4.canceled += _ => rightStickButton = false;
        boardInputActions.Board.ActionTrigger5.canceled += _ => aButton = false;
        boardInputActions.Board.ActionTrigger6.canceled += _ => bButton = false;
        boardInputActions.Board.ActionTrigger7.canceled += _ => xButton = false;
        boardInputActions.Board.ActionTrigger8.canceled += _ => yButton = false;
        boardInputActions.Board.ActionTrigger9.canceled += _ => northButton = false;
        boardInputActions.Board.ActionTrigger10.canceled += _ => eastButton = false;
        boardInputActions.Board.ActionTrigger11.canceled += _ => southButton = false;
        boardInputActions.Board.ActionTrigger12.canceled += _ => westButton = false;
        boardInputActions.Board.ActionTrigger13.canceled += _ => startButton = false;

        //boardInputActions.Board.SingleAxis1.performed += ctx => leftTrigger = ctx.ReadValue<float>();
        //boardInputActions.Board.SingleAxis2.performed += ctx => rightTrigger = ctx.ReadValue<float>();
        //boardInputActions.Board.DoubleAxis.performed += ctx => leftStick = ctx.ReadValue<Vector2>();
        //boardInputActions.Board.DoubleAxis1.performed += ctx => rightStick = ctx.ReadValue<Vector2>();
    }

    #endregion

    private void FixedUpdate()
    {
        if (!autoUpdate) return;

        if (flowController != null && flowController.PhysicsRigidbody != null)
        {
            flowController.Simulate(Time.fixedDeltaTime, new GamepadInput
            {
                leftStick = boardInputActions.Board.DoubleAxis1.ReadValue<Vector2>(),
                rightStick = boardInputActions.Board.DoubleAxis2.ReadValue<Vector2>(),
                leftTrigger = boardInputActions.Board.SingleAxis1.ReadValue<float>(),
                rightTrigger = boardInputActions.Board.SingleAxis2.ReadValue<float>(),
                leftShoulder = this.leftShoulder,
                rightShoulder = this.rightShoulder,
                leftStickButton = this.leftStickButton,
                rightStickButton = this.rightStickButton,
                aButton = this.aButton,
                bButton = this.bButton,
                xButton = this.xButton,
                yButton = this.yButton,
                northButton = this.northButton,
                eastButton = this.eastButton,
                southButton = this.southButton,
                westButton = this.westButton,
                startButton = this.startButton
            });
        }    
    }


   /* #region Network Callbacks

    protected override void TimeManager_OnTick()
    {
        UpdateModules(TimeManager.Tick);
        PerformReplicate(BuildMoveData());
        CreateReconcile();
    }

    private ReplicateData BuildMoveData()
    {
        if (!base.IsOwner) return default;
        
        ReplicateData data = new ReplicateData
        {
            CruiseInput = new GamepadInput
            {
                leftStick = boardInputActions.Board.DoubleAxis1.ReadValue<Vector2>(),
                rightStick = boardInputActions.Board.DoubleAxis2.ReadValue<Vector2>(),
                leftTrigger = boardInputActions.Board.SingleAxis1.ReadValue<float>(),
                rightTrigger = boardInputActions.Board.SingleAxis2.ReadValue<float>(),
                leftShoulder = this.leftShoulder,
                rightShoulder = this.rightShoulder,
                leftStickButton = this.leftStickButton,
                rightStickButton = this.rightStickButton,
                aButton = this.aButton,
                bButton = this.bButton,
                xButton = this.xButton,
                yButton = this.yButton,
                northButton = this.northButton,
                eastButton = this.eastButton,
                southButton = this.southButton,
                westButton = this.westButton,
                startButton = this.startButton
            }
        };

        //leftShoulder = false;
        //rightShoulder = false;
        //leftStickButton = false;
        //rightStickButton = false;
        //aButton = false;
        //bButton = false;
        //xButton = false;
        //yButton = false;
        //northButton = false;
        //eastButton = false;
        //southButton = false;
        //westButton = false;
        //startButton = false;

        return data;
    }

    private void UpdateModules(uint tick)
    {
        // Update all modules with the current tick
        foreach (var module in flowController.allModules)
        {
            module.GetComponent<ISetTick>()?.SetTick(tick);
        }
    }

    public override void CreateReconcile()
    {
        //Build the data using current information and call the reconcile method.
        ReconcileData rd = new(flowController.PhysicsRigidbody);
        rd.SetTick(TimeManager.Tick);

        // Get all bool values from modules that implement IReconsileBool
        List<bool> boolValues = rd.BoolValues = flowController.allModules
            .Select(module => module.GetComponent<IReconsileBool>())
            .Where(component => component != null)
            .Select(component => component.GetValue())
            .ToList();

        // Get all float values from modules that implement IReconsileFloat
        List<float> floatValues = rd.FloatValues = flowController.allModules
            .Select(module => module.GetComponent<IReconsileFloat>())
            .Where(component => component != null)
            .Select(component => component.GetValue())
            .ToList();
        rd.FloatValues = floatValues;

        // Get all vector3 values from modules that implement IReconsileVector3
        List<Vector3> vector3Values = rd.Vector3Values = flowController.allModules
            .Select(module => module.GetComponent<IReconsileVector3>())
            .Where(component => component != null)
            .Select(component => component.GetValue())
            .ToList();

        PerformReconcile(rd);
    }

    [Replicate]
    private void PerformReplicate(ReplicateData rd, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        uint rdTick = rd.GetTick();
        _lastReplicateTick = rdTick;

        //Debug.Log($"PerformReplicate: Tick {rdTick}, State {state}");
        if (state == ReplicateState.CurrentCreated)
        {
            flowController.Simulate((float)TimeManager.TickDelta, rd.CruiseInput);
            flowController.PhysicsRigidbody.Simulate();
        }
    }

    [Reconcile]
    private void PerformReconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        uint reconsileTick = rd.GetTick();

        if (reconsileTick <= _lastReconsileTick) return;

        _lastReconsileTick = reconsileTick;

        flowController.PhysicsRigidbody.Reconcile(rd.PredictionRigidBody);

        // Reconcile all float values from modules that implement IReconsileBool
        var allReconsileBool = flowController.allModules
            .Select(x => x.GetComponent<IReconsileBool>())
            .Where(component => component != null)
            .ToList();

        for (int i = 0; i < allReconsileBool.Count; i++)
        {
            allReconsileBool[i].Reconcile(rd.BoolValues[i], reconsileTick);
        }

        // Reconcile all float values from modules that implement IReconsileFloat
        var allReconsileFloat = flowController.allModules
            .Select(x => x.GetComponent<IReconsileFloat>())
            .Where(component => component != null)
            .ToList();

        for (int i = 0; i < allReconsileFloat.Count; i++)
        {
            allReconsileFloat[i].Reconcile(rd.FloatValues[i], reconsileTick);
        }

        // Reconcile all vector3 values from modules that implement IReconsileVector3
        var allReconsileVector3 = flowController.allModules
            .Select(x => x.GetComponent<IReconsileVector3>())
            .Where(component => component != null)
            .ToList();
        for (int i = 0; i < allReconsileVector3.Count; i++)
        {
            allReconsileVector3[i].Reconcile(rd.Vector3Values[i], reconsileTick);
        }
    }
    #endregion */
}