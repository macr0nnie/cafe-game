using UnityEngine;

/// The little computer by the register. Use it to open the shop screen, where
/// tips buy kitchen upgrades (and, later, friends join the cafe).
/// It also banks every tip into the shop's coins.
public class UpgradeComputer : MonoBehaviour, IInteractable
{
    public UpgradeCatalog Catalog;

    public float Progress { get { return 0f; } }
    public bool IsHold { get { return false; } }

    void Awake() { if (Catalog != null) CafeUpgrades.Catalog = Catalog; }

    void OnEnable()  { CafeEvents.OrderServed += OnServed; }
    void OnDisable() { CafeEvents.OrderServed -= OnServed; }

    void OnServed(Recipe r, int stars, int tips) { CafeUpgrades.Progress.AddCoins(tips); }

    public string Prompt(PlayerHands hands) { return "Use the shop computer (" + CafeUpgrades.Progress.Coins + " coins)"; }
    public bool CanInteract(PlayerHands hands) { return true; }
    public void Begin(PlayerHands hands) { }
    public void Hold(PlayerHands hands, float t01) { }
    public void End(PlayerHands hands, float t01) { CafeEvents.RaiseComputerOpened(); }
}
