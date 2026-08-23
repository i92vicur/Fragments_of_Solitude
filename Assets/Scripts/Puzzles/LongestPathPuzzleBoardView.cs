using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class LongestPathPuzzleBoardView : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LongestPathPuzzleManager puzzleManager;

    [Header("Layout")]
    [SerializeField] private float positionScale = 110f;
    [SerializeField] private float roadThickness = 16f;
    [SerializeField] private float nodeSize = 34f;
    [SerializeField] private bool showConnectionLengthLabels = true;
    [SerializeField] private bool showIntermediateNodeLabels = true;
    [SerializeField] private bool showResetRouteButton = true;

    [Header("Colors")]
    [SerializeField] private Color boardBackgroundColor = new Color(0.10f, 0.11f, 0.13f, 0.90f);
    [SerializeField] private Color roadColor = new Color(0.83f, 0.74f, 0.55f, 1f);
    [SerializeField] private Color usedRoadColor = new Color(0.33f, 0.79f, 0.93f, 1f);
    [SerializeField] private Color nodeColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color currentNodeColor = new Color(0.98f, 0.86f, 0.28f, 1f);
    [SerializeField] private Color startColor = new Color(0.27f, 0.72f, 0.40f, 1f);
    [SerializeField] private Color endColor = new Color(0.85f, 0.28f, 0.30f, 1f);
    [SerializeField] private Color labelColor = Color.white;

    private RectTransform boardRoot;
    private RectTransform generatedRoot;
    private Sprite whiteSprite;
    private Text statusText;
    private GameObject completionPanel;
    private Text completionTitleText;
    private Text completionResultText;
    private Text resetButtonLabel;
    private Text backToMenuButtonLabel;
    private readonly Dictionary<string, Image> roadImagesByConnectionId = new Dictionary<string, Image>();
    private readonly Dictionary<string, Image> nodeImagesByNodeId = new Dictionary<string, Image>();

    private void Awake()
    {
        boardRoot = GetComponent<RectTransform>();
        EnsureManager();
    }

    private void OnEnable()
    {
        EnsureManager();
        SubscribeToManager();
    }

    private void OnDisable()
    {
        if (puzzleManager == null)
            return;

        puzzleManager.ConnectionUsed -= HandleConnectionUsed;
        puzzleManager.CurrentNodeChanged -= HandleCurrentNodeChanged;
        puzzleManager.PuzzleReset -= HandlePuzzleReset;
        puzzleManager.PuzzleStateChanged -= HandlePuzzleStateChanged;
        puzzleManager.RouteCompleted -= HandleRouteCompleted;
    }

    private void Start()
    {
        EnsureManager();
        RebuildVisualBoard();
    }

    public void RebuildVisualBoard()
    {
        if (puzzleManager == null)
            return;

        roadImagesByConnectionId.Clear();
        nodeImagesByNodeId.Clear();
        EnsureGeneratedRoot();
        DisableLegacySceneChildren();
        ClearGeneratedChildren();

        CreateBoardBackdrop();
        CreateHudText();
        CreateCompletionPanel();
        CreateResetButton();
        CreateBackToMenuButton();

        IReadOnlyList<PuzzleNode> nodes = puzzleManager.Nodes;
        IReadOnlyList<PuzzleConnection> connections = puzzleManager.Connections;

        for (int i = 0; i < connections.Count; i++)
            CreateRoadView(connections[i], nodes);

        for (int i = 0; i < nodes.Count; i++)
            CreateNodeView(nodes[i], i);

        RefreshVisualState();
    }

    private void EnsureManager()
    {
        if (puzzleManager != null)
            return;

        puzzleManager = GetComponent<LongestPathPuzzleManager>();
        if (puzzleManager == null)
            puzzleManager = FindAnyObjectByType<LongestPathPuzzleManager>();
    }

    private void EnsureGeneratedRoot()
    {
        if (generatedRoot != null)
            return;

        GameObject generatedRootObject = new GameObject("GeneratedLongestPathVisuals", typeof(RectTransform));
        generatedRootObject.transform.SetParent(boardRoot, false);

        generatedRoot = generatedRootObject.GetComponent<RectTransform>();
        generatedRoot.anchorMin = Vector2.zero;
        generatedRoot.anchorMax = Vector2.one;
        generatedRoot.offsetMin = Vector2.zero;
        generatedRoot.offsetMax = Vector2.zero;
    }

    private void ClearGeneratedChildren()
    {
        for (int i = generatedRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = generatedRoot.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private void CreateBoardBackdrop()
    {
        GameObject backdrop = CreateUiObject("BoardBackdrop", generatedRoot, true);
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = new Vector2(0.5f, 0.5f);
        backdropRect.anchorMax = new Vector2(0.5f, 0.5f);
        backdropRect.anchoredPosition = Vector2.zero;
        backdropRect.sizeDelta = new Vector2(1100f, 720f);
        backdropRect.pivot = new Vector2(0.5f, 0.5f);
        Image backdropImage = backdrop.GetComponent<Image>();
        backdropImage.sprite = GetWhiteSprite();
        backdropImage.color = boardBackgroundColor;
    }

    private void CreateHudText()
    {
        GameObject statusObject = CreateUiObject("StatusText", generatedRoot, false);
        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(24f, -18f);
        statusRect.sizeDelta = new Vector2(300f, 88f);

        statusText = statusObject.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 20;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.color = Color.white;
        statusText.raycastTarget = false;
        statusText.text = "Distance: 0\nStart: A\nGoal: B";
    }

    private void CreateResetButton()
    {
        GameObject buttonObject = CreateUiObject("ResetRouteButton", generatedRoot, true);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-24f, -18f);
        buttonRect.sizeDelta = new Vector2(170f, 38f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = GetWhiteSprite();
        buttonImage.color = new Color(0.22f, 0.22f, 0.25f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(HandleResetRouteClicked);

        GameObject labelObject = CreateUiObject("Text", buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        resetButtonLabel = labelObject.AddComponent<Text>();
        resetButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resetButtonLabel.fontSize = 18;
        resetButtonLabel.fontStyle = FontStyle.Bold;
        resetButtonLabel.alignment = TextAnchor.MiddleCenter;
        resetButtonLabel.color = Color.white;
        resetButtonLabel.raycastTarget = false;
        resetButtonLabel.text = "Reset Route";
    }

    private void CreateBackToMenuButton()
    {
        GameObject buttonObject = CreateUiObject("BackToMenuButton", generatedRoot, true);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-24f, -66f);
        buttonRect.sizeDelta = new Vector2(170f, 38f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = GetWhiteSprite();
        buttonImage.color = new Color(0.22f, 0.22f, 0.25f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(HandleBackToMenuClicked);

        GameObject labelObject = CreateUiObject("Text", buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        backToMenuButtonLabel = labelObject.AddComponent<Text>();
        backToMenuButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        backToMenuButtonLabel.fontSize = 18;
        backToMenuButtonLabel.fontStyle = FontStyle.Bold;
        backToMenuButtonLabel.alignment = TextAnchor.MiddleCenter;
        backToMenuButtonLabel.color = Color.white;
        backToMenuButtonLabel.raycastTarget = false;
        backToMenuButtonLabel.text = "Back to Menu";
    }

    private void CreateCompletionPanel()
    {
        completionPanel = CreateUiObject("CompletionPanel", generatedRoot, true);
        RectTransform panelRect = completionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 180f);
        panelRect.sizeDelta = new Vector2(320f, 120f);

        Image panelImage = completionPanel.GetComponent<Image>();
        panelImage.sprite = GetWhiteSprite();
        panelImage.color = new Color(0.05f, 0.05f, 0.06f, 0.92f);

        GameObject titleObject = CreateUiObject("CompletionTitle", completionPanel.transform, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -16f);
        titleRect.sizeDelta = new Vector2(0f, 30f);

        completionTitleText = titleObject.AddComponent<Text>();
        completionTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        completionTitleText.fontSize = 22;
        completionTitleText.fontStyle = FontStyle.Bold;
        completionTitleText.alignment = TextAnchor.MiddleCenter;
        completionTitleText.color = Color.white;
        completionTitleText.raycastTarget = false;
        completionTitleText.text = "Route complete!";

        GameObject resultObject = CreateUiObject("CompletionResult", completionPanel.transform, false);
        RectTransform resultRect = resultObject.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0f, 0f);
        resultRect.anchorMax = new Vector2(1f, 1f);
        resultRect.offsetMin = new Vector2(20f, 16f);
        resultRect.offsetMax = new Vector2(-20f, -44f);

        completionResultText = resultObject.AddComponent<Text>();
        completionResultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        completionResultText.fontSize = 20;
        completionResultText.fontStyle = FontStyle.Bold;
        completionResultText.alignment = TextAnchor.MiddleCenter;
        completionResultText.color = new Color(0.92f, 0.93f, 0.96f, 1f);
        completionResultText.raycastTarget = false;
        completionResultText.text = "Valid route";

        completionPanel.SetActive(false);
    }

    private void CreateRoadView(PuzzleConnection connection, IReadOnlyList<PuzzleNode> nodes)
    {
        PuzzleNode startNode = FindNode(nodes, connection.StartNodeId);
        PuzzleNode endNode = FindNode(nodes, connection.EndNodeId);
        if (startNode == null || endNode == null)
            return;

        Vector2 startPosition = ToLocalPosition(startNode.WorldPosition);
        Vector2 endPosition = ToLocalPosition(endNode.WorldPosition);
        Vector2 midpoint = (startPosition + endPosition) * 0.5f;
        Vector2 delta = endPosition - startPosition;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        GameObject road = CreateUiObject($"Road_{connection.ConnectionId}", generatedRoot, false);
        RectTransform roadRect = road.GetComponent<RectTransform>();
        roadRect.anchorMin = new Vector2(0.5f, 0.5f);
        roadRect.anchorMax = new Vector2(0.5f, 0.5f);
        roadRect.pivot = new Vector2(0.5f, 0.5f);
        roadRect.anchoredPosition = midpoint;
        roadRect.sizeDelta = new Vector2(length, roadThickness);
        roadRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image roadImage = road.AddComponent<Image>();
        roadImage.sprite = GetWhiteSprite();
        roadImage.color = roadColor;
        roadImage.raycastTarget = true;

        Button roadButton = road.AddComponent<Button>();
        roadButton.transition = Selectable.Transition.None;
        roadButton.targetGraphic = roadImage;
        roadButton.onClick.AddListener(() => HandleConnectionSelected(connection.ConnectionId));

        roadImagesByConnectionId[connection.ConnectionId] = roadImage;

        if (!showConnectionLengthLabels)
            return;

        Vector2 labelOffset = new Vector2(-delta.y, delta.x).normalized * 18f;

        GameObject label = CreateUiObject($"RoadLength_{connection.ConnectionId}", generatedRoot, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = midpoint + labelOffset;
        labelRect.sizeDelta = new Vector2(60f, 24f);
        labelRect.localRotation = Quaternion.identity;

        Text labelText = label.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 18;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = labelColor;
        labelText.raycastTarget = false;
        labelText.text = Mathf.RoundToInt(connection.Length).ToString();
    }

    private void CreateNodeView(PuzzleNode node, int index)
    {
        bool isStartNode = node.NodeId == puzzleManager.StartNodeId;
        bool isEndNode = node.NodeId == puzzleManager.EndNodeId;

        GameObject nodeObject = CreateUiObject($"Node_{node.NodeId}", generatedRoot, false);
        RectTransform nodeRect = nodeObject.GetComponent<RectTransform>();
        nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
        nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
        nodeRect.pivot = new Vector2(0.5f, 0.5f);
        nodeRect.sizeDelta = Vector2.one * (isStartNode || isEndNode ? nodeSize * 1.35f : nodeSize);
        nodeRect.anchoredPosition = ToLocalPosition(node.WorldPosition);

        Image nodeImage = nodeObject.AddComponent<Image>();
        nodeImage.sprite = GetWhiteSprite();
        nodeImage.color = isStartNode ? startColor : isEndNode ? endColor : nodeColor;
        nodeImage.raycastTarget = true;

        Button nodeButton = nodeObject.AddComponent<Button>();
        nodeButton.transition = Selectable.Transition.None;
        nodeButton.targetGraphic = nodeImage;
        nodeButton.onClick.AddListener(() => HandleNodeSelected(node.NodeId));

        nodeImagesByNodeId[node.NodeId] = nodeImage;

        if (!showIntermediateNodeLabels && !isStartNode && !isEndNode)
            return;

        GameObject textObject = CreateUiObject($"NodeLabel_{node.NodeId}", nodeObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        Text nodeLabel = textObject.AddComponent<Text>();
        nodeLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nodeLabel.fontStyle = FontStyle.Bold;
        nodeLabel.fontSize = isStartNode || isEndNode ? 28 : 18;
        nodeLabel.alignment = TextAnchor.MiddleCenter;
        nodeLabel.color = isStartNode || isEndNode ? Color.white : new Color(0.16f, 0.16f, 0.18f, 1f);
        nodeLabel.raycastTarget = false;
        nodeLabel.text = node.NodeId;
    }

    private GameObject CreateUiObject(string objectName, Transform parent, bool includeImage)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        uiObject.transform.SetParent(parent, false);

        if (includeImage)
            uiObject.AddComponent<Image>();

        return uiObject;
    }

    private void DisableLegacySceneChildren()
    {
        for (int i = 0; i < boardRoot.childCount; i++)
        {
            Transform child = boardRoot.GetChild(i);
            if (child == generatedRoot)
                continue;

            child.gameObject.SetActive(false);
        }
    }

    private void SubscribeToManager()
    {
        if (puzzleManager == null)
            return;

        puzzleManager.ConnectionUsed -= HandleConnectionUsed;
        puzzleManager.CurrentNodeChanged -= HandleCurrentNodeChanged;
        puzzleManager.PuzzleReset -= HandlePuzzleReset;
        puzzleManager.PuzzleStateChanged -= HandlePuzzleStateChanged;

        puzzleManager.ConnectionUsed += HandleConnectionUsed;
        puzzleManager.CurrentNodeChanged += HandleCurrentNodeChanged;
        puzzleManager.PuzzleReset += HandlePuzzleReset;
        puzzleManager.PuzzleStateChanged += HandlePuzzleStateChanged;
        puzzleManager.RouteCompleted += HandleRouteCompleted;
    }

    private void HandleConnectionSelected(string connectionId)
    {
        if (puzzleManager == null)
            return;

        puzzleManager.TryMoveAlongConnection(connectionId);
    }

    private void HandleNodeSelected(string nodeId)
    {
        if (puzzleManager == null)
            return;

        puzzleManager.TryMoveToNode(nodeId);
    }

    private void HandleConnectionUsed(PuzzleConnection connection)
    {
        if (connection == null)
            return;

        if (roadImagesByConnectionId.TryGetValue(connection.ConnectionId, out Image roadImage))
            roadImage.color = usedRoadColor;

        RefreshStatusText();
    }

    private void HandleCurrentNodeChanged(PuzzleNode currentNode)
    {
        RefreshVisualState();
    }

    private void HandlePuzzleReset()
    {
        if (completionPanel != null)
            completionPanel.SetActive(false);

        RebuildVisualBoard();
    }

    private void HandlePuzzleStateChanged(LongestPathPuzzleResult result)
    {
        RefreshStatusText();
    }

    private void HandleResetRouteClicked()
    {
        if (puzzleManager == null)
            return;

        puzzleManager.ResetRoute();
    }

    private void HandleRouteCompleted(LongestPathPuzzleCompletionState completionState, float finalDistance)
    {
        if (completionPanel != null)
            completionPanel.SetActive(true);

        if (completionTitleText != null)
            completionTitleText.text = "Route complete!";

        if (completionResultText != null)
            completionResultText.text = completionState == LongestPathPuzzleCompletionState.OptimalRoute
                ? "Optimal route!"
                : "Valid route";

        if (statusText != null)
            statusText.text = $"Distance: {Mathf.RoundToInt(finalDistance)}\nStart: {puzzleManager.StartNodeId}\nGoal: {puzzleManager.EndNodeId}";
    }

    private void HandleBackToMenuClicked()
    {
        SceneLoader loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
            loader.LoadMainMenu();
    }

    private void RefreshVisualState()
    {
        if (puzzleManager == null)
            return;

        foreach (KeyValuePair<string, Image> roadEntry in roadImagesByConnectionId)
        {
            PuzzleConnection connection = puzzleManager.GetConnection(roadEntry.Key);
            if (connection != null)
                roadEntry.Value.color = connection.HasBeenUsed ? usedRoadColor : roadColor;
        }

        foreach (KeyValuePair<string, Image> nodeEntry in nodeImagesByNodeId)
        {
            PuzzleNode node = puzzleManager.GetNode(nodeEntry.Key);
            if (node == null)
                continue;

            if (node.NodeId == puzzleManager.CurrentNodeId)
            {
                if (node.NodeId == puzzleManager.StartNodeId)
                    nodeEntry.Value.color = currentNodeColor;
                else if (node.NodeId == puzzleManager.EndNodeId)
                    nodeEntry.Value.color = endColor;
                else
                    nodeEntry.Value.color = currentNodeColor;
            }
            else if (node.NodeId == puzzleManager.StartNodeId)
            {
                nodeEntry.Value.color = startColor;
            }
            else if (node.NodeId == puzzleManager.EndNodeId)
            {
                nodeEntry.Value.color = endColor;
            }
            else
            {
                nodeEntry.Value.color = nodeColor;
            }
        }

        if (resetButtonLabel != null)
            resetButtonLabel.text = "Reset Route";

        if (backToMenuButtonLabel != null)
            backToMenuButtonLabel.text = "Back to Menu";

        if (completionPanel != null && !puzzleManager.IsPuzzleFinished)
            completionPanel.SetActive(false);

        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        if (statusText == null || puzzleManager == null)
            return;

        statusText.text = $"Distance: {Mathf.RoundToInt(puzzleManager.TotalDistance)}\nStart: {puzzleManager.StartNodeId}\nGoal: {puzzleManager.EndNodeId}";
    }

    private Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }

    private Vector2 ToLocalPosition(Vector2 graphPosition)
    {
        return graphPosition * positionScale;
    }

    private PuzzleNode FindNode(IReadOnlyList<PuzzleNode> nodes, string nodeId)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].NodeId == nodeId)
                return nodes[i];
        }

        return null;
    }
}
