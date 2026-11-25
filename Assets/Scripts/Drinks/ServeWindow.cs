using UnityEngine;

/// Hand the finished drink over. With customers, the cup is set down on the tray
/// for its customer to collect; without, it is handed straight over.
public class ServeWindow : MonoBehaviour, IInteractable
{
    [Tooltip("Where served cups wait for pickup. Cups line up along its X axis.")]
    public Transform CupRest;
    public float CupSpacing = 0.14f;
    public int CupsPerRow = 4;

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
        var mgr = OrderManager.Instance;
        if (mgr != null)
        {
            // Cozy: a drink nobody ordered stays in your hands to fix or bin, it is never thrown away.
            if (!mgr.TryServe(hands.Cup)) return;
            if (mgr.CustomersOrder && CupRest != null)
            {
                SetDown(hands.ReleaseCup());
                return;
            }
        }
        else CafeMessage.Show("Served: " + hands.Cup.Describe());
        hands.DestroyCup();
    }

    void SetDown(GameObject cup)
    {
        if (cup == null) return;
        int slot = FreeSlot();
        cup.transform.SetParent(CupRest, true);
        cup.transform.localPosition = SlotPosition(slot);
        cup.transform.localRotation = Quaternion.identity;
    }

    Vector3 SlotPosition(int i)
    {
        float x = ((i % CupsPerRow) - (CupsPerRow - 1) * 0.5f) * CupSpacing;
        return new Vector3(x, 0f, (i / CupsPerRow) * CupSpacing);
    }

    /// First spot on the tray with no cup on it, so collected cups leave gaps that get reused.
    int FreeSlot()
    {
        for (int i = 0; ; i++)
        {
            Vector3 p = SlotPosition(i);
            bool taken = false;
            foreach (Transform c in CupRest)
                if ((c.localPosition - p).sqrMagnitude < CupSpacing * CupSpacing * 0.25f) { taken = true; break; }
            if (!taken) return i;
        }
    }
}
