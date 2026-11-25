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
    Image _band;
    GameObject _canvasGO;
    RectTransform _bandRT;
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
        var canvasGO = CafeUIKit.MakeCanvas(transform, "HUD_Canvas", 100).gameObject;
        _canvasGO = canvasGO;

        // Target band: the arc you are aiming for, around the outside of the progress ring.
        var bandGO = new GameObject("TargetBand", typeof(Image));
        bandGO.transform.SetParent(canvasGO.transform, false);
        _band = bandGO.GetComponent<Image>();
        _band.sprite = RingSprite(128, 0.80f);
        _band.type = Image.Type.Filled;
        _band.fillMethod = Image.FillMethod.Radial360;
        _band.fillOrigin = 2;                     // top
        _band.fillClockwise = true;
        _band.fillAmount = 0f;
        _band.color = new Color(0.78f, 0.94f, 0.88f, 0f);   // palette Accent
        _band.raycastTarget = false;
        _bandRT = bandGO.GetComponent<RectTransform>();
        _bandRT.anchorMin = _bandRT.anchorMax = new Vector2(0.5f, 0.5f);
        _bandRT.anchoredPosition = Vector2.zero;
        _bandRT.sizeDelta = new Vector2(110f, 110f);

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
        ringRT.sizeDelta = new Vector2(88f, 88f);

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

        // Text sits on little watercolour paper chips, so it reads over any background.
        _prompt    = Chip(canvasGO.transform, "Prompt",    30, new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), CafeUIKit.Paper);
        _cupLine   = Chip(canvasGO.transform, "CupLine",   24, new Vector2(0f, 0f),     new Vector2(28f, 28f),  CafeUIKit.Blush);
        _orderLine = Chip(canvasGO.transform, "Clock",     24, new Vector2(1f, 1f),     new Vector2(-28f, -28f), CafeUIKit.Mint);
        _toast     = Chip(canvasGO.transform, "Toast",     26, new Vector2(0.5f, 0f),   new Vector2(0f, 96f),   CafeUIKit.Butter);
    }

    internal static Text Chip(Transform parent, string name, int size, Vector2 anchor, Vector2 pos, Color tint)
    {
        var card = CafeUIKit.Card(parent, name, tint);
        card.raycastTarget = false;
        CafeUIKit.Place(card.rectTransform, anchor, pos, Vector2.zero);
        var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(30, 30, 14, 14);
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        var fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var t = CafeUIKit.Label(card.transform, "", size, CafeUIKit.Ink);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        card.gameObject.SetActive(false);
        return t;
    }

    internal static void SetChip(Text t, string text)
    {
        if (t == null) return;
        bool show = !string.IsNullOrEmpty(text);
        var go = t.transform.parent.gameObject;
        if (go.activeSelf != show) go.SetActive(show);
        if (show && t.text != text) t.text = text;
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
        if (_canvasGO != null && _canvasGO.activeSelf == CafeGame.MenuOpen) _canvasGO.SetActive(!CafeGame.MenuOpen);
        if (CafeGame.MenuOpen) return;

        bool hover = false;
        string prompt = "";
        float progress = 0f;
        Vector2 band = Vector2.zero;

        if (Interactor != null)
        {
            var f = Interactor.Focus;
            if (Interactor.IsBusy)
            {
                hover = true;
                progress = Interactor.ActiveProgress;
                var st = Interactor.ActiveStation;
                band = st != null ? st.Band : Vector2.zero;
                prompt = st != null && st.Hint != null ? st.Hint : "";
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

        if (_band != null)
        {
            bool show = band.y > band.x;
            _band.fillAmount = show ? band.y - band.x : 0f;
            // Clockwise from the top, so rotate the start of the fill round to band.x.
            _bandRT.localRotation = Quaternion.Euler(0f, 0f, -band.x * 360f);
            // Brightens while the progress needle is inside it: "let go now".
            bool inside = show && progress >= band.x && progress <= band.y;
            var c = _band.color;
            _band.color = new Color(c.r, c.g, c.b, !show ? 0f : (inside ? 1f : 0.55f));
            // Not colour-only: the words say it too.
            if (inside) prompt = "NOW!";
        }

        SetChip(_prompt, prompt);
        SetChip(_toast, CafeMessage.Visible ? CafeMessage.Current : "");

        var hands = Interactor != null ? Interactor.Hands : null;
        SetChip(_cupLine, hands != null && hands.HasCup ? "In the cup:  " + hands.Cup.ShortList() : "");

        var day = CafeDay.Instance;
        var om = OrderManager.Instance;
        string clock = day == null ? "" : (day.IsOpen ? "Day " + day.DayNumber + "   " + day.ClockText : "Closed");
        if (om != null && om.Served > 0) clock += "   served " + om.Served;
        SetChip(_orderLine, clock + "\n<size=18>Esc  menu     Tab  recipes</size>");
    }
}
