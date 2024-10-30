using System.Collections.Generic;
using UnityEngine;
using SpeedGame;

[DefaultExecutionOrder(100)]
public class Character : MonoBehaviour
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

    public float speedAccel = 10f;
    public float speedBrakeForce = 0.5f;
    public float speedMaxGrip = 1f;
    public float speedJumpForce = 500f;
    public float speedSteerAngle = 45f;
    public float speedAirSteerAngle = 10f;

    public AnimationCurve surfaceGrip;

    public Transform speedInputSpace = default;
    public Rigidbody physicsMovementBody;
    public Collider mainCollider;
    public Transform bodyVisual;

    private Vector3 facing = Vector3.forward;

    [HideInInspector]
    public bool SpeedMode = false;

    [HideInInspector]
    public Vector3 contactNormal, steepNormal;
    private int groundContactCount, steepContactCount;
    private bool grounded => groundContactCount > 0;
    private bool steep => steepContactCount > 0;

    private int jumpPhase;
    private float minGroundDotProduct;
    private int stepsSinceLastGrounded, stepsSinceLastJump;

    private Vector3 gravity, upAxis;

    private Vector3 velocity = Vector3.zero;

    private Vector2 moveValue = Vector2.zero;
    private bool acceleratePressed, brakePressed = false;
    private bool jumpPressed = false;
    private bool jumpReleased = false;
    private bool jumpPrevious = false;
    private float steerValue = 0f;

    private bool checkpointResetPressedPrev = false;
    private int checkpointResetPressTicks = 0;

    private void Awake()
    {
        physicsMovementBody.useGravity = false;
    }

    void Start()
    {
        minGroundDotProduct = Mathf.Cos(platformingMaxGroundAngle * Mathf.Deg2Rad);
    }

    public void Reset()
    {
        SpeedMode = false;

        facing = Vector3.forward;
        contactNormal = steepNormal = Vector3.zero;

        physicsMovementBody.linearVelocity = Vector3.zero;
        moveValue = Vector2.zero;
        acceleratePressed = brakePressed = false;
        jumpPressed = jumpReleased = jumpPrevious = false;
        steerValue = 0f;

        jumpPhase = 0;
        stepsSinceLastGrounded = stepsSinceLastJump = 0;
}

    private void FixedUpdate()
    {
        UpdateState();
        UpdateForces();

        if (SpeedMode)
        {
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

    public void UpdateInputs(CharacterInputSet inputs)
    {
        moveValue = new Vector2((float)inputs.MoveValueX / 100f, (float)inputs.MoveValueY / 100f);

        bool jump = (inputs.ButtonMask & Controller.JumpMask) == Controller.JumpMask;
        acceleratePressed = (inputs.ButtonMask & Controller.AccelerateMask) == Controller.AccelerateMask;
        brakePressed = (inputs.ButtonMask & Controller.BrakeMask) == Controller.BrakeMask;
        steerValue = (float)inputs.SteerValue / 100f;

        if (jump && !jumpPrevious)
        {
            jumpPressed = true;
        }
        else if (!jump && jumpPrevious)
        {
            jumpReleased = true;
        }

        jumpPrevious = jump;

        if ((inputs.ButtonMask & Controller.FullResetMask) == Controller.FullResetMask)
        {
            StageManager.FullReset();
        }
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
        if (moveValue.magnitude > 0f)
        {
            facing = new Vector3(moveValue.x, 0, moveValue.y);

            bodyVisual.LookAt(transform.position + facing, upAxis);
        }

        gravity = CustomGravity.GetGravity(transform.position, out upAxis);

        stepsSinceLastGrounded++;
        stepsSinceLastJump++;

        if (grounded && acceleratePressed)
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
        bodyVisual.rotation = speedInputSpace.rotation;

        gravity = CustomGravity.GetGravity(transform.position, out upAxis);

        stepsSinceLastGrounded++;
        stepsSinceLastJump++;

        if (grounded && !acceleratePressed && !brakePressed && physicsMovementBody.linearVelocity.magnitude <= 0.5f)
        {
            SpeedMode = false;
            facing = speedInputSpace.forward;
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

        Vector3 zAxis = UtilFunctions.ProjectDirectionOnPlane(transform.forward, contactNormal);
        Vector3 xAxis = UtilFunctions.ProjectDirectionOnPlane(transform.right, contactNormal);

        Vector3 adjustment = Vector3.zero;
        adjustment.x =
            moveValue.x * platformingMaxSpeed - Vector3.Dot(velocity, xAxis);
        adjustment.z =
            moveValue.y * platformingMaxSpeed - Vector3.Dot(velocity, zAxis);

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

        Vector3 zAxis = UtilFunctions.ProjectDirectionOnPlane(speedInputSpace.forward, contactNormal);

        speedInputSpace.rotation = Quaternion.LookRotation(zAxis, contactNormal);

        speedInputSpace.Rotate(0, steering * Time.fixedDeltaTime, 0, Space.Self);

        float slidingVel = Vector3.Dot(speedInputSpace.right, velocity);
        float slidingRatio = Vector3.Dot(speedInputSpace.right, velocity.normalized);
        float gripForce = -slidingVel * surfaceGrip.Evaluate(slidingRatio);

        gripForce = Mathf.Clamp(gripForce, -speedMaxGrip, speedMaxGrip);

        Vector3 slidingCorrection = speedInputSpace.right * gripForce;

        Vector3 movementDir = Vector3.zero;

        float rollingVel = Vector3.Dot(speedInputSpace.forward, velocity);
        float brakeForce = 0f;

        if (acceleratePressed)
        {
            movementDir = speedInputSpace.forward * speedAccel;
        }

        if (brakePressed)
        {
            brakeForce = speedBrakeForce;
        }

        if (!acceleratePressed && !brakePressed)
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
}
