using UnityEngine;

/// Hold to steam. Release in the sweet band for silky microfoam, overshoot for
/// stiff foam, let go early and it is only warm milk. The skill beat of the loop.
public class SteamWandStation : StationBase
{
    [Header("Timing bands (fraction of Duration)")]
    [Range(0f, 1f)] public float SweetStart = 0.55f;
    [Range(0f, 1f)] public float SweetEnd = 0.80f;

    [Header("Wand")]
    public Transform Wand;
    public Vector3 WandDownEuler = new Vector3(26f, 0f, 0f);
    Quaternion _wandRest;

    public override bool IsHold { get { return true; } }

    void Start() { if (Wand != null) _wandRest = Wand.localRotation; }

    public override string Prompt(PlayerHands hands)
    {
        if (hands == null || !hands.HasCup) return "Need a cup first";
        if (!hands.Cup.CanAdd(Ingredient.SteamedMilk) && !hands.Cup.CanAdd(Ingredient.Foam)
            && !hands.Cup.CanAdd(Ingredient.Milk))
            return "This cup already has milk";
        return "Hold to steam - release when it is glossy";
    }

    public override bool CanInteract(PlayerHands hands)
    {
        return hands != null && hands.HasCup
               && (hands.Cup.CanAdd(Ingredient.SteamedMilk)
                || hands.Cup.CanAdd(Ingredient.Foam)
                || hands.Cup.CanAdd(Ingredient.Milk));
    }

    public override void Begin(PlayerHands hands)
    {
        base.Begin(hands);
        if (Wand != null) Wand.localRotation = _wandRest * Quaternion.Euler(WandDownEuler);
    }

    public override void End(PlayerHands hands, float t01)
    {
        if (Wand != null) Wand.localRotation = _wandRest;

        if (hands != null && hands.HasCup)
        {
            var cup = hands.Cup;
            if (t01 >= SweetEnd)
            {
                if (cup.CanAdd(Ingredient.Foam)) cup.Add(Ingredient.Foam);
                CafeMessage.Show("Over-steamed - stiff, bubbly foam", 2.6f);
            }
            else if (t01 >= SweetStart)
            {
                if (cup.CanAdd(Ingredient.SteamedMilk)) cup.Add(Ingredient.SteamedMilk);
                CafeMessage.Show("Silky microfoam - perfect", 2.6f);
            }
            else if (t01 > 0.12f)
            {
                if (cup.CanAdd(Ingredient.Milk)) cup.Add(Ingredient.Milk);
                CafeMessage.Show("Pulled off early - barely warm milk", 2.6f);
            }
        }
        base.End(hands, t01);
    }
}
