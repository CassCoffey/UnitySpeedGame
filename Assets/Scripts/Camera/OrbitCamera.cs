using SpeedGame;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour 
{
    [SerializeField]
    Transform focus = default;

    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.5f;

    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    [SerializeField, Min(0f)]
    float alignDelay = 5f;

    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;

    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    [SerializeField]
    LayerMask obstructionMask = -1;

    Vector3 focusPoint, previousFocusPoint;
    Vector2 orbitAngles = new Vector2(45f, 0f);

    Quaternion gravityAlignment = Quaternion.identity;
    Quaternion orbitRotation;

    float lastManualRotationTime;

    InputAction lookAction;

    Camera regularCamera;

    void Awake()
    {
        regularCamera = GetComponent<Camera>();
        focusPoint = focus.position;
        transform.localRotation = orbitRotation = Quaternion.Euler(orbitAngles);
    }

    private void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
    }

    void LateUpdate()
    {
        gravityAlignment =
            Quaternion.FromToRotation(
                gravityAlignment * Vector3.up, CustomGravity.GetUpAxis(focusPoint)
            ) * gravityAlignment;
        UpdateFocusPoint();
        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            orbitRotation = Quaternion.Euler(orbitAngles);
        }

        Quaternion lookRotation = gravityAlignment * orbitRotation;

        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = focus.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        if (Physics.BoxCast(
            castFrom, CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance, obstructionMask
        ))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            lookPosition = rectPosition - rectOffset;
        }

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;
        if (focusRadius > 0f)
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;
            if (distance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }
            if (distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
            }
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else
        {
            focusPoint = targetPoint;
        }
    }

    bool ManualRotation()
    {
        Vector2 lookDir = lookAction.ReadValue<Vector2>();
        Vector2 input = new Vector2(-lookDir.y, lookDir.x);
        const float e = 0.001f;
        if (input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }

    bool AutomaticRotation()
    {
        if (Time.unscaledTime - lastManualRotationTime < alignDelay)
        {
            return false;
        }

        Vector3 alignedDelta =
            Quaternion.Inverse(gravityAlignment) *
            (focusPoint - previousFocusPoint);
        Vector2 movement = new Vector2(alignedDelta.x, alignedDelta.z);
        float movementDeltaSqr = movement.sqrMagnitude;
        if (movementDeltaSqr < 0.0001f)
        {
            return false;
        }

        float headingAngle = UtilFunctions.GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if (180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    void ConstrainAngles()
    {
        orbitAngles.x =
            Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (orbitAngles.y < 0f)
        {
            orbitAngles.y += 360f;
        }
        else if (orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }

    Vector3 CameraHalfExtends
    {
        get
        {
            Vector3 halfExtends;
            halfExtends.y =
                regularCamera.nearClipPlane *
                Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }

    void OnValidate()
    {
        if (maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }
}
