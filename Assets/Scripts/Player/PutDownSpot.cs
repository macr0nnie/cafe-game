using UnityEngine;

/// The bit of counter the player is looking at while holding a cup. Not a scene
/// object: PlayerInteractor moves one of these around and offers it as the focus,
/// so any flat counter top works without the scene builder marking it up.
public class PutDownSpot : IInteractable
{
    public Vector3 Point;
    public Quaternion Rotation = Quaternion.identity;

    public float Progress { get { return 0f; } }
    public bool IsHold { get { return false; } }

    public string Prompt(PlayerHands hands) { return "Set the cup down"; }
    public bool CanInteract(PlayerHands hands) { return hands != null && hands.HasCup && !hands.CupIsAway; }
    public void Begin(PlayerHands hands) { }
    public void Hold(PlayerHands hands, float t01) { }
    public void End(PlayerHands hands, float t01) { if (hands != null) hands.SetDown(Point, Rotation); }

    /// A surface a cup can stand on: facing up, and at table or counter height
    /// rather than on the floor or above the eyes.
    public static bool Settable(Vector3 normal, float pointY, float feetY, float eyeY)
    {
        return normal.y > 0.85f && pointY > feetY + 0.45f && pointY < eyeY - 0.25f;
    }
}
