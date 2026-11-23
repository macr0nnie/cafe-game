using UnityEngine;

/// How a simple station is worked. Each one is its own little verb.
public enum StationVerb
{
    Pour,   // hold, the level rises in the glass, let go at the line
    Scoop,  // tap once per scoop, each one lands in the cup
    Pump,   // one tap, a quick squirt
}

/// Adds one ingredient to the held cup, played as Pour, Scoop or Pump.
/// Nothing here can fail: a rough pour is still a pour, it just scores lower.
public class IngredientStation : StationBase
{
    public Ingredient Produces = Ingredient.Milk;
    public string VerbOverride;
    public StationVerb Verb = StationVerb.Pour;

    [Header("Pour")]
    [Tooltip("Where on the ring the line sits; release here for a full portion.")]
    [Range(0.3f, 0.95f)] public float LineAt = 0.72f;
    public float LineTolerance = 0.08f;

    [Header("Scoop")]
    public int Scoops = 3;
    public float IdleTimeout = 2f;

    int _scoops;
    float _idle;

    public override Vector2 Band
    {
        get { return Verb == StationVerb.Pour ? CafeUpgrades.Widen(new Vector2(LineAt - LineTolerance, LineAt + LineTolerance), UpgradeTarget.Pour) : Vector2.zero; }
    }

    public override string Hint
    {
        get
        {
            if (Verb == StationVerb.Pour)  return "Hold... let go at the line";
            if (Verb == StationVerb.Scoop) return "Tap E to scoop  (" + _scoops + " / " + Scoops + ")";
            return null;
        }
    }

    public override string Prompt(PlayerHands hands)
    {
        if (hands == null || !hands.HasCup) return "Need a cup first";
        if (!hands.Cup.CanAdd(Produces)) return "Already has " + Ingredients.Label(Produces);
        return (string.IsNullOrEmpty(VerbOverride) ? "Add " : VerbOverride + " ")
               + Ingredients.Label(Produces);
    }

    public override bool CanInteract(PlayerHands hands)
    {
        return hands != null && hands.HasCup && hands.Cup.CanAdd(Produces);
    }

    public override void Begin(PlayerHands hands)
    {
        base.Begin(hands);
        _scoops = 0;
        _idle = 0f;
        if (hands == null || !hands.HasCup) return;

        if (Verb == StationVerb.Pour)
            hands.Cup.ShowTargetLine(hands.Cup.LiquidLevel + Ingredients.Volume(Produces));
        else if (Verb == StationVerb.Scoop)
            Scoop(hands);       // the press that started it is the first scoop
    }

    void Scoop(PlayerHands hands)
    {
        _scoops++;
        _idle = 0f;
        Progress = (float)_scoops / Mathf.Max(1, Scoops);
        hands.Cup.SetPour(Produces, Ingredients.Volume(Produces) * Progress);
    }

    public override bool Tick(PlayerHands hands, bool held, bool pressed, float dt)
    {
        if (hands == null || !hands.HasCup) { End(hands, Progress); return true; }
        float vol = Ingredients.Volume(Produces);

        switch (Verb)
        {
            case StationVerb.Pour:
                if (!held) { End(hands, Progress); return true; }
                Progress = Mathf.Min(1f, Progress + dt / Mathf.Max(0.01f, Duration));
                hands.Cup.SetPour(Produces, vol * Portion(Progress, LineAt));
                if (Progress >= 1f) { End(hands, 1f); return true; }
                return false;

            case StationVerb.Scoop:
                if (pressed) Scoop(hands);
                else _idle += dt;
                if (_scoops >= Scoops || _idle > IdleTimeout) { End(hands, Progress); return true; }
                return false;

            default: // Pump: a short automatic squirt
                Progress = Mathf.Min(1f, Progress + dt / Mathf.Max(0.01f, Duration));
                hands.Cup.SetPour(Produces, vol * Progress);
                if (Progress >= 1f) { End(hands, 1f); return true; }
                return false;
        }
    }

    public override void End(PlayerHands hands, float t01)
    {
        if (hands != null && hands.HasCup)
        {
            var cup = hands.Cup;
            cup.HideTargetLine();

            if (Verb == StationVerb.Pour)
            {
                // A tap and release is a change of mind, not a pour.
                if (cup.PourAmount > Ingredients.Volume(Produces) * 0.15f)
                {
                    cup.CommitPour();
                    var band = Band;
                    cup.RecordQuality(BandQuality(t01, band));
                    if (t01 < band.x)      CafeMessage.Show("A little under the line", 2.2f);
                    else if (t01 <= band.y) CafeMessage.Show("Right on the line", 2.2f);
                    else                    CafeMessage.Show("Over the line - generous", 2.2f);
                }
            }
            else if (Verb == StationVerb.Scoop)
            {
                if (_scoops > 0) cup.CommitPour();
                if (_scoops > 0 && _scoops < Scoops) CafeMessage.Show("Only " + _scoops + " scoop" + (_scoops == 1 ? "" : "s") + " - it will do");
            }
            else cup.CommitPour();
        }
        base.End(hands, t01);
    }
}
