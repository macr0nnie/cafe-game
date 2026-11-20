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

    IInteractable _focus;
    IInteractable _active;
    StationBase _activeStation;
    float _t;

    public IInteractable Focus { get { return _focus; } }
    public float ActiveProgress { get { return _activeStation != null ? Mathf.Clamp01(_t) : 0f; } }
    public bool IsBusy { get { return _active != null; } }

    void Awake()
    {
        if (Eye == null) Eye = GetComponentInChildren<Camera>();
        if (Hands == null) Hands = GetComponentInParent<PlayerHands>();
    }

    void Update()
    {
        if (_active == null) UpdateFocus();

        if (_active == null)
        {
            if (CafeInput.InteractDown() && _focus != null && _focus.CanInteract(Hands))
            {
                _active = _focus;
                _activeStation = _active as StationBase;
                _t = 0f;
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

        // A timed station is running.
        float dur = Mathf.Max(0.01f, _activeStation.Duration);
        bool held = CafeInput.InteractHeld();

        if (held)
        {
            _t += Time.deltaTime / dur;
            _activeStation.Hold(Hands, Mathf.Clamp01(_t));

            // Press-and-wait stations finish themselves; hold stations wait for release.
            if (_t >= 1f && !_activeStation.IsHold)
            {
                _activeStation.End(Hands, 1f);
                _active = null; _activeStation = null; _t = 0f;
            }
            else if (_t >= 1f && _activeStation.IsHold)
            {
                _activeStation.End(Hands, 1f);
                _active = null; _activeStation = null; _t = 0f;
            }
        }
        else
        {
            _activeStation.End(Hands, Mathf.Clamp01(_t));
            _active = null; _activeStation = null; _t = 0f;
        }
    }

    void UpdateFocus()
    {
        _focus = null;
        if (Eye == null) return;

        RaycastHit hit;
        var ray = new Ray(Eye.transform.position, Eye.transform.forward);
        if (Physics.Raycast(ray, out hit, Range, Mask, QueryTriggerInteraction.Collide))
        {
            var it = hit.collider.GetComponentInParent<MonoBehaviour>() as IInteractable;
            if (it == null)
            {
                var comps = hit.collider.GetComponentsInParent<MonoBehaviour>();
                foreach (var c in comps)
                {
                    it = c as IInteractable;
                    if (it != null) break;
                }
            }
            _focus = it;
        }
    }
}
