using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneWorkshop
{
    // Desktop mouse/keyboard through the Input System package (the only input backend enabled,
    // as Android/Quest does not support mixing it with the legacy Input Manager).
    // Every query is safe when no mouse or keyboard exists, e.g. on the headset.
    public static class DesktopInput
    {
        public static bool HasMouse { get { return Mouse.current != null; } }
        public static Vector2 MousePosition { get { return HasMouse ? Mouse.current.position.ReadValue() : Vector2.zero; } }
        public static Vector2 MouseDelta { get { return HasMouse ? Mouse.current.delta.ReadValue() : Vector2.zero; } }
        public static float Scroll { get { return HasMouse ? Mouse.current.scroll.ReadValue().y : 0f; } }
        public static bool LeftPressed { get { return HasMouse && Mouse.current.leftButton.wasPressedThisFrame; } }
        public static bool RightPressed { get { return HasMouse && Mouse.current.rightButton.wasPressedThisFrame; } }
        public static bool MiddleHeld { get { return HasMouse && Mouse.current.middleButton.isPressed; } }

        public static bool ShiftHeld
        {
            get { return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed); }
        }

        public static bool KeyPressed(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        }
    }
}
