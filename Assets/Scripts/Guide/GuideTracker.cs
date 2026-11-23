using System.Collections.Generic;
using UnityEngine;

/// Works out this player's next drink-making step and which things in the cafe
/// do it. State only: the HUD and the highlighter read it, nothing here draws.
/// One per player, so a second barista gets their own guide.
public class GuideTracker : MonoBehaviour
{
    public PlayerHands Hands;
    [Tooltip("How far to look for the thing that does the next step.")]
    public float SearchRadius = 20f;

    /// The order this player is working on, or null.
    public OrderTicket Target { get; private set; }
    public GuideStep Step { get; private set; }
    public string Text { get { return DrinkGuide.Describe(Step, Target != null ? Target.Recipe.Name : ""); } }

    /// The nearest thing that does the next step, for the HUD marker. Null when none.
    public Transform NextTarget { get; private set; }

    readonly List<Recipe> _recipes = new List<Recipe>();
    readonly Collider[] _hits = new Collider[512];
    float _nextSearch;

    void Awake() { if (Hands == null) Hands = GetComponent<PlayerHands>(); }

    void Update()
    {
        var om = OrderManager.Instance;
        var open = om != null ? om.Open : null;
        var inCup = Hands != null && Hands.HasCup ? Hands.Cup.Added : null;

        if (Target != null && (open == null || !open.Contains(Target))) Target = null;
        if (open != null && open.Count > 0
            && (Target == null || DrinkGuide.HasExtra(inCup, Target.Recipe)))
        {
            _recipes.Clear();
            foreach (var t in open) _recipes.Add(t.Recipe);
            int i = DrinkGuide.Pick(_recipes, inCup);
            Target = i >= 0 ? open[i] : null;
        }

        bool customerWaiting = false;
        if (Target == null)
            foreach (var c in Customer.All)
                if (c.CanInteract(Hands)) { customerWaiting = true; break; }

        Step = DrinkGuide.Next(Target != null ? Target.Recipe : null, Hands != null && Hands.HasCup,
                               inCup, LooseCup.All.Count > 0, customerWaiting);

        if (Time.unscaledTime >= _nextSearch) FindNextTarget();
    }

    void FindNextTarget()
    {
        _nextSearch = Time.unscaledTime + 0.25f;
        NextTarget = null;
        float best = float.PositiveInfinity;
        int n = Physics.OverlapSphereNonAlloc(transform.position, SearchRadius, _hits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var it = _hits[i].GetComponentInParent<IInteractable>();
            if (!IsNext(it)) continue;
            float d = (_hits[i].bounds.center - transform.position).sqrMagnitude;
            if (d < best) { best = d; NextTarget = ((Component)it).transform; }
        }
    }

    /// True when using `it` is the next step.
    public bool IsNext(IInteractable it)
    {
        if (it == null) return false;
        switch (Step.Kind)
        {
            case GuideStepKind.TakeOrder: return it is Customer && it.CanInteract(Hands);
            case GuideStepKind.TakeCup:   return it is CupDispenser;
            case GuideStepKind.PickUpCup: return it is LooseCup;
            case GuideStepKind.Serve:     return it is ServeWindow;
            case GuideStepKind.Add:
                var st = it as StationBase;
                return st != null && st.Makes(Step.Ingredient);
        }
        return false;
    }
}
