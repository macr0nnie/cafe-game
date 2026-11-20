using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Looks at what is in front of the player and runs the interaction.
public class PlayerInteractor : MonoBehaviour
{
    public Camera Eye;
    public PlayerHands Hands;
    public float Range = 2.6f;
    public LayerMask Mask = ~0;

    [Header("Aim assist")]
    [Tooltip("How far off-centre (degrees) the aim can be and still pick an interactable. Scaled by the Aim help setting; 0 turns assist off.")]
    [Range(0f, 30f)] public float AssistDegrees = 12f;
    [Tooltip("Extra degrees the current target keeps, so focus does not flicker between neighbours.")]
    [Range(0f, 10f)] public float StickyDegrees = 4f;

    IInteractable _focus;
    IInteractable _active;
    StationBase _activeStation;
    readonly Collider[] _nearby = new Collider[32];

    public IInteractable Focus { get { return _focus; } }
    public float ActiveProgress { get { return _activeStation != null ? Mathf.Clamp01(_activeStation.Progress) : 0f; } }
    public StationBase ActiveStation { get { return _activeStation; } }
    public bool IsBusy { get { return _active != null; } }

    void Awake()
    {
        if (Eye == null) Eye = GetComponentInChildren<Camera>();
        if (Hands == null) Hands = GetComponentInParent<PlayerHands>();
    }

    void Update()
    {
        if (CafeGame.MenuOpen) return;
        if (_active == null) UpdateFocus();

        if (_active == null)
        {
            if (CafeInput.InteractDown() && _focus != null && _focus.CanInteract(Hands))
            {
                _active = _focus;
                _activeStation = _active as StationBase;
                _active.Begin(Hands);

                // Anything that is not a timed station resolves at once.
                if (_activeStation == null)
                {
                    _active.End(Hands, 1f);
                    _active = null;
                }
            }
            return;
        }

        // A station is running its minigame; it decides when it is done.
        if (_activeStation.Tick(Hands, CafeInput.InteractHeld(), CafeInput.InteractDown(), Time.deltaTime))
        {
            _active = null; _activeStation = null;
        }
    }

    void UpdateFocus()
    {
        var prev = _focus;
        _focus = null;
        if (Eye == null) return;

        Vector3 o = Eye.transform.position, f = Eye.transform.forward;

        // Looking straight at something always wins.
        RaycastHit hit;
        if (Physics.Raycast(o, f, out hit, Range, Mask, QueryTriggerInteraction.Collide))
        {
            _focus = hit.collider.GetComponentInParent<IInteractable>();
            if (_focus != null) return;
        }
        // The "Aim help" slider scales the cone; 0 turns assist off.
        float cone = AssistDegrees * CafeSettings.AimAssist;
        if (cone <= 0f) return;

        // Otherwise take the visible interactable closest to the crosshair inside a small cone,
        // so small things (cups, dispensers) and things behind a counter edge are easy to hit.
        float best = float.PositiveInfinity;
        int n = Physics.OverlapSphereNonAlloc(o, Range, _nearby, Mask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var c = _nearby[i];
            var it = c.GetComponentInParent<IInteractable>();
            if (it == null) continue;

            Vector3 p = ClosestPoint(c, AimAssist.PointOnRay(o, f, c.bounds.center, Range));
            float s = AimAssist.Score(o, f, p, Range, cone, it == prev, StickyDegrees);
            if (s >= best || !Visible(o, p, it)) continue;
            best = s;
            _focus = it;
        }
    }

    static Vector3 ClosestPoint(Collider c, Vector3 p)
    {
        // Collider.ClosestPoint does not support concave mesh colliders.
        var mc = c as MeshCollider;
        return mc != null && !mc.convex ? c.bounds.ClosestPoint(p) : c.ClosestPoint(p);
    }

    bool Visible(Vector3 o, Vector3 p, IInteractable it)
    {
        Vector3 d = p - o;
        float dist = d.magnitude;
        if (dist < 1e-3f) return true;
        RaycastHit hit;
        // Stop just short of the surface; anything in between that is not the target blocks it.
        if (!Physics.Raycast(o, d / dist, out hit, dist - 0.02f, Mask, QueryTriggerInteraction.Ignore)) return true;
        return hit.collider.GetComponentInParent<IInteractable>() == it;
    }
}
