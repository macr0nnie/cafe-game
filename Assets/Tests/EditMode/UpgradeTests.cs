using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// Shop progress, upgrade catalogue and how upgrades ease each minigame.
public class UpgradeTests
{
    readonly List<Object> _made = new List<Object>();

    UpgradeDef Def(string id, int cost, UpgradeTarget t, float ease = 0.4f, UpgradeDef requires = null)
    {
        var u = ScriptableObject.CreateInstance<UpgradeDef>();
        u.Id = id; u.Title = id; u.Cost = cost; u.Target = t; u.Ease = ease; u.Requires = requires;
        _made.Add(u);
        return u;
    }

    [TearDown]
    public void CleanUp()
    {
        foreach (var o in _made) Object.DestroyImmediate(o);
        _made.Clear();
    }

    [Test]
    public void Buy_WithEnoughCoins_SpendsAndInstalls()
    {
        var p = new ShopProgress();
        var u = Def("a", 40, UpgradeTarget.Grinder);
        int changed = 0;
        p.Changed += () => changed++;
        p.AddCoins(50);

        Assert.IsTrue(p.TryBuy(u));
        Assert.AreEqual(10, p.Coins);
        Assert.IsTrue(p.Owns(u));
        Assert.AreEqual(2, changed);
        Assert.AreEqual("Installed", p.WhyNot(u));
        Assert.IsFalse(p.TryBuy(u), "Buying twice must not charge again");
        Assert.AreEqual(10, p.Coins);
    }

    [Test]
    public void Buy_RefusesWhenShortOrMissingPrerequisite()
    {
        var p = new ShopProgress();
        var t1 = Def("t1", 40, UpgradeTarget.Pour);
        var t2 = Def("t2", 10, UpgradeTarget.Pour, 0.4f, t1);
        p.AddCoins(30);

        Assert.IsFalse(p.TryBuy(t1));
        StringAssert.Contains("10 more coins", p.WhyNot(t1));
        Assert.IsFalse(p.TryBuy(t2));
        StringAssert.Contains("first", p.WhyNot(t2));
        Assert.AreEqual(30, p.Coins);
    }

    [Test]
    public void AddCoins_IgnoresZeroAndNegative()
    {
        var p = new ShopProgress();
        p.AddCoins(-5); p.AddCoins(0);
        Assert.AreEqual(0, p.Coins);
    }

    [Test]
    public void Save_RoundTrips()
    {
        var p = new ShopProgress();
        var a = Def("espresso_1", 10, UpgradeTarget.Espresso);
        var b = Def("pour_1", 10, UpgradeTarget.Pour);
        p.AddCoins(55);
        p.TryBuy(a); p.TryBuy(b);

        var back = ShopProgress.Parse(p.Serialize());
        Assert.AreEqual(35, back.Coins);
        Assert.IsTrue(back.Owns(a) && back.Owns(b));
        Assert.AreEqual(0, ShopProgress.Parse("").Coins);
        Assert.AreEqual(0, ShopProgress.Parse("garbage").Coins);
    }

    [Test]
    public void Ease_SumsPerStationAndCaps()
    {
        var p = new ShopProgress();
        var all = new[] { Def("g1", 0, UpgradeTarget.Grinder, 0.5f), Def("g2", 0, UpgradeTarget.Grinder, 0.5f),
                          Def("e1", 0, UpgradeTarget.Espresso, 0.3f) };
        foreach (var u in all) p.TryBuy(u);

        Assert.AreEqual(ShopProgress.MaxEase, p.Ease(UpgradeTarget.Grinder, all), 1e-5f);
        Assert.AreEqual(0.3f, p.Ease(UpgradeTarget.Espresso, all), 1e-5f);
        Assert.AreEqual(0f, p.Ease(UpgradeTarget.SteamWand, all));
        Assert.AreEqual(0f, p.Ease(UpgradeTarget.Grinder, null));
    }

    [Test]
    public void Widen_OnlyEverMakesTheBandEasier()
    {
        var band = new Vector2(0.55f, 0.80f);
        Assert.AreEqual(band, CafeUpgrades.Widen(band, 0f));
        for (float e = 0.1f; e <= 1f; e += 0.1f)
        {
            var w = CafeUpgrades.Widen(band, e);
            Assert.LessOrEqual(w.x, band.x);
            Assert.GreaterOrEqual(w.y, band.y);
            Assert.GreaterOrEqual(w.x, 0.05f);
            Assert.LessOrEqual(w.y, 0.98f);
        }
        Assert.AreEqual(Vector2.zero, CafeUpgrades.Widen(Vector2.zero, 1f), "Stations with no band keep none");
    }

    [Test]
    public void Grinder_FewerTapsWhenUpgraded()
    {
        Assert.AreEqual(0.2f, CafeUpgrades.GrindPerTap(0.2f, 0f), 1e-5f);
        Assert.Greater(CafeUpgrades.GrindPerTap(0.2f, 0.8f), 0.2f);
        Assert.Less(CafeUpgrades.GrindSpinDown(0.25f, 0.8f), 0.25f);
        Assert.Greater(CafeUpgrades.GrindSpinDown(0.25f, 1f), 0f, "It still spins down a little");
    }

    [Test]
    public void Catalogue_IdsUniqueAndTiersChainWithinOneStation()
    {
        var ids = new HashSet<string>();
        var byId = new Dictionary<string, CafeUpgradeLibrary.Spec>();
        foreach (var s in CafeUpgradeLibrary.Specs)
        {
            Assert.IsFalse(string.IsNullOrEmpty(s.Id));
            Assert.IsTrue(ids.Add(s.Id), "Duplicate upgrade id " + s.Id);
            Assert.IsFalse(s.Id.Contains(",") || s.Id.Contains("|"), "Ids are save keys and cannot hold , or |");
            Assert.Greater(s.Cost, 0);
            if (s.Requires != null)
            {
                Assert.IsTrue(byId.ContainsKey(s.Requires), s.Id + " must come after " + s.Requires);
                Assert.AreEqual(byId[s.Requires].Target, s.Target);
                Assert.Greater(s.Cost, byId[s.Requires].Cost);
            }
            byId[s.Id] = s;
        }
    }
}
