using SpeedGame;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class ChaseCamera : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.5f;

    [SerializeField, Range(1f, 1500f)]
    float rotationSpeed = 1200f;

    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    [SerializeField, Range(0f, 90f)]
    float angle = 40f;

    [SerializeField, Range(0f, 90f)]
    float angleThreshold = 30f;

    Vector3 surfaceNormal = Vector3.up;

    CameraManager manager = default;

    void Awake()
    {
        manager = GetComponent<CameraManager>();
    }

    public void CameraUpdate()
    {
        manager.gravityAlignment =
        Quaternion.FromToRotation(
            manager.gravityAlignment * Vector3.up, manager.focus.contactNormal
        ) * manager.gravityAlignment;

        UpdateFocusPoint();
        if (AutomaticRotation())
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
            castFrom, manager.CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance, manager.obstructionMask
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

    bool AutomaticRotation()
    {
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
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        manager.orbitAngles.y = Mathf.MoveTowardsAngle(manager.orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    void ConstrainAngles()
    {
        manager.orbitAngles.x =
            Mathf.Clamp(manager.orbitAngles.x, -360f, 360f);

        if (manager.orbitAngles.y < 0f)
        {
            manager.orbitAngles.y += 360f;
        }
        else if (manager.orbitAngles.y >= 360f)
        {
            manager.orbitAngles.y -= 360f;
        }
    }
}
