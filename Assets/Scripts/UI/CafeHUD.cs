using UnityEngine;
using UnityEngine.UI;

/// Builds the whole HUD at runtime: pink heart crosshair, interaction prompt,
/// radial progress on the crosshair, and the toast line.
public class CafeHUD : MonoBehaviour
{
    [Header("Crosshair")]
    public Color HeartIdle   = new Color(1f, 0.70f, 0.80f, 0.62f);
    public Color HeartActive = new Color(1f, 0.47f, 0.64f, 0.96f);
    public float IdleSize    = 26f;
    public float ActiveSize  = 34f;

    public PlayerInteractor Interactor;

    Image _heart;
    Image _ring;
    Text  _prompt;
    Text  _toast;
    Text  _cupLine;
    Text  _orderLine;
    RectTransform _heartRT;
    float _size;
    Color _col;

    void Awake()
    {
        BuildCanvas();
        _size = IdleSize;
        _col = HeartIdle;
    }

    void BuildCanvas()
    {
        var canvasGO = new GameObject("HUD_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Radial progress sits just behind the heart.
        var ringGO = new GameObject("Progress", typeof(Image));
        ringGO.transform.SetParent(canvasGO.transform, false);
        _ring = ringGO.GetComponent<Image>();
        _ring.sprite = RingSprite(96, 0.78f);
        _ring.type = Image.Type.Filled;
        _ring.fillMethod = Image.FillMethod.Radial360;
        _ring.fillOrigin = 2;                     // top
        _ring.fillClockwise = true;
        _ring.fillAmount = 0f;
        _ring.color = new Color(1f, 0.55f, 0.72f, 0.95f);
        _ring.raycastTarget = false;
        var ringRT = ringGO.GetComponent<RectTransform>();
        ringRT.anchorMin = ringRT.anchorMax = new Vector2(0.5f, 0.5f);
        ringRT.anchoredPosition = Vector2.zero;
        ringRT.sizeDelta = new Vector2(64f, 64f);

        // The heart itself.
        var heartGO = new GameObject("HeartCrosshair", typeof(Image));
        heartGO.transform.SetParent(canvasGO.transform, false);
        _heart = heartGO.GetComponent<Image>();
        _heart.sprite = HeartSprite(96);
        _heart.color = HeartIdle;
        _heart.raycastTarget = false;
        _heartRT = heartGO.GetComponent<RectTransform>();
        _heartRT.anchorMin = _heartRT.anchorMax = new Vector2(0.5f, 0.5f);
        _heartRT.anchoredPosition = Vector2.zero;
        _heartRT.sizeDelta = new Vector2(IdleSize, IdleSize);

        _prompt = MakeText(canvasGO.transform, "Prompt", 30, new Vector2(0.5f, 0.5f),
                           new Vector2(0f, -58f), new Vector2(900f, 44f), TextAnchor.MiddleCenter);
        _cupLine = MakeText(canvasGO.transform, "CupLine", 24, new Vector2(0f, 0f),
                            new Vector2(330f, 54f), new Vector2(620f, 34f), TextAnchor.LowerLeft);
        _cupLine.color = new Color(1f, 0.86f, 0.90f, 0.95f);
        _orderLine = MakeText(canvasGO.transform, "OrderLine", 24, new Vector2(1f, 1f),
                              new Vector2(-180f, -44f), new Vector2(320f, 34f), TextAnchor.UpperRight);
        _orderLine.color = new Color(1f, 0.93f, 0.86f, 0.9f);
        _toast  = MakeText(canvasGO.transform, "Toast", 26, new Vector2(0.5f, 0f),
                           new Vector2(0f, 96f), new Vector2(1100f, 40f), TextAnchor.MiddleCenter);
        _toast.color = new Color(1f, 0.93f, 0.88f, 1f);
    }

    static Text MakeText(Transform parent, string name, int size, Vector2 anchor,
                         Vector2 pos, Vector2 sizeDelta, TextAnchor align)
    {
        var go = new GameObject(name, typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = BuiltinFont();
        t.fontSize = size;
        t.alignment = align;
        t.color = new Color(1f, 1f, 1f, 0.95f);
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        var o = go.GetComponent<Outline>();
        o.effectColor = new Color(0.25f, 0.18f, 0.24f, 0.75f);
        o.effectDistance = new Vector2(1.6f, -1.6f);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    static Font _font;
    static Font BuiltinFont()
    {
        if (_font != null) return _font;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_font == null)
        {
            var names = Font.GetOSInstalledFontNames();
            if (names != null && names.Length > 0) _font = Font.CreateDynamicFontFromOSFont(names[0], 32);
        }
        return _font;
    }

    // ---------------------------------------------------------------- sprites

    /// Rasterises the classic heart curve (x^2+y^2-1)^3 - x^2 y^3 = 0.
    public static Sprite HeartSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        const float Extent = 1.35f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // 3x3 supersample for a clean edge
            float cov = 0f;
            for (int sy = 0; sy < 3; sy++)
            for (int sx = 0; sx < 3; sx++)
            {
                float u = ((x + (sx + 0.5f) / 3f) / size * 2f - 1f) * Extent;
                float v = (1f - (y + (sy + 0.5f) / 3f) / size * 2f) * Extent;
                v += 0.18f;                              // sit it slightly low in the box
                float q = u * u + v * v - 1f;
                if (q * q * q - u * u * v * v * v <= 0f) cov += 1f / 9f;
            }
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, cov));
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// A thin ring, used as the radial progress indicator.
    public static Sprite RingSprite(int size, float innerFrac)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float outer = 0.5f, inner = 0.5f * innerFrac;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (x + 0.5f) / size - 0.5f;
            float v = (y + 0.5f) / size - 0.5f;
            float d = Mathf.Sqrt(u * u + v * v);
            float aa = 1.2f / size;
            float a = Mathf.Clamp01((outer - d) / aa) * Mathf.Clamp01((d - inner) / aa);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ---------------------------------------------------------------- update

