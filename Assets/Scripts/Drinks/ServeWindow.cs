using UnityEngine;

/// Hand the finished drink over.
public class ServeWindow : MonoBehaviour, IInteractable
{
    public float Progress { get { return 0f; } }
    public bool IsHold { get { return false; } }

    public string Prompt(PlayerHands hands)
    {
        if (hands == null || !hands.HasCup) return "Nothing to serve";
        if (hands.Cup.IsEmpty) return "The cup is empty";
        return "Serve: " + hands.Cup.ShortList();
    }

    public bool CanInteract(PlayerHands hands)
    {
        return hands != null && hands.HasCup && !hands.Cup.IsEmpty;
    }

    public void Begin(PlayerHands hands) { }
    public void Hold(PlayerHands hands, float t01) { }

    public void End(PlayerHands hands, float t01)
    {
        if (hands == null || !hands.HasCup) return;
        if (OrderManager.Instance != null)
        {
            // Cozy: a drink nobody ordered stays in your hands to fix or bin, it is never thrown away.
            if (!OrderManager.Instance.TryServe(hands.Cup)) return;
        }
        else CafeMessage.Show("Served: " + hands.Cup.Describe());
        hands.DestroyCup();
    }
}
