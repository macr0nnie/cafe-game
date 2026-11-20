using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// The cafe's UI style, made in code: watercolour paper washes with soft
/// pigment-pooled edges, brush-stroke dividers, hand-drawn ticks and blobs,
/// and a handwritten heading font. Every screen builds from these parts, so
/// the look stays the same everywhere.
public static class CafeUIKit
{
    // ---------------------------------------------------------------- palette
    // All from the cafe palette. Ink is the darkest palette tone, for contrast.
    public static readonly Color Ink      = Hex("#4A3A3F");
    public static readonly Color InkSoft  = Hex("#8E7F9A");
    public static readonly Color Paper    = Hex("#FFF5E6");
    public static readonly Color Pink     = Hex("#F6C6D0");
    public static readonly Color Blush    = Hex("#FBE0EA");
    public static readonly Color Mint     = Hex("#C7F0E0");
    public static readonly Color Sage     = Hex("#B5E3C0");
    public static readonly Color Lavender = Hex("#E9D5F0");
    public static readonly Color Peach    = Hex("#FFD6A5");
    public static readonly Color Butter   = Hex("#F3E0B8");

    public static Color Hex(string h)
    {
        Color c;
        ColorUtility.TryParseHtmlString(h, out c);
        return c;
    }

    public static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    // ---------------------------------------------------------------- fonts

    static Font _body, _hand;

    public static Font Body
    {
        get
        {
            if (_body != null) return _body;
            _body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _body;
        }
    }

    /// Handwritten font for headings. Uses one installed with Windows; falls back to Body.
    // shortcut: OS fonts only exist on the player's machine; ship a bundled font asset before release.
    public static Font Hand
    {
        get
        {
            if (_hand != null) return _hand;
            var installed = Font.GetOSInstalledFontNames();
            foreach (var want in new[] { "Segoe Print", "Ink Free", "Segoe Script", "Comic Sans MS" })
                if (System.Array.IndexOf(installed, want) >= 0) { _hand = Font.CreateDynamicFontFromOSFont(want, 48); break; }
            if (_hand == null) _hand = Body;
            return _hand;
        }
    }

    // ---------------------------------------------------------------- sprites

    static Sprite _wash, _brush, _blob, _ring, _tick;

    /// A watercolour wash: wobbly edge, pigment pooled at the rim, paper grain.
    /// Greyscale, so Image.color tints it to any pastel. 9-sliced.
    public static Sprite Wash { get { return _wash != null ? _wash : (_wash = MakeWash(160, 52, 7.3f)); } }
    /// Tapered horizontal brush stroke, for underlines and dividers.
    public static Sprite Brush { get { return _brush != null ? _brush : (_brush = MakeBrush(256, 40, 3.1f)); } }
    /// Round watercolour blob, for colour swatches and the check box fill.
    public static Sprite Blob { get { return _blob != null ? _blob : (_blob = MakeBlob(64, 1.7f)); } }
    /// Hand-drawn circle outline, the empty check box.
    public static Sprite Ring { get { return _ring != null ? _ring : (_ring = MakeRing(64, 5.9f)); } }
    /// Hand-drawn tick.
    public static Sprite Tick { get { return _tick != null ? _tick : (_tick = MakeTick(64)); } }

    static float Smooth(float e0, float e1, float x)
    {
        float t = Mathf.Clamp01((x - e0) / (e1 - e0));
        return t * t * (3f - 2f * t);
    }

