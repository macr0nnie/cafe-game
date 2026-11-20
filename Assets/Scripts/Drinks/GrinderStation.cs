using UnityEngine;

/// Grinds beans into the portafilter. Arms the espresso machine; does not touch
/// the cup, which is why the machine refuses to pull without it.
public class GrinderStation : StationBase
{
    public EspressoStation Machine;
    public Transform GroundsVisual;

    public override string Prompt(PlayerHands hands)
    {
        if (Machine == null) return "Grinder";
        if (Machine.HasGrounds) return "Basket is already loaded";
        return "Grind and dose";
    }

    public override bool CanInteract(PlayerHands hands)
    {
        return Machine != null && !Machine.HasGrounds;
    }

    public override void End(PlayerHands hands, float t01)
    {
        if (t01 >= 0.999f && Machine != null)
        {
            Machine.HasGrounds = true;
            if (Machine.GroundsVisual != null) Machine.GroundsVisual.gameObject.SetActive(true);
            if (GroundsVisual != null) GroundsVisual.gameObject.SetActive(true);
            CafeMessage.Show("Basket dosed - machine is ready");
        }
        base.End(hands, t01);
    }
}
