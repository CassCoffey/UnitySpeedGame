using SpeedGame;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.IMGUI.Controls.CapsuleBoundsHandle;
using UnityEngine.UIElements.Experimental;

[DefaultExecutionOrder(10)]
public class PlayerController : MonoBehaviour
{
    public static byte JumpPressedMask          = 0b00000001;
    public static byte JumpReleasedMask         = 0b00000010;
    public static byte AcceleratePressedMask    = 0b00000100;
    public static byte BrakePressedMask         = 0b00001000;
    public static byte TestPressedMask          = 0b10000000;

    public Transform platformInputSpace = default;

    private Character character = null;

    private InputAction moveAction;
    private InputAction steerAction;
    private InputAction accelerateAction;
    private InputAction brakeAction;
    private InputAction jumpAction;

    private InputAction testAction;

    private ReplayData replay;

    Vector2 moveValue;
    Vector3 forwardAxis;
    sbyte steerValue;
    byte buttonMask = 0b00000000;

    private uint tick = 0;

    private CharacterInputSet previousInputs;

    void Start()
    {
        character = GetComponent<Character>();

        moveAction = InputSystem.actions.FindAction("Move");
        steerAction = InputSystem.actions.FindAction("Steering");
        accelerateAction = InputSystem.actions.FindAction("Accelerate");
        brakeAction = InputSystem.actions.FindAction("Brake");
        jumpAction = InputSystem.actions.FindAction("Jump");

        testAction = InputSystem.actions.FindAction("THE TEST BUTTON");

        replay = new ReplayData();
        replay.inputQueue = new Queue<CharacterInputSet>();
    }

    void Update()
    {
        if (accelerateAction.IsPressed())
        {
            buttonMask |= AcceleratePressedMask;
        }

        if (brakeAction.IsPressed())
        {
            buttonMask |= BrakePressedMask;
        }

        if (jumpAction.WasPerformedThisFrame())
        {
            buttonMask |= JumpPressedMask;
        }

        if (jumpAction.WasCompletedThisFrame())
        {
            buttonMask |= JumpReleasedMask;
        }

        if (testAction.WasPerformedThisFrame())
        {
            buttonMask |= TestPressedMask;
        }
    }

    unsafe void FixedUpdate()
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

        steerValue = (sbyte)(steerAction.ReadValue<float>() * 100f);

        Vector3 rightAxis = Vector3.Cross(upAxis, forwardAxis);
        Vector3 facingXAxis = moveValue.x * rightAxis;
        Vector3 facingZAxis = moveValue.y * forwardAxis;

        Vector3 moveVector = facingXAxis + facingZAxis;

        moveValue = new Vector2(moveVector.x, moveVector.z);

        sbyte moveValueX = (sbyte)(moveValue.x * 100f);
        sbyte moveValueY = (sbyte)(moveValue.y * 100f);

        CharacterInputSet inputs = new CharacterInputSet(moveValueX, moveValueY, buttonMask, steerValue, tick);

        if (previousInputs != inputs)
        {
            character.UpdateInputs(inputs);

            if (replay.inputQueue != null)
            {
                replay.inputQueue.Enqueue(inputs);
            }
        }

        if ((buttonMask & TestPressedMask) == TestPressedMask)
        {
            ReplayFunctions.WriteReplay(replay);
        }

        if (tick == 3000)
        {
            ReplayFunctions.WriteReplay(replay);
        }

        buttonMask = 0b00000000;
        previousInputs = inputs;

        tick++;
    }
}