using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Contextos exclusivos: solo se habilita un mapa a la vez, así B (Dash) y B (Cancel) nunca reciben la misma pulsación.
public enum GameInputContext
{
    None,
    Gameplay,
    Menu
}

// Acceso común a InputSystem_Actions para los sistemas de juego.
// Uso: Initialize(asset) una vez y después SetContext(...). Nada se lee de forma efectiva hasta habilitar un contexto.
// Teclado y ratón siguen funcionando con sus lecturas actuales; esta clase no las sustituye.
public static class GameInput
{
    private const string PlayerMapName = "Player";
    private const string UIMapName = "UI";
    private const float AimDeadzone = 0.2f;

    private static readonly GameButton Unavailable = new GameButton(null);
    private static readonly List<GameButton> buttons = new List<GameButton>();

    private static InputActionAsset asset;
    private static InputActionMap playerMap;
    private static InputActionMap uiMap;
    private static InputAction moveAction;
    private static InputAction navigateAction;
    private static GameInputContext context;
    private static GameButton playerPause = Unavailable;
    private static GameButton menuPause = Unavailable;

    public static bool IsInitialized => asset != null;

    // Mapa Player
    public static GameButton Jump { get; private set; } = Unavailable;
    public static GameButton Dash { get; private set; } = Unavailable;
    public static GameButton Attack { get; private set; } = Unavailable;
    public static GameButton Interact { get; private set; } = Unavailable;
    public static GameButton Crouch { get; private set; } = Unavailable;
    public static GameButton Sprint { get; private set; } = Unavailable;
    public static GameButton Secondary { get; private set; } = Unavailable;
    public static GameButton Special { get; private set; } = Unavailable;
    public static GameButton Previous { get; private set; } = Unavailable;
    public static GameButton Next { get; private set; } = Unavailable;

    // Start/P según el contexto: Player en juego, UI en menús. Así la pausa se puede abrir y cerrar sin perder la lectura.
    public static GameButton Pause => context == GameInputContext.Menu ? menuPause : playerPause;

    public static GameInputContext Context => context;

    // Mapa UI. En gamepad, Submit es A y Cancel es B por los usages del layout.
    public static GameButton Submit { get; private set; } = Unavailable;
    public static GameButton Cancel { get; private set; } = Unavailable;

    public static Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

    public static Vector2 Navigate => navigateAction != null ? navigateAction.ReadValue<Vector2>() : Vector2.zero;

    // Stick derecho de los gamepads conectados. No usa Look ni activeControl, así que el ratón no lo interrumpe.
    // Devuelve el stick con más inclinación; el apuntado con ratón sigue en sus scripts actuales.
    public static Vector2 Aim
    {
        get
        {
            Vector2 best = Vector2.zero;
            foreach (Gamepad pad in Gamepad.all)
            {
                Vector2 stick = pad.rightStick.ReadValue();
                if (stick.sqrMagnitude > best.sqrMagnitude)
                {
                    best = stick;
                }
            }

            return best.magnitude < AimDeadzone ? Vector2.zero : best;
        }
    }

    public static void Initialize(InputActionAsset actionsAsset)
    {
        if (actionsAsset == null)
        {
            Debug.LogError("GameInput: no se ha asignado un InputActionAsset.");
            return;
        }

        playerMap?.Disable();
        uiMap?.Disable();
        context = GameInputContext.None;

        foreach (GameButton button in buttons)
        {
            button.Dispose();
        }

        buttons.Clear();

        asset = actionsAsset;
        playerMap = asset.FindActionMap(PlayerMapName);
        uiMap = asset.FindActionMap(UIMapName);

        moveAction = FindAction(playerMap, "Move");
        navigateAction = FindAction(uiMap, "Navigate");

        Jump = Wrap(playerMap, "Jump");
        Dash = Wrap(playerMap, "Dash");
        Attack = Wrap(playerMap, "Attack");
        Interact = Wrap(playerMap, "Interact");
        Crouch = Wrap(playerMap, "Crouch");
        Sprint = Wrap(playerMap, "Sprint");
        playerPause = Wrap(playerMap, "Pause");
        Secondary = Wrap(playerMap, "Secondary");
        Special = Wrap(playerMap, "Special");
        Previous = Wrap(playerMap, "Previous");
        Next = Wrap(playerMap, "Next");

        Submit = Wrap(uiMap, "Submit");
        Cancel = Wrap(uiMap, "Cancel");
        menuPause = Wrap(uiMap, "Pause");
    }

    // Solo un mapa habilitado a la vez. En Menu, Player queda deshabilitado y Dash no puede activarse con B.
    public static void SetContext(GameInputContext newContext)
    {
        if (!IsInitialized)
        {
            Debug.LogError("GameInput: SetContext llamado antes de Initialize.");
            return;
        }

        context = newContext;
        SetMapEnabled(playerMap, newContext == GameInputContext.Gameplay);
        SetMapEnabled(uiMap, newContext == GameInputContext.Menu);
    }

    private static void SetMapEnabled(InputActionMap map, bool enabled)
    {
        if (map == null)
        {
            return;
        }

        if (enabled)
        {
            map.Enable();
        }
        else
        {
            map.Disable();
        }
    }

    private static GameButton Wrap(InputActionMap map, string actionName)
    {
        GameButton button = new GameButton(FindAction(map, actionName));
        buttons.Add(button);
        return button;
    }

    private static InputAction FindAction(InputActionMap map, string actionName)
    {
        InputAction action = map?.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"GameInput: no se encontró la acción '{actionName}'.");
        }

        return action;
    }
}
