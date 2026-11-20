using UnityEngine;

public interface IInteractable
{
    string Prompt(PlayerHands hands);
    bool CanInteract(PlayerHands hands);
    bool IsHold { get; }
    void Begin(PlayerHands hands);
    void Hold(PlayerHands hands, float t01);
    void End(PlayerHands hands, float t01);
    float Progress { get; }
}
