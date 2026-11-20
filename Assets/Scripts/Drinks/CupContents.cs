using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// What is in the cup, plus the visual that shows it. The fill rises over time
/// rather than snapping, so a pour reads as a pour.
public class CupContents : MonoBehaviour
{
    public List<Ingredient> Added = new List<Ingredient>();

    [Header("Visuals")]
    public Transform LiquidVisual;
    public Renderer LiquidRenderer;
    public Transform PearlVisual;
    public Transform IceVisual;
    public Transform CreamVisual;

    [Header("Feel")]
    public float MaxFill = 1f;
    [Tooltip("Fill units per second. Lower looks like a slower pour.")]
    public float FillSpeed = 0.42f;
    public float ColourBlendSpeed = 3.2f;

    float _shownFill;
    Color _shownColour = new Color(1f, 1f, 1f, 1f);
    MaterialPropertyBlock _mpb;

    public float ShownFill { get { return _shownFill; } }
    public bool IsEmpty { get { return Added.Count == 0; } }
    public bool IsSettled { get { return Mathf.Abs(_shownFill - Fill) < 0.002f; } }

    void Awake()
    {
        _shownColour = CurrentColour();
        _shownFill = Fill;
        Refresh();
    }

    void Update()
    {
        float target = Fill;
        if (!Mathf.Approximately(_shownFill, target))
            _shownFill = Mathf.MoveTowards(_shownFill, target, FillSpeed * Time.deltaTime);

        _shownColour = Color.Lerp(_shownColour, CurrentColour(), Time.deltaTime * ColourBlendSpeed);
        Apply();
    }

    public float Fill
    {
        get
        {
            float f = 0f;
            foreach (var i in Added) f += Ingredients.Volume(i);
            return Mathf.Clamp01(f);
        }
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

    public void Add(Ingredient i)
    {
        Added.Add(i);
        Refresh();
    }

    public void Clear()
    {
        Added.Clear();
        _shownFill = 0f;
        Refresh();
    }

    public Color CurrentColour()
    {
        float tw = 0f; Vector3 acc = Vector3.zero;
        foreach (var s in Added)
        {
            if (s == Ingredient.Ice) continue;          // ice dilutes, it does not tint
            float w = Ingredients.Volume(s);
            Color c = Ingredients.Colour(s);
            acc += new Vector3(c.r, c.g, c.b) * w;
            tw += w;
        }
        if (tw <= 0f) return Color.white;
        acc /= tw;
        return new Color(acc.x, acc.y, acc.z);
    }

    /// Toggles the chunky bits. Fill and colour are handled per-frame.
    public void Refresh()
    {
        if (PearlVisual != null) PearlVisual.gameObject.SetActive(Added.Contains(Ingredient.BobaPearls));
        if (IceVisual   != null) IceVisual.gameObject.SetActive(Added.Contains(Ingredient.Ice));
        if (CreamVisual != null) CreamVisual.gameObject.SetActive(Added.Contains(Ingredient.WhippedCream));
        Apply();
    }

    void Apply()
    {
        if (LiquidVisual != null)
        {
            bool any = _shownFill > 0.002f;
            if (LiquidVisual.gameObject.activeSelf != any) LiquidVisual.gameObject.SetActive(any);
            if (any)
            {
                var s = LiquidVisual.localScale;
                LiquidVisual.localScale = new Vector3(s.x, Mathf.Max(0.01f, _shownFill * MaxFill), s.z);
            }
        }

        if (LiquidRenderer != null)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            LiquidRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", _shownColour);
            _mpb.SetColor("_ShadowTint", _shownColour * 0.76f);
            LiquidRenderer.SetPropertyBlock(_mpb);
        }
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
