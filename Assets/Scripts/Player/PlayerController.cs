using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class PlayerController : MonoBehaviour
{
    public float platformingMaxSpeed = 10f;
    public float platformingMaxAccel = 10f;
    public float platformingMaxAirAccel = 1f;
    public float platformingJumpHeight = 2f;
    public float platformingMaxGroundAngle = 40f;
    public float platformingMaxSnapSpeed = 8f;
    public float platformingSnapProbeDist = 1f;
    public LayerMask platformingProbeMask = -1;
    public int maxAirJumps = 1;

    public float cameraSpeed = 0.5f;

    public float speedAccel = 15f;
    public float speedJumpForce = 500f;

    public Transform platformInputSpace = default;
    public Transform speedInputSpace = default;
    public Rigidbody physicsMovementBody;
    public Transform bodyVisual;
    public Transform cameraOrbitRot;

    bool SpeedMode = false;

    Vector3 heading;
    Vector3 orthoNormMove;
    Vector3 contactNormal, steepNormal;
    int groundContactCount, steepContactCount;
    bool grounded => groundContactCount > 0;
    bool steep => steepContactCount > 0;

    int jumpPhase;
    float minGroundDotProduct;
    int stepsSinceLastGrounded, stepsSinceLastJump;

    Vector3 velocity = Vector3.zero;

    InputAction moveAction;
    InputAction steerAction;
    InputAction accelerateAction;
    InputAction brakeAction;
    InputAction jumpAction;

    Vector2 moveValue = Vector2.zero;
    Vector3 moveValue3D = Vector3.zero;

    bool jumpPressed = false;
    bool jumpReleased = false;
    float steerValue = 0f;
    float accelerateValue = 0f;
    float brakeValue = 0f;

    List<Collider> nearbyObjects = new List<Collider>();

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        steerAction = InputSystem.actions.FindAction("Steering");
        accelerateAction = InputSystem.actions.FindAction("Accelerate");
        brakeAction = InputSystem.actions.FindAction("Brake");
        jumpAction = InputSystem.actions.FindAction("Jump");

        minGroundDotProduct = Mathf.Cos(platformingMaxGroundAngle * Mathf.Deg2Rad);
    }

    void Update()
    {
        moveValue = moveAction.ReadValue<Vector2>();

        if (platformInputSpace)
        {
            Vector3 forward = platformInputSpace.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = platformInputSpace.right;
            right.y = 0f;
            right.Normalize();
            moveValue3D = (forward * moveValue.y + right * moveValue.x);
        } 
        else
        {
            moveValue3D = new Vector3(moveValue.x, 0, moveValue.y);
        }

        if (!SpeedMode)
        {
            // adjust movement to camera in platforming mode
            //moveValue3D = Quaternion.AngleAxis(lookDir3D.y, Vector3.up) * moveValue3D;
        }

        steerValue = steerAction.ReadValue<float>();
        accelerateValue = accelerateAction.ReadValue<float>();
        brakeValue = brakeAction.ReadValue<float>();

        if (jumpAction.WasPerformedThisFrame())
        {
            jumpPressed = true;
        }

        if (jumpAction.WasCompletedThisFrame())
        {
            jumpReleased = true;
        }

        bodyVisual.LookAt(transform.position + orthoNormMove, SpeedMode ? contactNormal : Vector3.up);
     }

    private void FixedUpdate()
    {
        UpdateState();
        UpdateForces();

        if (SpeedMode)
        {
            CheckWallrunning();
            if (jumpReleased)
            {
                Vector3 jumpVelocity = contactNormal * Mathf.Sqrt(-2f * Physics.gravity.y * platformingJumpHeight);
                velocity += jumpVelocity;
            }
        }
        else
        {
            if (jumpPressed)
            {
                PlatformingJump();
            }
        }

        physicsMovementBody.linearVelocity = velocity;

        ResetInputs();
        ClearState();
    }

    private void ClearState()
    {
        groundContactCount = steepContactCount = 0;
        contactNormal = steepNormal = Vector3.zero;
    }

    private void ResetInputs()
    {
        jumpPressed = false;
        jumpReleased = false;
    }

    private void UpdateState()
    {
        stepsSinceLastGrounded++;
        stepsSinceLastJump++;

        heading = physicsMovementBody.linearVelocity.normalized;
        orthoNormMove = moveValue3D;

        if (grounded || SnapToGround() || CheckSteepContacts())
        {
            stepsSinceLastGrounded = 0;
            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            contactNormal.Normalize();
        }
        else
        {
            contactNormal = Vector3.up;
        }

        //if (accelerateValue > 0.1f)
        //{
        //    SpeedMode = true;
        //}
    }

    private void UpdateForces()
    {
        velocity = physicsMovementBody.linearVelocity;

        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        Vector3 desiredVelocity = moveValue3D * platformingMaxSpeed;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = grounded ? platformingMaxAccel : platformingMaxAirAccel;
        float maxSpeedChange = acceleration;

        float newX =
            Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ =
            Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    private void PlatformingJump()
    {
        Vector3 jumpDirection;

        if (grounded)
        {
            jumpDirection = contactNormal;
        }
        else if (steep)
        {
            jumpDirection = steepNormal;
            jumpPhase = 0;
        }
        else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0)
            {
                jumpPhase = 1;
            }
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }

        stepsSinceLastJump = 0;
        jumpPhase++;
        float jumpVelocity = Mathf.Sqrt(-2f * Physics.gravity.y * platformingJumpHeight);
        jumpDirection = (jumpDirection + Vector3.up).normalized;
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        if (alignedSpeed > 0f)
        {
            jumpVelocity = Mathf.Max(jumpVelocity - alignedSpeed, 0f);
        }
        velocity += jumpDirection * jumpVelocity;
    }

    private void CheckGrounded()
    {
        Ray groundRay = new Ray(transform.position, -contactNormal);
        if (Physics.SphereCast(groundRay, 0.3f, 0.6f))
        {
            groundContactCount = 1;
            contactNormal = Vector3.up;
        }
    }

    private void CheckWallrunning()
    {
        RaycastHit groundHit;
        Ray groundRay = new Ray(transform.position, -contactNormal);
        if (Physics.SphereCast(groundRay, 0.3f, out groundHit, 0.6f))
        {
            groundContactCount = 1;
            contactNormal = -(groundHit.point - transform.position).normalized;

            Vector3.OrthoNormalize(ref contactNormal, ref orthoNormMove);
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
                groundContactCount = 1;
                contactNormal = bestUp;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (normal.y > -0.01f)
            {
                steepContactCount += 1;
                steepNormal += normal;
            }
        }
    }

    bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
        {
            return false;
        }
        float speed = velocity.magnitude;
        if (speed > platformingMaxSnapSpeed)
        {
            return false;
        }
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, platformingSnapProbeDist, platformingProbeMask))
        {
            return false;
        }
        if (hit.normal.y < minGroundDotProduct)
        {
            return false;
        }

        groundContactCount += 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }

        return false;
    }

    bool CheckSteepContacts()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (steepNormal.y >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        nearbyObjects.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        nearbyObjects.Remove(other);
    }

    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }
}
