using UnityEngine.InputSystem;

// Controles de mando del nivel 4 (A = salto cargado en fase 1, RB = dash en ambas fases).
// Se lee el mando directamente para no modificar los bindings globales de GameInput.
public static class Level4GamepadInput
{
    public static bool IsJumpHeld()
    {
        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad.buttonSouth.isPressed)
                return true;
        }

        return false;
    }

    public static bool DashPressedThisFrame()
    {
        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad.rightShoulder.wasPressedThisFrame)
                return true;
        }

        return false;
    }
}
