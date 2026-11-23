using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// The drink guide's rules: step order, which order a cup belongs to, and where a cup can be set down.
public class DrinkGuideTests
{
    static Recipe R(string name) { return RecipeBook.ByName(name); }
    static List<Ingredient> Cup(params Ingredient[] i) { return new List<Ingredient>(i); }

    [Test]
    public void NoOrder_WaitsOrPointsAtTheTill()
    {
        Assert.AreEqual(GuideStepKind.WaitForOrder, DrinkGuide.Next(null, false, null, false, false).Kind);
        Assert.AreEqual(GuideStepKind.TakeOrder, DrinkGuide.Next(null, false, null, false, true).Kind);
    }

    [Test]
    public void Latte_GoesCupThenShotThenMilkThenServe()
    {
        var latte = R("Latte");
        var s = DrinkGuide.Next(latte, false, null, false, false);
        Assert.AreEqual(GuideStepKind.TakeCup, s.Kind);
        Assert.AreEqual(1, s.Index);
        Assert.AreEqual(4, s.Count);

        s = DrinkGuide.Next(latte, true, Cup(), false, false);
        Assert.AreEqual(GuideStepKind.Add, s.Kind);
        Assert.AreEqual(Ingredient.EspressoShot, s.Ingredient);
        Assert.AreEqual(2, s.Index);

        s = DrinkGuide.Next(latte, true, Cup(Ingredient.EspressoShot), false, false);
        Assert.AreEqual(Ingredient.SteamedMilk, s.Ingredient);
        Assert.AreEqual(3, s.Index);

        s = DrinkGuide.Next(latte, true, Cup(Ingredient.EspressoShot, Ingredient.SteamedMilk), false, false);
        Assert.AreEqual(GuideStepKind.Serve, s.Kind);
        Assert.AreEqual(4, s.Index);
    }

    [Test]
    public void IcedDrinks_PutIceInFirst()
    {
        // TechniqueNote calls ice after milk a mistake, so the guide must never lead you there.
        foreach (var r in RecipeBook.All)
        {
            var order = DrinkGuide.Ordered(r);
            int ice = order.IndexOf(Ingredient.Ice);
            int milk = order.FindIndex(i => i == Ingredient.Milk || i == Ingredient.SteamedMilk || i == Ingredient.Foam);
            if (ice >= 0 && milk >= 0) Assert.Less(ice, milk, r.Name);
        }
    }

    [Test]
    public void FollowingTheGuide_NeverEarnsATechniqueComplaint()
    {
        foreach (var r in RecipeBook.All)
        {
            var go = new GameObject("cup");
            try
            {
                var cup = go.AddComponent<CupContents>();
                foreach (var i in DrinkGuide.Ordered(r)) cup.Added.Add(i);
                string note = cup.TechniqueNote();
                Assert.IsFalse(note != null && (note.Contains("after the milk") || note.Contains("melts fast")), r.Name + ": " + note);
                Assert.That(cup.ScoreAgainst(r), Is.EqualTo(1f).Within(1e-4f), r.Name);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }

    [Test]
    public void CupSetDown_PointsBackToIt()
    {
        Assert.AreEqual(GuideStepKind.PickUpCup, DrinkGuide.Next(R("Latte"), false, null, true, false).Kind);
        Assert.AreEqual(GuideStepKind.PickUpCup, DrinkGuide.Next(null, false, null, true, true).Kind);
    }

    [Test]
    public void Pick_EmptyCupTakesOldest_PartCupTakesBestMatch()
    {
        var open = new List<Recipe> { R("Hot Tea"), R("Latte"), R("Espresso") };
        Assert.AreEqual(0, DrinkGuide.Pick(open, null));
        Assert.AreEqual(1, DrinkGuide.Pick(open, Cup(Ingredient.EspressoShot, Ingredient.SteamedMilk)));
        Assert.AreEqual(-1, DrinkGuide.Pick(new List<Recipe>(), null));
    }

    [Test]
    public void HasExtra_SpotsIngredientsTheOrderDoesNotWant()
    {
        Assert.IsFalse(DrinkGuide.HasExtra(Cup(Ingredient.EspressoShot), R("Latte")));
        Assert.IsTrue(DrinkGuide.HasExtra(Cup(Ingredient.TeaBrew), R("Latte")));
    }

    [Test]
    public void Describe_SaysTheStepInWords()
    {
        var s = DrinkGuide.Next(R("Latte"), true, Cup(), false, false);
        Assert.AreEqual("Latte  ·  step 2 of 4:  add espresso shot", DrinkGuide.Describe(s, "Latte"));
    }

    [Test]
    public void PutDown_OnlyOnFlatSurfacesAtCounterHeight()
    {
        // feet at 0, eyes at 1.63
        Assert.IsTrue(PutDownSpot.Settable(Vector3.up, 0.91f, 0f, 1.63f));      // counter
        Assert.IsFalse(PutDownSpot.Settable(Vector3.up, 0.02f, 0f, 1.63f));     // floor
        Assert.IsFalse(PutDownSpot.Settable(Vector3.forward, 0.91f, 0f, 1.63f)); // wall
        Assert.IsFalse(PutDownSpot.Settable(Vector3.up, 1.55f, 0f, 1.63f));     // shelf at eye level
    }
}
