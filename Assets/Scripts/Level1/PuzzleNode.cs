/*
PuzzleNode.cs

Purpose:
- Controls the puzzle node flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PuzzleNode
{
    [SerializeField] private string nodeId;
    [SerializeField] private Vector2 worldPosition;
    [SerializeField] private List<PuzzleConnection> connectedConnections = new List<PuzzleConnection>();

    public string NodeId => nodeId;
    public Vector2 WorldPosition => worldPosition;
    public IReadOnlyList<PuzzleConnection> ConnectedConnections => connectedConnections;

    public PuzzleNode(string nodeId, Vector2 worldPosition)
    {
        this.nodeId = nodeId;
        this.worldPosition = worldPosition;
    }

    public void SetWorldPosition(Vector2 position)
    {
        worldPosition = position;
    }

    public void SetConnectedConnections(IEnumerable<PuzzleConnection> connectionReferences)
    {
        connectedConnections.Clear();
        connectedConnections.AddRange(connectionReferences);
    }

    public void AddConnectedConnection(PuzzleConnection connection)
    {
        if (connection == null || connectedConnections.Contains(connection))
            return;

        connectedConnections.Add(connection);
    }
}
