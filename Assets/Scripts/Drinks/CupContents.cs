using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// What is in the cup, plus the visual that shows it. Liquids stack as separate
/// layers inside a clear glass, so every pour is visible going in. The level
/// rises over time rather than snapping, so a pour reads as a pour.
public class CupContents : MonoBehaviour
{
    public List<Ingredient> Added = new List<Ingredient>();

    [Header("Visuals")]
    [Tooltip("Template liquid column. Cloned once per layer at runtime.")]
    public Transform LiquidVisual;
    public Renderer LiquidRenderer;
    public Transform PearlVisual;
    public Transform IceVisual;
    public Transform CreamVisual;

    [Header("Feel")]
    public float MaxFill = 1f;
    [Tooltip("Fill units per second. Lower looks like a slower pour.")]
    public float FillSpeed = 1.1f;
    [Tooltip("Layers widen a little towards the rim, following the glass taper.")]
    public float TaperWiden = 0.13f;

    // Poured amount per entry in Added, so a short pour looks short.
    readonly List<float> _amounts = new List<float>();
    readonly List<float> _quality = new List<float>();
    readonly List<Renderer> _layers = new List<Renderer>();

    bool _pouring;
    Ingredient _pourIng;
    float _pourAmt;

    Transform _pivot;
    Transform _line;
    Vector3 _layerScale = Vector3.one;
    float _shownFill;
    MaterialPropertyBlock _mpb;

    static readonly Color LineColour = new Color(0.78f, 0.94f, 0.88f, 1f);   // palette Accent #C7F0E0

    public bool IsEmpty { get { return Added.Count == 0; } }
    /// What the slosh spring should tilt: all the layers together.
    public Transform SloshRoot { get { return _pivot != null ? _pivot : LiquidVisual; } }

    void Awake()
    {
        EnsurePivot();
        _shownFill = TargetFill();
        Refresh();
    }

    void Update()
    {
        _shownFill = Mathf.MoveTowards(_shownFill, TargetFill(), FillSpeed * Time.deltaTime);
        Apply();
    }

    /// Nominal fill, used for "is there room". Visuals use the poured amounts.
    public float Fill
    {
        get
        {
            float f = 0f;
            foreach (var i in Added) f += Ingredients.Volume(i);
            return Mathf.Clamp01(f);
        }
    }

    /// Height of the committed liquid, in fill units. Where the next layer starts.
    public float LiquidLevel
    {
        get
        {
            float f = 0f;
            for (int i = 0; i < Added.Count; i++) if (IsLiquid(Added[i])) f += _amounts[i];
            return f;
        }
    }

    float TargetFill()
    {
        float f = LiquidLevel;
        if (_pouring && IsLiquid(_pourIng)) f += _pourAmt;
        return Mathf.Clamp01(f);
    }

    public static bool IsLiquid(Ingredient i)
    {
        return i != Ingredient.Ice && i != Ingredient.BobaPearls && i != Ingredient.WhippedCream;
    }

    public bool CanAdd(Ingredient i)
    {
        // Shots can be doubled; nothing else repeats.
        if (i != Ingredient.EspressoShot && Added.Contains(i)) return false;
        if (i == Ingredient.EspressoShot && CountOf(i) >= 2) return false;
        return Fill + Ingredients.Volume(i) <= 1.02f;
    }

    public int CountOf(Ingredient i)
    {
        int n = 0;
        foreach (var a in Added) if (a == i) n++;
        return n;
    }

    public void Add(Ingredient i) { Add(i, Ingredients.Volume(i)); }

    public void Add(Ingredient i, float amount)
    {
        Added.Add(i);
        _amounts.Add(Mathf.Max(0f, amount));
        Refresh();
    }

    // ---------------------------------------------------------------- live pour

    /// Show an ingredient going in before it is committed. Stations call this
    /// every frame of their minigame, then CommitPour or CancelPour.
    public void SetPour(Ingredient i, float amount)
    {
        _pouring = true;
        _pourIng = i;
        _pourAmt = Mathf.Max(0f, amount);
        Refresh();
    }

