using UnityEngine;

/// Pure targeting maths, kept apart from physics so it can be unit tested and
/// shared by any look source (mouse today, a gamepad stick later).
public static class AimAssist
{
    /// How far off the aim ray a point is, in degrees, or +Infinity when it is
    /// out of range or outside the assist cone. Lower is a better target.
    /// The current focus gets `stickyDegrees` knocked off so the pick does not
    /// flicker between two neighbours.
    public static float Score(Vector3 origin, Vector3 forward, Vector3 point,
                              float range, float coneDegrees, bool isCurrent, float stickyDegrees)
    {
        Vector3 to = point - origin;
        float dist = to.magnitude;
        if (dist > range) return float.PositiveInfinity;
        float angle = dist < 1e-4f ? 0f : Vector3.Angle(forward, to);
        if (isCurrent) angle -= stickyDegrees;
        return angle <= coneDegrees ? angle : float.PositiveInfinity;
    }

    /// The point on the aim ray (clamped to 0..range) nearest `target`, used to
    /// ask a collider for its surface point closest to where the player looks.
    public static Vector3 PointOnRay(Vector3 origin, Vector3 forward, Vector3 target, float range)
    {
        float t = Mathf.Clamp(Vector3.Dot(target - origin, forward), 0f, range);
        return origin + forward * t;
    }
}
