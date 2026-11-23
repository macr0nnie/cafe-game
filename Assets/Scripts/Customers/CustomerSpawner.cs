using System.Collections.Generic;
using UnityEngine;

/// Sends customers in through the door while the cafe is open, as long as there is
/// room in the line. Every position it hands out is an anchor, so the layout can move.
public class CustomerSpawner : MonoBehaviour
{
    public Customer Prefab;
    [Tooltip("Where customers appear and where they leave. +Z faces into the room.")]
    public Transform Door;
    [Tooltip("Where customers stand to collect a served drink, facing the counter.")]
    public Transform Pickup;
    public CustomerQueue Queue;
    [Tooltip("Outfit colours, picked at random per customer.")]
    public Material[] Outfits = new Material[0];

    [Header("Flow")]
    public float FirstArrival = 4f;
    public float SecondsBetween = 16f;
    [Tooltip("Most customers inside at once, seated or not.")]
    public int MaxInside = 10;

    readonly List<Customer> _inside = new List<Customer>();
    readonly System.Random _rng = new System.Random();
    float _next;
    bool _open = true;

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

    void Start() { _next = Time.time + FirstArrival; }

    void OnDayStarted() { _open = true; _next = Time.time + FirstArrival; }
    void OnDayEnded()   { _open = false; }

    void Update()
    {
        if (!_open || Time.time < _next) return;
        _inside.RemoveAll(c => c == null);
        if (_inside.Count >= MaxInside || Queue == null || !Queue.HasRoom) return;
        Spawn();
        _next = Time.time + SecondsBetween;
    }

    public Customer Spawn()
    {
        if (Prefab == null || Door == null) return null;
        var c = Instantiate(Prefab, Door.position, Door.rotation, transform);
        c.name = "Customer_" + (_inside.Count + 1);
        c.Dress(Outfits.Length > 0 ? Outfits[_rng.Next(Outfits.Length)] : null, _rng);
        c.Begin(Queue, Pickup, Door, RecipeBook.Random());
        _inside.Add(c);
        return c;
    }
}
