using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Every full-screen menu, styled as a cafe menu board on watercolour paper:
/// title, pause, settings, recipe book and the end-of-day summary.
/// Keys: Esc opens the pause menu or goes back a step, Tab opens the recipe
/// book. Arrow keys and Enter work on every screen as well as the mouse.
public class CafeMenus : MonoBehaviour
{
    public enum Screen { None, Title, Pause, Settings, Recipes, Summary }

    readonly Dictionary<Screen, RectTransform> _screens = new Dictionary<Screen, RectTransform>();
    readonly Dictionary<Screen, Selectable> _first = new Dictionary<Screen, Selectable>();
    Screen _current = Screen.None;
    Screen _backTo = Screen.None;     // where Esc goes from settings and recipes
    RectTransform _canvas;
    Image _backdrop;
    float _pop;

    Text _titleStart;
    Text _summaryBody;
    Text _summaryHeading;
    Image[] _summaryStars;

    // Today's numbers, gathered from events.
    int _served, _perfect, _starSum, _tips;

    void Awake()
    {
        EnsureEventSystem();
        _canvas = (RectTransform)CafeUIKit.MakeCanvas(transform, "Menus_Canvas", 200).transform;

        _backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _backdrop.transform.SetParent(_canvas, false);
        CafeUIKit.Stretch(_backdrop.rectTransform);
        _backdrop.color = CafeUIKit.WithAlpha(CafeUIKit.Lavender, 0.6f);

        BuildTitle();
        BuildPause();
        BuildSettings();
        BuildRecipes();
        BuildSummary();
        Show(Screen.None);
    }

    void OnEnable()
    {
        CafeEvents.OrderServed += OnServed;
        CafeEvents.DayStarted += OnDayStarted;
        CafeEvents.DayEnded += OnDayEnded;
    }

    void OnDisable()
    {
        CafeEvents.OrderServed -= OnServed;
        CafeEvents.DayStarted -= OnDayStarted;
        CafeEvents.DayEnded -= OnDayEnded;
    }

    void Start() { Show(Screen.Title); }

    void OnServed(Recipe r, int stars, int tips)
    {
        _served++; _starSum += stars; _tips += tips;
        if (stars >= 5) _perfect++;
    }

    void OnDayStarted() { _served = _perfect = _starSum = _tips = 0; }
    void OnDayEnded()   { Show(Screen.Summary); }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    // ---------------------------------------------------------------- flow

    public void Show(Screen s)
    {
        _current = s;
        foreach (var kv in _screens) kv.Value.gameObject.SetActive(kv.Key == s);
        _backdrop.gameObject.SetActive(s != Screen.None);
        // The title sits over a near-opaque wash; in-game menus let the cafe show through.
        _backdrop.color = CafeUIKit.WithAlpha(CafeUIKit.Lavender, s == Screen.Title ? 0.92f : 0.6f);
        CafeGame.SetMenuOpen(s != Screen.None);
        _pop = 0f;

        if (s == Screen.Title && _titleStart != null)
            _titleStart.text = CafeDay.Instance != null && CafeDay.Instance.IsOpen ? "Back to work" : "Open the cafe";
        if (s == Screen.Summary) FillSummary();

        Selectable first;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(_first.TryGetValue(s, out first) && first != null ? first.gameObject : null);
    }

    void OpenSub(Screen s) { _backTo = _current == Screen.Title ? Screen.Title : Screen.Pause; Show(s); }

    void Back()
    {
        switch (_current)
        {
            case Screen.Pause:    Show(Screen.None); break;
            case Screen.Settings:
            case Screen.Recipes:  Show(_backTo == Screen.Title ? Screen.Title : (_backTo == Screen.Pause ? Screen.Pause : Screen.None)); break;
        }
    }

    void StartOrResume()
    {
        var day = CafeDay.Instance;
        Show(Screen.None);
        if (day != null && !day.IsOpen) day.StartDay();
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        if (CafeInput.CancelDown())
        {
            if (_current == Screen.None) Show(Screen.Pause);
            else Back();
        }
        else if (CafeInput.RecipesDown())
        {
            if (_current == Screen.None) { _backTo = Screen.None; Show(Screen.Recipes); }
            else if (_current == Screen.Recipes) Back();
        }

        // Each screen settles in with a little pop, on unscaled time while paused.
        RectTransform rt;
        if (_current != Screen.None && _screens.TryGetValue(_current, out rt) && _pop < 1f)
        {
            _pop = Mathf.MoveTowards(_pop, 1f, Time.unscaledDeltaTime * 5f);
            float e = 1f - Mathf.Pow(1f - _pop, 3f);
            rt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
        }
    }

    // ---------------------------------------------------------------- shared layout

