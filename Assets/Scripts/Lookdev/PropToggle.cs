using UnityEngine;

/// A door or lid you can open and close (oven, fridge...). The look comes from
/// StationClips on the same object; this only tracks open/closed and the prompt.
public class PropToggle : MonoBehaviour, IInteractable
{
    public string Noun = "oven";
    public bool IsOpen;

    public float Progress { get { return 0f; } }
    public bool IsHold { get { return false; } }

    public string Prompt(PlayerHands hands) { return (IsOpen ? "Close the " : "Open the ") + Noun; }
    public bool CanInteract(PlayerHands hands) { return true; }
    public void Begin(PlayerHands hands) { }
    public void Hold(PlayerHands hands, float t01) { }
    public void End(PlayerHands hands, float t01) { IsOpen = !IsOpen; }
}
