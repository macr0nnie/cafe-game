using UnityEngine;

/// Grinds beans into the portafilter. Arms the espresso machine; does not touch
/// the cup, which is why the machine refuses to pull without it.
/// Minigame: tap repeatedly to crank the grinder. Stop tapping and it spins down.
public class GrinderStation : StationBase
{
    public EspressoStation Machine;
    public Transform GroundsVisual;

    [Header("Crank")]
    public float PerTap = 0.2f;
    [Tooltip("Progress lost per second when you stop tapping.")]
    public float SpinDown = 0.25f;
    [Tooltip("Seconds without a tap before the grinder gives up.")]
    public float IdleTimeout = 1.4f;

    float _idle;

    public override string Hint { get { return "Tap E fast to grind"; } }

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

    public override void Begin(PlayerHands hands)
    {
        base.Begin(hands);
        Progress = PerTap;      // the press that started it counts
        _idle = 0f;
    }

    public override bool Tick(PlayerHands hands, bool held, bool pressed, float dt)
    {
        if (pressed) { Progress += PerTap; _idle = 0f; }
        else
        {
            _idle += dt;
            Progress = Mathf.Max(0f, Progress - SpinDown * dt);
        }

        if (Progress >= 1f) { End(hands, 1f); return true; }
        if (_idle > IdleTimeout) { End(hands, Progress); return true; }
        return false;
    }

    protected override void Update()
    {
        // The burr spins as fast as you crank it.
        if (_running && MovingPart != null)
            MovingPart.Rotate(MovingPartAxis, MovingPartSpeed * Mathf.Max(0f, 1f - _idle * 2f) * Time.deltaTime, Space.Self);
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
        else CafeMessage.Show("Grinder spun down - tap faster");
        base.End(hands, t01);
    }
}
