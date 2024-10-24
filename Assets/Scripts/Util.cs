using System;
using UnityEngine;

namespace SpeedGame
{
    public static class UtilFunctions
    {
        public const long TicksPerDateTimeTick = 20 * 10000;

        public static Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        public static float GetAngle(Vector2 direction)
        {
            float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
            return direction.x < 0f ? 360f - angle : angle;
        }

        public static long ConvertGameToDatetimeTicks(uint ticks)
        {
            // milliseconds
            long realTicks = ticks * 20;
            // convert to ticks
            realTicks *= TimeSpan.TicksPerMillisecond;

            return realTicks;
        }

        public static TimeSpan GetTrueTriggerTime(Character character, Collider trigger, uint tick)
        {
            long currentTick = ConvertGameToDatetimeTicks(tick);

            Vector3 charVel = character.GetComponent<Rigidbody>().linearVelocity * Time.fixedDeltaTime;
            Vector3 charCurPos = character.transform.position;
            Vector3 charPrevPos = charCurPos - charVel;

            // Create a plane to represent the closest side of the trigger box
            Vector3 triggerNormal = trigger.transform.forward;
            Vector3 triggerPoint = trigger.ClosestPoint(charPrevPos);
            Plane triggerPlane = new Plane(triggerNormal, triggerPoint);

            // Character is a sphere, so we can use their radius to find the contact point
            float charRadius = ((SphereCollider)character.mainCollider).radius;
            Vector3 headingOnContactNormal = Vector3.Project(charVel, triggerNormal).normalized;
            charCurPos += (headingOnContactNormal * charRadius);
            charPrevPos += (headingOnContactNormal * charRadius);

            while (triggerPlane.GetSide(charCurPos) == triggerPlane.GetSide(charPrevPos))
            {
                // Trigger tends to be called an update too late, find the actual two points from when the character passed over it
                charCurPos -= charVel;
                charPrevPos -= charVel;
            }

            float totalDist = (charCurPos - charPrevPos).magnitude;

            Ray charHeading = new Ray(charPrevPos, charVel);
            if (triggerPlane.Raycast(charHeading, out float collisionDist))
            {
                float tickFraction = collisionDist / totalDist;

                long result = Mathf.RoundToInt(tickFraction * (float)TicksPerDateTimeTick);

                result = currentTick - result;

                return new TimeSpan(result);
            } 
            else
            {
                Debug.Log("Something is wrong, failed to find trigger collision point.");
                return TimeSpan.MinValue;
            }
        } 
    }
}
