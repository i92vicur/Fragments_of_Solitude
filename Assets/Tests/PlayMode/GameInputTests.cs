#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

// Pruebas de integración de GameInput/GameButton con el asset real InputSystem_Actions.inputactions.
// Los tests síncronos ([Test]) comprueban estados que cambian al instante; los [UnityTest] avanzan fotogramas
// para Pressed/Released/HeldDuration, porque el Input System encola eventos y los procesa en el siguiente fotograma.
public class GameInputTests : InputTestFixture
{
    private const float Tolerance = 0.001f;

    private Keyboard keyboard;
    private Gamepad gamepad;
    private Mouse mouse;
    private InputActionAsset actionsAsset;
    private readonly List<InputActionAsset> createdAssets = new List<InputActionAsset>();

    public override void Setup()
    {
        base.Setup();

        keyboard = InputSystem.AddDevice<Keyboard>();
        gamepad = InputSystem.AddDevice<Gamepad>();
        mouse = InputSystem.AddDevice<Mouse>();

        actionsAsset = LoadActionsAsset();
        GameInput.Initialize(actionsAsset);
    }

    public override void TearDown()
    {
        if (GameInput.IsInitialized)
        {
            GameInput.SetContext(GameInputContext.None);
        }

        foreach (InputActionAsset asset in createdAssets)
        {
            if (asset != null)
            {
                Object.Destroy(asset);
            }
        }

        createdAssets.Clear();
        actionsAsset = null;

        base.TearDown();
    }

    // Carga el asset como instancia independiente por test, así no se comparten estados entre pruebas.
    private InputActionAsset LoadActionsAsset()
    {
        string json = File.ReadAllText(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions"));
        InputActionAsset asset = InputActionAsset.FromJson(json);
        createdAssets.Add(asset);
        return asset;
    }

    private static void AssertVectorApprox(Vector2 expected, Vector2 actual, string message)
    {
        Assert.AreEqual(expected.x, actual.x, Tolerance, message + " (x)");
        Assert.AreEqual(expected.y, actual.y, Tolerance, message + " (y)");
    }

    // 1. Inicialización

    [Test]
    public void Initialize_FindsMapsAndActions()
    {
        Assert.IsTrue(GameInput.IsInitialized);
        Assert.AreEqual(GameInputContext.None, GameInput.Context);

        Assert.IsTrue(GameInput.Jump.IsAvailable, "Jump");
        Assert.IsTrue(GameInput.Dash.IsAvailable, "Dash");
        Assert.IsTrue(GameInput.Attack.IsAvailable, "Attack");
        Assert.IsTrue(GameInput.Interact.IsAvailable, "Interact");
        Assert.IsTrue(GameInput.Pause.IsAvailable, "Pause");
        Assert.IsTrue(GameInput.Submit.IsAvailable, "Submit");
        Assert.IsTrue(GameInput.Cancel.IsAvailable, "Cancel");
    }

    [Test]
    public void Initialize_NoContextEnabledByDefault()
    {
        Assert.IsFalse(actionsAsset.FindActionMap("Player").enabled, "Player");
        Assert.IsFalse(actionsAsset.FindActionMap("UI").enabled, "UI");
    }

    // 4. Contextos

    [Test]
    public void SetContext_Gameplay_EnablesOnlyPlayerMap()
    {
        GameInput.SetContext(GameInputContext.Gameplay);

        Assert.AreEqual(GameInputContext.Gameplay, GameInput.Context);
        Assert.IsTrue(actionsAsset.FindActionMap("Player").enabled, "Player");
        Assert.IsFalse(actionsAsset.FindActionMap("UI").enabled, "UI");
    }

    [Test]
    public void SetContext_Menu_EnablesOnlyUIMap()
    {
        GameInput.SetContext(GameInputContext.Menu);

        Assert.AreEqual(GameInputContext.Menu, GameInput.Context);
        Assert.IsFalse(actionsAsset.FindActionMap("Player").enabled, "Player");
        Assert.IsTrue(actionsAsset.FindActionMap("UI").enabled, "UI");
    }

    [Test]
    public void SetContext_None_DisablesBothMaps()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        GameInput.SetContext(GameInputContext.None);

        Assert.IsFalse(actionsAsset.FindActionMap("Player").enabled, "Player");
        Assert.IsFalse(actionsAsset.FindActionMap("UI").enabled, "UI");
    }

