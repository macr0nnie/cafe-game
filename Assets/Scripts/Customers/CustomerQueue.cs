using System.Collections.Generic;
using UnityEngine;

/// The line at the till. Spots are anchors, front of the line first, each facing the counter.
/// Customers ask which spot is theirs each frame, so the line shuffles forward on its own.
public class CustomerQueue : MonoBehaviour
{
    public Transform[] Spots = new Transform[0];

    readonly List<Object> _line = new List<Object>();

    public int Count { get { return _line.Count; } }
    public bool HasRoom { get { return _line.Count < Spots.Length; } }

    public void Join(Object who)  { if (who != null && !_line.Contains(who)) _line.Add(who); }
    public void Leave(Object who) { _line.Remove(who); }
    public int PlaceOf(Object who) { return _line.IndexOf(who); }
    public bool IsFront(Object who) { return _line.Count > 0 && _line[0] == who; }

    /// The anchor this customer should stand at, or null when they are not in line.
    public Transform SpotFor(Object who)
    {
        int i = PlaceOf(who);
        if (i < 0 || Spots.Length == 0) return null;
        return Spots[Mathf.Min(i, Spots.Length - 1)];
    }
}
