using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// The open orders as little painted menu cards down the left of the screen.
/// Each ingredient gets a hand-drawn circle that is ticked once it is in the
/// cup you are holding, so you always know what is left to add.
/// Rebuilds only when the orders change (events); ticks update each frame.
public class OrderBoard : MonoBehaviour
{
    public PlayerHands Hands;

    static readonly Color[] Tints =
    {
        CafeUIKit.Blush, CafeUIKit.Mint, CafeUIKit.Lavender, CafeUIKit.Butter, CafeUIKit.Peach,
    };

    class Line { public Ingredient Ing; public int Nth; public Image Tick; public Text Label; }
    class Card { public RectTransform Root; public List<Line> Lines = new List<Line>(); public float Shown; }

    RectTransform _column;
    readonly List<Card> _cards = new List<Card>();
    bool _dirty = true;

    void Awake()
    {
        if (Hands == null) Hands = GetComponent<PlayerHands>();
        var canvas = CafeUIKit.MakeCanvas(transform, "OrderBoard_Canvas", 90);
        _column = CafeUIKit.Node(canvas.transform, "Orders");
        CafeUIKit.Place(_column, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(300f, 0f));
        var v = CafeUIKit.Stack(_column.gameObject, 0, 0, 14f, TextAnchor.UpperLeft);
        v.childControlHeight = true;
        _column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void OnEnable()
    {
        CafeEvents.OrdersChanged += MarkDirty;
        CafeEvents.MenuToggled += OnMenu;
    }

    void OnDisable()
    {
        CafeEvents.OrdersChanged -= MarkDirty;
        CafeEvents.MenuToggled -= OnMenu;
    }

    void MarkDirty() { _dirty = true; }
    void OnMenu(bool open) { if (_column != null) _column.gameObject.SetActive(!open); }

    void Rebuild()
    {
        _dirty = false;
        foreach (var c in _cards) Destroy(c.Root.gameObject);
        _cards.Clear();

        var om = OrderManager.Instance;
        if (om == null) return;
        for (int i = 0; i < om.Open.Count; i++)
        {
            var t = om.Open[i];
            if (t == null || t.Recipe == null) continue;
            _cards.Add(MakeCard(t.Recipe, Tints[i % Tints.Length]));
        }
    }

    Card MakeCard(Recipe r, Color tint)
    {
        var card = new Card();
        var img = CafeUIKit.Card(_column, "Order_" + r.Name, tint);
        img.raycastTarget = false;
        card.Root = img.rectTransform;
        CafeUIKit.Stack(img.gameObject, 26, 20, 2f, TextAnchor.UpperLeft);

        var title = CafeUIKit.Label(img.transform, r.Name, 28, CafeUIKit.Ink, TextAnchor.MiddleLeft, true);
        CafeUIKit.Size(title, -1f, 40f);
        CafeUIKit.Divider(img.transform, CafeUIKit.WithAlpha(CafeUIKit.Ink, 0.35f), 200f, 10f);

        var seen = new Dictionary<Ingredient, int>();
        foreach (var ing in r.Steps)
        {
            int nth; seen.TryGetValue(ing, out nth); seen[ing] = nth + 1;

            var row = CafeUIKit.Node(img.transform, "Line");
            CafeUIKit.Size(row, -1f, 30f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ring.transform.SetParent(row, false);
            ring.sprite = CafeUIKit.Ring; ring.color = CafeUIKit.Ink; ring.raycastTarget = false;
            CafeUIKit.Size(ring, 26f, 26f);
            var tick = new GameObject("Tick", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            tick.transform.SetParent(ring.transform, false);
            tick.sprite = CafeUIKit.Tick; tick.color = CafeUIKit.Ink; tick.raycastTarget = false;
            CafeUIKit.Place(tick.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(2f, 3f), new Vector2(24f, 24f));

            // Ingredient colour as a little blob, name beside it: never colour alone.
            var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            dot.transform.SetParent(row, false);
            dot.sprite = CafeUIKit.Blob; dot.color = Ingredients.Colour(ing); dot.raycastTarget = false;
            CafeUIKit.Size(dot, 18f, 18f);

            var label = CafeUIKit.Label(row, Ingredients.Label(ing), 22, CafeUIKit.Ink, TextAnchor.MiddleLeft);
            CafeUIKit.Size(label).flexibleWidth = 1f;

            card.Lines.Add(new Line { Ing = ing, Nth = nth + 1, Tick = tick, Label = label });
        }
        return card;
    }

    void Update()
    {
        if (_dirty) Rebuild();

        var cup = Hands != null && Hands.HasCup ? Hands.Cup : null;
        float dt = Time.unscaledDeltaTime;
        foreach (var c in _cards)
        {
            // Pops in softly when it arrives.
            c.Shown = Mathf.MoveTowards(c.Shown, 1f, dt * 4f);
            float e = 1f - (1f - c.Shown) * (1f - c.Shown);
            c.Root.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, e);

            foreach (var l in c.Lines)
            {
                bool done = cup != null && cup.CountOf(l.Ing) >= l.Nth;
                if (l.Tick.enabled != done) l.Tick.enabled = done;
                // Done lines fade back a little; the tick carries the meaning.
                l.Label.color = done ? CafeUIKit.WithAlpha(CafeUIKit.Ink, 0.55f) : CafeUIKit.Ink;
            }
        }
    }
}
