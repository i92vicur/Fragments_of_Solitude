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
