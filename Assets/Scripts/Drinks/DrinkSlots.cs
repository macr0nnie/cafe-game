using System.Collections.Generic;

/// Bridges the ingredient list the stations still fill to the slot model that scoring
/// reads. Orders and cups both go through the same mapping, so a cup that holds what a
/// recipe lists always reads as an exact match.
// shortcut: cups have no size or latte art yet, so both default (Medium, Plain) on both
// sides; drop this bridge once stations write a DrinkBuild directly.
public static class DrinkSlots
{
    public static DrinkBuild FromIngredients(IEnumerable<Ingredient> added)
    {
        var b = new DrinkBuild { Base = DrinkBase.None };
        bool chocolate = false;
        if (added == null) return b;

        foreach (var i in added)
        {
            switch (i)
            {
                case Ingredient.EspressoShot: b.Shots++; break;
                case Ingredient.Water:        b.Water = true; break;
                case Ingredient.TeaBrew:      SetBase(b, DrinkBase.Tea); break;
                case Ingredient.Matcha:       SetBase(b, DrinkBase.Matcha); break;
                case Ingredient.Chocolate:    chocolate = true; break;
                case Ingredient.Milk:         Milk(b, MilkTex.Cold); break;
                case Ingredient.SteamedMilk:  Milk(b, MilkTex.Steamed); break;
                case Ingredient.Foam:         Milk(b, MilkTex.Foamed); break;
                case Ingredient.Ice:          b.Ice = IceLevel.Full; break;
                case Ingredient.WhippedCream: b.Toppings |= Topping.Cream; break;
                case Ingredient.BobaPearls:   b.Toppings |= Topping.Boba; break;
                case Ingredient.VanillaSyrup:    Pump(b, Syrup.Vanilla); break;
                case Ingredient.CaramelSyrup:    Pump(b, Syrup.Caramel); break;
                case Ingredient.StrawberrySyrup: Pump(b, Syrup.Strawberry); break;
                case Ingredient.TaroSyrup:       Pump(b, Syrup.Taro); break;
            }
        }

        // Espresso wins the base; chocolate with a shot is mocha sauce, on its own it is hot chocolate.
        if (b.Shots > 0) { b.Base = DrinkBase.Espresso; b.BaseSet = true; }
        if (chocolate)
        {
            if (b.BaseSet) Pump(b, Syrup.Chocolate);
            else SetBase(b, DrinkBase.Chocolate);
        }
        return b;
    }

    /// The order a customer places, in slots.
    public static DrinkSpec SpecFor(Recipe r)
    {
        var b = FromIngredients(r == null ? null : r.Steps);
        return new DrinkSpec
        {
            Name = r == null ? "" : r.Name,
            Size = b.Size, Base = b.Base, Shots = b.Shots, Water = b.Water,
            Milk = b.Milk, Texture = b.Texture, Syrup = b.Syrup, Pumps = b.Pumps,
            Ice = b.Ice, Toppings = b.Toppings, Art = b.Art,
        };
    }

    static void SetBase(DrinkBuild b, DrinkBase v)
    {
        if (b.BaseSet && b.Base != DrinkBase.None) return;   // first base poured stays the base
        b.Base = v; b.BaseSet = true;
    }

    static void Milk(DrinkBuild b, MilkTex t)
    {
        if (b.Milk == MilkType.None) b.Milk = MilkType.Whole;
        // Foam on steamed milk makes it a foamed drink; plain milk never undoes steaming.
        if (t > b.Texture) b.Texture = t;
    }

    /// One slot for syrup: the first flavour names it, every pump counts, so a second
    /// flavour shows up as too many pumps instead of vanishing.
    static void Pump(DrinkBuild b, Syrup s)
    {
        if (b.Syrup == Syrup.None) b.Syrup = s;
        b.Pumps++;
    }
}
