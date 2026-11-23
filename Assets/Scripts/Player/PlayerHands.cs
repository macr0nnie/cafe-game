using UnityEngine;

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

    GameObject _cupGO;
    CupContents _cup;
    Transform _target;

    public bool HasCup { get { return _cup != null; } }
    public CupContents Cup { get { return _cup; } }
    public bool CupIsAway { get { return _target != null && _target != HoldAnchor; } }

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

        _target = HoldAnchor;
        CafeMessage.Show("Took a cup");
    }

    /// Set the cup down at a station for the duration of a step.
    public void LendCupTo(Transform anchor)
    {
        if (_cupGO == null || anchor == null) return;
        _target = anchor;
    }

    public void ReturnCup()
    {
        if (_cupGO == null) return;
        _target = HoldAnchor;
    }

    /// Let go of the cup without destroying it, e.g. setting it down for a customer.
    public GameObject ReleaseCup()
    {
        var go = _cupGO;
        _cupGO = null;
        _cup = null;
        _target = null;
        return go;
    }

    /// Put the cup down on a surface; it stays there as a LooseCup until picked up.
    public void SetDown(Vector3 point, Quaternion rotation)
    {
        if (_cupGO == null || CupIsAway) return;
        var go = ReleaseCup();
        go.transform.SetPositionAndRotation(point, rotation);
        if (go.GetComponent<LooseCup>() == null) go.AddComponent<LooseCup>();
        CafeEvents.RaiseCupMoved();
    }

    /// Take a set-down cup back into the hand.
    public void PickUp(LooseCup loose)
    {
        if (_cup != null || loose == null) return;
        _cupGO = loose.gameObject;
        _cup = _cupGO.GetComponent<CupContents>();
        Destroy(loose);
        foreach (var c in _cupGO.GetComponentsInChildren<Collider>()) c.enabled = false;
        _target = HoldAnchor;
        CafeEvents.RaiseCupMoved();
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
