using UnityEngine;

/// Hold to steam. Release in the sweet band for silky microfoam, overshoot for
/// stiff foam, let go early and it is only warm milk. The skill beat of the loop.
/// The milk rises in the glass while you hold, so you can see the pour.
public class SteamWandStation : StationBase
{
    [Header("Timing bands (fraction of Duration)")]
    [Range(0f, 1f)] public float SweetStart = 0.55f;
    [Range(0f, 1f)] public float SweetEnd = 0.80f;

    [Header("Wand")]
    public Transform Wand;
    public Vector3 WandDownEuler = new Vector3(26f, 0f, 0f);
    Quaternion _wandRest;

    public override bool IsHold { get { return true; } }
    /// The silky-milk band, wider with steam wand upgrades.
    public override Vector2 Band { get { return CafeUpgrades.Widen(new Vector2(SweetStart, SweetEnd), UpgradeTarget.SteamWand); } }
    public override string Hint { get { return "Hold... let go in the band for silky milk"; } }

    void Start() { if (Wand != null) _wandRest = Wand.localRotation; }

    public override string Prompt(PlayerHands hands)
    {
        if (hands == null || !hands.HasCup) return "Need a cup first";
        if (Preview(hands.Cup) == null) return "This cup already has milk";
        if (!hands.Cup.CanAdd(Ingredient.SteamedMilk)) return "Hold to foam it up";
        return "Hold to steam - release when it is glossy";
    }

    public override bool CanInteract(PlayerHands hands)
    {
        return hands != null && hands.HasCup && Preview(hands.Cup) != null;
    }

    /// What will show going in while you steam: steamed milk, or foam on a second pass.
    static Ingredient? Preview(CupContents cup)
    {
        if (cup.CanAdd(Ingredient.SteamedMilk)) return Ingredient.SteamedMilk;
        if (cup.CanAdd(Ingredient.Foam)) return Ingredient.Foam;
        if (cup.CanAdd(Ingredient.Milk)) return Ingredient.Milk;
        return null;
    }

    public override void Begin(PlayerHands hands)
    {
        base.Begin(hands);
        if (Wand != null) Wand.localRotation = _wandRest * Quaternion.Euler(WandDownEuler);
    }

    public override void Hold(PlayerHands hands, float t01)
    {
        base.Hold(hands, t01);
        if (hands == null || !hands.HasCup) return;
        var ing = Preview(hands.Cup);
        if (ing.HasValue)
            hands.Cup.SetPour(ing.Value, Ingredients.Volume(ing.Value)
                                         * Mathf.Min(1f, Portion(t01, (SweetStart + SweetEnd) * 0.5f)));
    }

    public override void End(PlayerHands hands, float t01)
    {
        if (Wand != null) Wand.localRotation = _wandRest;

        if (hands != null && hands.HasCup)
        {
            var cup = hands.Cup;
            cup.CancelPour();   // what actually goes in depends on when you let go
            bool added = true;
            var band = Band;
            if (t01 >= band.y)
            {
                if (cup.CanAdd(Ingredient.Foam)) cup.Add(Ingredient.Foam);
                else added = false;
                CafeMessage.Show("Over-steamed - stiff, bubbly foam", 2.6f);
            }
            else if (t01 >= band.x)
            {
                // A second pass on milk that is already steamed foams it, which is how cappuccinos get made.
                if (cup.CanAdd(Ingredient.SteamedMilk)) cup.Add(Ingredient.SteamedMilk);
                else if (cup.CanAdd(Ingredient.Foam)) cup.Add(Ingredient.Foam);
                else added = false;
                CafeMessage.Show("Silky microfoam - perfect", 2.6f);
            }
            else if (t01 > 0.12f)
            {
                if (cup.CanAdd(Ingredient.Milk)) cup.Add(Ingredient.Milk);
                else added = false;
                CafeMessage.Show("Pulled off early - barely warm milk", 2.6f);
            }
            else added = false;

            if (added) cup.RecordQuality(BandQuality(t01, band));
        }
        base.End(hands, t01);
    }
}
