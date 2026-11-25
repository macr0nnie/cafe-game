using UnityEngine;
using UnityEngine.AI;

/// A customer: walks in, queues at the till, orders when the barista talks to them,
/// sits (or waits by the pickup if every seat is taken), collects their drink when it
/// is served, rates it, and leaves. Everything it reacts to arrives through CafeEvents.
[RequireComponent(typeof(NavMeshAgent))]
public class Customer : MonoBehaviour, IInteractable
{
    public enum State { Queueing, Ordering, ToSeat, Seated, ToPickup, Rating, Leaving }

    [Header("Parts")]
    [Tooltip("Everything above the hips. Dropped when seated.")]
    public Transform Body;
    public Transform LegL, LegR;
    [Tooltip("Where a collected cup is held.")]
    public Transform Hand;
    [Tooltip("Speech bubble root. Turned to face the camera.")]
    public Transform BubbleRoot;
    public TextMesh BubbleText;
    public Transform BubbleBack;
    [Tooltip("Renderers that take the outfit colour.")]
    public Renderer[] OutfitParts = new Renderer[0];
    [Tooltip("Optional extras (ears, scarf, tail). Each is switched on at random.")]
    public GameObject[] Extras = new GameObject[0];

    [Header("Feel")]
    public float RateSeconds = 4f;
    public float StepSwing = 26f;
    public float SitSpeed = 1.4f;
    [Tooltip("How long the order stays in the bubble after ordering.")]
    public float OrderBubbleSeconds = 6f;

    [Header("Bubble text")]
    public float CharsAcross = 16f;

    CustomerQueue _queue;
    Transform _pickup, _exit;
    Recipe _wants;

    NavMeshAgent _agent;
    State _state;
    Seat _seat;
    Vector3 _waitSpot;
    OrderTicket _ticket;
    DrinkRating _rating;
    CupContents _cup;
    float _timer, _phase, _sit;
    Vector3 _bodyRest;
    Vector3 _lastDest = new Vector3(float.NaN, 0f, 0f);

