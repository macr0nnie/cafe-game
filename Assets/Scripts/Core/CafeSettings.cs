using System;
using UnityEngine;

/// Player settings, saved between sessions. Anything that cares listens to Changed.
public static class CafeSettings
{
    public static event Action Changed;

    static bool _loaded;
    static float _mouse = 1f, _ui = 1f, _volume = 0.8f;
    static bool _bob;
    static float _aim = 1f;

    /// Multiplier on look speed.
    public static float MouseSensitivity { get { Load(); return _mouse; } set { _mouse = Mathf.Clamp(value, 0.2f, 3f); Save(); } }
    /// Multiplier on every UI canvas.
    public static float UiScale          { get { Load(); return _ui; }    set { _ui = Mathf.Clamp(value, 0.75f, 1.6f); Save(); } }
    public static float Volume           { get { Load(); return _volume; } set { _volume = Mathf.Clamp01(value); Save(); } }
    /// Head bob while walking. Off by default: it is the main motion-sickness trigger.
    public static bool  CameraBob        { get { Load(); return _bob; }   set { _bob = value; Save(); } }

    /// How strongly looking near a station snaps focus to it, 0 = off.
    public static float AimAssist        { get { Load(); return _aim; }   set { _aim = Mathf.Clamp01(value); Save(); } }

    static void Load()
    {
        if (_loaded) return;
        _loaded = true;
        _mouse  = PlayerPrefs.GetFloat("set.mouse", 1f);
        _ui     = PlayerPrefs.GetFloat("set.ui", 1f);
        _volume = PlayerPrefs.GetFloat("set.volume", 0.8f);
        _bob    = PlayerPrefs.GetInt("set.bob", 0) == 1;
        _aim    = PlayerPrefs.GetFloat("set.aim", 1f);
        AudioListener.volume = _volume;
    }

    static void Save()
    {
        PlayerPrefs.SetFloat("set.mouse", _mouse);
        PlayerPrefs.SetFloat("set.ui", _ui);
        PlayerPrefs.SetFloat("set.volume", _volume);
        PlayerPrefs.SetInt("set.bob", _bob ? 1 : 0);
        PlayerPrefs.SetFloat("set.aim", _aim);
        AudioListener.volume = _volume;
        if (Changed != null) Changed();
    }

    /// Reference resolution for a CanvasScaler at the current UI size.
    public static Vector2 ReferenceResolution { get { return new Vector2(1920f, 1080f) / UiScale; } }
}
