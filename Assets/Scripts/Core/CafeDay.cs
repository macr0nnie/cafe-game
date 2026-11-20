using UnityEngine;

/// The opening hours. A day runs from OpenHour to CloseHour in DayLengthSeconds
/// of play, then the cafe closes and the day's summary shows.
public class CafeDay : MonoBehaviour
{
    public static CafeDay Instance;

    public float DayLengthSeconds = 420f;
    public int OpenHour = 8;
    public int CloseHour = 16;

    float _elapsed;
    bool _open;
    int _dayNumber;

    public bool IsOpen { get { return _open; } }
    public int DayNumber { get { return _dayNumber; } }
    public float Fraction { get { return Mathf.Clamp01(_elapsed / Mathf.Max(1f, DayLengthSeconds)); } }

    /// "10:30 am" style clock, in quarter hours so it does not flicker.
    public string ClockText
    {
        get
        {
            float hours = Mathf.Lerp(OpenHour, CloseHour, Fraction);
            int h = Mathf.FloorToInt(hours);
            int m = Mathf.FloorToInt((hours - h) * 4f) * 15;
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return h12 + ":" + m.ToString("00") + (h < 12 ? " am" : " pm");
        }
    }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void StartDay()
    {
        _elapsed = 0f;
        _open = true;
        _dayNumber++;
        CafeEvents.RaiseDayStarted();
    }

    void Update()
    {
        if (!_open) return;
        _elapsed += Time.deltaTime;
        if (_elapsed >= DayLengthSeconds)
        {
            _open = false;
            CafeEvents.RaiseDayEnded();
        }
    }
}
