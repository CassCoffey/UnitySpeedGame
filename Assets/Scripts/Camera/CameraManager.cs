using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Character focus = default;

    public LayerMask obstructionMask = -1;

    private OrbitCamera orbitCam = default;
    private ChaseCamera chaseCam = default;

    [HideInInspector]
    public Vector3 focusPoint, previousFocusPoint;
    [HideInInspector]
    public Vector2 orbitAngles = new Vector2(45f, 0f);

    [HideInInspector]
    public Quaternion gravityAlignment = Quaternion.identity;
    [HideInInspector]
    public Quaternion orbitRotation;

    [HideInInspector]
    public Camera regularCamera;

    void Start()
    {
        orbitCam = GetComponent<OrbitCamera>();
        chaseCam = GetComponent<ChaseCamera>();

        regularCamera = GetComponent<Camera>();
        focusPoint = focus.transform.position;
        transform.localRotation = orbitRotation = Quaternion.Euler(orbitAngles);
    }

    void LateUpdate()
    {
        if (focus.SpeedMode)
        {
            chaseCam.CameraUpdate();
        }
        else
        {
            orbitCam.CameraUpdate();
        }
    }

    public Vector3 CameraHalfExtends
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
}
