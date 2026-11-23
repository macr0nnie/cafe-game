using System.Collections.Generic;
using UnityEngine;

/// Plays the Blender-authored prop animations (doors, pumps, portafilters, the matcha
/// whisk...) on the imported cafe. Each clip moves one named object relative to its
/// rest pose, so clips on different objects never fight each other.
public class LookdevAnimator : MonoBehaviour
{
    public TextAsset Data;

    class Playback
    {
        public LookdevClip Clip;
        public Transform Target;
        public float Time;
        public bool Stopping;
        public float Settle;
    }

    const float SettleTime = 0.2f;

    struct RestPose { public Vector3 P; public Quaternion R; public Vector3 S; }

    Dictionary<string, LookdevClip> _clips;
    readonly Dictionary<string, Transform> _byName = new Dictionary<string, Transform>();
    readonly Dictionary<Transform, RestPose> _rest = new Dictionary<Transform, RestPose>();
    readonly Dictionary<Transform, Playback> _playing = new Dictionary<Transform, Playback>();
    readonly List<Transform> _done = new List<Transform>();

    void Awake() { Load(); }

    void Load()
    {
        if (_clips != null) return;
        _clips = Data != null ? LookdevData.Parse(Data.text).ById() : new Dictionary<string, LookdevClip>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (!_byName.ContainsKey(t.name)) _byName.Add(t.name, t);
    }

    public bool Has(string objectName, string clipName)
    {
        Load();
        return _clips.ContainsKey(LookdevClip.Key(objectName, clipName)) && _byName.ContainsKey(objectName);
    }

    /// Starts a clip on its object, replacing whatever that object was playing.
    public void Play(string objectName, string clipName)
    {
        Load();
        LookdevClip clip; Transform target;
        if (!_clips.TryGetValue(LookdevClip.Key(objectName, clipName), out clip)) return;
        if (!_byName.TryGetValue(objectName, out target)) return;
        if (!_rest.ContainsKey(target))
            _rest[target] = new RestPose { P = target.localPosition, R = target.localRotation, S = target.localScale };
        _playing[target] = new Playback { Clip = clip, Target = target };
    }

    /// Stops a looping clip: it finishes its current cycle and settles at rest.
    public void Stop(string objectName, string clipName)
    {
        Transform target;
        Playback pb;
        if (!_byName.TryGetValue(objectName, out target)) return;
        if (_playing.TryGetValue(target, out pb) && pb.Clip.clip == clipName) pb.Stopping = true;
    }

    void Update()
    {
        float dt = UnityEngine.Time.deltaTime;
        _done.Clear();
        foreach (var kv in _playing)
        {
            var pb = kv.Value;
            pb.Time += dt;
            var rest = _rest[pb.Target];
            var pose = LookdevPose.Sample(pb.Clip, pb.Time);
            bool finished = !pb.Clip.loop && pb.Time >= pb.Clip.length;
            if (pb.Stopping)
            {
                // A stopped loop eases back to rest instead of popping.
                pb.Settle += dt / SettleTime;
                pose = LookdevPose.Lerp(pose, LookdevPose.Rest, pb.Settle);
                finished = pb.Settle >= 1f;
            }
            pose.ApplyTo(pb.Target, rest.P, rest.R, rest.S);
            if (finished) _done.Add(kv.Key);
        }
        foreach (var t in _done) _playing.Remove(t);
    }
}