    /// A centred menu board: handwritten heading, brush underline, then content.
    RectTransform Board(Screen s, string heading, string subheading, Vector2 size, Color tint)
    {
        var card = CafeUIKit.Card(_canvas, "Screen_" + s, tint);
        var rt = card.rectTransform;
        CafeUIKit.Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        rt.pivot = new Vector2(0.5f, 0.5f);
        CafeUIKit.Stack(card.gameObject, 64, 44, 12f);

        var h = CafeUIKit.Label(card.transform, heading, 64, CafeUIKit.Ink, TextAnchor.MiddleCenter, true);
        CafeUIKit.Size(h, -1f, 84f);
        if (!string.IsNullOrEmpty(subheading))
        {
            var sub = CafeUIKit.Label(card.transform, subheading, 24, CafeUIKit.InkSoft);
            CafeUIKit.Size(sub, -1f, 30f);
        }
        CafeUIKit.Divider(card.transform, CafeUIKit.Hex("#F0A0B0"), 320f, 18f);
        _screens[s] = rt;
        return rt;
    }

    void Hint(Transform parent, string text)
    {
        var spacer = CafeUIKit.Node(parent, "Spacer");
        CafeUIKit.Size(spacer, -1f, 6f).flexibleHeight = 1f;
        var t = CafeUIKit.Label(parent, text, 20, CafeUIKit.InkSoft);
        CafeUIKit.Size(t, -1f, 26f);
    }

    Button Btn(Transform parent, string text, Color tint, UnityEngine.Events.UnityAction a)
    {
        return CafeUIKit.Button(parent, text, tint, a);
    }

    // ---------------------------------------------------------------- screens

    void BuildTitle()
    {
        var b = Board(Screen.Title, "Cozy Cafe", "a little coffee shop, one cup at a time",
                      new Vector2(640f, 640f), CafeUIKit.Paper);
        var start = Btn(b, "Open the cafe", CafeUIKit.Pink, StartOrResume);
        _titleStart = start.GetComponentInChildren<Text>();
        _first[Screen.Title] = start;
        Btn(b, "Recipe book", CafeUIKit.Mint, () => OpenSub(Screen.Recipes));
        Btn(b, "Settings", CafeUIKit.Lavender, () => OpenSub(Screen.Settings));
        Btn(b, "Close up shop", CafeUIKit.Butter, Quit);
        Hint(b, "Arrow keys + Enter, or the mouse");
    }

    void BuildPause()
    {
        var b = Board(Screen.Pause, "Tea break", "the cafe waits for you",
                      new Vector2(600f, 640f), CafeUIKit.Paper);
        _first[Screen.Pause] = Btn(b, "Back to the counter", CafeUIKit.Pink, () => Show(Screen.None));
        Btn(b, "Recipe book", CafeUIKit.Mint, () => OpenSub(Screen.Recipes));
        Btn(b, "Settings", CafeUIKit.Lavender, () => OpenSub(Screen.Settings));
        Btn(b, "Title screen", CafeUIKit.Butter, () => Show(Screen.Title));
        Hint(b, "Esc  back to the cafe     Tab  recipe book");
    }

    void BuildSettings()
    {
        var b = Board(Screen.Settings, "Settings", null, new Vector2(760f, 700f), CafeUIKit.Paper);
        var first = CafeUIKit.Slider(b, "Mouse speed", 0.2f, 3f, CafeSettings.MouseSensitivity,
                                     v => v.ToString("0.0") + "x", v => CafeSettings.MouseSensitivity = v);
        _first[Screen.Settings] = first;
        CafeUIKit.Slider(b, "Text & UI size", 0.75f, 1.6f, CafeSettings.UiScale,
                         v => Mathf.RoundToInt(v * 100f) + "%", v => CafeSettings.UiScale = v);
        CafeUIKit.Slider(b, "Volume", 0f, 1f, CafeSettings.Volume,
                         v => Mathf.RoundToInt(v * 100f) + "%", v => CafeSettings.Volume = v);
        CafeUIKit.Slider(b, "Aim help", 0f, 1f, CafeSettings.AimAssist,
                         v => v < 0.01f ? "Off" : Mathf.RoundToInt(v * 100f) + "%", v => CafeSettings.AimAssist = v);
        CafeUIKit.Toggle(b, "Camera bob when walking", CafeSettings.CameraBob, v => CafeSettings.CameraBob = v);
        var spacer = CafeUIKit.Node(b, "Spacer"); CafeUIKit.Size(spacer, -1f, 8f);
        Btn(b, "Done", CafeUIKit.Pink, Back);
        Hint(b, "Esc  back");
    }

