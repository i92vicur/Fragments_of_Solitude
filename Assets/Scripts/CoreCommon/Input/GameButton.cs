using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Estado de un botón de InputSystem_Actions, alimentado por los eventos started/canceled de la acción.
// Pressed y Released solo son true en el fotograma en que ocurren: léelos en Update.
// Released también se produce si el mapa se deshabilita mientras el botón está pulsado (cancelación).
// Held y HeldDuration se actualizan con esos mismos eventos, así que no dependen de que alguien lea el botón cada fotograma.
public sealed class GameButton : IDisposable
{
    private readonly InputAction action;

    private int pressedFrame = -1;
    private int releasedFrame = -1;
    private bool held;
    private float heldSinceTime;
    private long lastStartedTimestamp; // [LAT] temporal

    public GameButton(InputAction action)
    {
        this.action = action;

        if (action != null)
        {
            action.started += OnStarted;
            action.canceled += OnCanceled;
        }
    }

    public bool IsAvailable => action != null;

    public bool Pressed => pressedFrame == Time.frameCount;

    public bool Released => releasedFrame == Time.frameCount;

    public bool Held => held;

    public long LastStartedTimestamp => lastStartedTimestamp; // [LAT] temporal

    // Tiempo escalado desde que el botón empezó a mantenerse. Vale 0 si no está mantenido.
    public float HeldDuration => held ? Time.time - heldSinceTime : 0f;

    // Desuscribe los eventos. Hay que llamarlo al sustituir el asset.
    public void Dispose()
    {
        if (action == null)
        {
            return;
        }

        action.started -= OnStarted;
        action.canceled -= OnCanceled;
        held = false;
    }

    private void OnStarted(InputAction.CallbackContext context)
    {
        pressedFrame = Time.frameCount;
        lastStartedTimestamp = System.Diagnostics.Stopwatch.GetTimestamp(); // [LAT] temporal
        held = true;
        heldSinceTime = Time.time;
    }

    private void OnCanceled(InputAction.CallbackContext context)
    {
        if (!held)
        {
            return;
        }

        releasedFrame = Time.frameCount;
        held = false;
    }
}
