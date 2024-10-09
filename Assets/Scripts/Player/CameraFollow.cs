using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Transform orbitPos;
    public float rotSmoothSpeed = 0.125f;
    public float posSmoothSpeed = 0.125f;

    void FixedUpdate()
    {
        Vector3 desiredPosition = orbitPos.position;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, posSmoothSpeed);
        transform.position = smoothedPosition;

        Quaternion desiredrotation = Quaternion.LookRotation(player.transform.position - transform.position);
        Quaternion smoothedrotation = Quaternion.Lerp(transform.rotation, desiredrotation, rotSmoothSpeed);
        transform.rotation = smoothedrotation;
    }
}