    static Sprite MakeWash(int n, int border, float seed)
    {
        var tex = NewTex(n, n);
        float m = 0.07f, r = 0.16f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
            // Signed distance to a rounded box, in uv.
            float qx = Mathf.Abs(u - 0.5f) - (0.5f - m - r);
            float qy = Mathf.Abs(v - 0.5f) - (0.5f - m - r);
            float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                      + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            // Wobbly, bleeding edge.
            d += (Mathf.PerlinNoise(u * 7f + seed, v * 7f) - 0.5f) * 0.045f
               + (Mathf.PerlinNoise(u * 23f, v * 23f + seed) - 0.5f) * 0.012f;

            float alpha = Smooth(0.012f, -0.008f, d);
            float val = 0.97f
                      + (Mathf.PerlinNoise(u * 48f + seed, v * 48f) - 0.5f) * 0.07f     // paper grain
                      + (Mathf.PerlinNoise(u * 2.6f, v * 2.6f + seed) - 0.5f) * 0.06f;  // uneven wash
            // Pigment pools where the wash dried: a darker rim just inside the edge.
            float rim = Smooth(-0.045f, 0f, d);
            val -= rim * 0.13f;
            tex.SetPixel(x, y, new Color(val, val, val, alpha));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    static Sprite MakeBrush(int w, int h, float seed)
    {
        var tex = NewTex(w, h);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
            float taper = Smooth(0f, 0.12f, u) * Smooth(1f, 0.82f, u);
            float centre = 0.5f + (Mathf.PerlinNoise(u * 3f + seed, 0.3f) - 0.5f) * 0.25f;
            float half = 0.32f * taper + 0.02f;
            float band = Smooth(half, half - 0.08f, Mathf.Abs(v - centre));
            float streak = 0.75f + 0.25f * Mathf.PerlinNoise(u * 4f, v * 18f + seed);   // dry-brush streaks
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, band * streak));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeBlob(int n, float seed)
    {
        var tex = NewTex(n, n);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n - 0.5f, v = (y + 0.5f) / n - 0.5f;
            float a = Mathf.Atan2(v, u);
            float rad = 0.40f + (Mathf.PerlinNoise(Mathf.Cos(a) * 1.5f + seed, Mathf.Sin(a) * 1.5f) - 0.5f) * 0.10f;
            float d = Mathf.Sqrt(u * u + v * v) - rad;
            float val = 1f - Smooth(-0.08f, 0f, d) * 0.15f;
            tex.SetPixel(x, y, new Color(val, val, val, Smooth(0.02f, -0.01f, d)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeRing(int n, float seed)
    {
        var tex = NewTex(n, n);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n - 0.5f, v = (y + 0.5f) / n - 0.5f;
            float a = Mathf.Atan2(v, u);
            float rad = 0.38f + (Mathf.PerlinNoise(Mathf.Cos(a) + seed, Mathf.Sin(a)) - 0.5f) * 0.06f;
            float thick = 0.035f + 0.02f * Mathf.PerlinNoise(a * 0.8f + seed, 0.5f);  // pen pressure
            float d = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - rad) - thick;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Smooth(0.015f, -0.01f, d)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeTick(int n)
    {
        var tex = NewTex(n, n);
        Vector2 a = new Vector2(0.18f, 0.52f), b = new Vector2(0.42f, 0.24f), c = new Vector2(0.86f, 0.84f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
            float d = Mathf.Min(SegDist(p, a, b) - 0.07f, SegDist(p, b, c) - 0.055f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Smooth(0.015f, -0.01f, d)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    // ---------------------------------------------------------------- building blocks

    public static Canvas MakeCanvas(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referenceResolution = CafeSettings.ReferenceResolution;
        CafeSettings.Changed += () => { if (scaler != null) scaler.referenceResolution = CafeSettings.ReferenceResolution; };
        return canvas;
    }

    public static RectTransform Rect(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        return rt != null ? rt : go.AddComponent<RectTransform>();
    }

    /// A plain RectTransform holder.
    public static RectTransform Node(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// Watercolour card. Add a layout group to stack content inside it.
    public static Image Card(Transform parent, string name, Color tint)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = Wash;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.4f;
        img.color = tint;
        img.raycastTarget = true;   // cards swallow clicks so they never fall through
        return img;
    }

    public static VerticalLayoutGroup Stack(GameObject go, int padH, int padV, float spacing,
                                            TextAnchor align = TextAnchor.UpperCenter)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(padH, padH, padV, padV);
        v.spacing = spacing;
        v.childAlignment = align;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        return v;
    }

    public static Text Label(Transform parent, string text, int size, Color colour,
                             TextAnchor align = TextAnchor.MiddleCenter, bool hand = false)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = hand ? Hand : Body;
        t.fontSize = size;
        t.color = colour;
        t.alignment = align;
        t.text = text;
        t.raycastTarget = false;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static Image Divider(Transform parent, Color tint, float width = 300f, float height = 16f)
    {
        var holder = Node(parent, "Divider");
        var le = holder.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height; le.minHeight = height;
        var img = new GameObject("Stroke", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(holder, false);
        img.sprite = Brush;
        img.color = tint;
        img.raycastTarget = false;
        Place(img.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
        return img;
    }

    public static LayoutElement Size(Component c, float w = -1f, float h = -1f)
    {
        var le = c.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        if (w >= 0f) { le.preferredWidth = w; le.minWidth = w; }
        if (h >= 0f) { le.preferredHeight = h; le.minHeight = h; }
        return le;
    }

    /// A painted paper tag that lifts on hover and squishes on press.
    public static Button Button(Transform parent, string text, Color tint, UnityAction onClick,
                                float height = 64f, int fontSize = 30)
    {
        var card = Card(parent, "Button_" + text, tint);
        Size(card, -1f, height);
        var btn = card.gameObject.AddComponent<Button>();
        btn.targetGraphic = card;
        // Colour shifts stay subtle; the lift and the underline carry the state.
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
        cb.selectedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
        cb.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        var nav = btn.navigation; nav.mode = Navigation.Mode.Vertical; btn.navigation = nav;
        if (onClick != null) btn.onClick.AddListener(onClick);

        var label = Label(card.transform, text, fontSize, Ink, TextAnchor.MiddleCenter, true);
        Stretch(label.rectTransform, 6f);

        // Underline stroke that draws in on hover or keyboard focus: not colour-only.
        var line = new GameObject("Underline", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        line.transform.SetParent(card.transform, false);
        line.sprite = Brush;
        line.color = WithAlpha(Ink, 0.55f);
        line.raycastTarget = false;
        line.type = Image.Type.Filled;
        line.fillMethod = Image.FillMethod.Horizontal;
        line.fillAmount = 0f;
        var lrt = line.rectTransform;
        lrt.anchorMin = new Vector2(0.22f, 0f); lrt.anchorMax = new Vector2(0.78f, 0f);
        lrt.pivot = new Vector2(0.5f, 0f);
        lrt.anchoredPosition = new Vector2(0f, 8f);
        lrt.sizeDelta = new Vector2(0f, 10f);

        var feel = card.gameObject.AddComponent<CafeButtonFeel>();
        feel.Underline = line;
        return btn;
    }

    /// Labelled slider on one line: name on the left, painted track, value on the right.
    public static Slider Slider(Transform parent, string name, float min, float max, float value,
                                System.Func<float, string> format, UnityAction<float> onChange)
    {
        var row = Node(parent, "Row_" + name);
        Size(row, -1f, 56f);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 18f; h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = true;

        var nameT = Label(row, name, 26, Ink, TextAnchor.MiddleLeft);
        Size(nameT, 230f);

        var trackHolder = Node(row, "Slider");
        Size(trackHolder, 300f).flexibleWidth = 1f;
        var slider = trackHolder.gameObject.AddComponent<Slider>();

        var track = new GameObject("Track", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        track.transform.SetParent(trackHolder, false);
        track.sprite = Brush; track.color = WithAlpha(InkSoft, 0.45f);
        track.rectTransform.anchorMin = new Vector2(0f, 0.5f); track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        track.rectTransform.sizeDelta = new Vector2(0f, 18f);

        var fillArea = Node(trackHolder, "FillArea");
        fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(-20f, 18f);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(fillArea, false);
        fill.sprite = Brush; fill.color = Pink;
        fill.rectTransform.sizeDelta = Vector2.zero;

        var handleArea = Node(trackHolder, "HandleArea");
        Stretch(handleArea); handleArea.offsetMin = new Vector2(14f, 0f); handleArea.offsetMax = new Vector2(-14f, 0f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        handle.transform.SetParent(handleArea, false);
        handle.sprite = Blob; handle.color = Hex("#F0A0B0");
        handle.rectTransform.sizeDelta = new Vector2(36f, 36f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = min; slider.maxValue = max; slider.value = value;
        var nav = slider.navigation; nav.mode = Navigation.Mode.Vertical; slider.navigation = nav;

        var valT = Label(row, format(value), 26, Ink, TextAnchor.MiddleRight);
        Size(valT, 90f);
        slider.onValueChanged.AddListener(v => { valT.text = format(v); if (onChange != null) onChange(v); });
        return slider;
    }

    /// A hand-drawn circle that gets a tick when on, with the state also written out.
    public static Toggle Toggle(Transform parent, string name, bool value, UnityAction<bool> onChange)
    {
        var row = Node(parent, "Row_" + name);
        Size(row, -1f, 56f);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 18f; h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = true;

        var nameT = Label(row, name, 26, Ink, TextAnchor.MiddleLeft);
        Size(nameT, 230f).flexibleWidth = 1f;

        var boxHolder = Node(row, "Box");
        Size(boxHolder, 48f, 48f);
        var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        ring.transform.SetParent(boxHolder, false);
        ring.sprite = Ring; ring.color = Ink;
        Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
        var tick = new GameObject("Tick", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        tick.transform.SetParent(ring.transform, false);
        tick.sprite = Tick; tick.color = Ink;
        Place(tick.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(3f, 4f), new Vector2(40f, 40f));

        var stateT = Label(row, value ? "On" : "Off", 26, Ink, TextAnchor.MiddleRight);
        Size(stateT, 90f);

        var toggle = boxHolder.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = ring;
        toggle.graphic = tick;
        toggle.isOn = value;
        var nav = toggle.navigation; nav.mode = Navigation.Mode.Vertical; toggle.navigation = nav;
        toggle.onValueChanged.AddListener(v => { stateT.text = v ? "On" : "Off"; if (onChange != null) onChange(v); });
        return toggle;
    }

    /// Small coloured blob plus its name, so colour is never the only cue.
    public static RectTransform Swatch(Transform parent, Color colour, string text, int size = 24)
    {
        var row = Node(parent, "Swatch");
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f; h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        Size(row, -1f, size + 6f);

        var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        dot.transform.SetParent(row, false);
        dot.sprite = Blob; dot.color = colour; dot.raycastTarget = false;
        Size(dot, size, size);
        var t = Label(row, text, size, Ink, TextAnchor.MiddleLeft);
        Size(t).flexibleWidth = 1f;
        return row;
    }
}
