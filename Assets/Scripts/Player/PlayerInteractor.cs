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
