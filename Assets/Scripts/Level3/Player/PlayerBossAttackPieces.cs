/*
PlayerBossAttackPieces.cs

Purpose:
- Controls the player boss attack pieces flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerBossAttackPieces : MonoBehaviour
{
    public const int RequiredPiecesForMegaAttack = 3;

    private int piecesCollected = 0;

    public int PiecesCollected => piecesCollected;
    public bool HasMegaAttackAvailable => piecesCollected >= RequiredPiecesForMegaAttack;
    public int PiecesStillNeeded => Mathf.Max(0, RequiredPiecesForMegaAttack - piecesCollected);

    public event Action<int> OnPiecesChanged;

    private void Awake()
    {
        piecesCollected = 0;
    }

    public void AddPiece()
    {
        piecesCollected++;
        OnPiecesChanged?.Invoke(piecesCollected);
    }

    public bool TryConsumePieces(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"PlayerBossAttackPieces.TryConsumePieces called with non-positive amount: {amount}", this);
            return false;
        }

        if (piecesCollected < amount)
            return false;

        piecesCollected -= amount;
        OnPiecesChanged?.Invoke(piecesCollected);
        return true;
    }
}