    [Test]
    public void Context_InactiveActionsDoNotRespond()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(gamepad.buttonSouth);

        Assert.IsTrue(GameInput.Jump.Held, "A en gameplay debe saltar");
        Assert.IsFalse(GameInput.Submit.Held, "Submit (UI) no debe responder en gameplay");
        Release(gamepad.buttonSouth);

        GameInput.SetContext(GameInputContext.Menu);
        Press(gamepad.buttonSouth);

        Assert.IsTrue(GameInput.Submit.Held, "A en menú debe confirmar");
        Assert.IsFalse(GameInput.Jump.Held, "Jump (Player) no debe responder en menú");
        Release(gamepad.buttonSouth);
    }

    [Test]
    public void Context_NoneIgnoresAllButtons()
    {
        GameInput.SetContext(GameInputContext.None);
        Press(gamepad.buttonSouth);
        Press(keyboard.spaceKey);

        Assert.IsFalse(GameInput.Jump.Held);
        Assert.IsFalse(GameInput.Submit.Held);
        Release(gamepad.buttonSouth);
        Release(keyboard.spaceKey);
    }

    // 2. Movimiento

    [Test]
    public void Move_LeftStick_MatchesStickValue()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Set(gamepad.leftStick, new Vector2(0.8f, 0f));

        Vector2 expected = gamepad.leftStick.ReadValue();
        Assert.Greater(expected.x, 0.5f, "El stick debe superar la zona muerta del layout");
        AssertVectorApprox(expected, GameInput.Move, "Move con stick izquierdo");
    }

    [Test]
    public void Move_Keyboard_WASD_ReturnsDigitalValue()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(keyboard.dKey);

        AssertVectorApprox(new Vector2(1f, 0f), GameInput.Move, "Move con D");
        Release(keyboard.dKey);
        AssertVectorApprox(Vector2.zero, GameInput.Move, "Move tras soltar D");
    }

    [Test]
    public void Move_IsZeroOutsideGameplay()
    {
        GameInput.SetContext(GameInputContext.Menu);
        Set(gamepad.leftStick, new Vector2(1f, 0f));

        AssertVectorApprox(Vector2.zero, GameInput.Move, "Move en menú");
    }

    // 3. Apuntado

    [Test]
    public void Aim_RightStick_InsideDeadzone_ReturnsZero()
    {
        Set(gamepad.rightStick, new Vector2(0.1f, 0.1f));

        AssertVectorApprox(Vector2.zero, GameInput.Aim, "Aim dentro de la zona muerta");
    }

    [Test]
    public void Aim_RightStick_OutsideDeadzone_ReturnsStickValue()
    {
        Set(gamepad.rightStick, new Vector2(0f, 0.9f));

        Vector2 expected = gamepad.rightStick.ReadValue();
        Assert.Greater(expected.magnitude, 0.2f, "El stick debe superar la zona muerta de GameInput");
        AssertVectorApprox(expected, GameInput.Aim, "Aim con stick derecho");
    }

    [Test]
    public void Aim_IgnoresMouseMovement()
    {
        Set(mouse.delta, new Vector2(40f, 20f));

        AssertVectorApprox(Vector2.zero, GameInput.Aim, "Aim con ratón moviéndose");
    }

    // 5. Botones

    [Test]
    public void Dash_InGameplay_IsB()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(gamepad.buttonEast);

        Assert.IsTrue(GameInput.Dash.Held, "Dash");
        Assert.IsFalse(GameInput.Cancel.Held, "Cancel no debe activarse en gameplay");
        Release(gamepad.buttonEast);
    }

    [Test]
    public void Cancel_InMenu_IsB_AndDashStaysIdle()
    {
        GameInput.SetContext(GameInputContext.Menu);
        Press(gamepad.buttonEast);

        Assert.IsTrue(GameInput.Cancel.Held, "Cancel");
        Assert.IsFalse(GameInput.Dash.Held, "Dash no debe activarse en menú");
        Release(gamepad.buttonEast);
    }

    [Test]
    public void Pause_InGameplay_ReadsStart()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(gamepad.startButton);

        Assert.IsTrue(GameInput.Pause.Pressed, "Pause en gameplay con Start");
        Assert.IsTrue(GameInput.Pause.Held);
        Release(gamepad.startButton);
    }

    [Test]
    public void Pause_InMenu_ReadsStartThroughUIMap()
    {
        GameInput.SetContext(GameInputContext.Menu);
        Press(gamepad.startButton);

        Assert.IsTrue(GameInput.Pause.Pressed, "Pause en menú con Start");
        Assert.IsTrue(GameInput.Pause.Held);
        Release(gamepad.startButton);
    }

    [UnityTest]
    public IEnumerator Jump_Gamepad_PressedOnlyOnFirstFrame_HeldWhileDown_ReleasedOnUp()
    {
        GameInput.SetContext(GameInputContext.Gameplay);

        Press(gamepad.buttonSouth);
        yield return null;

        Assert.IsTrue(GameInput.Jump.Pressed, "Pressed en el primer fotograma");
        Assert.IsTrue(GameInput.Jump.Held, "Held tras pulsar");
        Assert.IsFalse(GameInput.Jump.Released, "Released no debe activarse al pulsar");

        yield return null;
        yield return null;

        Assert.IsFalse(GameInput.Jump.Pressed, "Pressed no debe repetirse mientras se mantiene");
        Assert.IsTrue(GameInput.Jump.Held, "Held sigue activo mientras se mantiene");

        Release(gamepad.buttonSouth);
        yield return null;

        Assert.IsTrue(GameInput.Jump.Released, "Released al soltar");
        Assert.IsFalse(GameInput.Jump.Held, "Held se apaga al soltar");
        Assert.IsFalse(GameInput.Jump.Pressed);
    }

    [Test]
    public void Jump_Keyboard_Space_PressedAndReleasedInSameFrame()
    {
        GameInput.SetContext(GameInputContext.Gameplay);

        Press(keyboard.spaceKey);
        Assert.IsTrue(GameInput.Jump.Pressed, "Space debe activar Pressed");
        Assert.IsTrue(GameInput.Jump.Held);

        Release(keyboard.spaceKey);
        Assert.IsTrue(GameInput.Jump.Released, "Space debe activar Released");
        Assert.IsFalse(GameInput.Jump.Held);
    }

    // 6. Duración

    [UnityTest]
    public IEnumerator HeldDuration_GrowsWhileHeld_ResetsAfterRelease()
    {
        GameInput.SetContext(GameInputContext.Gameplay);

        Press(gamepad.buttonSouth);
        yield return null;
        float first = GameInput.Jump.HeldDuration;

        yield return null;
        yield return null;
        yield return null;
        float later = GameInput.Jump.HeldDuration;

        Assert.Greater(later, first, "HeldDuration debe crecer mientras se mantiene");

        Release(gamepad.buttonSouth);
        yield return null;

        Assert.AreEqual(0f, GameInput.Jump.HeldDuration, Tolerance, "HeldDuration a 0 tras soltar");

        Press(gamepad.buttonSouth);
        yield return null;

        Assert.Less(GameInput.Jump.HeldDuration, later, "Nueva pulsación debe reiniciar HeldDuration");
        Release(gamepad.buttonSouth);
        yield return null;
    }

    // 7. Cambio de contexto con botones mantenidos

    [UnityTest]
    public IEnumerator SwitchingContextWhileHeld_EndsHoldOfInactiveMap()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(gamepad.buttonSouth);
        yield return null;

        Assert.IsTrue(GameInput.Jump.Held, "Precondición: Jump mantenido");

        GameInput.SetContext(GameInputContext.Menu);
        yield return null;

        Assert.IsFalse(GameInput.Jump.Held, "Deshabilitar Player debe cancelar Jump");
        Assert.IsFalse(GameInput.Submit.Held, "A ya pulsado al habilitar UI no debe iniciar Submit");

        Release(gamepad.buttonSouth);
        yield return null;
        Press(gamepad.buttonSouth);
        yield return null;

        Assert.IsTrue(GameInput.Submit.Held, "Una nueva pulsación de A debe iniciar Submit en el mapa UI");

        Release(gamepad.buttonSouth);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReturningToGameplayWhileStillHeld_DoesNotRestartJump()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(gamepad.buttonSouth);
        yield return null;

        GameInput.SetContext(GameInputContext.Menu);
        yield return null;
        GameInput.SetContext(GameInputContext.Gameplay);
        yield return null;

        Assert.IsFalse(GameInput.Jump.Held, "Volver a gameplay con A pulsada no debe reactivar Jump sin nueva pulsación");

        Release(gamepad.buttonSouth);
        yield return null;
    }

    // 9. Interacción Hold

    [Test]
    public void Interact_Hold_KeyboardE_HeldWhilePressed()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(keyboard.eKey);

        Assert.IsTrue(GameInput.Interact.Pressed, "Interact debe iniciarse al pulsar E");
        Assert.IsTrue(GameInput.Interact.Held);

        currentTime += 2.0;
        InputSystem.Update();

        Assert.IsTrue(GameInput.Interact.Held, "Hold no debe cancelarse mientras E sigue pulsada");

        Release(keyboard.eKey);

        Assert.IsTrue(GameInput.Interact.Released, "Released al soltar E");
        Assert.IsFalse(GameInput.Interact.Held);
    }

    [Test]
    public void Interact_Tap_ReleasedBeforeHoldCompletes_ClearsHeld()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(keyboard.eKey);
        Release(keyboard.eKey);

        Assert.IsFalse(GameInput.Interact.Held, "Una pulsación corta no debe dejar Held activo");
        Assert.IsTrue(GameInput.Interact.Released);
    }

    [Test]
    public void Interact_Gamepad_Y_HoldsLikeKeyboard()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(gamepad.buttonNorth);

        Assert.IsTrue(GameInput.Interact.Held, "Y debe activar Interact");

        currentTime += 2.0;
        InputSystem.Update();

        Assert.IsTrue(GameInput.Interact.Held);
        Release(gamepad.buttonNorth);
        Assert.IsFalse(GameInput.Interact.Held);
    }

    // 8. Reinicialización

    [Test]
    public void Reinitialize_SameAsset_DisposesOldButtons()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(gamepad.buttonSouth);
        GameButton oldJump = GameInput.Jump;
        Assert.IsTrue(oldJump.Held, "Precondición: Jump mantenido");

        GameInput.Initialize(actionsAsset);

        Assert.AreEqual(GameInputContext.None, GameInput.Context, "Initialize reinicia el contexto");
        Assert.IsFalse(oldJump.Held, "El botón anterior debe quedar sin estado");
        Assert.AreNotSame(oldJump, GameInput.Jump, "Se crea un botón nuevo");
        Assert.IsFalse(actionsAsset.FindActionMap("Player").enabled, "Player deshabilitado tras reinicializar");

        Release(gamepad.buttonSouth);
        GameInput.SetContext(GameInputContext.Gameplay);
        Press(gamepad.buttonSouth);

        Assert.IsTrue(GameInput.Jump.Held, "El botón nuevo responde");
        Assert.IsFalse(oldJump.Held, "El botón viejo no recibe eventos (sin suscripción duplicada)");
        Release(gamepad.buttonSouth);
    }

    [Test]
    public void Reinitialize_NewAsset_DisablesPreviousMaps()
    {
        GameInput.SetContext(GameInputContext.Gameplay);
        InputActionAsset previous = actionsAsset;

        InputActionAsset replacement = LoadActionsAsset();
        GameInput.Initialize(replacement);

        Assert.IsFalse(previous.FindActionMap("Player").enabled, "El asset anterior debe quedar deshabilitado");
        Assert.IsFalse(replacement.FindActionMap("Player").enabled, "El asset nuevo empieza sin contexto");
        Assert.AreEqual(GameInputContext.None, GameInput.Context);
    }
}
#endif
