using UnityEngine;

/// Damped spring slosh. The surface lags behind the cup when you start and stop
/// walking, and settles when you stand still. Put this on the cup root and point
/// it at the liquid transform.
public class LiquidWobble : MonoBehaviour
{
    public Transform Liquid;

    [Header("Spring")]
    public float Stiffness = 42f;
    public float Damping = 7.5f;
    [Tooltip("Degrees of tilt at full slosh.")]
    public float MaxTilt = 11f;
    public float DriveFromAcceleration = 0.55f;

    Vector3 _lastPos;
    Vector3 _lastVel;
    Vector2 _tilt;      // x = pitch, y = roll
    Vector2 _tiltVel;
    bool _primed;

    void OnEnable()
    {
        _lastPos = transform.position;
        _lastVel = Vector3.zero;
        _tilt = Vector2.zero;
        _tiltVel = Vector2.zero;
        _primed = false;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 pos = transform.position;
        Vector3 vel = (pos - _lastPos) / dt;
        _lastPos = pos;

        if (!_primed) { _lastVel = vel; _primed = true; return; }

        Vector3 accel = (vel - _lastVel) / dt;
        _lastVel = vel;

        // Drive in the cup's own frame, so the slosh direction follows the cup.
        Vector3 local = transform.InverseTransformDirection(accel);
        Vector2 drive = new Vector2(local.z, -local.x) * DriveFromAcceleration;
        drive = Vector2.ClampMagnitude(drive, 80f);

        _tiltVel += (drive - _tilt * Stiffness) * dt;
        _tiltVel -= _tiltVel * Mathf.Clamp01(Damping * dt);
        _tilt += _tiltVel * dt;
        _tilt = Vector2.ClampMagnitude(_tilt, MaxTilt);

        if (Liquid != null)
            Liquid.localRotation = Quaternion.Euler(_tilt.x, 0f, _tilt.y);
    }
}
