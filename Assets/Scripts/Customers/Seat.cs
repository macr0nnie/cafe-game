using System.Collections.Generic;
using UnityEngine;

/// A place a customer can sit. Put it on a chair or bench spot with +Z facing the way a
/// seated customer looks. Seats register themselves, so a layout change only moves seats.
public class Seat : MonoBehaviour
{
    public static readonly List<Seat> All = new List<Seat>();

    [Tooltip("Where a customer stands before sitting, in this seat's local space (clear of the chair).")]
    public Vector3 ApproachOffset = new Vector3(0f, 0f, -0.6f);
    [Tooltip("How far a seated customer's body drops, in metres. Bench and chair heights differ.")]
    public float SitDrop = 0.42f;

    public Object Occupant { get; private set; }
    public bool IsFree { get { return Occupant == null; } }
    public Vector3 ApproachPoint { get { return transform.TransformPoint(ApproachOffset); } }

    void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); Occupant = null; }

    public bool Claim(Object who)
    {
        if (Occupant != null || who == null) return false;
        Occupant = who;
        return true;
    }

    public void Release(Object who)
    {
        if (Occupant == who) Occupant = null;
    }

    /// The roll-th free seat (wrapping), or null when every seat is taken.
    /// Random rolls spread customers around the room instead of piling up by the counter.
    public static Seat PickFree(IList<Seat> seats, int roll)
    {
        int free = 0;
        foreach (var s in seats) if (s != null && s.IsFree) free++;
        if (free == 0) return null;
        int want = ((roll % free) + free) % free;
        foreach (var s in seats)
        {
            if (s == null || !s.IsFree) continue;
            if (want-- == 0) return s;
        }
        return null;
    }
}
