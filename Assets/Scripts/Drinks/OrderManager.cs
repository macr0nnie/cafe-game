using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Line")]
    [Tooltip("Left and right ends of the clothesline, in world space.")]
    public Transform LineStart;
    public Transform LineEnd;
    public GameObject TicketPrefab;

    [Header("Flow")]
    public int MaxOpenOrders = 4;
    public float FirstOrderDelay = 4f;
    public float SecondsBetweenOrders = 18f;

    [Header("Scoring")]
    public float AcceptThreshold = 0.6f;
    [Tooltip("Seconds a drink can take and still earn the full speed tip.")]
    public float ParSeconds = 60f;

    readonly List<OrderTicket> _open = new List<OrderTicket>();
    float _next;
    int _served;
    int _perfect;
    bool _accepting = true;

    public int Served { get { return _served; } }
    public int Perfect { get { return _perfect; } }
    public int OpenCount { get { return _open.Count; } }
    public IList<OrderTicket> Open { get { return _open; } }

    void Awake() { Instance = this; }

    void OnEnable()
    {
        CafeEvents.DayStarted += OnDayStarted;
        CafeEvents.DayEnded += OnDayEnded;
    }

    void OnDisable()
    {
        CafeEvents.DayStarted -= OnDayStarted;
        CafeEvents.DayEnded -= OnDayEnded;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start() { _next = Time.time + FirstOrderDelay; }

    void OnDayStarted()
    {
        _accepting = true;
        _served = 0;
        _perfect = 0;
        _next = Time.time + FirstOrderDelay;
    }

    /// Closing time: the remaining customers are thanked and the line is cleared.
    void OnDayEnded()
    {
        _accepting = false;
        foreach (var t in _open) if (t != null) Destroy(t.gameObject);
        _open.Clear();
        CafeEvents.RaiseOrdersChanged();
    }

    void Update()
    {
        if (_accepting && Time.time >= _next && _open.Count < MaxOpenOrders)
        {
            AddOrder(RecipeBook.Random());
            _next = Time.time + SecondsBetweenOrders;
        }
    }

    public void AddOrder(Recipe r)
    {
        if (TicketPrefab == null || LineStart == null || LineEnd == null)
        {
            CafeMessage.Show("New order: " + r.Name);
            return;
        }

        var go = Instantiate(TicketPrefab, transform);
        go.SetActive(true);
        go.name = "Ticket_" + r.Name;

        var ticket = go.GetComponent<OrderTicket>();
        if (ticket == null) ticket = go.AddComponent<OrderTicket>();

        var paper = go.transform.childCount > 0 ? go.transform.GetChild(0) : go.transform;
        ticket.Setup(r, paper, Random.Range(0f, 10f));
        ticket.PlacedAt = Time.time;
        ticket.Nudge(16f);

        ticket.WriteSlip(r);

        _open.Add(ticket);
        Reflow();
        CafeMessage.Show("New order: " + r.Name, 2.6f);
        CafeEvents.RaiseOrderPlaced(ticket);
    }

    /// Space the tickets evenly along the line.
    void Reflow()
    {
        if (LineStart == null || LineEnd == null) return;
        int n = _open.Count;
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0.5f : Mathf.Lerp(0.12f, 0.88f, (float)i / (n - 1));
            _open[i].transform.position = Vector3.Lerp(LineStart.position, LineEnd.position, t);
            _open[i].transform.rotation = LineStart.rotation;
        }
    }

    /// Serve the cup against the best-matching open order.
    /// Returns true when an order took the cup.
    public bool TryServe(CupContents cup)
    {
        if (cup == null) return false;

        if (_open.Count == 0)
        {
            CafeMessage.Show("No orders up - made a " + cup.Describe(), 3f);
            return false;
        }

        int best = -1;
        float bestScore = -1f;
        for (int i = 0; i < _open.Count; i++)
        {
            float s = cup.ScoreAgainst(_open[i].Recipe);
            if (s > bestScore) { bestScore = s; best = i; }
        }

        if (best < 0 || bestScore < AcceptThreshold)
        {
            CafeMessage.Show("Not what anyone ordered - that is " + cup.Describe(), 3.4f);
            return false;
        }

        var t = _open[best];
        var recipe = t.Recipe;
        float waited = Time.time - t.PlacedAt;
        _open.RemoveAt(best);
        if (t != null) Destroy(t.gameObject);
        Reflow();
        foreach (var o in _open) o.Nudge(10f);

        _served++;
        int stars = DrinkScore.Stars(bestScore, cup.Execution);
        int tips = DrinkScore.Payout(stars, waited, ParSeconds);
        string note = cup.TechniqueNote();
        if (bestScore >= 0.999f) _perfect++;
        CafeMessage.Show(recipe.Name + (bestScore >= 0.999f ? " - exactly right, " : " - close enough, ")
            + stars + "/5 stars, +" + tips + " tips" + (note == null ? "" : ". " + note), 3.4f);

        CafeEvents.RaiseOrderServed(recipe, stars, tips);
        return true;
    }
}
