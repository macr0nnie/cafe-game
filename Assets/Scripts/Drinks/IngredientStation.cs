using UnityEngine;

/// Press and wait. Adds one ingredient to the held cup when the timer completes.
public class IngredientStation : StationBase
{
    public Ingredient Produces = Ingredient.Milk;
    public string VerbOverride;

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

    public override void End(PlayerHands hands, float t01)
    {
        if (t01 >= 0.999f && hands != null && hands.HasCup && hands.Cup.CanAdd(Produces))
            hands.Cup.Add(Produces);
        base.End(hands, t01);
    }
}
