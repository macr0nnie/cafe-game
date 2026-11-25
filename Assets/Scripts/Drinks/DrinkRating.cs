using System.Collections.Generic;

/// A customer's verdict on the drink they were handed, scored on the slot model:
/// accuracy (right drink) and execution (made well) stay separate, then become stars.
public class DrinkRating
{
    public List<SlotResult> Slots;
    public float Accuracy;
    public float Execution;
    public int Stars;
    public int Tips;
    public string Headline;

    public bool Exact { get { return Accuracy >= 0.999f; } }

    public static DrinkRating Rate(IEnumerable<Ingredient> made, float execution, Recipe ordered,
                                   float secondsWaited, float parSeconds)
    {
        var r = new DrinkRating();
        r.Slots = DrinkScore.Compare(DrinkSlots.FromIngredients(made), DrinkSlots.SpecFor(ordered));
        r.Accuracy = DrinkScore.Accuracy(r.Slots);
        r.Execution = execution;
        r.Stars = DrinkScore.Stars(r.Accuracy, execution);
        r.Tips = DrinkScore.Payout(r.Stars, secondsWaited, parSeconds);
        r.Headline = DrinkScore.Headline(r.Slots, execution);
        return r;
    }

    /// What the customer says, in words and numbers so it never relies on colour.
    public string Reaction()
    {
        string mood = Stars >= 5 ? "Perfect!" : Stars == 4 ? "Lovely!" : Stars == 3 ? "Pretty good" : "Hmm, okay";
        return mood + "\n" + Stars + " / 5 stars\n" + Headline;
    }
}
