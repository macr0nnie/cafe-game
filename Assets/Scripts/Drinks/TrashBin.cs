using UnityEngine;

/// Tip it out and start again.
public class TrashBin : MonoBehaviour, IInteractable
{
    public float Progress { get { return 0f; } }
    public bool IsHold { get { return false; } }

    public string Prompt(PlayerHands hands)
    {
        if (hands == null || !hands.HasCup) return "Nothing to bin";
        return "Tip it out";
    }

    public bool CanInteract(PlayerHands hands) { return hands != null && hands.HasCup; }
    public void Begin(PlayerHands hands) { }
    public void Hold(PlayerHands hands, float t01) { }
    public void End(PlayerHands hands, float t01) { if (hands != null) hands.DestroyCup(); }
}
