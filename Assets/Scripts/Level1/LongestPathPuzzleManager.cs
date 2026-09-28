/*
LongestPathPuzzleManager.cs

Purpose:
- Controls the longest path puzzle manager flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LongestPathPuzzleResult
{
    InProgress,
    ReachedEnd,
    InvalidMove
}

public enum LongestPathPuzzleCompletionState
{
    ValidRoute,
    OptimalRoute
}

[DisallowMultipleComponent]
public sealed class LongestPathPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Graph")]
    [SerializeField] private string startNodeId = "A";
    [SerializeField] private string endNodeId = "B";
    [SerializeField] private List<PuzzleNode> nodes = new List<PuzzleNode>();
    [SerializeField] private List<PuzzleConnection> connections = new List<PuzzleConnection>();

    [Header("Runtime State")]
    [SerializeField] private string currentNodeId = string.Empty;
    [SerializeField] private float totalDistance;
    [SerializeField] private bool puzzleFinished;
    [SerializeField] private bool hasKnownOptimalDistance;
    [SerializeField] private float knownOptimalDistance;
    [SerializeField] private bool logOptimalDistanceOnInitialize = true;

    private readonly List<string> usedConnectionHistory = new List<string>();

    public event Action<PuzzleNode> CurrentNodeChanged;
    public event Action<PuzzleConnection> ConnectionUsed;
    public event Action PuzzleReset;
    public event Action<LongestPathPuzzleResult> PuzzleStateChanged;
    public event Action<float> OptimalDistanceUpdated;
    public event Action<LongestPathPuzzleCompletionState, float> RouteCompleted;

    public IReadOnlyList<PuzzleNode> Nodes => nodes;
    public IReadOnlyList<PuzzleConnection> Connections => connections;
    public string StartNodeId => startNodeId;
    public string EndNodeId => endNodeId;
    public string CurrentNodeId => currentNodeId;
    public float TotalDistance => totalDistance;
    public bool IsPuzzleFinished => puzzleFinished;
    public bool HasKnownOptimalDistance => hasKnownOptimalDistance;
    public float KnownOptimalDistance => knownOptimalDistance;

    private void Awake()
    {
        EnsureGraphInitialized();
        RecalculateOptimalDistance();
        ResetPuzzle();

        if (logOptimalDistanceOnInitialize)
            Debug.Log($"Longest Path optimal distance: {knownOptimalDistance}", this);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EnsureGraphInitialized();
            RecalculateOptimalDistance();
        }
    }

    public void ResetPuzzle()
    {
        foreach (PuzzleConnection connection in connections)
            connection.ResetUsage();

        usedConnectionHistory.Clear();
        currentNodeId = startNodeId;
        totalDistance = 0f;
        puzzleFinished = false;
        PuzzleReset?.Invoke();
        RaiseCurrentNodeChanged();
        PuzzleStateChanged?.Invoke(LongestPathPuzzleResult.InProgress);
    }

    public void ResetRoute()
    {
        ResetPuzzle();
    }

    public bool UndoLastMove()
    {
        if (usedConnectionHistory.Count == 0)
            return false;

        string lastConnectionId = usedConnectionHistory[usedConnectionHistory.Count - 1];
        PuzzleConnection lastConnection = GetConnection(lastConnectionId);

        if (lastConnection == null || !lastConnection.HasBeenUsed)
        {
            usedConnectionHistory.RemoveAt(usedConnectionHistory.Count - 1);
            return false;
        }

        string previousNodeId = lastConnection.GetOtherNodeId(currentNodeId);
        if (string.IsNullOrEmpty(previousNodeId))
            return false;

        lastConnection.ResetUsage();
        usedConnectionHistory.RemoveAt(usedConnectionHistory.Count - 1);
        currentNodeId = previousNodeId;
        totalDistance = Mathf.Max(0f, totalDistance - lastConnection.Length);
        puzzleFinished = false;

        RaiseCurrentNodeChanged();
        PuzzleStateChanged?.Invoke(LongestPathPuzzleResult.InProgress);
        return true;
    }

    public void SetKnownOptimalDistance(float optimalDistance)
    {
        hasKnownOptimalDistance = true;
        knownOptimalDistance = optimalDistance;
        OptimalDistanceUpdated?.Invoke(optimalDistance);
    }

    public PuzzleNode GetNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return null;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].NodeId == nodeId)
                return nodes[i];
        }

        return null;
    }

    public PuzzleConnection GetConnection(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            return null;

        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].ConnectionId == connectionId)
                return connections[i];
        }

        return null;
    }

    public IReadOnlyList<PuzzleConnection> GetAvailableConnectionsFromCurrentNode()
    {
        return connections.Where(connection => !connection.HasBeenUsed && connection.CanTraverseFrom(currentNodeId)).ToList();
    }

    public bool TryMoveToNode(string targetNodeId)
    {
        if (puzzleFinished || string.IsNullOrEmpty(targetNodeId))
            return false;

        PuzzleConnection chosenConnection = null;

        foreach (PuzzleConnection connection in GetCurrentNode().ConnectedConnections)
        {
            if (connection == null || connection.HasBeenUsed)
                continue;

            if (connection.GetOtherNodeId(currentNodeId) != targetNodeId)
                continue;

            if (chosenConnection != null)
                return false;

            chosenConnection = connection;
        }

        return chosenConnection != null && TryMoveAlongConnection(chosenConnection.ConnectionId);
    }

    public bool TryMoveAlongConnection(string connectionId)
    {
        if (puzzleFinished)
            return false;

        PuzzleConnection connection = GetConnection(connectionId);
        if (connection == null || connection.HasBeenUsed || !connection.CanTraverseFrom(currentNodeId))
        {
            PuzzleStateChanged?.Invoke(LongestPathPuzzleResult.InvalidMove);
            return false;
        }

        string nextNodeId = connection.GetOtherNodeId(currentNodeId);
        if (string.IsNullOrEmpty(nextNodeId))
        {
            PuzzleStateChanged?.Invoke(LongestPathPuzzleResult.InvalidMove);
            return false;
        }

        connection.MarkUsed();
        usedConnectionHistory.Add(connectionId);
        currentNodeId = nextNodeId;
        totalDistance += connection.Length;
        ConnectionUsed?.Invoke(connection);
        RaiseCurrentNodeChanged();

        if (currentNodeId == endNodeId)
        {
            puzzleFinished = true;
            LongestPathPuzzleCompletionState completionState = GetCompletionState();
            RouteCompleted?.Invoke(completionState, totalDistance);
            PuzzleStateChanged?.Invoke(LongestPathPuzzleResult.ReachedEnd);
        }
        else
        {
            PuzzleStateChanged?.Invoke(LongestPathPuzzleResult.InProgress);
        }

        return true;
    }

    public bool IsValidMove(string connectionId)
    {
        PuzzleConnection connection = GetConnection(connectionId);
        return connection != null && !connection.HasBeenUsed && connection.CanTraverseFrom(currentNodeId);
    }

    public PuzzleNode GetCurrentNode()
    {
        return GetNode(currentNodeId);
    }

    public float RecalculateOptimalDistance()
    {
        float optimalDistance = CalculateMaximumRouteDistance();
        SetKnownOptimalDistance(optimalDistance);
        return optimalDistance;
    }

    private void EnsureGraphInitialized()
    {
        if (nodes.Count > 0 && connections.Count > 0)
            return;

        nodes = new List<PuzzleNode>
        {
            new PuzzleNode("A", new Vector2(-5.3f, 3.8f)),
            new PuzzleNode("N1", new Vector2(-3.3f, 2.8f)),
            new PuzzleNode("N2", new Vector2(-1.2f, 1.8f)),
            new PuzzleNode("N3", new Vector2(1.0f, 3.6f)),
            new PuzzleNode("N4", new Vector2(3.2f, 1.8f)),
            new PuzzleNode("N5", new Vector2(5.2f, 3.7f)),
            new PuzzleNode("N6", new Vector2(4.4f, 0.1f)),
            new PuzzleNode("N7", new Vector2(2.7f, -1.9f)),
            new PuzzleNode("N8", new Vector2(0.5f, -0.8f)),
            new PuzzleNode("B", new Vector2(6.2f, -3.1f))
        };

        connections = new List<PuzzleConnection>
        {
            new PuzzleConnection("A_N1", "A", "N1", 6f),
            new PuzzleConnection("N1_N2", "N1", "N2", 5f),
            new PuzzleConnection("N1_N3", "N1", "N3", 8f),
            new PuzzleConnection("N2_N3", "N2", "N3", 4f),
            new PuzzleConnection("N2_N8", "N2", "N8", 7f),
            new PuzzleConnection("N3_N4", "N3", "N4", 6f),
            new PuzzleConnection("N4_N5", "N4", "N5", 5f),
            new PuzzleConnection("N4_N6", "N4", "N6", 4f),
            new PuzzleConnection("N5_B", "N5", "B", 10f),
            new PuzzleConnection("N6_N7", "N6", "N7", 3f),
            new PuzzleConnection("N7_B", "N7", "B", 11f),
            new PuzzleConnection("N7_N8", "N7", "N8", 4f),
            new PuzzleConnection("N8_N6", "N8", "N6", 5f),
            new PuzzleConnection("N8_B", "N8", "B", 7f, bidirectional: false)
        };

        RebuildNodeConnectionLinks();
    }

    private float CalculateMaximumRouteDistance()
    {
        PuzzleNode startNode = GetNode(startNodeId);
        PuzzleNode endNode = GetNode(endNodeId);
        if (startNode == null || endNode == null)
            return 0f;

        bool[] usedConnections = new bool[connections.Count];
        return ExploreMaximumDistance(startNodeId, usedConnections, 0f);
    }

    private float ExploreMaximumDistance(string nodeId, bool[] usedConnections, float runningDistance)
    {
        if (nodeId == endNodeId)
            return runningDistance;

        float bestDistance = float.NegativeInfinity;

        for (int i = 0; i < connections.Count; i++)
        {
            if (usedConnections[i])
                continue;

            PuzzleConnection connection = connections[i];
            if (!connection.CanTraverseFrom(nodeId))
                continue;

            string nextNodeId = connection.GetOtherNodeId(nodeId);
            if (string.IsNullOrEmpty(nextNodeId))
                continue;

            usedConnections[i] = true;
            float candidateDistance = ExploreMaximumDistance(nextNodeId, usedConnections, runningDistance + connection.Length);
            if (candidateDistance > bestDistance)
                bestDistance = candidateDistance;
            usedConnections[i] = false;
        }

        return bestDistance;
    }

    private void RebuildNodeConnectionLinks()
    {
        Dictionary<string, PuzzleNode> nodeLookup = nodes.ToDictionary(node => node.NodeId, node => node);

        foreach (PuzzleNode node in nodes)
            node.SetConnectedConnections(Array.Empty<PuzzleConnection>());

        foreach (PuzzleConnection connection in connections)
        {
            if (nodeLookup.TryGetValue(connection.StartNodeId, out PuzzleNode startNode))
                startNode.AddConnectedConnection(connection);

            if (nodeLookup.TryGetValue(connection.EndNodeId, out PuzzleNode endNode) && connection.IsBidirectional)
                endNode.AddConnectedConnection(connection);
        }
    }

    private void RaiseCurrentNodeChanged()
    {
        CurrentNodeChanged?.Invoke(GetCurrentNode());
    }

    private LongestPathPuzzleCompletionState GetCompletionState()
    {
        if (!hasKnownOptimalDistance)
            return LongestPathPuzzleCompletionState.ValidRoute;

        return Mathf.Approximately(totalDistance, knownOptimalDistance)
            ? LongestPathPuzzleCompletionState.OptimalRoute
            : LongestPathPuzzleCompletionState.ValidRoute;
    }
}
