using UnityEngine;

/// Shared behaviour for anything the player operates to build a drink.
public abstract class StationBase : MonoBehaviour, IInteractable
{
    [Header("Timing")]
    public float Duration = 2f;

    [Header("Cup")]
    [Tooltip("If set, the held cup is placed here while this station runs, then handed back.")]
    public Transform CupAnchor;

    [Header("Pour")]
    [Tooltip("Mesh that hangs from its own origin; scaled on Y while pouring.")]
    public Transform PourStream;
    public float PourLength = 0.10f;
    [Range(0f, 1f)] public float PourStartAt = 0.18f;
    [Range(0f, 1f)] public float PourEndAt = 0.94f;

    [Header("Feel")]
    public ParticleSystem Steam;
    public ParticleSystem Splash;
    public Transform MovingPart;
    public Vector3 MovingPartAxis = Vector3.up;
    public float MovingPartSpeed = 240f;
    public AudioSource Sound;

    protected bool _running;
    float _pourShown;

    public float Progress { get; protected set; }
    public virtual bool IsHold { get { return false; } }

    public abstract string Prompt(PlayerHands hands);
    public abstract bool CanInteract(PlayerHands hands);

    /// True when using this now is the way to get `i` into a cup. The drink guide uses it
    /// to point at the next station, so a new station only has to answer this.
    public virtual bool Makes(Ingredient i) { return false; }

    public virtual void Begin(PlayerHands hands)
    {
        _running = true;
        Progress = 0f;
        SetFX(true);
        if (CupAnchor != null && hands != null && hands.HasCup) hands.LendCupTo(CupAnchor);
    }

    public virtual void Hold(PlayerHands hands, float t01)
    {
        Progress = t01;
    }

    public virtual void End(PlayerHands hands, float t01)
    {
        _running = false;
        Progress = 0f;
        SetFX(false);
        if (hands != null && hands.HasCup) hands.Cup.CancelPour();   // no-op once a station committed
        if (hands != null) hands.ReturnCup();
    }

    // ---------------------------------------------------------------- minigame

    /// Shown in place of the prompt while this station runs: what to do right now.
    public virtual string Hint { get { return null; } }

    /// Target window on the HUD progress ring, as (start, end) in 0..1. Zero = none.
    public virtual Vector2 Band { get { return Vector2.zero; } }

    /// One frame of the station's minigame. Returns true when the step is over.
    /// Default: hold to fill the ring, let go to stop early.
    public virtual bool Tick(PlayerHands hands, bool held, bool pressed, float dt)
    {
        if (!held) { End(hands, Progress); return true; }
        Hold(hands, Mathf.Clamp01(Progress + dt / Mathf.Max(0.01f, Duration)));
        if (Progress >= 1f) { End(hands, 1f); return true; }
        return false;
    }

    /// 1 inside the band, easing down to a floor outside it. Never zero: cozy.
    protected static float BandQuality(float t, Vector2 band)
    {
        if (t >= band.x && t <= band.y) return 1f;
        float miss = t < band.x ? band.x - t : t - band.y;
        return Mathf.Lerp(0.35f, 0.85f, Mathf.Clamp01(1f - miss / 0.3f));
    }

    /// How much of a full portion has gone in, given the ring position and where
    /// the band centre sits. Starts when the stream lands, can overshoot.
    protected float Portion(float t, float target)
    {
        return Mathf.Max(0f, (t - PourStartAt) / Mathf.Max(0.01f, target - PourStartAt));
    }

    protected virtual void Update()
    {
        if (_running && MovingPart != null)
            MovingPart.Rotate(MovingPartAxis, MovingPartSpeed * Time.deltaTime, Space.Self);

        // Stream eases in and out rather than popping on.
        if (PourStream != null)
        {
            bool pouring = _running && Progress >= PourStartAt && Progress <= PourEndAt;
            _pourShown = Mathf.MoveTowards(_pourShown, pouring ? 1f : 0f, Time.deltaTime * 7f);

            bool visible = _pourShown > 0.01f;
            if (PourStream.gameObject.activeSelf != visible)
                PourStream.gameObject.SetActive(visible);

            if (visible)
            {
                var s = PourStream.localScale;
                PourStream.localScale = new Vector3(s.x, PourLength * _pourShown, s.z);
            }
        }
    }

    protected void SetFX(bool on)
    {
        if (Steam != null)  { if (on) Steam.Play();  else Steam.Stop(); }
        if (Splash != null) { if (on) Splash.Play(); else Splash.Stop(); }
        if (Sound != null)
        {
            if (on && !Sound.isPlaying) Sound.Play();
            else if (!on) Sound.Stop();
        }
    }
}
