using System.Collections.Generic;
using UnityEngine;

/// One line of the scoring card: a slot, what was asked, what arrived.
public struct SlotResult
{
    public string Slot;
    public string Wanted;
    public string Got;
    public bool Ok;
    public bool Checked;     // false when the order did not care about this slot
}

/// Scores a build against a spec on two independent axes:
///   accuracy  - did you make the right drink
///   execution - did you make it well
/// Neither one hides the other, and both read clearly to a player.
public static class DrinkScore
{
    public static List<SlotResult> Compare(DrinkBuild b, DrinkSpec s)
    {
        var r = new List<SlotResult>();
        if (b == null || s == null) return r;

        Add(r, "Size", DrinkWords.Size(s.Size), DrinkWords.Size(b.Size), b.Size == s.Size);

        if (s.Base != DrinkBase.None || b.BaseSet)
            Add(r, "Base", DrinkWords.Base(s.Base),
                    b.BaseSet ? DrinkWords.Base(b.Base) : "nothing",
                    b.BaseSet && b.Base == s.Base);

        if (s.Base == DrinkBase.Espresso)
            Add(r, "Shots", s.Shots.ToString(), b.Shots.ToString(), b.Shots == s.Shots);

        if (s.Water || b.Water)
            Add(r, "Hot water", s.Water ? "hot water" : "none", b.Water ? "hot water" : "none", b.Water == s.Water);

        if (s.Milk != MilkType.None || b.Milk != MilkType.None)
            Add(r, "Milk", DrinkWords.Milk(s.Milk), DrinkWords.Milk(b.Milk), b.Milk == s.Milk);

        if (s.Milk != MilkType.None)
            Add(r, "Texture", DrinkWords.Tex(s.Texture),
                    b.Texture == MilkTex.None ? "none" : DrinkWords.Tex(b.Texture),
                    b.Texture == s.Texture);

        if (s.Syrup != Syrup.None || b.Syrup != Syrup.None)
        {
            Add(r, "Syrup", DrinkWords.Syrup(s.Syrup), DrinkWords.Syrup(b.Syrup), b.Syrup == s.Syrup);
            if (s.Syrup != Syrup.None)
                Add(r, "Pumps", s.Pumps.ToString(), b.Pumps.ToString(), b.Pumps == s.Pumps);
        }

        if (s.Ice != IceLevel.None || b.Ice != IceLevel.None)
            Add(r, "Ice", DrinkWords.Ice(s.Ice), DrinkWords.Ice(b.Ice), b.Ice == s.Ice);

        if (s.Toppings != Topping.None || b.Toppings != Topping.None)
            Add(r, "Toppings", Join(DrinkWords.SplitToppings(s.Toppings)),
                    Join(DrinkWords.SplitToppings(b.Toppings)), b.Toppings == s.Toppings);

        if (s.Art != LatteArt.Plain)
            Add(r, "Latte art", DrinkWords.Art(s.Art), DrinkWords.Art(b.Art), b.Art == s.Art);

        return r;
    }

    static void Add(List<SlotResult> r, string slot, string want, string got, bool ok)
    {
        r.Add(new SlotResult { Slot = slot, Wanted = want, Got = got, Ok = ok, Checked = true });
    }

    static string Join(List<string> l)
    {
        if (l.Count == 0) return "none";
        return string.Join(", ", l.ToArray());
    }

    /// Fraction of the checked slots that matched.
    public static float Accuracy(List<SlotResult> results)
    {
        if (results == null || results.Count == 0) return 0f;
        int ok = 0, n = 0;
        foreach (var x in results) { if (!x.Checked) continue; n++; if (x.Ok) ok++; }
        return n == 0 ? 0f : (float)ok / n;
    }

    /// Stars out of five. Accuracy carries most of it; execution lifts a correct
    /// drink from passable to excellent but cannot rescue the wrong drink.
    public static int Stars(float accuracy, float execution)
    {
        float v = accuracy * 0.72f + execution * 0.28f;
        if (accuracy < 0.5f) v = Mathf.Min(v, 0.38f);      // wrong drink caps low
        return Mathf.Clamp(Mathf.RoundToInt(v * 5f), 1, 5);
    }

    /// Cozy framing: name the single biggest miss, warmly, or praise the save.
    public static string Headline(List<SlotResult> results, float execution)
    {
        SlotResult worst = default(SlotResult);
        bool found = false;
        foreach (var x in results)
        {
            if (!x.Checked || x.Ok) continue;
            worst = x; found = true; break;
        }

        if (!found)
        {
            if (execution >= 0.85f) return "Exactly right, and beautifully made";
            if (execution >= 0.6f)  return "Exactly right";
            return "Right drink, a little rough";
        }

        return "Close - wanted " + worst.Wanted.ToLower() + ", got " + worst.Got.ToLower();
    }

    /// What the player earns. Cozy: never negative, speed only ever adds.
    public static int Payout(int stars, float secondsTaken, float parSeconds)
    {
        int basePay = 3 + stars * 2;
        float speed = Mathf.Clamp01((parSeconds - secondsTaken) / Mathf.Max(1f, parSeconds));
        return basePay + Mathf.RoundToInt(speed * 4f);
    }
}