    public float PourAmount { get { return _pouring ? _pourAmt : 0f; } }

    public void CommitPour()
    {
        if (!_pouring) return;
        _pouring = false;
        Add(_pourIng, _pourAmt);
    }

    public void CancelPour()
    {
        if (!_pouring) return;
        _pouring = false;
        Refresh();
    }

    /// A fill mark on the glass, for pour-to-the-line stations. Level in fill units.
    public void ShowTargetLine(float level)
    {
        EnsurePivot();
        if (_pivot == null) return;
        if (_line == null)
        {
            var go = new GameObject("TargetLine", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = CafeMesh.Torus(0.0405f, 0.0022f, 1f, 32, 6, "TargetLine");
            var r = go.GetComponent<MeshRenderer>();
            if (LiquidRenderer != null) r.sharedMaterial = LiquidRenderer.sharedMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", LineColour);
            mpb.SetColor("_ShadowTint", LineColour);
            mpb.SetColor("_EmissionColor", LineColour * 0.55f);
            r.SetPropertyBlock(mpb);
            _line = go.transform;
            _line.localRotation = Quaternion.Euler(90f, 0f, 0f);    // the torus is built in XY
        }
        _line.localPosition = _pivot.localPosition + Vector3.up * Mathf.Clamp01(level) * MaxFill;
        _line.gameObject.SetActive(true);
    }

    public void HideTargetLine()
    {
        if (_line != null) _line.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- quality

    public void RecordQuality(float q) { _quality.Add(Mathf.Clamp01(q)); }

    /// How well the steps were done, 0..1. A cup with no skill steps counts as clean.
    public float Execution
    {
        get
        {
            if (_quality.Count == 0) return 1f;
            float s = 0f;
            foreach (var q in _quality) s += q;
            return s / _quality.Count;
        }
    }

    public void Clear()
    {
        Added.Clear();
        _amounts.Clear();
        _quality.Clear();
        _pouring = false;
        _shownFill = 0f;
        Refresh();
    }

    // ---------------------------------------------------------------- visuals

    void EnsurePivot()
    {
        if (_pivot != null || LiquidVisual == null) return;
        _pivot = new GameObject("LiquidLayers").transform;
        _pivot.SetParent(LiquidVisual.parent, false);
        _pivot.localPosition = LiquidVisual.localPosition;
        _pivot.localRotation = LiquidVisual.localRotation;
        _layerScale = LiquidVisual.localScale;
        LiquidVisual.SetParent(_pivot, false);
        LiquidVisual.localPosition = Vector3.zero;
        LiquidVisual.localRotation = Quaternion.identity;
        if (LiquidRenderer == null) LiquidRenderer = LiquidVisual.GetComponent<Renderer>();
        if (LiquidRenderer != null) _layers.Add(LiquidRenderer);
    }

    Renderer Layer(int i)
    {
        while (_layers.Count <= i)
        {
            var clone = Instantiate(LiquidVisual.gameObject, _pivot, false);
            clone.name = "Layer_" + _layers.Count;
            _layers.Add(clone.GetComponent<Renderer>());
        }
        return _layers[i];
    }

    /// Toggles the chunky bits. Layers are handled per-frame.
    public void Refresh()
    {
        Reveal(PearlVisual, ChunkFraction(Ingredient.BobaPearls));
        Reveal(IceVisual, ChunkFraction(Ingredient.Ice));
        if (CreamVisual != null) CreamVisual.gameObject.SetActive(ChunkFraction(Ingredient.WhippedCream) > 0f);
        Apply();
    }

    /// 1 when committed, partial while it is being scooped in, 0 when absent.
    float ChunkFraction(Ingredient i)
    {
        if (Added.Contains(i)) return 1f;
        if (_pouring && _pourIng == i) return Mathf.Clamp01(_pourAmt / Ingredients.Volume(i));
        return 0f;
    }

    static void Reveal(Transform group, float frac)
    {
        if (group == null) return;
        group.gameObject.SetActive(frac > 0f);
        int n = group.childCount;
        int show = Mathf.CeilToInt(frac * n - 0.001f);
        for (int c = 0; c < n; c++) group.GetChild(c).gameObject.SetActive(c < show);
    }

    void Apply()
    {
        if (_pivot == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        int used = 0;
        float y = 0f;
        int count = Added.Count + (_pouring ? 1 : 0);
        for (int k = 0; k < count && y < _shownFill; k++)
        {
            bool live = k == Added.Count;
            Ingredient ing = live ? _pourIng : Added[k];
            if (!IsLiquid(ing)) continue;

            float h = Mathf.Min(live ? _pourAmt : _amounts[k], _shownFill - y);
            if (h <= 0.0005f) continue;

            var r = Layer(used++);
            if (!r.gameObject.activeSelf) r.gameObject.SetActive(true);
            float widen = 1f + TaperWiden * y;
            var t = r.transform;
            t.localPosition = new Vector3(0f, y * MaxFill, 0f);
            t.localScale = new Vector3(_layerScale.x * widen, Mathf.Max(0.001f, h * MaxFill), _layerScale.z * widen);

            Color c = Ingredients.Colour(ing);
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_ShadowTint", c * 0.76f);
            r.SetPropertyBlock(_mpb);
            y += h;
        }

        for (int k = used; k < _layers.Count; k++)
            if (_layers[k].gameObject.activeSelf) _layers[k].gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- scoring

    /// 0 = wrong drink, 1 = exact. Set based, so order never blocks you.
    public float ScoreAgainst(Recipe r)
    {
        if (r == null) return 0f;
        var need = new List<Ingredient>(r.Steps);
        int hits = 0, extra = 0;
        foreach (var a in Added)
        {
            if (need.Remove(a)) hits++;
            else extra++;
        }
        int missing = need.Count;
        float total = r.Steps.Length;
        if (total <= 0f) return 0f;
        return Mathf.Clamp01((hits - 0.5f * extra - 0.75f * missing) / total);
    }

    public bool Matches(Recipe r) { return ScoreAgainst(r) >= 0.999f; }

    /// Barista technique, judged on the order things went in. Flavour, not a fail
    /// state: a correct drink built in a daft order still counts.
    public string TechniqueNote()
    {
        int ice = Added.IndexOf(Ingredient.Ice);
        int milk = FirstIndexOfAny(Ingredient.Milk, Ingredient.SteamedMilk, Ingredient.Foam);
        int syrup = FirstIndexOfAny(Ingredient.VanillaSyrup, Ingredient.CaramelSyrup,
                                    Ingredient.StrawberrySyrup, Ingredient.TaroSyrup,
                                    Ingredient.Chocolate, Ingredient.Matcha);
        int shot = Added.IndexOf(Ingredient.EspressoShot);

        if (syrup >= 0 && milk >= 0 && syrup > milk) return "syrup after the milk - it will sit on top";
        if (ice >= 0 && milk >= 0 && ice > milk)     return "ice last melts fast, pack it first next time";
        if (shot >= 0 && milk >= 0 && shot > milk)   return "shot poured over the milk - nice layering";
        if (ice >= 0 && ice == 0)                    return "ice first, textbook";
        if (syrup >= 0 && syrup == 0)                return "syrup in the bottom, good";
        return null;
    }

    int FirstIndexOfAny(params Ingredient[] any)
    {
        for (int i = 0; i < Added.Count; i++)
            foreach (var a in any) if (Added[i] == a) return i;
        return -1;
    }

    public string Describe()
    {
        if (Added.Count == 0) return "empty cup";
        var sb = new StringBuilder();
        for (int i = 0; i < Added.Count; i++)
        {
            if (i > 0) sb.Append(i == Added.Count - 1 ? " and " : ", ");
            sb.Append(Ingredients.Label(Added[i]));
        }
        return sb.ToString();
    }

    /// Short list for the HUD.
    public string ShortList()
    {
        if (Added.Count == 0) return "empty";
        var sb = new StringBuilder();
        for (int i = 0; i < Added.Count; i++)
        {
            if (i > 0) sb.Append(" + ");
            sb.Append(Ingredients.Label(Added[i]));
        }
        return sb.ToString();
    }
}
