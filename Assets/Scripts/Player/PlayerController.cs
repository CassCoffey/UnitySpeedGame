using SpeedGame;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.IMGUI.Controls.CapsuleBoundsHandle;

[DefaultExecutionOrder(10)]
public class PlayerController : MonoBehaviour
{
    public Transform platformInputSpace = default;

    private Character character = null;

    private InputAction moveAction;
    private InputAction steerAction;
    private InputAction accelerateAction;
    private InputAction brakeAction;
    private InputAction jumpAction;

    private InputAction testButton;

    private ReplayData replay;

    Vector2 moveValue;
    Vector3 rightAxis, forwardAxis;
    float steerValue, accelerateValue, brakeValue;
    bool jumpPressed, jumpReleased, testPressed;

    private long tick = 0;

    void Start()
    {
        character = GetComponent<Character>();

        moveAction = InputSystem.actions.FindAction("Move");
        steerAction = InputSystem.actions.FindAction("Steering");
        accelerateAction = InputSystem.actions.FindAction("Accelerate");
        brakeAction = InputSystem.actions.FindAction("Brake");
        jumpAction = InputSystem.actions.FindAction("Jump");

        testButton = InputSystem.actions.FindAction("THE TEST BUTTON");

        replay = new ReplayData();
        replay.inputQueue = new Queue<CharacterInputSet>();
    }

    void Update()
    {
        if (jumpAction.WasPerformedThisFrame())
        {
            jumpPressed = true;
        }

        if (jumpAction.WasCompletedThisFrame())
        {
            jumpReleased = true;
        }

        if (testButton.WasPerformedThisFrame())
        {
            testPressed = true;
        }
    }

    void FixedUpdate()
    {
        Vector3 upAxis = CustomGravity.GetUpAxis(transform.position);

        moveValue = moveAction.ReadValue<Vector2>();

        Transform inputSpace = platformInputSpace;

        if (inputSpace)
        {
            rightAxis = UtilFunctions.ProjectDirectionOnPlane(inputSpace.right, upAxis);
            forwardAxis = UtilFunctions.ProjectDirectionOnPlane(inputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = UtilFunctions.ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = UtilFunctions.ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }

        steerValue = steerAction.ReadValue<float>();
        accelerateValue = accelerateAction.ReadValue<float>();
        brakeValue = brakeAction.ReadValue<float>();

        CharacterInputSet inputs = new CharacterInputSet(moveValue, rightAxis, forwardAxis, jumpPressed, jumpReleased, steerValue, accelerateValue, brakeValue, tick);

        character.UpdateInputs(inputs);

        if (replay.inputQueue != null)
        {
            replay.inputQueue.Enqueue(inputs);
        }

        if (testPressed)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            ReplayFunctions.WriteReplay(replay);
            watch.Stop();
        }

        jumpPressed = jumpReleased = testPressed = false;

        tick++;
    }
}