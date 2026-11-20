using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum Ingredient
{
    EspressoShot, Water, Milk, SteamedMilk, Foam, Ice,
    TeaBrew, Matcha, Chocolate,
    VanillaSyrup, CaramelSyrup, StrawberrySyrup, TaroSyrup,
    BobaPearls, WhippedCream
}

public static class Ingredients
{
    public static string Label(Ingredient i)
    {
        switch (i)
        {
            case Ingredient.EspressoShot:    return "espresso shot";
            case Ingredient.Water:           return "hot water";
            case Ingredient.Milk:            return "milk";
            case Ingredient.SteamedMilk:     return "steamed milk";
            case Ingredient.Foam:            return "foam";
            case Ingredient.Ice:             return "ice";
            case Ingredient.TeaBrew:         return "brewed tea";
            case Ingredient.Matcha:          return "matcha";
            case Ingredient.Chocolate:       return "chocolate";
            case Ingredient.VanillaSyrup:    return "vanilla";
            case Ingredient.CaramelSyrup:    return "caramel";
            case Ingredient.StrawberrySyrup: return "strawberry";
            case Ingredient.TaroSyrup:       return "taro";
            case Ingredient.BobaPearls:      return "boba";
            case Ingredient.WhippedCream:    return "whipped cream";
        }
        return i.ToString();
    }

    /// Colour each ingredient contributes to the drink. From 09-liquids-and-animations.md.
    public static Color Colour(Ingredient i)
    {
        switch (i)
        {
            case Ingredient.EspressoShot:    return Hex("#6B4430");
            case Ingredient.Water:           return Hex("#DCECF7");
            case Ingredient.Milk:            return Hex("#FFFAF2");
            case Ingredient.SteamedMilk:     return Hex("#FFF7EC");
            case Ingredient.Foam:            return Hex("#FFFAF2");
            case Ingredient.Ice:             return Hex("#E8F4F8");
            case Ingredient.TeaBrew:         return Hex("#A8633C");
            case Ingredient.Matcha:          return Hex("#9BBF6A");
            case Ingredient.Chocolate:       return Hex("#8A5A44");
            case Ingredient.VanillaSyrup:    return Hex("#F3E0B8");
            case Ingredient.CaramelSyrup:    return Hex("#C98B4B");
            case Ingredient.StrawberrySyrup: return Hex("#F0A0B0");
            case Ingredient.TaroSyrup:       return Hex("#C9B8E8");
            case Ingredient.BobaPearls:      return Hex("#4A3A3F");
            case Ingredient.WhippedCream:    return Hex("#FFFDF7");
        }
        return Color.white;
    }

    /// How much of the cup each addition fills, 0..1.
    public static float Volume(Ingredient i)
    {
        switch (i)
        {
            case Ingredient.EspressoShot: return 0.18f;
            case Ingredient.Water:        return 0.40f;
            case Ingredient.Milk:         return 0.42f;
            case Ingredient.SteamedMilk:  return 0.42f;
            case Ingredient.Foam:         return 0.14f;
            case Ingredient.Ice:          return 0.25f;
            case Ingredient.TeaBrew:      return 0.55f;
            case Ingredient.Matcha:       return 0.16f;
            case Ingredient.Chocolate:    return 0.16f;
            case Ingredient.BobaPearls:   return 0.18f;
            case Ingredient.WhippedCream: return 0.12f;
            default:                      return 0.08f;   // syrups
        }
    }

    static Color Hex(string h)
    {
        Color c;
        ColorUtility.TryParseHtmlString(h, out c);
        return c;
    }
}

[System.Serializable]
public class Recipe
{
    public string Name;
    public Ingredient[] Steps;      // what must end up in the cup
    public bool Iced;
    public string Hint;             // shown on the order ticket

    public Recipe(string name, bool iced, string hint, params Ingredient[] steps)
    {
        Name = name; Iced = iced; Hint = hint; Steps = steps;
    }

    public Color ResultColour()
    {
        // Weighted by volume, so a latte reads as milky coffee rather than mud.
        float tw = 0f; Vector3 acc = Vector3.zero;
        foreach (var s in Steps)
        {
            if (s == Ingredient.Ice) continue;          // ice does not tint
            float w = Ingredients.Volume(s);
            Color c = Ingredients.Colour(s);
            acc += new Vector3(c.r, c.g, c.b) * w;
            tw += w;
        }
        if (tw <= 0f) return Color.white;
        acc /= tw;
        return new Color(acc.x, acc.y, acc.z);
    }
}

public static class RecipeBook
{
    public static readonly Recipe[] All = new Recipe[]
    {
        new Recipe("Espresso", false, "short and dark",
            Ingredient.EspressoShot),

        new Recipe("Americano", false, "shot, then hot water",
            Ingredient.EspressoShot, Ingredient.Water),

        new Recipe("Latte", false, "shot with steamed milk",
            Ingredient.EspressoShot, Ingredient.SteamedMilk),

        new Recipe("Cappuccino", false, "shot, steamed milk, lots of foam",
            Ingredient.EspressoShot, Ingredient.SteamedMilk, Ingredient.Foam),

        new Recipe("Caramel Latte", false, "latte with caramel",
            Ingredient.EspressoShot, Ingredient.SteamedMilk, Ingredient.CaramelSyrup),

        new Recipe("Vanilla Latte", false, "latte with vanilla",
            Ingredient.EspressoShot, Ingredient.SteamedMilk, Ingredient.VanillaSyrup),

        new Recipe("Mocha", false, "shot, chocolate, steamed milk",
            Ingredient.EspressoShot, Ingredient.Chocolate, Ingredient.SteamedMilk),

        new Recipe("Iced Latte", true, "shot over milk and ice",
            Ingredient.EspressoShot, Ingredient.Milk, Ingredient.Ice),

        new Recipe("Matcha Latte", false, "matcha whisked with steamed milk",
            Ingredient.Matcha, Ingredient.SteamedMilk),

        new Recipe("Hot Tea", false, "just brewed tea",
            Ingredient.TeaBrew),

        new Recipe("Iced Tea", true, "brewed tea over ice",
            Ingredient.TeaBrew, Ingredient.Ice),

        new Recipe("Taro Boba", true, "taro, milk, pearls, ice",
            Ingredient.TaroSyrup, Ingredient.Milk, Ingredient.BobaPearls, Ingredient.Ice),

        new Recipe("Strawberry Milk", true, "strawberry, milk, ice",
            Ingredient.StrawberrySyrup, Ingredient.Milk, Ingredient.Ice),

        new Recipe("Hot Chocolate", false, "chocolate with steamed milk, cream on top",
            Ingredient.Chocolate, Ingredient.SteamedMilk, Ingredient.WhippedCream),
    };

    public static Recipe Random()
    {
        return All[UnityEngine.Random.Range(0, All.Length)];
    }

    public static Recipe ByName(string n)
    {
        foreach (var r in All) if (r.Name == n) return r;
        return null;
    }
}
