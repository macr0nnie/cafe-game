using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float WalkSpeed = 2.4f;
    public float SprintSpeed = 3.8f;
    public float Acceleration = 14f;
    public float Gravity = -14f;

    [Header("Look")]
    public Transform CameraPivot;
    public float Sensitivity = 2.2f;
    public float PitchLimit = 85f;

    [Header("Head bob")]
    public float BobAmplitude = 0.022f;
    public float BobFrequency = 8.5f;

    CharacterController _cc;
    Vector3 _velocity;
    Vector3 _planar;
    float _pitch;
    float _bobT;
    Vector3 _camRest;
    bool _locked = true;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (CameraPivot == null && Camera.main != null) CameraPivot = Camera.main.transform;
        if (CameraPivot != null) _camRest = CameraPivot.localPosition;
        SetLock(true);
    }

    void SetLock(bool on)
    {
        _locked = on;
        Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !on;
    }

    void Update()
    {
        if (CafeInput.CancelDown()) SetLock(!_locked);
        if (!_locked) return;

        // Look
        Vector2 look = CafeInput.Look() * Sensitivity;
        transform.Rotate(Vector3.up, look.x, Space.World);
        _pitch = Mathf.Clamp(_pitch - look.y, -PitchLimit, PitchLimit);
        if (CameraPivot != null)
            CameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // Move
        Vector2 mv = CafeInput.Move();
        Vector3 wish = (transform.right * mv.x + transform.forward * mv.y);
        if (wish.sqrMagnitude > 1f) wish.Normalize();
        float speed = CafeInput.SprintHeld() ? SprintSpeed : WalkSpeed;
        _planar = Vector3.MoveTowards(_planar, wish * speed, Acceleration * Time.deltaTime);

        if (_cc.isGrounded && _velocity.y < 0f) _velocity.y = -2f;
        _velocity.y += Gravity * Time.deltaTime;

        _cc.Move((_planar + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime);

        // Head bob, gentle. Sells the cosy pace more than anything else here.
        if (CameraPivot != null)
        {
            float sp = new Vector2(_planar.x, _planar.z).magnitude;
            if (sp > 0.1f)
            {
                _bobT += Time.deltaTime * BobFrequency * (sp / WalkSpeed);
                float yb = Mathf.Sin(_bobT) * BobAmplitude;
                float xb = Mathf.Cos(_bobT * 0.5f) * BobAmplitude * 0.6f;
                CameraPivot.localPosition = Vector3.Lerp(CameraPivot.localPosition,
                    _camRest + new Vector3(xb, yb, 0f), Time.deltaTime * 12f);
            }
            else
            {
                CameraPivot.localPosition = Vector3.Lerp(CameraPivot.localPosition,
                    _camRest, Time.deltaTime * 8f);
            }
        }
    }
}
