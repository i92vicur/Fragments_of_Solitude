/*
PlayerBossAttackPieces.cs

Purpose: Tracks how many Boss Attack Pieces the player has collected from chests.

Design decisions:
- Single responsibility: Only stores piece count and availability state. It does not implement
  chest logic, HUD logic, or the mega attack itself.
- Event-driven: OnPiecesChanged fires after additions or consumption so HUD/gameplay systems can
  react immediately without polling or duplicating state.
- Run-local state: The counter is reset in Awake, so a scene reload starts a fresh run at 0.
- Explicit consumption: TryConsumePieces(amount) is the only way for gameplay systems to spend
  pieces. This prevents the count from being duplicated or mutated in multiple places.
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