    public State Current { get { return _state; } }
    public Recipe Wants { get { return _wants; } }

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (Body != null) _bodyRest = Body.localPosition;
        Say("");
    }

    void OnEnable()
    {
        CafeEvents.DrinkHandedOff += OnDrinkHandedOff;
        CafeEvents.DayEnded += OnDayEnded;
    }

    void OnDisable()
    {
        CafeEvents.DrinkHandedOff -= OnDrinkHandedOff;
        CafeEvents.DayEnded -= OnDayEnded;
        if (_queue != null) _queue.Leave(this);
        if (_seat != null) _seat.Release(this);
    }

    /// Called by the spawner straight after it places the customer at the door.
    public void Begin(CustomerQueue queue, Transform pickup, Transform exit, Recipe wants)
    {
        _queue = queue; _pickup = pickup; _exit = exit; _wants = wants;
        if (_queue != null) _queue.Join(this);
        _state = State.Queueing;
    }

    /// Picks an outfit and extras. Roll comes from the spawner so variety is in one place.
    public void Dress(Material outfit, System.Random rng)
    {
        if (outfit != null) foreach (var r in OutfitParts) if (r != null) r.sharedMaterial = outfit;
        foreach (var e in Extras) if (e != null) e.SetActive(rng.Next(2) == 0);
    }

    // ------------------------------------------------------------------ flow

    void Update()
    {
        switch (_state)
        {
            case State.Queueing:
            {
                var spot = _queue != null ? _queue.SpotFor(this) : null;
                if (spot == null) { Leave(); break; }
                GoTo(spot.position);
                if (Arrived())
                {
                    FaceTowards(spot.rotation);
                    if (_queue.IsFront(this)) { _state = State.Ordering; Say("Hi! Ready to order"); }
                }
                break;
            }
            case State.Ordering:
            {
                var spot = _queue != null ? _queue.SpotFor(this) : null;
                if (spot != null) FaceTowards(spot.rotation);
                break;
            }
            case State.ToSeat:
                if (Arrived(_seat != null ? 0.3f : 0.5f))
                {
                    if (_seat != null) { _agent.enabled = false; _state = State.Seated; }
                    else { FaceTowards(_pickup != null ? _pickup.rotation : transform.rotation); _state = State.Seated; }
                }
                break;
            case State.Seated:
                if (_seat != null)
                {
                    // Slide from the approach spot onto the seat and sink down.
                    transform.position = Vector3.MoveTowards(transform.position, _seat.transform.position, SitSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, _seat.transform.rotation, 1f - Mathf.Exp(-8f * Time.deltaTime));
                    _sit = Mathf.MoveTowards(_sit, 1f, SitSpeed * 2f * Time.deltaTime);
                }
                break;
            case State.ToPickup:
                if (Arrived(0.6f)) TakeDrink();
                break;
            case State.Rating:
                if (_pickup != null) FaceTowards(_pickup.rotation);
                _timer -= Time.deltaTime;
                if (_timer <= 0f) Leave();
                break;
            case State.Leaving:
                if (Arrived(0.6f)) Destroy(gameObject);
                break;
        }

        if (_timer > 0f && _state != State.Rating)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f && _ticket != null) Say("");
        }
        Animate();
    }

    void TakeOrder()
    {
        if (_queue != null) _queue.Leave(this);
        if (OrderManager.Instance != null) _ticket = OrderManager.Instance.AddOrder(_wants);
        Say(_wants.Name + ", please!");
        _timer = OrderBubbleSeconds;

        _seat = Seat.PickFree(Seat.All, Random.Range(0, 1 << 16));
        if (_seat != null && _seat.Claim(this))
            GoTo(Sample(_seat.ApproachPoint));
        else
        {
            _seat = null;
            // No seat: hover near the pickup, spread out a little.
            Vector2 off = Random.insideUnitCircle * 0.8f;
            Vector3 c = _pickup != null ? _pickup.position + _pickup.forward * -0.9f : transform.position;
            GoTo(Sample(c + new Vector3(off.x, 0f, off.y)));
        }
        _state = State.ToSeat;

        if (_ticket == null) Leave();   // nothing to wait for: the order line is not set up
    }

    void OnDrinkHandedOff(OrderTicket ticket, DrinkRating rating, CupContents cup)
    {
        if (_ticket == null || !ReferenceEquals(ticket, _ticket)) return;
        _rating = rating;
        _cup = cup;
        StandUp();
        Say("That's mine!");
        _timer = 0f;
        if (_pickup != null) GoTo(_pickup.position);
        _state = State.ToPickup;
    }

    void TakeDrink()
    {
        if (_cup != null && Hand != null)
        {
            _cup.transform.SetParent(Hand, false);
            _cup.transform.localPosition = Vector3.zero;
            _cup.transform.localRotation = Quaternion.identity;
        }
        _state = State.Rating;
        _timer = RateSeconds;
        if (_rating != null)
        {
            Say(_rating.Reaction());
            // The same verdict in the message strip, for anyone not looking at the bubble.
            CafeMessage.Show(_wants.Name + ": " + _rating.Stars + "/5 stars, +" + _rating.Tips + " tips. "
                             + _rating.Headline, 3.6f);
        }
    }

    void OnDayEnded()
    {
        if (_state == State.Rating || _state == State.Leaving || _state == State.ToPickup) return;
        Say("See you tomorrow!");
        Leave();
    }

    void Leave()
    {
        if (_queue != null) _queue.Leave(this);
        StandUp();
        if (_seat != null) { _seat.Release(this); _seat = null; }
        _state = State.Leaving;
        if (_exit != null) GoTo(_exit.position);
        else Destroy(gameObject);
    }

    void StandUp()
    {
        _sit = 0f;
        if (_agent.enabled) return;
        // Step back onto the walkable floor before the agent wakes, or it cannot find the mesh.
        if (_seat != null) transform.position = Sample(_seat.ApproachPoint);
        _agent.enabled = true;
        _lastDest.x = float.NaN;
    }

    // ------------------------------------------------------------------ navigation

    void GoTo(Vector3 p)
    {
        if (!_agent.enabled || !_agent.isOnNavMesh) return;
        if ((p - _lastDest).sqrMagnitude < 0.01f) return;
        _lastDest = p;
        _agent.SetDestination(p);
    }

    /// Slack lets a shared spot (pickup, door) count as reached when another customer is standing on it.
    bool Arrived(float slack = 0.05f)
    {
        if (!_agent.enabled || !_agent.isOnNavMesh) return false;
        return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + slack;
    }

    static Vector3 Sample(Vector3 p)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(p, out hit, 1.2f, NavMesh.AllAreas) ? hit.position : p;
    }

    void FaceTowards(Quaternion target)
    {
        Vector3 f = target * Vector3.forward; f.y = 0f;
        if (f.sqrMagnitude < 1e-4f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(f), 1f - Mathf.Exp(-6f * Time.deltaTime));
    }

    // ------------------------------------------------------------------ look

    void Animate()
    {
        float speed = _agent.enabled ? _agent.velocity.magnitude : 0f;
        float walk = Mathf.Clamp01(speed / Mathf.Max(0.1f, _agent.speed));
        _phase += Time.deltaTime * speed * 7f;
        float swing = Mathf.Sin(_phase) * StepSwing * walk;
        float drop = _seat != null ? _seat.SitDrop : 0.42f;

        // Seated: legs swing forward to sit. Walking: legs swing, body bobs gently.
        if (LegL != null) LegL.localRotation = Quaternion.Euler(Mathf.Lerp(swing, -85f, _sit), 0f, 0f);
        if (LegR != null) LegR.localRotation = Quaternion.Euler(Mathf.Lerp(-swing, -85f, _sit), 0f, 0f);
        if (Body != null)
            Body.localPosition = _bodyRest + Vector3.up * (Mathf.Abs(Mathf.Cos(_phase)) * 0.025f * walk - drop * _sit);
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (BubbleRoot != null && cam != null) BubbleRoot.rotation = cam.transform.rotation;
    }

    void Say(string text)
    {
        if (BubbleRoot == null || BubbleText == null) return;
        bool show = !string.IsNullOrEmpty(text);
        BubbleRoot.gameObject.SetActive(show);
        if (!show) return;
        BubbleText.text = text;

        string[] lines = text.Split('\n');
        int longest = 1;
        foreach (var l in lines) longest = Mathf.Max(longest, l.Length);
        // shortcut: sizes from character count like the order slips; measure the mesh if fonts change.
        float fit = Mathf.Min(1f, CharsAcross / longest);
        BubbleText.transform.localScale = Vector3.one * fit;
        if (BubbleBack != null)
        {
            var s = BubbleBack.localScale;
            BubbleBack.localScale = new Vector3(s.x, 0.09f + 0.075f * lines.Length * fit, s.z);
        }
    }

    // ------------------------------------------------------------------ IInteractable

    public float Progress { get { return 0f; } }
    public bool IsHold { get { return false; } }
    public string Prompt(PlayerHands hands)
    {
        switch (_state)
        {
            case State.Ordering: return "Take order";
            case State.Queueing: return "Waiting in line";
            case State.ToSeat:
            case State.Seated:
            case State.ToPickup: return "Waiting for: " + (_wants != null ? _wants.Name : "a drink");
            case State.Rating:   return _rating != null ? "Rated it " + _rating.Stars + "/5" : "Enjoying it";
        }
        return "Heading home";
    }
    public bool CanInteract(PlayerHands hands) { return _state == State.Ordering; }
    public void Begin(PlayerHands hands) { }
    public void Hold(PlayerHands hands, float t01) { }
    public void End(PlayerHands hands, float t01) { if (_state == State.Ordering) TakeOrder(); }
}
