using System.Collections.Generic;
using UnityEngine;

/// A cup set down on a counter. Aim at it and press E to pick it back up.
public class LooseCup : MonoBehaviour, IInteractable
{
    /// Every cup currently sitting on a surface, so the guide can point back to it.
    public static readonly List<LooseCup> All = new List<LooseCup>();

    BoxCollider _box;

    public float Progress { get { return 0f; } }
    public bool IsHold { get { return false; } }

    void OnEnable()
    {
        All.Add(this);
        // A trigger around the glass so it can be aimed at; the cup's own colliders stay off.
        _box = gameObject.AddComponent<BoxCollider>();
        _box.isTrigger = true;
        var b = new Bounds(transform.position, Vector3.zero);
        foreach (var r in GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        Vector3 s = transform.lossyScale;
        _box.center = transform.InverseTransformPoint(b.center);
        _box.size = new Vector3(b.size.x / Mathf.Max(1e-4f, s.x), b.size.y / Mathf.Max(1e-4f, s.y), b.size.z / Mathf.Max(1e-4f, s.z));
    }

    void OnDisable()
    {
        All.Remove(this);
        if (_box != null) Destroy(_box);
    }

    public string Prompt(PlayerHands hands)
    {
        if (hands != null && hands.HasCup) return "Hands full";
        var cup = GetComponent<CupContents>();
        return "Pick up cup" + (cup != null && !cup.IsEmpty ? " (" + cup.ShortList() + ")" : "");
    }

    public bool CanInteract(PlayerHands hands) { return hands != null && !hands.HasCup; }
    public void Begin(PlayerHands hands) { }
    public void Hold(PlayerHands hands, float t01) { }
    public void End(PlayerHands hands, float t01) { if (hands != null) hands.PickUp(this); }
}
