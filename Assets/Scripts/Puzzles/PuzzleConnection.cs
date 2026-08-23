using UnityEngine;

[System.Serializable]
public sealed class PuzzleConnection
{
    [SerializeField] private string connectionId;
    [SerializeField] private string startNodeId;
    [SerializeField] private string endNodeId;
    [SerializeField, Min(0f)] private float length = 1f;
    [SerializeField] private bool bidirectional = true;
    [SerializeField] private bool hasBeenUsed;

    public string ConnectionId => connectionId;
    public string StartNodeId => startNodeId;
    public string EndNodeId => endNodeId;
    public float Length => length;
    public bool IsBidirectional => bidirectional;
    public bool HasBeenUsed => hasBeenUsed;

    public PuzzleConnection(string connectionId, string startNodeId, string endNodeId, float length, bool bidirectional = true)
    {
        this.connectionId = connectionId;
        this.startNodeId = startNodeId;
        this.endNodeId = endNodeId;
        this.length = Mathf.Max(0f, length);
        this.bidirectional = bidirectional;
    }

    public bool Connects(string nodeId)
    {
        return startNodeId == nodeId || (bidirectional && endNodeId == nodeId);
    }

    public bool CanTraverseFrom(string nodeId)
    {
        return startNodeId == nodeId || (bidirectional && endNodeId == nodeId);
    }

    public string GetOtherNodeId(string nodeId)
    {
        if (nodeId == startNodeId)
            return endNodeId;

        if (bidirectional && nodeId == endNodeId)
            return startNodeId;

        return string.Empty;
    }

    public void MarkUsed()
    {
        hasBeenUsed = true;
    }

    public void ResetUsage()
    {
        hasBeenUsed = false;
    }
}
