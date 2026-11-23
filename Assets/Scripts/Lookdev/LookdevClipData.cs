using System;
using System.Collections.Generic;
using UnityEngine;

/// Animation data exported from the Blender lookdev file (tools/export_for_unity.py).
/// Each clip moves one object by a delta from its rest pose, already in Unity axes.
[Serializable]
public class LookdevKey
{
    public float t;
    public float[] p;   // delta position, parent space
    public float[] r;   // delta rotation quaternion x, y, z, w (applied before the rest rotation)
    public float[] s;   // scale multiplier
}

[Serializable]
public class LookdevClip
{
    public string clip;
    public string @object;
    public bool loop;
    public float length;
    public LookdevKey[] keys;

    public string Id { get { return Key(@object, clip); } }
    public static string Key(string objectName, string clipName) { return objectName + "|" + clipName; }
}

[Serializable]
public class LookdevSlot
{
    public string id;
    public float[] centre, stand, cup_anchor;
    public float[] footprint_x, footprint_y;
    public bool under_counter;
    public string tiers, note;
}

/// A customer point from Blender (door, queue spot, pickup, seat): where it is and
/// which way someone standing or sitting there faces. Already in Unity axes.
[Serializable]
public class LookdevPoint
{
    public string name;
    public float[] pos;
    public float[] facing;
    public string kind;     // chair, stool, sofa, or empty for standing spots

    public Vector3 Position { get { return new Vector3(pos[0], pos[1], pos[2]); } }
    public Quaternion Rotation
    {
        get
        {
            var f = new Vector3(facing[0], 0f, facing[2]);
            return f.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(f.normalized, Vector3.up) : Quaternion.identity;
        }
    }
}

[Serializable]
public class LookdevData
{
    public float fps;
    public LookdevClip[] clips;
    public LookdevSlot[] slots;
    public LookdevPoint[] points;

    public static LookdevData Parse(string json)
    {
        var d = JsonUtility.FromJson<LookdevData>(json);
        if (d.clips == null) d.clips = new LookdevClip[0];
        if (d.slots == null) d.slots = new LookdevSlot[0];
        if (d.points == null) d.points = new LookdevPoint[0];
        return d;
    }

    public Dictionary<string, LookdevClip> ById()
    {
        var map = new Dictionary<string, LookdevClip>();
        foreach (var c in clips) map[c.Id] = c;
        return map;
    }
}

/// A sampled pose: the delta to apply on top of an object's rest pose.
public struct LookdevPose
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public static readonly LookdevPose Rest = new LookdevPose
        { Position = Vector3.zero, Rotation = Quaternion.identity, Scale = Vector3.one };

    /// Pose at time t (seconds). Loops wrap; one-shots hold the last key.
    public static LookdevPose Sample(LookdevClip c, float t)
    {
        var k = c.keys;
        if (k == null || k.Length == 0) return Rest;
        if (c.loop && c.length > 0f) t = Mathf.Repeat(t, c.length);
        if (t <= k[0].t) return FromKey(k[0]);
        for (int i = 1; i < k.Length; i++)
        {
            if (t > k[i].t) continue;
            float span = Mathf.Max(1e-5f, k[i].t - k[i - 1].t);
            return Lerp(FromKey(k[i - 1]), FromKey(k[i]), (t - k[i - 1].t) / span);
        }
        return FromKey(k[k.Length - 1]);
    }

    static LookdevPose FromKey(LookdevKey k)
    {
        return new LookdevPose
        {
            Position = V(k.p, 0f),
            Rotation = k.r != null && k.r.Length == 4 ? new Quaternion(k.r[0], k.r[1], k.r[2], k.r[3]) : Quaternion.identity,
            Scale = V(k.s, 1f),
        };
    }

    static Vector3 V(float[] a, float fallback)
    {
        return a != null && a.Length == 3 ? new Vector3(a[0], a[1], a[2]) : new Vector3(fallback, fallback, fallback);
    }

    public static LookdevPose Lerp(LookdevPose a, LookdevPose b, float u)
    {
        u = Mathf.Clamp01(u);
        return new LookdevPose
        {
            Position = Vector3.Lerp(a.Position, b.Position, u),
            Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, u),
            Scale = Vector3.Lerp(a.Scale, b.Scale, u),
        };
    }

    /// Blender applies delta rotation after the object's own rotation (parent space).
    public void ApplyTo(Transform tr, Vector3 restPos, Quaternion restRot, Vector3 restScale)
    {
        tr.localPosition = restPos + Position;
        tr.localRotation = Rotation * restRot;
        tr.localScale = Vector3.Scale(restScale, Scale);
    }
}
