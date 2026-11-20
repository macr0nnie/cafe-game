using UnityEngine;

/// Whether a menu has the player's attention. While one is open the cafe is
/// paused, the cursor is free and gameplay input is ignored.
public static class CafeGame
{
    public static bool MenuOpen { get; private set; }

    public static void SetMenuOpen(bool open)
    {
        if (MenuOpen == open) return;
        MenuOpen = open;
        Time.timeScale = open ? 0f : 1f;
        CafeEvents.RaiseMenuToggled(open);
    }
}
