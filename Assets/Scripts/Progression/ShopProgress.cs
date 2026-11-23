using System;
using System.Collections.Generic;

/// The cafe's coins and installed upgrades. Plain data with one entry point per
/// change (AddCoins, TryBuy), so a networked host can own a single copy later and
/// clients send those two calls to it instead of changing it themselves.
public class ShopProgress
{
    /// Upgrades on one station stack up to this much ease, so a minigame never plays itself.
    public const float MaxEase = 0.8f;

    public event Action Changed;

    public int Coins { get; private set; }
    readonly HashSet<string> _owned = new HashSet<string>();

    public bool Owns(UpgradeDef u) { return u != null && _owned.Contains(u.Id); }

    /// Why this upgrade cannot be bought right now, in words for the screen; null if it can.
    public string WhyNot(UpgradeDef u)
    {
        if (u == null) return "Unknown upgrade";
        if (Owns(u)) return "Installed";
        if (u.Requires != null && !Owns(u.Requires)) return "Needs " + u.Requires.Title + " first";
        if (Coins < u.Cost) return "Needs " + (u.Cost - Coins) + " more coins";
        return null;
    }

    public bool TryBuy(UpgradeDef u)
    {
        if (WhyNot(u) != null) return false;
        Coins -= u.Cost;
        _owned.Add(u.Id);
        if (Changed != null) Changed();
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        Coins += amount;
        if (Changed != null) Changed();
    }

    /// Total ease the installed upgrades give one station, 0..MaxEase.
    public float Ease(UpgradeTarget target, IEnumerable<UpgradeDef> catalog)
    {
        float sum = 0f;
        if (catalog == null) return 0f;
        foreach (var u in catalog)
            if (u != null && u.Target == target && Owns(u)) sum += u.Ease;
        return Math.Min(MaxEase, Math.Max(0f, sum));
    }

    // ---------------------------------------------------------------- save

    /// "coins|id,id,id". Ids never contain '|' or ','.
    public string Serialize()
    {
        return Coins + "|" + string.Join(",", _owned);
    }

    public static ShopProgress Parse(string s)
    {
        var p = new ShopProgress();
        if (string.IsNullOrEmpty(s)) return p;
        var parts = s.Split('|');
        int coins;
        if (int.TryParse(parts[0], out coins)) p.Coins = Math.Max(0, coins);
        if (parts.Length > 1)
            foreach (var id in parts[1].Split(','))
                if (id.Length > 0) p._owned.Add(id);
        return p;
    }
}
