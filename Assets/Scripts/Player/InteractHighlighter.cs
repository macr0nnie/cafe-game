using System.Collections.Generic;
using UnityEngine;

/// Draws soft outlines on things the player can use nearby. Three looks, told apart
/// by outline thickness and glow as well as colour, and backed by the HUD text:
///   usable  - thin, faint outline: you could use this now
///   next    - thicker outline and a warm glow: the drink guide's next step
///   aimed   - thickest outline and a bright glow: what E will use
/// Presentation only: reads PlayerInteractor and GuideTracker, never changes game state.
public class InteractHighlighter : MonoBehaviour
{
    public PlayerInteractor Interactor;
    public GuideTracker Guide;
    [Tooltip("Cafe/Highlight. Assigned by the scene builder so it is included in builds.")]
    public Shader HighlightShader;
    public float Radius = 5f;
    public float RescanSeconds = 0.25f;

    const string ChildName = "_Highlight";
    enum Tier { None, Usable, Next, Aimed }

    class Entry
    {
        public IInteractable It;
        public readonly List<GameObject> Shells = new List<GameObject>();
        public Tier Shown = Tier.None;
        public bool Near;
    }

    readonly Dictionary<MonoBehaviour, Entry> _entries = new Dictionary<MonoBehaviour, Entry>();
    readonly List<MonoBehaviour> _dead = new List<MonoBehaviour>();
    readonly Collider[] _hits = new Collider[256];
    Material[] _mats;
    float _nextScan;

    void Awake()
    {
        if (Interactor == null) Interactor = GetComponent<PlayerInteractor>();
        if (Guide == null) Guide = GetComponent<GuideTracker>();
        if (HighlightShader == null) HighlightShader = Shader.Find("Cafe/Highlight");
        if (HighlightShader == null) { enabled = false; Debug.LogWarning("Cafe/Highlight shader missing: no outlines"); return; }

        // Locked palette: LineDark outline, LampWarm and WarmWhite glow, Boba ink for the aimed rim.
        _mats = new Material[4];
        _mats[(int)Tier.Usable] = Make(CafeUIKit.Hex("#8E7F9A"), 0.55f, 0.0022f, CafeUIKit.Hex("#FFD6A5"), 0f, 0f);
        _mats[(int)Tier.Next]   = Make(CafeUIKit.Hex("#8E7F9A"), 0.90f, 0.0040f, CafeUIKit.Hex("#FFD6A5"), 0.10f, 0.45f);
        _mats[(int)Tier.Aimed]  = Make(CafeUIKit.Hex("#4A3A3F"), 0.85f, 0.0058f, CafeUIKit.Hex("#FFFAF2"), 0.18f, 0.60f);
    }

    Material Make(Color outline, float alpha, float width, Color glow, float fill, float rim)
    {
        var m = new Material(HighlightShader) { name = "Highlight", hideFlags = HideFlags.DontSave };
        outline.a = alpha;
        m.SetColor("_OutlineColor", outline);
        m.SetFloat("_Width", width);
        m.SetColor("_GlowColor", glow);
        m.SetFloat("_Fill", fill);
        m.SetFloat("_Rim", rim);
        return m;
    }

    void OnEnable()  { CafeEvents.CupMoved += Rescan; }
    void OnDisable() { CafeEvents.CupMoved -= Rescan; }

    void Update()
    {
        if (Time.unscaledTime >= _nextScan) Rescan();

        var hands = Interactor != null ? Interactor.Hands : null;
        var focus = Interactor != null ? Interactor.Focus : null;

        _dead.Clear();
        foreach (var kv in _entries)
        {
            if (kv.Key == null) { _dead.Add(kv.Key); continue; }
            var e = kv.Value;
            Tier want = Tier.None;
            if (e.Near && !CafeGame.MenuOpen)
            {
                if (focus == e.It) want = Tier.Aimed;
                else if (Guide != null && Guide.IsNext(e.It)) want = Tier.Next;
                else if (e.It.CanInteract(hands)) want = Tier.Usable;
            }
            if (want != e.Shown) Show(e, want);
        }
        foreach (var k in _dead) { Clear(_entries[k]); _entries.Remove(k); }
    }

    void Rescan()
    {
        _nextScan = Time.unscaledTime + RescanSeconds;
        foreach (var e in _entries.Values) e.Near = false;

        int n = Physics.OverlapSphereNonAlloc(transform.position, Radius, _hits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var it = _hits[i].GetComponentInParent<IInteractable>();
            var mb = it as MonoBehaviour;
            if (mb == null) continue;
            Entry e;
            if (!_entries.TryGetValue(mb, out e)) _entries[mb] = e = Build(mb, it);
            e.Near = true;
        }
    }

    /// One shell per mesh this interactable owns (not meshes owned by an interactable inside it).
    Entry Build(MonoBehaviour mb, IInteractable it)
    {
        var e = new Entry { It = it };
        foreach (var r in mb.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.name == ChildName || r.GetComponentInParent<IInteractable>() != it) continue;
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var go = new GameObject(ChildName);
            go.layer = r.gameObject.layer;
            go.transform.SetParent(r.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var sr = go.AddComponent<MeshRenderer>();
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            sr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            sr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            go.SetActive(false);
            e.Shells.Add(go);
        }
        return e;
    }

    void Show(Entry e, Tier t)
    {
        e.Shown = t;
        foreach (var go in e.Shells)
        {
            if (go == null) continue;
            if (t != Tier.None)
            {
                var r = go.GetComponent<MeshRenderer>();
                int subs = Mathf.Max(1, go.GetComponent<MeshFilter>().sharedMesh.subMeshCount);
                if (r.sharedMaterials.Length != subs || r.sharedMaterial != _mats[(int)t])
                {
                    var arr = new Material[subs];
                    for (int i = 0; i < subs; i++) arr[i] = _mats[(int)t];
                    r.sharedMaterials = arr;
                }
            }
            go.SetActive(t != Tier.None);
        }
    }

    static void Clear(Entry e)
    {
        foreach (var go in e.Shells) if (go != null) Destroy(go);
    }

    void OnDestroy()
    {
        foreach (var e in _entries.Values) Clear(e);
        if (_mats != null) foreach (var m in _mats) if (m != null) Destroy(m);
    }
}
