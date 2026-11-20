using System.Collections.Generic;
using UnityEngine;

/// Display names and colours for every slot value. One place, so the ticket,
/// the HUD and the scoring card all read the same.
public static class DrinkWords
{
    public static string Size(CupSize s)
    {
        switch (s) { case CupSize.Small: return "Small";
                     case CupSize.Large: return "Large"; }
        return "Medium";
    }

    public static string Base(DrinkBase b)
    {
        switch (b) { case DrinkBase.Tea: return "tea";
                     case DrinkBase.Matcha: return "matcha";
                     case DrinkBase.Chocolate: return "chocolate"; }
        return "espresso";
    }

    public static string Milk(MilkType m)
    {
        switch (m) { case MilkType.Oat: return "oat milk";
                     case MilkType.Almond: return "almond milk";
                     case MilkType.Whole: return "whole milk"; }
        return "no milk";
    }

    public static string Tex(MilkTex t)
    {
        switch (t) { case MilkTex.Cold: return "cold";
                     case MilkTex.Steamed: return "steamed";
                     case MilkTex.Foamed: return "foamed"; }
        return "";
    }

    public static string Syrup(Syrup s)
    {
        switch (s) { case global::Syrup.Vanilla: return "vanilla";
                     case global::Syrup.Caramel: return "caramel";
                     case global::Syrup.Strawberry: return "strawberry";
                     case global::Syrup.Taro: return "taro";
                     case global::Syrup.Hazelnut: return "hazelnut"; }
        return "no syrup";
    }

    public static string Ice(IceLevel i)
    {
        switch (i) { case IceLevel.Light: return "light ice";
                     case IceLevel.Full: return "full ice"; }
        return "no ice";
    }

    public static string Art(LatteArt a)
    {
        switch (a) { case LatteArt.Heart: return "heart";
                     case LatteArt.Rosetta: return "rosetta"; }
        return "plain";
    }

    public static List<string> SplitToppings(Topping t)
    {
        var l = new List<string>();
        if ((t & Topping.Cream)    != 0) l.Add("whipped cream");
        if ((t & Topping.Cocoa)    != 0) l.Add("cocoa dust");
        if ((t & Topping.Cinnamon) != 0) l.Add("cinnamon");
        if ((t & Topping.Drizzle)  != 0) l.Add("caramel drizzle");
        if ((t & Topping.Boba)     != 0) l.Add("boba");
        return l;
    }

    // ---------------------------------------------------------------- colour

    static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString(h, out c); return c; }

    public static Color BaseColour(DrinkBase b)
    {
        switch (b) { case DrinkBase.Tea: return Hex("#A8633C");
                     case DrinkBase.Matcha: return Hex("#9BBF6A");
                     case DrinkBase.Chocolate: return Hex("#8A5A44"); }
        return Hex("#6B4430");
    }

    public static Color MilkColour(MilkType m)
    {
        switch (m) { case MilkType.Oat: return Hex("#F3E6D2");
                     case MilkType.Almond: return Hex("#F6EEDF"); }
        return Hex("#FFFAF2");
    }

    public static Color SyrupColour(Syrup s)
    {
        switch (s) { case global::Syrup.Vanilla: return Hex("#F3E0B8");
                     case global::Syrup.Caramel: return Hex("#C98B4B");
                     case global::Syrup.Strawberry: return Hex("#F0A0B0");
                     case global::Syrup.Taro: return Hex("#C9B8E8");
                     case global::Syrup.Hazelnut: return Hex("#C9A074"); }
        return Color.white;
    }

    /// How full the cup is, roughly, for the liquid visual.
    public static float Volume(CupSize s)
    {
        switch (s) { case CupSize.Small: return 0.72f;
                     case CupSize.Large: return 1.0f; }
        return 0.88f;
    }

    /// Blend the build into one liquid colour, weighted so a latte reads as
    /// milky coffee rather than mud. Ice does not tint.
    public static Color BlendColour(DrinkBuild b)
    {
        float w = 0f; Vector3 acc = Vector3.zero;

        if (b.BaseSet)
        {
            float bw = b.Base == DrinkBase.Espresso ? 0.20f * Mathf.Max(1, b.Shots) : 0.45f;
            Color c = BaseColour(b.Base);
            acc += new Vector3(c.r, c.g, c.b) * bw; w += bw;
        }
        if (b.Milk != MilkType.None)
        {
            float mw = b.Texture == MilkTex.Foamed ? 0.30f : 0.45f;
            Color c = MilkColour(b.Milk);
            acc += new Vector3(c.r, c.g, c.b) * mw; w += mw;
        }
        if (b.Syrup != global::Syrup.None && b.Pumps > 0)
        {
            float sw = 0.06f * b.Pumps;
            Color c = SyrupColour(b.Syrup);
            acc += new Vector3(c.r, c.g, c.b) * sw; w += sw;
        }

        if (w <= 0f) return Color.white;
        acc /= w;
        return new Color(acc.x, acc.y, acc.z);
    }
}
