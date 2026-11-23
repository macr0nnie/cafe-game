using UnityEngine;

/// Pulls a shot. Needs grounds from the grinder first, and sets the cup on the
/// drip tray under the spouts while it runs.
/// Minigame: tap to start the pull, tap again to stop it while the crema is in
/// the band. Short or long still gives you a shot, just a rougher one.
public class EspressoStation : StationBase
{
    public bool HasGrounds;
    public Transform GroundsVisual;

    [Header("Crema band (fraction of Duration)")]
    public Vector2 CremaBand = new Vector2(0.60f, 0.80f);
    [Tooltip("Stopping before this hands the basket back untouched.")]
    public float MinPull = 0.15f;

    /// The crema band, wider with espresso upgrades.
    public override Vector2 Band { get { return CafeUpgrades.Widen(CremaBand, UpgradeTarget.Espresso); } }
    public override string Hint { get { return "Tap again to stop the shot in the band"; } }

    public override string Prompt(PlayerHands hands)
    {
        if (!HasGrounds) return "No grounds in the basket - grind first";
        if (hands == null || !hands.HasCup) return "Need a cup first";
        if (!hands.Cup.CanAdd(Ingredient.EspressoShot)) return "No room for another shot";
        return hands.Cup.CountOf(Ingredient.EspressoShot) > 0 ? "Pull a second shot" : "Pull a shot";
    }

    public override bool CanInteract(PlayerHands hands)
    {
        return HasGrounds && hands != null && hands.HasCup
               && hands.Cup.CanAdd(Ingredient.EspressoShot);
    }

    public override bool Tick(PlayerHands hands, bool held, bool pressed, float dt)
    {
        Progress = Mathf.Min(1f, Progress + dt / Mathf.Max(0.01f, Duration));
        if (hands != null && hands.HasCup)
        {
            float mid = (CremaBand.x + CremaBand.y) * 0.5f;
            hands.Cup.SetPour(Ingredient.EspressoShot,
                              Ingredients.Volume(Ingredient.EspressoShot) * Mathf.Min(1.4f, Portion(Progress, mid)));
        }
        if (pressed || Progress >= 1f) { End(hands, Progress); return true; }
        return false;
    }

    public override void End(PlayerHands hands, float t01)
    {
        if (hands != null && hands.HasCup && t01 >= MinPull)
        {
            var band = Band;
            hands.Cup.CommitPour();
            hands.Cup.RecordQuality(BandQuality(t01, band));
            HasGrounds = false;
            if (GroundsVisual != null) GroundsVisual.gameObject.SetActive(false);

            if (t01 < band.x)       CafeMessage.Show("Short pull - a touch sour", 2.6f);
            else if (t01 <= band.y) CafeMessage.Show("Golden crema - perfect shot", 2.6f);
            else                    CafeMessage.Show("Long pull - a little bitter", 2.6f);
        }
        else if (t01 < MinPull)
        {
            CafeMessage.Show("Stopped before it started - basket is still loaded");
        }
        base.End(hands, t01);
    }
}
