using UnityEngine;

/// Take a clean cup.
public class CupDispenser : MonoBehaviour, IInteractable
{
    public GameObject CupPrefab;
    public float Progress { get { return 0f; } }
    public bool IsHold { get { return false; } }

    public string Prompt(PlayerHands hands)
    {
        if (hands != null && hands.HasCup) return "Hands full";
        return "Take a cup";
    }

    public bool CanInteract(PlayerHands hands) { return hands != null && !hands.HasCup; }
    public void Begin(PlayerHands hands) { }
    public void Hold(PlayerHands hands, float t01) { }

    public void End(PlayerHands hands, float t01)
    {
        if (hands != null && !hands.HasCup) hands.TakeNewCup(CupPrefab);
    }
}
