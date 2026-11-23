using NUnit.Framework;

/// Customers rate on the slot model. Every recipe made exactly must read as exact,
/// and the old model's blind spots (americano vs espresso, latte vs cappuccino) must not.
public class DrinkRatingTests
{
    [Test]
    public void EveryRecipeMadeExactly_IsExactAndFiveStars()
    {
        foreach (var r in RecipeBook.All)
        {
            var rating = DrinkRating.Rate(r.Steps, 1f, r, 0f, 60f);
            Assert.IsTrue(rating.Exact, r.Name + ": " + rating.Headline);
            Assert.AreEqual(5, rating.Stars, r.Name);
        }
    }

    [Test]
    public void OrderOfPouring_DoesNotChangeAccuracy()
    {
        var latte = RecipeBook.ByName("Iced Latte");
        var made = new[] { Ingredient.Ice, Ingredient.Milk, Ingredient.EspressoShot };
        Assert.IsTrue(DrinkRating.Rate(made, 1f, latte, 0f, 60f).Exact);
    }

    [Test]
    public void EspressoForAnAmericano_IsNotExact()
    {
        var r = DrinkRating.Rate(new[] { Ingredient.EspressoShot }, 1f, RecipeBook.ByName("Americano"), 0f, 60f);
        Assert.IsFalse(r.Exact);
        StringAssert.Contains("hot water", r.Headline);
    }

    [Test]
    public void LatteForACappuccino_NamesTheMissingFoam()
    {
        var made = new[] { Ingredient.EspressoShot, Ingredient.SteamedMilk };
        var r = DrinkRating.Rate(made, 1f, RecipeBook.ByName("Cappuccino"), 0f, 60f);
        Assert.IsFalse(r.Exact);
        StringAssert.Contains("foamed", r.Headline);
        Assert.Less(r.Stars, 5);
    }

    [Test]
    public void WrongDrink_CapsLowButNeverBelowOneStar()
    {
        var r = DrinkRating.Rate(new[] { Ingredient.TeaBrew }, 1f, RecipeBook.ByName("Taro Boba"), 0f, 60f);
        Assert.That(r.Stars, Is.InRange(1, 2));
        Assert.Greater(r.Tips, 0, "Cozy: a rating never takes money away.");
    }

    [Test]
    public void ExtraSyrupFlavour_ShowsUpAsWrongPumps()
    {
        var made = new[] { Ingredient.EspressoShot, Ingredient.SteamedMilk, Ingredient.CaramelSyrup, Ingredient.VanillaSyrup };
        Assert.IsFalse(DrinkRating.Rate(made, 1f, RecipeBook.ByName("Caramel Latte"), 0f, 60f).Exact);
    }

    [Test]
    public void Mocha_KeepsEspressoBaseWithChocolateSauce()
    {
        var b = DrinkSlots.FromIngredients(RecipeBook.ByName("Mocha").Steps);
        Assert.AreEqual(DrinkBase.Espresso, b.Base);
        Assert.AreEqual(Syrup.Chocolate, b.Syrup);
        Assert.AreEqual(DrinkBase.Chocolate, DrinkSlots.FromIngredients(RecipeBook.ByName("Hot Chocolate").Steps).Base);
    }

    [Test]
    public void Reaction_SaysTheStarsInWords()
    {
        var r = DrinkRating.Rate(RecipeBook.All[0].Steps, 1f, RecipeBook.All[0], 0f, 60f);
        StringAssert.Contains("5 / 5 stars", r.Reaction());
    }
}
