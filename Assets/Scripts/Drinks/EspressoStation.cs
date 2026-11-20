using UnityEngine;

/// Pulls a shot. Needs grounds from the grinder first, and sets the cup on the
/// drip tray under the spouts while it runs.
public class EspressoStation : StationBase
{
    public bool HasGrounds;
    public Transform GroundsVisual;

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

    public override void End(PlayerHands hands, float t01)
    {
        if (t01 >= 0.999f && hands != null && hands.HasCup
            && hands.Cup.CanAdd(Ingredient.EspressoShot))
        {
            hands.Cup.Add(Ingredient.EspressoShot);
            HasGrounds = false;
            if (GroundsVisual != null) GroundsVisual.gameObject.SetActive(false);
            CafeMessage.Show("Shot pulled");
        }
        else if (t01 > 0.05f && t01 < 0.999f)
        {
            CafeMessage.Show("Stopped early - the basket is spent");
            HasGrounds = false;
            if (GroundsVisual != null) GroundsVisual.gameObject.SetActive(false);
        }
        base.End(hands, t01);
    }
}
