using System.Collections.Generic;
using System.Text;
using UnityEngine;

// A drink is a set of slots, not a fixed ingredient list. Two latte orders are
// rarely the same drink, which is where the variety comes from.

public enum CupSize   { Small, Medium, Large }
public enum DrinkBase { Espresso, Tea, Matcha, Chocolate }
public enum MilkType  { None, Whole, Oat, Almond }
public enum MilkTex   { None, Cold, Steamed, Foamed }
public enum Syrup     { None, Vanilla, Caramel, Strawberry, Taro, Hazelnut }
public enum IceLevel  { None, Light, Full }
public enum LatteArt  { Plain, Heart, Rosetta }

[System.Flags]
public enum Topping
{
    None       = 0,
    Cream      = 1 << 0,
    Cocoa      = 1 << 1,
    Cinnamon   = 1 << 2,
    Drizzle    = 1 << 3,
    Boba       = 1 << 4,
}

/// What the customer asked for.
[System.Serializable]
public class DrinkSpec
{
    public string Name = "Latte";
    public CupSize Size = CupSize.Medium;
    public DrinkBase Base = DrinkBase.Espresso;
    public int Shots = 1;                       // espresso only
    public MilkType Milk = MilkType.Whole;
    public MilkTex Texture = MilkTex.Steamed;
    public Syrup Syrup = Syrup.None;
    public int Pumps = 0;
    public IceLevel Ice = IceLevel.None;
    public Topping Toppings = Topping.None;
    public LatteArt Art = LatteArt.Plain;

    public bool IsIced { get { return Ice != IceLevel.None; } }

    /// The lines that go on the paper ticket, under the drink name.
    public List<string> TicketLines()
    {
        var l = new List<string>();
        if (Base == DrinkBase.Espresso && Shots > 0)
            l.Add(Shots + (Shots == 1 ? " shot" : " shots"));
        if (Milk != MilkType.None)
        {
            string m = DrinkWords.Milk(Milk);
            if (Texture == MilkTex.Cold) m = "cold " + m;
            else if (Texture == MilkTex.Foamed) m = "extra foam";
            l.Add(m);
        }
        if (Syrup != Syrup.None && Pumps > 0)
            l.Add(Pumps + (Pumps == 1 ? " pump " : " pumps ") + DrinkWords.Syrup(Syrup));
        if (Ice != IceLevel.None)
            l.Add(DrinkWords.Ice(Ice));
        foreach (var t in DrinkWords.SplitToppings(Toppings))
            l.Add(t);
        if (Art != LatteArt.Plain)
            l.Add(DrinkWords.Art(Art));
        return l;
    }

    public string FullName()
    {
        var sb = new StringBuilder();
        sb.Append(DrinkWords.Size(Size)).Append(' ');
        if (IsIced) sb.Append("Iced ");
        sb.Append(Name);
        return sb.ToString();
    }
}

/// What the player actually built. Same slots, filled in as they work.
[System.Serializable]
public class DrinkBuild
{
    public CupSize Size = CupSize.Medium;
    public DrinkBase Base = DrinkBase.Espresso;
    public int Shots;
    public MilkType Milk = MilkType.None;
    public MilkTex Texture = MilkTex.None;
    public Syrup Syrup = Syrup.None;
    public int Pumps;
    public IceLevel Ice = IceLevel.None;
    public Topping Toppings = Topping.None;
    public LatteArt Art = LatteArt.Plain;

    public bool BaseSet;        // a base was actually pulled or brewed

    /// Execution quality, one entry per station used. Averaged for the second axis.
    readonly List<float> _quality = new List<float>();

    public void RecordQuality(float q) { _quality.Add(Mathf.Clamp01(q)); }

    public float ExecutionQuality
    {
        get
        {
            if (_quality.Count == 0) return 0f;
            float s = 0f;
            foreach (var q in _quality) s += q;
            return s / _quality.Count;
        }
    }

    public int StepsTaken { get { return _quality.Count; } }

    public bool IsEmpty
    {
        get
        {
            return !BaseSet && Milk == MilkType.None && Syrup == Syrup.None
                   && Ice == IceLevel.None && Toppings == Topping.None;
        }
    }

    public void Clear()
    {
        Shots = 0; BaseSet = false;
        Milk = MilkType.None; Texture = MilkTex.None;
        Syrup = Syrup.None; Pumps = 0;
        Ice = IceLevel.None; Toppings = Topping.None; Art = LatteArt.Plain;
        _quality.Clear();
    }

    public string ShortList()
    {
        var parts = new List<string>();
        if (Shots > 0) parts.Add(Shots + (Shots == 1 ? " shot" : " shots"));
        else if (BaseSet) parts.Add(DrinkWords.Base(Base));
        if (Milk != MilkType.None) parts.Add(DrinkWords.Tex(Texture) + " " + DrinkWords.Milk(Milk));
        if (Syrup != Syrup.None && Pumps > 0) parts.Add(Pumps + "x " + DrinkWords.Syrup(Syrup));
        if (Ice != IceLevel.None) parts.Add(DrinkWords.Ice(Ice));
        foreach (var t in DrinkWords.SplitToppings(Toppings)) parts.Add(t);
        if (parts.Count == 0) return "empty";
        return string.Join(" + ", parts.ToArray());
    }
}
