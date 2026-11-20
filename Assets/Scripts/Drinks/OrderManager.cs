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

    readonly List<OrderTicket> _open = new List<OrderTicket>();
    float _next;
    int _served;
    int _perfect;

    public int Served { get { return _served; } }
    public int Perfect { get { return _perfect; } }
    public int OpenCount { get { return _open.Count; } }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start() { _next = Time.time + FirstOrderDelay; }

    void Update()
    {
        if (Time.time >= _next && _open.Count < MaxOpenOrders)
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
        ticket.Nudge(16f);

        // Write the drink onto the slip.
        var tm = go.GetComponentInChildren<TextMesh>();
        if (tm != null) tm.text = r.Name + "\n" + r.Hint;

        _open.Add(ticket);
        Reflow();
        CafeMessage.Show("New order: " + r.Name, 2.6f);
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
        string name = t.Recipe.Name;
        _open.RemoveAt(best);
        if (t != null) Destroy(t.gameObject);
        Reflow();
        foreach (var o in _open) o.Nudge(10f);

        _served++;
        string note = cup.TechniqueNote();
        if (bestScore >= 0.999f)
        {
            _perfect++;
            CafeMessage.Show(note == null
                ? name + " - exactly right"
                : name + " - exactly right. " + note, 3.4f);
        }
        else
        {
            CafeMessage.Show(name + " - close enough ("
                + Mathf.RoundToInt(bestScore * 100f) + "%)"
                + (note == null ? "" : ". " + note), 3.4f);
        }
        return true;
    }
}
