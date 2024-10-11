using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

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

    Vector3 facing;

    public float speedAccel = 10f;
    public float speedBrakeForce = 0.5f;
    public float speedMaxGrip = 1f;
    public float speedJumpForce = 500f;
    public float speedSteerAngle = 45f;
    public float speedAirSteerAngle = 10f;

    public AnimationCurve surfaceGrip;

    public float cameraSpeed = 0.5f;

    public Transform platformInputSpace = default;
    public Transform speedInputSpace = default;
    public Rigidbody physicsMovementBody;
    public Transform bodyVisual;
    public Transform cameraOrbitRot;

    bool SpeedMode = false;

    Vector3 contactNormal, steepNormal;
    int groundContactCount, steepContactCount;
    bool grounded => groundContactCount > 0;
    bool steep => steepContactCount > 0;

    int jumpPhase;
    float minGroundDotProduct;
    int stepsSinceLastGrounded, stepsSinceLastJump;

    Vector3 gravity, upAxis, rightAxis, forwardAxis;

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

    private void Awake()
    {
        physicsMovementBody.useGravity = false;
    }

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
        moveValue3D = new Vector3(moveValue.x, 0f, moveValue.y);

        Transform inputSpace = platformInputSpace;
        if (SpeedMode)
        {
            inputSpace = speedInputSpace;
        }
        
        if (inputSpace)
        {
            rightAxis = ProjectDirectionOnPlane(inputSpace.right, upAxis);
            forwardAxis =
                ProjectDirectionOnPlane(inputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
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

        if (SpeedMode)
        {
            bodyVisual.rotation = speedInputSpace.rotation;
        } 
        else
        {
            if (moveValue3D.magnitude > 0f)
            {
                facing = platformInputSpace.TransformDirection(moveValue3D);
                facing.y = 0f;

                facing.Normalize();

                bodyVisual.LookAt(transform.position + facing, upAxis);
            }
        }
     }

    private void FixedUpdate()
    {
        UpdateState();
        UpdateForces();

        if (SpeedMode)
        {
            //CheckWallrunning();
            if (jumpReleased)
            {
                SpeedJump();
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
        if (SpeedMode)
        {
            UpdateSpeedState();
        }
        else
        {
            UpdatePlatformingState();
        }
    }

    private void UpdatePlatformingState()
    {
        gravity = CustomGravity.GetGravity(transform.position, out upAxis);

        stepsSinceLastGrounded++;
        stepsSinceLastJump++;

        if (grounded && accelerateValue > 0.1f)
        {
            SpeedMode = true;
            speedInputSpace.rotation = Quaternion.LookRotation(facing, upAxis);
        }

        if (grounded || SnapToGround() || CheckSteepContacts())
        {
            stepsSinceLastGrounded = 0;
            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = upAxis;
        }
    }

    private void UpdateSpeedState()
    {
        gravity = CustomGravity.GetGravity(transform.position, out upAxis);

        stepsSinceLastGrounded++;
        stepsSinceLastJump++;

        if (grounded && accelerateValue < 0.1f && brakeValue < 0.1f && physicsMovementBody.linearVelocity.magnitude <= 0.5f)
        {
            SpeedMode = false;
        }

        if (grounded)
        {
            stepsSinceLastGrounded = 0;
            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = upAxis;
        }
    }

    private void UpdateForces()
    {
        if (SpeedMode)
        {
            UpdateSpeedForces();
        } 
        else
        {
            UpdatePlatformingForces();
        }
    }

    private void UpdatePlatformingForces()
    {
        velocity = physicsMovementBody.linearVelocity;

        Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
        Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);

        Vector3 adjustment = Vector3.zero;
        adjustment.x =
            moveValue3D.x * platformingMaxSpeed - Vector3.Dot(velocity, xAxis);
        adjustment.z =
            moveValue3D.z * platformingMaxSpeed - Vector3.Dot(velocity, zAxis);

        float acceleration = grounded ? platformingMaxAccel : platformingMaxAirAccel;

        adjustment =
            Vector3.ClampMagnitude(adjustment, acceleration * Time.fixedDeltaTime);

        velocity += xAxis * adjustment.x + zAxis * adjustment.z;

        if (grounded)
        {
            velocity +=
                contactNormal *
                (Vector3.Dot(gravity, contactNormal) * Time.fixedDeltaTime);
        }
        else
        {
            velocity += gravity * Time.fixedDeltaTime;
        }
    }

    private void UpdateSpeedForces()
    {
        velocity = physicsMovementBody.linearVelocity;

        float steering = grounded ? steerValue * speedSteerAngle : steerValue * speedAirSteerAngle;

        Vector3 zAxis = ProjectDirectionOnPlane(speedInputSpace.forward, contactNormal);

        speedInputSpace.rotation = Quaternion.LookRotation(zAxis, contactNormal);

        speedInputSpace.Rotate(speedInputSpace.up, steering * Time.fixedDeltaTime);

        float slidingVel = Vector3.Dot(speedInputSpace.right, velocity);
        float slidingRatio = Vector3.Dot(speedInputSpace.right, velocity.normalized);
        float gripForce = -slidingVel * surfaceGrip.Evaluate(slidingRatio);

        gripForce = Mathf.Clamp(gripForce, -speedMaxGrip, speedMaxGrip);

        Vector3 slidingCorrection = speedInputSpace.right * gripForce;

        Vector3 movementDir = speedInputSpace.forward * accelerateValue * speedAccel;

        float rollingVel = Vector3.Dot(speedInputSpace.forward, velocity);
        float brakeForce = brakeValue * speedBrakeForce;

        if (accelerateValue <= 0f && brakeValue <= 0.1f)
        {
            brakeForce = speedBrakeForce * 0.1f;
        }

        float brakeGoal = Mathf.MoveTowards(rollingVel, 0f, brakeForce);
        brakeForce = brakeGoal - rollingVel;

        movementDir += speedInputSpace.forward * brakeForce;

        Vector3 downForce = 0.2f * gravity.magnitude * -contactNormal;

        if (grounded)
        {
            velocity += movementDir;

            velocity += slidingCorrection;

            velocity += downForce * Time.fixedDeltaTime;
        }

        velocity += gravity * Time.fixedDeltaTime;
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
        float jumpVelocity = Mathf.Sqrt(2f * gravity.magnitude * platformingJumpHeight);
        jumpDirection = (jumpDirection + upAxis).normalized;
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        if (alignedSpeed > 0f)
        {
            jumpVelocity = Mathf.Max(jumpVelocity - alignedSpeed, 0f);
        }
        velocity += jumpDirection * jumpVelocity;
    }

    private void SpeedJump()
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
        float jumpVelocity = Mathf.Sqrt(2f * gravity.magnitude * platformingJumpHeight);
        jumpDirection = (jumpDirection + upAxis).normalized;
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
            contactNormal = upAxis;
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

            //Vector3.OrthoNormalize(ref contactNormal, ref orthoNormMove);
            //orthoNormMove *= moveValue3D.magnitude;
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
                float tempDot = Vector3.Dot(upAxis, tempUp);

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
            float upDot = Vector3.Dot(upAxis, normal);
            if (!SpeedMode)
            {
                if (upDot >= minGroundDotProduct)
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
            else
            {
                groundContactCount += 1;
                contactNormal += normal;
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
        if (!Physics.Raycast(transform.position, -upAxis, out RaycastHit hit, platformingSnapProbeDist, platformingProbeMask))
        {
            return false;
        }
        float upDot = Vector3.Dot(upAxis, hit.normal);
        if (upDot < minGroundDotProduct)
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
            float upDot = Vector3.Dot(upAxis, steepNormal);
            if (upDot >= minGroundDotProduct)
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

    Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
    {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }
}
