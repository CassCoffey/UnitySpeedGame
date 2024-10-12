using UnityEngine;

namespace SpeedGame
{
    public static class UtilFunctions
    {
        public static Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        public static float GetAngle(Vector2 direction)
        {
            float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
            return direction.x < 0f ? 360f - angle : angle;
        }
    }
}
