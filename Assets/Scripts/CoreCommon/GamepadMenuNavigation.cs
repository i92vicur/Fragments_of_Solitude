using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Muestra en el botón seleccionado con mando el mismo estado visual que el hover del ratón.
// Los Selectable y los EventTrigger ya reaccionan a PointerEnter/PointerExit, así que no se añaden estilos nuevos.
public sealed class GamepadHoverBridge
{
    private Button highlighted;
    private Button lastSelected;
    private bool gamepadActive;

    // Llamar una vez por frame con el botón seleccionado por el EventSystem (o null).
    public void Tick(EventSystem es, Button selected)
    {
        if (selected != lastSelected)
        {
            lastSelected = selected;
            // Un clic de ratón también cambia la selección; solo cuenta como mando si el puntero no está encima.
            if (selected != null && !IsPointerOver(selected))
                gamepadActive = true;
        }

        if (IsMouseMoving())
            gamepadActive = false;

        Button target = gamepadActive ? selected : null;
        if (target == highlighted)
            return;

        // Si el ratón está encima del botón anterior, el propio módulo gestionará su salida.
        if (highlighted != null && !IsPointerOver(highlighted))
            Fire(es, highlighted, false);

        highlighted = target;
        if (target != null)
            Fire(es, target, true);
    }

    private static bool IsMouseMoving()
    {
        return Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0f;
    }

    private static bool IsPointerOver(Button button)
    {
        Mouse mouse = Mouse.current;
        RectTransform rect = button.transform as RectTransform;
        if (mouse == null || rect == null)
            return false;

        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera camera = canvas != null ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, mouse.position.ReadValue(), camera);
    }

    private static void Fire(EventSystem es, Button button, bool enter)
    {
        if (es == null || button == null || !button.gameObject.activeInHierarchy)
            return;

        PointerEventData data = new PointerEventData(es);
        if (enter)
            ExecuteEvents.Execute<IPointerEnterHandler>(button.gameObject, data, ExecuteEvents.pointerEnterHandler);
        else
            ExecuteEvents.Execute<IPointerExitHandler>(button.gameObject, data, ExecuteEvents.pointerExitHandler);
    }
}

// Navegación con mando compartida por los menús de Level 2 y Level 5.
// Se apoya en GameInput (Navigate/Submit) y en la selección del EventSystem, así que el ratón sigue funcionando igual.
public static class GamepadMenuNavigation
{
    private const float NavigateThresholdSqr = 0.25f;

    public static void EnsureGameInput()
    {
        if (GameInput.IsInitialized) return;

        GameInput.Initialize(InputSystem.actions);
        GameInput.SetContext(GameInputContext.Gameplay);
    }

    public static void ApplySelectionStyle(Button btn)
    {
        // Mantiene el color original en reposo; el seleccionado usa un tono dorado para verse sin ratón
        var colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 0.95f);
        colors.pressedColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        colors.selectedColor = new Color(0.9f, 0.7f, 0.15f, 1f);
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        btn.colors = colors;
    }

    // Mueve la selección con Navigate (un paso por flanco) y devuelve el botón seleccionado.
    // Si no hay selección válida dentro de la lista, selecciona el primero.
    public static Button UpdateSelection(EventSystem es, IReadOnlyList<Button> buttons, ref bool navLatched)
    {
        if (es == null || buttons == null || buttons.Count == 0)
        {
            navLatched = false;
            return null;
        }

        int index = -1;
        GameObject current = es.currentSelectedGameObject;
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].gameObject == current)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            index = 0;
            es.SetSelectedGameObject(buttons[index].gameObject);
        }

        Vector2 nav = GameInput.Navigate;
        bool navActive = nav.sqrMagnitude > NavigateThresholdSqr;
        if (navActive && !navLatched)
        {
            // Arriba/izquierda = anterior, abajo/derecha = siguiente; manda el eje dominante
            int step;
            if (Mathf.Abs(nav.y) >= Mathf.Abs(nav.x))
                step = nav.y > 0f ? -1 : 1;
            else
                step = nav.x < 0f ? -1 : 1;

            index = (index + step + buttons.Count) % buttons.Count;
            es.SetSelectedGameObject(buttons[index].gameObject);
        }
        navLatched = navActive;

        return buttons[index];
    }

    public static bool NavigateActive => GameInput.Navigate.sqrMagnitude > NavigateThresholdSqr;

    public static void ClearSelection(EventSystem es)
    {
        if (es != null && es.currentSelectedGameObject != null)
            es.SetSelectedGameObject(null);
    }

    public static void Submit(EventSystem es, Button button)
    {
        if (es == null || button == null || !button.interactable) return;

        ExecuteEvents.Execute(button.gameObject, new BaseEventData(es), ExecuteEvents.submitHandler);
    }
}
