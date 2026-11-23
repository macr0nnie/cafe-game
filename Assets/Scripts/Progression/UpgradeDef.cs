using UnityEngine;

/// Which minigame an upgrade makes easier.
public enum UpgradeTarget
{
    Espresso,   // wider crema band
    Grinder,    // more grind per tap, slower spin-down
    SteamWand,  // wider silky-milk band
    Pour,       // wider line on every pour station (milk, water, tea)
}

/// One kitchen upgrade, bought at the shop computer. Pure data: the stations read
/// how much easier they are through CafeUpgrades, so new upgrades need no code.
[CreateAssetMenu(menuName = "Cafe/Upgrade", fileName = "Upgrade")]
public class UpgradeDef : ScriptableObject
{
    [Tooltip("Stable save key. Never rename once shipped.")]
    public string Id;
    public string Title;
    [TextArea] public string Description;
    public int Cost = 60;
    public UpgradeTarget Target;
    [Tooltip("How much easier, 0..1. Several upgrades on one station add up, capped at ShopProgress.MaxEase.")]
    [Range(0f, 1f)] public float Ease = 0.4f;
    [Tooltip("Must be installed first (the previous tier), or none.")]
    public UpgradeDef Requires;
}
