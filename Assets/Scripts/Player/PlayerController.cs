using SpeedGame;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(10)]
public class PlayerController : Controller
{
    public Transform platformInputSpace = default;

    private InputAction moveAction;
    private InputAction steerAction;
    private InputAction accelerateAction;
    private InputAction brakeAction;
    private InputAction jumpAction;

    private InputAction checkpointResetAction;
    private InputAction fullResetAction;

    private ReplayData replay;

    Vector2 moveValue;
    Vector3 forwardAxis;
    sbyte steerValue;
    byte buttonMask = 0b00000000;

    private CharacterInputSet previousInputs;

    protected override void Awake()
    {
        base.Awake();

        moveAction = InputSystem.actions.FindAction("Move");
        steerAction = InputSystem.actions.FindAction("Steering");
        accelerateAction = InputSystem.actions.FindAction("Accelerate");
        brakeAction = InputSystem.actions.FindAction("Brake");
        jumpAction = InputSystem.actions.FindAction("Jump");

        checkpointResetAction = InputSystem.actions.FindAction("Checkpoint Reset");
        fullResetAction = InputSystem.actions.FindAction("Full Reset");

        replay = new ReplayData();
        replay.inputQueue = new Queue<CharacterInputSet>();
    }

    void Update()
    {
        if (accelerateAction.IsPressed())
        {
            buttonMask |= AccelerateMask;
        }

        if (brakeAction.IsPressed())
        {
            buttonMask |= BrakeMask;
        }

        if (jumpAction.WasPerformedThisFrame())
        {
            buttonMask |= JumpPressedMask;
        }

        if (jumpAction.WasCompletedThisFrame())
        {
            buttonMask |= JumpReleasedMask;
        }

        if (checkpointResetAction.WasPerformedThisFrame())
        {
            buttonMask |= CheckpointResetMask;
        }

        if (fullResetAction.WasPerformedThisFrame())
        {
            buttonMask |= FullResetMask;
        }
    }

    protected override void FixedUpdate()
    {
        Vector3 upAxis = CustomGravity.GetUpAxis(transform.position);

        moveValue = moveAction.ReadValue<Vector2>();

        Transform inputSpace = platformInputSpace;

        if (inputSpace)
        {
            forwardAxis = UtilFunctions.ProjectDirectionOnPlane(inputSpace.forward, upAxis);
        }
        else
        {
            forwardAxis = UtilFunctions.ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }

        steerValue = (sbyte)Mathf.RoundToInt(steerAction.ReadValue<float>() * 100f);

        Vector3 rightAxis = Vector3.Cross(upAxis, forwardAxis);
        Vector3 facingXAxis = moveValue.x * rightAxis;
        Vector3 facingZAxis = moveValue.y * forwardAxis;

        Vector3 moveVector = facingXAxis + facingZAxis;

        moveValue = new Vector2(moveVector.x, moveVector.z);

        sbyte moveValueX = (sbyte)Mathf.RoundToInt(moveValue.x * 100f);
        sbyte moveValueY = (sbyte)Mathf.RoundToInt(moveValue.y * 100f);

        CharacterInputSet inputs = new CharacterInputSet(moveValueX, moveValueY, buttonMask, steerValue, tick);

        if (previousInputs != inputs)
        {
            character.UpdateInputs(inputs);

            if (replay.inputQueue != null)
            {
                replay.inputQueue.Enqueue(inputs);
            }
        }

        buttonMask = 0b00000000;
        previousInputs = inputs;

        base.FixedUpdate();
    }
}