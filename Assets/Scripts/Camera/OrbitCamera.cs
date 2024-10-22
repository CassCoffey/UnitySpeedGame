using SpeedGame;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour 
{
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

    InputAction lookAction;

    float lastManualRotationTime;

    CameraManager manager = default;

    void Awake()
    {
        manager = GetComponent<CameraManager>();
    }

    private void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
    }

    public void CameraUpdate()
    {
        manager.gravityAlignment =
        Quaternion.FromToRotation(
            manager.gravityAlignment * Vector3.up, CustomGravity.GetUpAxis(manager.focusPoint)
        ) * manager.gravityAlignment;

        UpdateFocusPoint();
        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            manager.orbitRotation = Quaternion.Euler(manager.orbitAngles);
        }

        Quaternion lookRotation = manager.gravityAlignment * manager.orbitRotation;

        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = manager.focusPoint - lookDirection * distance;

        Vector3 rectOffset = lookDirection * manager.regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = manager.focus.transform.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        if (Physics.BoxCast(
            castFrom, manager.CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance, obstructionMask
        ))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            lookPosition = rectPosition - rectOffset;
        }

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void UpdateFocusPoint()
    {
        manager.previousFocusPoint = manager.focusPoint;
        Vector3 targetPoint = manager.focus.transform.position;
        if (focusRadius > 0f)
        {
            float distance = Vector3.Distance(targetPoint, manager.focusPoint);
            float t = 1f;
            if (distance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }
            if (distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
            }
            manager.focusPoint = Vector3.Lerp(targetPoint, manager.focusPoint, t);
        }
        else
        {
            manager.focusPoint = targetPoint;
        }
    }

    bool ManualRotation()
    {
        Vector2 lookDir = lookAction.ReadValue<Vector2>();
        Vector2 input = new Vector2(-lookDir.y, lookDir.x);
        const float e = 0.001f;
        if (input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            manager.orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
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
            Quaternion.Inverse(manager.gravityAlignment) *
            (manager.focusPoint - manager.previousFocusPoint);
        Vector2 movement = new Vector2(alignedDelta.x, alignedDelta.z);
        float movementDeltaSqr = movement.sqrMagnitude;
        if (movementDeltaSqr < 0.0001f)
        {
            return false;
        }

        float headingAngle = UtilFunctions.GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(manager.orbitAngles.y, headingAngle));
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if (180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }
        manager.orbitAngles.y = Mathf.MoveTowardsAngle(manager.orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    void ConstrainAngles()
    {
        manager.orbitAngles.x =
            Mathf.Clamp(manager.orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (manager.orbitAngles.y < 0f)
        {
            manager.orbitAngles.y += 360f;
        }
        else if (manager.orbitAngles.y >= 360f)
        {
            manager.orbitAngles.y -= 360f;
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
