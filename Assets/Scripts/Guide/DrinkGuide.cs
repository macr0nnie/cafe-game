using System.Collections.Generic;

public enum GuideStepKind { WaitForOrder, TakeOrder, TakeCup, PickUpCup, Add, Serve }

/// One thing to do next. Index/Count say how far through the drink you are
/// (cup = step 1, then each ingredient, serve = last); both 0 outside a drink.
public struct GuideStep
{
    public GuideStepKind Kind;
    public Ingredient Ingredient;
    public int Index;
    public int Count;
}

/// The drink-making order, as pure rules: no scene, no Unity objects, so it can be
/// unit tested and run the same on every client. Guides, never blocks: any order
/// still makes a drink, this is just the easy path.
public static class DrinkGuide
{
    /// Where an ingredient sits in a well-built drink. Matches what TechniqueNote praises:
    /// pearls and ice first, flavour in the bottom, shot before milk, toppings last.
    public static int Rank(Ingredient i)
    {
        switch (i)
        {
            case Ingredient.BobaPearls:      return 0;
            case Ingredient.Ice:             return 1;
            case Ingredient.VanillaSyrup:
            case Ingredient.CaramelSyrup:
            case Ingredient.StrawberrySyrup:
            case Ingredient.TaroSyrup:
            case Ingredient.Chocolate:
            case Ingredient.Matcha:          return 2;
            case Ingredient.EspressoShot:    return 3;
            case Ingredient.TeaBrew:
            case Ingredient.Water:           return 4;
            case Ingredient.Milk:            return 5;
            case Ingredient.SteamedMilk:     return 6;
            case Ingredient.Foam:            return 7;
            default:                         return 8;   // whipped cream and other toppings
        }
    }

    /// The recipe's ingredients in the order to add them (stable for equal ranks).
    public static List<Ingredient> Ordered(Recipe r)
    {
        var list = new List<Ingredient>(r.Steps);
        for (int i = 1; i < list.Count; i++)          // insertion sort: stable, recipes are tiny
            for (int j = i; j > 0 && Rank(list[j]) < Rank(list[j - 1]); j--)
            { var t = list[j]; list[j] = list[j - 1]; list[j - 1] = t; }
        return list;
    }

    /// What to do next for `order` (null = no order yet), given what is in the held cup.
    public static GuideStep Next(Recipe order, bool hasCup, IList<Ingredient> inCup,
                                 bool cupSetDown, bool customerWaiting)
    {
        var s = new GuideStep();
        if (order == null)
        {
            s.Kind = !hasCup && cupSetDown ? GuideStepKind.PickUpCup
                   : customerWaiting ? GuideStepKind.TakeOrder : GuideStepKind.WaitForOrder;
            return s;
        }

        var steps = Ordered(order);
        s.Count = steps.Count + 2;
        if (!hasCup)
        {
            s.Kind = cupSetDown ? GuideStepKind.PickUpCup : GuideStepKind.TakeCup;
            s.Index = 1;
            return s;
        }

        var have = inCup != null ? new List<Ingredient>(inCup) : new List<Ingredient>();
        for (int i = 0; i < steps.Count; i++)
        {
            if (have.Remove(steps[i])) continue;
            s.Kind = GuideStepKind.Add;
            s.Ingredient = steps[i];
            s.Index = 2 + i;
            return s;
        }
        s.Kind = GuideStepKind.Serve;
        s.Index = s.Count;
        return s;
    }

    /// Which open order a cup is being made for: the closest match, oldest first on ties
    /// (so an empty cup goes to the order that has waited longest). -1 when none are open.
    public static int Pick(IList<Recipe> open, IList<Ingredient> inCup)
    {
        int best = -1;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < open.Count; i++)
        {
            float sc = CupContents.Score(inCup ?? new Ingredient[0], open[i]);
            if (sc > bestScore) { bestScore = sc; best = i; }
        }
        return best;
    }

    /// True when the cup holds something the recipe does not want (time to re-pick the order).
    public static bool HasExtra(IList<Ingredient> inCup, Recipe r)
    {
        if (inCup == null || r == null) return false;
        var need = new List<Ingredient>(r.Steps);
        foreach (var a in inCup) if (!need.Remove(a)) return true;
        return false;
    }

    /// The line on the HUD. Words carry the meaning, never colour alone.
    public static string Describe(GuideStep s, string drink)
    {
        string head = s.Count > 0 ? drink + "  ·  step " + s.Index + " of " + s.Count + ":  " : "";
        switch (s.Kind)
        {
            case GuideStepKind.WaitForOrder: return "Waiting for the next order";
            case GuideStepKind.TakeOrder:    return "Take the order at the till";
            case GuideStepKind.PickUpCup:    return head + "pick your cup back up";
            case GuideStepKind.TakeCup:      return head + "take a cup";
            case GuideStepKind.Add:          return head + "add " + Ingredients.Label(s.Ingredient);
            default:                         return head + "serve it at the pickup shelf";
        }
    }
}