    void BuildRecipes()
    {
        var b = Board(Screen.Recipes, "Menu", "what goes in each drink", new Vector2(1180f, 900f), CafeUIKit.Paper);

        // Two columns, like a menu board. Each drink: name, then its ingredients
        // with a colour blob and the name written out.
        var cols = CafeUIKit.Node(b, "Columns");
        CafeUIKit.Size(cols).flexibleHeight = 1f;
        var hz = cols.gameObject.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = 36f; hz.childControlWidth = true; hz.childControlHeight = true;
        hz.childForceExpandWidth = true; hz.childForceExpandHeight = true;

        var all = RecipeBook.All;
        int half = (all.Length + 1) / 2;
        for (int c = 0; c < 2; c++)
        {
            var col = CafeUIKit.Node(cols, "Col" + c);
            CafeUIKit.Stack(col.gameObject, 0, 0, 2f, TextAnchor.UpperLeft);
            for (int i = c * half; i < Mathf.Min(all.Length, (c + 1) * half); i++)
            {
                var r = all[i];
                var name = CafeUIKit.Label(col, r.Name + (r.Iced ? "  <size=18>(iced)</size>" : ""), 28,
                                           CafeUIKit.Ink, TextAnchor.LowerLeft, true);
                CafeUIKit.Size(name, -1f, 38f);
                var steps = new List<string>();
                foreach (var s in r.Steps) steps.Add(Ingredients.Label(s));
                var line = CafeUIKit.Node(col, "Steps");
                var h = line.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 14f; h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = true; h.childControlHeight = true;
                h.childForceExpandWidth = false; h.childForceExpandHeight = false;
                CafeUIKit.Size(line, -1f, 26f);
                foreach (var s in r.Steps) CafeUIKit.Swatch(line, Ingredients.Colour(s), Ingredients.Label(s), 18);
            }
        }

        var row = CafeUIKit.Node(b, "Buttons");
        CafeUIKit.Size(row, -1f, 64f);
        var bh = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        bh.childAlignment = TextAnchor.MiddleCenter; bh.childControlWidth = true; bh.childControlHeight = true;
        bh.childForceExpandWidth = false;
        var close = Btn(row, "Close the book", CafeUIKit.Mint, Back);
        CafeUIKit.Size(close, 340f, 64f);
        _first[Screen.Recipes] = close;
        Hint(b, "Tab or Esc  close");
    }

    void BuildSummary()
    {
        var b = Board(Screen.Summary, "Closing time", null, new Vector2(680f, 700f), CafeUIKit.Paper);
        _summaryHeading = b.GetComponentInChildren<Text>();

        var stars = CafeUIKit.Node(b, "Stars");
        CafeUIKit.Size(stars, -1f, 56f);
        var sh = stars.gameObject.AddComponent<HorizontalLayoutGroup>();
        sh.spacing = 10f; sh.childAlignment = TextAnchor.MiddleCenter;
        sh.childControlWidth = true; sh.childControlHeight = true; sh.childForceExpandWidth = false;
        _summaryStars = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            var dot = new GameObject("Star" + i, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            dot.transform.SetParent(stars, false);
            dot.sprite = CafeUIKit.Blob; dot.raycastTarget = false;
            CafeUIKit.Size(dot, 44f, 44f);
            _summaryStars[i] = dot;
        }

        _summaryBody = CafeUIKit.Label(b, "", 28, CafeUIKit.Ink);
        CafeUIKit.Size(_summaryBody, -1f, 220f);

        _first[Screen.Summary] = Btn(b, "Open tomorrow", CafeUIKit.Pink, StartOrResume);
        Btn(b, "Title screen", CafeUIKit.Butter, () => Show(Screen.Title));
    }

    void FillSummary()
    {
        int day = CafeDay.Instance != null ? CafeDay.Instance.DayNumber : 1;
        if (_summaryHeading != null) _summaryHeading.text = "Day " + day + " done";
        float avg = _served > 0 ? (float)_starSum / _served : 0f;
        int filled = Mathf.RoundToInt(avg);
        for (int i = 0; i < _summaryStars.Length; i++)
            _summaryStars[i].color = i < filled ? CafeUIKit.Hex("#F0A0B0") : CafeUIKit.WithAlpha(CafeUIKit.InkSoft, 0.25f);

        _summaryBody.text =
            "Average rating  <b>" + avg.ToString("0.0") + " / 5</b>\n" +
            "Drinks served  <b>" + _served + "</b>\n" +
            "Five-star drinks  <b>" + _perfect + "</b>\n" +
            "Tips in the jar  <b>" + _tips + "</b>\n\n" +
            (_served == 0 ? "A quiet day. Tomorrow will be busier." : "Lovely work today.");
    }
}
