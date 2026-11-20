using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Reads input through whichever system the project is set to, so this compiles
/// and runs on Old, New or Both without touching project settings.
public static class CafeInput
{
    public static Vector2 Move()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }

    public static Vector2 Look()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m != null) return m.delta.ReadValue() * 0.05f;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
        return Vector2.zero;
#endif
    }

    public static bool InteractHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        var m = Mouse.current;
        if (k != null || m != null)
            return (k != null && k.eKey.isPressed) || (m != null && m.leftButton.isPressed);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.E) || Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    public static bool InteractDown()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        var m = Mouse.current;
        if (k != null || m != null)
            return (k != null && k.eKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    public static bool CancelDown()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null) return k.escapeKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    public static bool RecipesDown()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null) return k.tabKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Tab);
#else
        return false;
#endif
    }

    public static bool SprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null) return k.leftShiftKey.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift);
#else
        return false;
#endif
    }
}
