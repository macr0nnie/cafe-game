using UnityEngine;

/// The cafe's shop progress and what it does to each minigame. Stations ask here
/// how forgiving to be; the shop computer sets the catalogue and sells upgrades.
/// Upgrades only ever widen windows or speed things up, so orders still never fail.
public static class CafeUpgrades
{
    const string SaveKey = "shop.progress";

    static ShopProgress _progress;

    /// What the shop sells. Set by the shop computer when the scene loads.
    public static UpgradeCatalog Catalog;

    public static ShopProgress Progress
    {
        get
        {
            if (_progress != null) return _progress;
            _progress = ShopProgress.Parse(PlayerPrefs.GetString(SaveKey, ""));
            _progress.Changed += () => { PlayerPrefs.SetString(SaveKey, _progress.Serialize()); PlayerPrefs.Save(); };
            return _progress;
        }
    }

    public static float Ease(UpgradeTarget t)
    {
        return Catalog == null ? 0f : Progress.Ease(t, Catalog.All);
    }

    /// The station's band with this station's upgrades applied.
    public static Vector2 Widen(Vector2 band, UpgradeTarget t) { return Widen(band, Ease(t)); }

    /// Each side of the band grows by up to 0.1 of the ring at full ease. A zero band stays zero.
    public static Vector2 Widen(Vector2 band, float ease)
    {
        if (band == Vector2.zero || ease <= 0f) return band;
        float grow = 0.1f * Mathf.Clamp01(ease);
        return new Vector2(Mathf.Max(0.05f, band.x - grow), Mathf.Min(0.98f, band.y + grow));
    }

    /// Grinder: each tap grinds more (up to double) at full ease.
    public static float GrindPerTap(float perTap, float ease) { return perTap * (1f + Mathf.Clamp01(ease)); }

    /// Grinder: it spins down more slowly (down to 40%) at full ease.
    public static float GrindSpinDown(float spinDown, float ease) { return spinDown * (1f - 0.6f * Mathf.Clamp01(ease)); }

    /// Words for the screen: what an upgrade on this station does.
    public static string Effect(UpgradeTarget t)
    {
        switch (t)
        {
            case UpgradeTarget.Espresso:  return "Wider golden-crema window";
            case UpgradeTarget.Grinder:   return "More grind per tap, slower spin-down";
            case UpgradeTarget.SteamWand: return "Wider silky-milk window";
            default:                      return "Wider line on milk, water and tea pours";
        }
    }
}
