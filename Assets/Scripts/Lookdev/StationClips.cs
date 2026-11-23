using System;
using UnityEngine;

/// Plays prop animations when the interactable on this object is used.
/// Purely visual: it only listens to CafeEvents, so stations know nothing about it.
public class StationClips : MonoBehaviour
{
    [Serializable]
    public struct ClipRef
    {
        public string Object;
        public string Clip;
        public ClipRef(string obj, string clip) { Object = obj; Clip = clip; }
    }

    public LookdevAnimator Animator;
    [Tooltip("Played once when the player starts using this.")]
    public ClipRef[] OnBegin = new ClipRef[0];
    [Tooltip("Looping clips that run while the player is using this.")]
    public ClipRef[] WhileRunning = new ClipRef[0];
    [Tooltip("Played once when the player is done.")]
    public ClipRef[] OnEnd = new ClipRef[0];

    IInteractable _self;

    void Awake() { _self = GetComponent<IInteractable>(); }

    void OnEnable()
    {
        CafeEvents.InteractionStarted += Started;
        CafeEvents.InteractionEnded += Ended;
    }

    void OnDisable()
    {
        CafeEvents.InteractionStarted -= Started;
        CafeEvents.InteractionEnded -= Ended;
    }

    void Started(IInteractable i)
    {
        if (i != _self || Animator == null || _self is PropToggle) return;
        PlayAll(OnBegin);
        PlayAll(WhileRunning);
    }

    void Ended(IInteractable i)
    {
        if (i != _self || Animator == null) return;
        // Doors: OnBegin is the opening clip, OnEnd the closing one.
        var toggle = _self as PropToggle;
        if (toggle != null) { PlayAll(toggle.IsOpen ? OnBegin : OnEnd); return; }

        foreach (var c in WhileRunning) Animator.Stop(c.Object, c.Clip);
        PlayAll(OnEnd);
    }

    void PlayAll(ClipRef[] clips)
    {
        foreach (var c in clips) Animator.Play(c.Object, c.Clip);
    }
}
