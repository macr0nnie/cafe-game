using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Carries the cup. The cup lives in world space and chases a target transform,
/// so it can be handed to a station and taken back without a reparent snap, and
/// so the slosh sees real world motion.
public class PlayerHands : MonoBehaviour
{
    public Transform HoldAnchor;
    public GameObject DefaultCupPrefab;

    [Header("Follow")]
    public float FollowSpeed = 16f;
    public float RotateSpeed = 14f;

    [Header("Held item")]
    [Tooltip("Spare layer the held cup is drawn on, by its own overlay camera, so it never clips into the counter or machines.")]
    public int HeldLayer = 30;

    GameObject _cupGO;
    CupContents _cup;
    Transform _target;

    public bool HasCup { get { return _cup != null; } }
    public CupContents Cup { get { return _cup; } }
    public bool CupIsAway { get { return _target != null && _target != HoldAnchor; } }

    void Awake()
    {
        // The eye camera stops drawing the held layer; a child overlay camera draws
        // only that layer on top, after clearing depth, so the cup can't sink into walls.
        var eye = HoldAnchor != null ? HoldAnchor.GetComponentInParent<Camera>() : GetComponentInChildren<Camera>();
        if (eye == null) return;
        eye.cullingMask &= ~(1 << HeldLayer);

        var go = new GameObject("HeldItemCamera", typeof(Camera));
        go.transform.SetParent(eye.transform, false);
        var cam = go.GetComponent<Camera>();
        cam.fieldOfView = eye.fieldOfView;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 3f;
        cam.cullingMask = 1 << HeldLayer;
        cam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
        eye.GetUniversalAdditionalCameraData().cameraStack.Add(cam);
    }

    void SetTarget(Transform t)
    {
        _target = t;
        // In the hand it draws on top; set down at a station it is part of the world again.
        int layer = t == HoldAnchor ? HeldLayer : 0;
        if (_cupGO != null)
            foreach (var tr in _cupGO.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = layer;
    }

    public void TakeNewCup(GameObject prefab)
    {
        if (_cup != null) return;
        var src = prefab != null ? prefab : DefaultCupPrefab;
        if (src == null) { CafeMessage.Show("No cup prefab assigned"); return; }
        if (HoldAnchor == null) { CafeMessage.Show("No hold anchor on the player"); return; }

        _cupGO = Instantiate(src);
        _cupGO.name = "HeldCup";
        _cupGO.transform.SetPositionAndRotation(HoldAnchor.position, HoldAnchor.rotation);
        _cupGO.SetActive(true);

        foreach (var c in _cupGO.GetComponentsInChildren<Collider>()) c.enabled = false;

        _cup = _cupGO.GetComponent<CupContents>();
        if (_cup == null) _cup = _cupGO.AddComponent<CupContents>();
        _cup.Refresh();

        var wob = _cupGO.GetComponent<LiquidWobble>();
        if (wob == null) wob = _cupGO.AddComponent<LiquidWobble>();
        if (wob.Liquid == null) wob.Liquid = _cup.SloshRoot;

        SetTarget(HoldAnchor);
        CafeMessage.Show("Took a cup");
    }

    /// Set the cup down at a station for the duration of a step.
    public void LendCupTo(Transform anchor)
    {
        if (_cupGO == null || anchor == null) return;
        SetTarget(anchor);
    }

    public void ReturnCup()
    {
        if (_cupGO == null) return;
        SetTarget(HoldAnchor);
    }

    public void DestroyCup()
    {
        if (_cupGO != null) Destroy(_cupGO);
        _cupGO = null;
        _cup = null;
        _target = null;
    }

    void Update()
    {
        if (_cupGO == null || _target == null) return;
        float dt = Time.deltaTime;
        _cupGO.transform.position = Vector3.Lerp(_cupGO.transform.position, _target.position,
                                                 1f - Mathf.Exp(-FollowSpeed * dt));
        _cupGO.transform.rotation = Quaternion.Slerp(_cupGO.transform.rotation, _target.rotation,
                                                     1f - Mathf.Exp(-RotateSpeed * dt));
    }
}
