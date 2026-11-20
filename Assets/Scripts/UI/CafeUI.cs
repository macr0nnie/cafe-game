using UnityEngine;
using UnityEngine.UI;

/// Small toast line at the bottom of the screen.
public static class CafeMessage
{
    public static string Current = "";
    public static float Until;

    public static void Show(string msg, float seconds = 2.2f)
    {
        Current = msg;
        Until = Time.time + seconds;
    }

    public static bool Visible { get { return Time.time < Until && !string.IsNullOrEmpty(Current); } }
}