    void Update()
    {
        bool hover = false;
        string prompt = "";
        float progress = 0f;

        if (Interactor != null)
        {
            var f = Interactor.Focus;
            if (Interactor.IsBusy)
            {
                hover = true;
                progress = Interactor.ActiveProgress;
                prompt = "";
            }
            else if (f != null)
            {
                hover = f.CanInteract(Interactor.Hands);
                prompt = f.Prompt(Interactor.Hands);
                if (hover) prompt = "[E]  " + prompt;
            }
        }

        float wantSize = hover ? ActiveSize : IdleSize;
        Color wantCol = hover ? HeartActive : HeartIdle;

        // A soft beat while hovering, so the heart feels alive rather than static.
        if (hover) wantSize *= 1f + Mathf.Sin(Time.time * 5.5f) * 0.045f;

        _size = Mathf.Lerp(_size, wantSize, Time.deltaTime * 12f);
        _col = Color.Lerp(_col, wantCol, Time.deltaTime * 12f);

        if (_heartRT != null) _heartRT.sizeDelta = new Vector2(_size, _size);
        if (_heart != null) _heart.color = _col;

        if (_ring != null)
        {
            _ring.fillAmount = progress;
            var c = _ring.color;
            _ring.color = new Color(c.r, c.g, c.b, progress > 0.001f ? 0.95f : 0f);
        }

        if (_prompt != null) _prompt.text = prompt;
        if (_toast != null) _toast.text = CafeMessage.Visible ? CafeMessage.Current : "";

        if (_cupLine != null)
        {
            var hands = Interactor != null ? Interactor.Hands : null;
            _cupLine.text = (hands != null && hands.HasCup)
                ? "In the cup:  " + hands.Cup.ShortList()
                : "";
        }

        if (_orderLine != null)
        {
            var om = OrderManager.Instance;
            _orderLine.text = om == null ? "" :
                (om.OpenCount == 0 ? "no orders up"
                                   : om.OpenCount + (om.OpenCount == 1 ? " order up" : " orders up"))
                + (om.Served > 0 ? "   served " + om.Served : "");
        }
    }
}
