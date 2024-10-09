using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class PlayerController : MonoBehaviour
{
    public float platformingAccel = 8f;
    public float platformingjumpForce = 200f;
    public float maxPlatformingSpeed = 10f;

    public float cameraSpeed = 0.5f;

    public float speedAccel = 15f;
    public float speedJumpForce = 500f;

    public Rigidbody physicsMovementBody;
    public Transform bodyVisual;
    public Transform cameraOrbitRot;

    bool SpeedMode = false;

    Vector3 heading;
    Vector3 orthoNormMove;
    Vector3 groundedUp = Vector3.up;
    bool grounded = true;

    InputAction moveAction;
    InputAction lookAction;
    InputAction steerAction;
    InputAction accelerateAction;
    InputAction brakeAction;
    InputAction jumpAction;

    Vector2 moveValue = Vector2.zero;
    Vector3 moveValue3D = Vector3.zero;

    Vector3 lookDir3D = Vector3.zero;

    bool jumpPressed = false;
    bool jumpReleased = false;
    float steerValue = 0f;
    float accelerateValue = 0f;
    float brakeValue = 0f;

    List<Collider> nearbyObjects = new List<Collider>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        steerAction = InputSystem.actions.FindAction("Steering");
        accelerateAction = InputSystem.actions.FindAction("Accelerate");
        brakeAction = InputSystem.actions.FindAction("Brake");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        moveValue = moveAction.ReadValue<Vector2>();
        moveValue3D = new Vector3(moveValue.x, 0, moveValue.y);

        steerValue = steerAction.ReadValue<float>();
        accelerateValue = accelerateAction.ReadValue<float>();
        brakeValue = brakeAction.ReadValue<float>();

        UpdateCamera();

        if (jumpAction.WasPerformedThisFrame())
        {
            jumpPressed = true;
        }

        if (jumpAction.WasCompletedThisFrame())
        {
            jumpReleased = true;
        }

        bodyVisual.LookAt(transform.position + orthoNormMove, groundedUp);
     }

    private void FixedUpdate()
    {
        heading = physicsMovementBody.linearVelocity.normalized;
        orthoNormMove = moveValue3D;
        grounded = false;

        if (accelerateValue > 0.1f)
        {
            SpeedMode = true;
        }

        if (SpeedMode)
        {
            CheckWallrunning();
        }
        else
        {
            CheckGrounded();
        }

        if (grounded)
        {
            if (physicsMovementBody.linearVelocity.magnitude < maxPlatformingSpeed)
            {
                physicsMovementBody.AddForce(orthoNormMove * platformingAccel);
            }

            if (jumpReleased)
            {
                physicsMovementBody.AddForce(groundedUp * platformingjumpForce);
            }
        }

        ResetInputs();
    }

    private void ResetInputs()
    {
        jumpPressed = false;
        jumpReleased = false;
    }

    private void CheckGrounded()
    {
        Ray groundRay = new Ray(transform.position, -groundedUp);
        if (Physics.SphereCast(groundRay, 0.3f, 0.6f))
        {
            grounded = true;
            groundedUp = Vector3.up;
        }
    }

    private void CheckWallrunning()
    {
        RaycastHit groundHit;
        Ray groundRay = new Ray(transform.position, -groundedUp);
        if (Physics.SphereCast(groundRay, 0.3f, out groundHit, 0.6f))
        {
            grounded = true;
            groundedUp = -(groundHit.point - transform.position).normalized;

            Vector3.OrthoNormalize(ref groundedUp, ref orthoNormMove);
            orthoNormMove *= moveValue3D.magnitude;
        }
        else
        {
            // not on previous ground, check for another
            Vector3 bestUp = Vector3.zero;
            float closestDot = -1f;

            foreach (Collider col in nearbyObjects)
            {
                Vector3 collisionPoint = col.ClosestPoint(transform.position);
                Vector3 tempUp = -(collisionPoint - transform.position).normalized;
                float tempDot = Vector3.Dot(Vector3.up, tempUp);

                if (tempDot > closestDot)
                {
                    bestUp = tempUp;
                    closestDot = tempDot;
                }
            }

            if (bestUp != Vector3.zero)
            {
                grounded = true;
                groundedUp = bestUp;
            }
        }
    }

    private void UpdateCamera()
    {
        Vector2 lookDir = lookAction.ReadValue<Vector2>();

        lookDir3D.x -= lookDir.y * cameraSpeed;
        lookDir3D.y += lookDir.x * cameraSpeed;

        lookDir3D.x = Mathf.Clamp(lookDir3D.x, -80f, 50f);

        cameraOrbitRot.rotation = Quaternion.Euler(lookDir3D);
    }

    private void OnTriggerEnter(Collider other)
    {
        nearbyObjects.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        nearbyObjects.Remove(other);
    }
}
