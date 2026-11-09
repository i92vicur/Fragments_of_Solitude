/*
LongestPathPuzzleBoardView.cs

Purpose:
- Controls the longest path puzzle board view flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class LongestPathPuzzleBoardView : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LongestPathPuzzleManager puzzleManager;

    [Header("Layout")]
    [SerializeField] private float positionScale = 120f;
    [SerializeField] private float roadThickness = 30f;
    [SerializeField] private float nodeSize = 42f;
    [SerializeField] private bool showConnectionLengthLabels = true;
    [SerializeField] private bool showIntermediateNodeLabels = true;

    [Header("Colors")]
    [SerializeField] private Color boardBackgroundColor = new Color(0.10f, 0.11f, 0.13f, 0.90f);
    [SerializeField] private Color roadColor = new Color(0.83f, 0.74f, 0.55f, 1f);
    [SerializeField] private Color usedRoadColor = new Color(0.33f, 0.79f, 0.93f, 1f);
    [SerializeField] private Color nodeColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color currentNodeColor = new Color(0.98f, 0.86f, 0.28f, 1f);
    [SerializeField] private Color startColor = new Color(0.27f, 0.72f, 0.40f, 1f);
    [SerializeField] private Color endColor = new Color(0.85f, 0.28f, 0.30f, 1f);
    [SerializeField] private Color labelColor = Color.white;

    [Header("Gamepad")]
    [SerializeField, Range(1f, 1.3f)] private float selectionZoomScale = 1.12f;
    [SerializeField, Min(0.01f)] private float selectionZoomDurationSeconds = 0.12f;

    private RectTransform boardRoot;
    private RectTransform generatedRoot;
    private Sprite whiteSprite;
    private Text statusText;
    private GameObject completionPanel;
    private Text completionTitleText;
    private Text completionResultText;
    private Button repeatPuzzleButton;
    private Text repeatPuzzleButtonLabel;
    private Text resetButtonLabel;
    private Text backToMenuButtonLabel;
    private readonly Dictionary<string, Image> roadImagesByConnectionId = new Dictionary<string, Image>();
    private readonly Dictionary<string, Image> nodeImagesByNodeId = new Dictionary<string, Image>();

    private const float GamepadMinDirectionAlignment = 0.5f;

    private string gamepadCursorNodeId;
    private string gamepadKnownCurrentNodeId;
    private bool gamepadCursorActive;
    private bool gamepadNavigationLatched;
    private Vector3 lastMousePosition;
    private RectTransform zoomedNodeRect;
    private Vector3 zoomedBaseScale = Vector3.one;
    private float zoomProgress;

    private void Awake()
    {
        boardRoot = GetComponent<RectTransform>();
        EnsureManager();

        GamepadMenuNavigation.EnsureGameInput();
        GameInput.SetContext(GameInputContext.Menu);

        // Evita que el StandaloneInputModule de la escena dispare Submit/navegación en paralelo al mando propio.
        EventSystem es = FindAnyObjectByType<EventSystem>();
        if (es != null)
            es.sendNavigationEvents = false;

        lastMousePosition = Input.mousePosition;
    }

    private void OnEnable()
    {
        EnsureManager();
        SubscribeToManager();
    }

    private void Update()
    {
        if (puzzleManager == null)
            return;

        UpdateGamepadInput();
        UpdateSelectionZoom();
    }

    private void OnDisable()
    {
        RestoreZoomedNode();

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

        RestoreZoomedNode();
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

        gamepadKnownCurrentNodeId = puzzleManager.CurrentNodeId;
        gamepadCursorNodeId = gamepadKnownCurrentNodeId;

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
        backdropRect.sizeDelta = new Vector2(1500f, 750f);
        backdropRect.pivot = new Vector2(0.5f, 0.5f);
        Image backdropImage = backdrop.GetComponent<Image>();
        backdropImage.sprite = GetWhiteSprite();
        backdropImage.color = boardBackgroundColor;

        Color zoneA = new Color(0.20f, 0.26f, 0.28f, 0.35f);
        Color zoneB = new Color(0.24f, 0.20f, 0.27f, 0.30f);
        Color zoneC = new Color(0.18f, 0.22f, 0.25f, 0.28f);

        CreateMapZone("ZoneA", new Vector2(-260f, 180f), new Vector2(360f, 220f), zoneA);
        CreateMapZone("ZoneB", new Vector2(120f, -90f), new Vector2(500f, 250f), zoneB);
        CreateMapZone("ZoneC", new Vector2(-110f, -220f), new Vector2(420f, 200f), zoneC);
    }

    private void CreateMapZone(string name, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject zone = CreateUiObject(name, generatedRoot, true);
        RectTransform zoneRect = zone.GetComponent<RectTransform>();
        zoneRect.anchorMin = new Vector2(0.5f, 0.5f);
        zoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        zoneRect.pivot = new Vector2(0.5f, 0.5f);
        zoneRect.anchoredPosition = anchoredPosition;
        zoneRect.sizeDelta = size;

        Image zoneImage = zone.GetComponent<Image>();
        zoneImage.sprite = GetWhiteSprite();
        zoneImage.color = color;
    }

    private void CreateHudText()
    {
        GameObject statusObject = CreateUiObject("StatusText", generatedRoot, false);
        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(-154f, -38f);
        statusRect.sizeDelta = new Vector2(300f, 128f);

        statusText = statusObject.AddComponent<Text>();
        statusText.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        statusText.fontSize = 17;
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.color = Color.white;
        statusText.raycastTarget = false;
        statusText.text = "Distance: 0\n\nStart: A\n\nGoal: B\n";
    }

    private void CreateResetButton()
    {
        GameObject buttonObject = CreateUiObject("ResetRouteButton", generatedRoot, true);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-1100f, -628f);
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
        resetButtonLabel.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        resetButtonLabel.fontSize = 16;
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
        buttonRect.anchoredPosition = new Vector2(-1100f, -680f);
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
        backToMenuButtonLabel.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        backToMenuButtonLabel.fontSize = 16;
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
        completionTitleText.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        completionTitleText.fontSize = 19;
        //completionTitleText.fontStyle = FontStyle.Bold;
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
        completionResultText.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        completionResultText.fontSize = 17;
        //completionResultText.fontStyle = FontStyle.Bold;
        completionResultText.alignment = TextAnchor.MiddleCenter;
        completionResultText.color = new Color(0.92f, 0.93f, 0.96f, 1f);
        completionResultText.raycastTarget = false;
        completionResultText.text = "Valid route";

        GameObject repeatButtonObject = CreateUiObject("RepeatPuzzleButton", completionPanel.transform, true);
        RectTransform repeatButtonRect = repeatButtonObject.GetComponent<RectTransform>();
        repeatButtonRect.anchorMin = new Vector2(0.5f, 0f);
        repeatButtonRect.anchorMax = new Vector2(0.5f, 0f);
        repeatButtonRect.pivot = new Vector2(0.5f, 0f);
        repeatButtonRect.anchoredPosition = new Vector2(-90f, 12f);
        repeatButtonRect.sizeDelta = new Vector2(150f, 32f);

        Image repeatButtonImage = repeatButtonObject.GetComponent<Image>();
        repeatButtonImage.sprite = GetWhiteSprite();
        repeatButtonImage.color = new Color(0.24f, 0.24f, 0.35f, 1f);

        repeatPuzzleButton = repeatButtonObject.AddComponent<Button>();
        repeatPuzzleButton.transition = Selectable.Transition.ColorTint;
        repeatPuzzleButton.targetGraphic = repeatButtonImage;
        repeatPuzzleButton.onClick.AddListener(HandleRepeatPuzzleClicked);

        GameObject repeatLabelObject = CreateUiObject("RepeatPuzzleLabel", repeatButtonObject.transform, false);
        RectTransform repeatLabelRect = repeatLabelObject.GetComponent<RectTransform>();
        repeatLabelRect.anchorMin = Vector2.zero;
        repeatLabelRect.anchorMax = Vector2.one;
        repeatLabelRect.offsetMin = Vector2.zero;
        repeatLabelRect.offsetMax = Vector2.zero;

        repeatPuzzleButtonLabel = repeatLabelObject.AddComponent<Text>();
        repeatPuzzleButtonLabel.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        repeatPuzzleButtonLabel.fontSize = 16;
        //repeatPuzzleButtonLabel.fontStyle = FontStyle.Bold;
        repeatPuzzleButtonLabel.alignment = TextAnchor.MiddleCenter;
        repeatPuzzleButtonLabel.color = Color.white;
        repeatPuzzleButtonLabel.raycastTarget = false;
        repeatPuzzleButtonLabel.text = "Repeat Puzzle";

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

        GameObject roadShadow = CreateUiObject($"RoadShadow_{connection.ConnectionId}", generatedRoot, false);
        RectTransform roadShadowRect = roadShadow.GetComponent<RectTransform>();
        roadShadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        roadShadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        roadShadowRect.pivot = new Vector2(0.5f, 0.5f);
        roadShadowRect.anchoredPosition = midpoint + Vector2.down * 10f;
        roadShadowRect.sizeDelta = new Vector2(length + 14f, roadThickness + 12f);
        roadShadowRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image roadShadowImage = roadShadow.AddComponent<Image>();
        roadShadowImage.sprite = GetWhiteSprite();
        roadShadowImage.color = new Color(0.0f, 0.0f, 0.0f, 0.20f);
        roadShadowImage.raycastTarget = false;

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
        roadImage.type = Image.Type.Sliced;

        Button roadButton = road.AddComponent<Button>();
        roadButton.transition = Selectable.Transition.None;
        roadButton.targetGraphic = roadImage;
        roadButton.onClick.AddListener(() => HandleConnectionSelected(connection.ConnectionId));

        roadImagesByConnectionId[connection.ConnectionId] = roadImage;

        if (!showConnectionLengthLabels)
            return;

        Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
        float labelDistance = Mathf.Clamp(length * 0.16f, 18f, 42f);
        Vector2 labelOffset = normal * labelDistance;

        GameObject label = CreateUiObject($"RoadLength_{connection.ConnectionId}", generatedRoot, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = midpoint + labelOffset;
        labelRect.sizeDelta = new Vector2(62f, 28f);
        labelRect.localRotation = Quaternion.identity;

        Text labelText = label.AddComponent<Text>();
        labelText.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        labelText.fontSize = 16;
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

        GameObject nodeShadow = CreateUiObject($"NodeShadow_{node.NodeId}", nodeObject.transform, false);
        RectTransform nodeShadowRect = nodeShadow.GetComponent<RectTransform>();
        nodeShadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        nodeShadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        nodeShadowRect.pivot = new Vector2(0.5f, 0.5f);
        nodeShadowRect.anchoredPosition = new Vector2(0f, -4f);
        nodeShadowRect.sizeDelta = Vector2.one * (isStartNode || isEndNode ? nodeSize * 1.6f : nodeSize * 1.45f);

        Image nodeShadowImage = nodeShadow.AddComponent<Image>();
        nodeShadowImage.sprite = GetWhiteSprite();
        nodeShadowImage.color = new Color(0f, 0f, 0f, 0.22f);
        nodeShadowImage.raycastTarget = false;

        Image nodeImage = nodeObject.AddComponent<Image>();
        nodeImage.sprite = GetWhiteSprite();
        nodeImage.color = isStartNode ? startColor : isEndNode ? endColor : nodeColor;
        nodeImage.raycastTarget = true;
        nodeImage.type = Image.Type.Simple;

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
        nodeLabel.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        nodeLabel.fontSize = isStartNode || isEndNode ? 24 : 16;
        nodeLabel.alignment = TextAnchor.MiddleCenter;
        nodeLabel.color = isStartNode || isEndNode ? Color.white : new Color(0.14f, 0.14f, 0.16f, 1f);
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

    private void UpdateGamepadInput()
    {
        if (GameInput.Cancel.Pressed)
        {
            RestoreZoomedNode();
            HandleBackToMenuClicked();
            return;
        }

        // Cualquier cambio del nodo actual (mando, ratón o reset) recoloca el cursor sobre él.
        if (puzzleManager.CurrentNodeId != gamepadKnownCurrentNodeId)
        {
            gamepadKnownCurrentNodeId = puzzleManager.CurrentNodeId;
            gamepadCursorNodeId = gamepadKnownCurrentNodeId;
        }

        // Ratón o clic ocultan el cursor de mando; el stick o A lo reactivan.
        if (Input.mousePosition != lastMousePosition || Input.GetMouseButtonDown(0))
            gamepadCursorActive = false;
        lastMousePosition = Input.mousePosition;

        UpdateGamepadCursorFromStick();

        if (GameInput.Submit.Pressed)
        {
            gamepadCursorActive = true;
            // Misma ruta que el clic: sobre el nodo actual deshace, sobre otro nodo intenta avanzar.
            HandleNodeSelected(gamepadCursorNodeId);
        }
    }

    private void UpdateGamepadCursorFromStick()
    {
        if (!GamepadMenuNavigation.NavigateActive)
        {
            gamepadNavigationLatched = false;
            return;
        }

        if (gamepadNavigationLatched)
            return;

        gamepadNavigationLatched = true;
        gamepadCursorActive = true;

        Vector2 nav = GameInput.Navigate;
        Vector2 direction = Mathf.Abs(nav.x) > Mathf.Abs(nav.y)
            ? new Vector2(Mathf.Sign(nav.x), 0f)
            : new Vector2(0f, Mathf.Sign(nav.y));

        string nextNodeId = FindGamepadNeighbourNodeId(direction);
        if (!string.IsNullOrEmpty(nextNodeId))
            gamepadCursorNodeId = nextNodeId;
    }

    // Candidatos: nodo actual (para deshacer) y destinos legales desde él. Nunca nodos desconectados.
    private List<string> GetGamepadCandidateNodeIds()
    {
        List<string> candidateNodeIds = new List<string> { puzzleManager.CurrentNodeId };
        if (puzzleManager.IsPuzzleFinished)
            return candidateNodeIds;

        foreach (PuzzleConnection connection in puzzleManager.GetAvailableConnectionsFromCurrentNode())
        {
            string nextNodeId = connection.GetOtherNodeId(puzzleManager.CurrentNodeId);
            if (!candidateNodeIds.Contains(nextNodeId))
                candidateNodeIds.Add(nextNodeId);
        }

        return candidateNodeIds;
    }

    private string FindGamepadNeighbourNodeId(Vector2 direction)
    {
        PuzzleNode cursorNode = puzzleManager.GetNode(gamepadCursorNodeId);
        if (cursorNode == null)
            return null;

        string bestNodeId = null;
        float bestScore = float.MaxValue;

        foreach (string candidateNodeId in GetGamepadCandidateNodeIds())
        {
            if (candidateNodeId == cursorNode.NodeId)
                continue;

            PuzzleNode candidate = puzzleManager.GetNode(candidateNodeId);
            if (candidate == null)
                continue;

            Vector2 offset = candidate.WorldPosition - cursorNode.WorldPosition;
            float distance = offset.magnitude;
            if (distance <= Mathf.Epsilon)
                continue;

            float alignment = Vector2.Dot(offset / distance, direction);
            if (alignment < GamepadMinDirectionAlignment)
                continue;

            // Penaliza los candidatos desviados del eje para evitar saltos erráticos en diagonal.
            float score = distance * (1f + 2f * (1f - alignment));
            if (score < bestScore)
            {
                bestScore = score;
                bestNodeId = candidateNodeId;
            }
        }

        return bestNodeId;
    }

    private void UpdateSelectionZoom()
    {
        RectTransform target = gamepadCursorActive ? GetNodeRect(gamepadCursorNodeId) : null;
        if (target != zoomedNodeRect)
        {
            RestoreZoomedNode();
            zoomedNodeRect = target;
            if (zoomedNodeRect != null)
            {
                zoomedBaseScale = zoomedNodeRect.localScale;
                zoomProgress = 0f;
            }
        }

        if (zoomedNodeRect == null)
            return;

        zoomProgress = Mathf.MoveTowards(zoomProgress, 1f, Time.deltaTime / Mathf.Max(0.0001f, selectionZoomDurationSeconds));
        zoomedNodeRect.localScale = Vector3.Lerp(zoomedBaseScale, zoomedBaseScale * selectionZoomScale, zoomProgress);
    }

    private void RestoreZoomedNode()
    {
        if (zoomedNodeRect != null)
            zoomedNodeRect.localScale = zoomedBaseScale;

        zoomedNodeRect = null;
        zoomProgress = 0f;
    }

    private RectTransform GetNodeRect(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !nodeImagesByNodeId.TryGetValue(nodeId, out Image nodeImage) || nodeImage == null)
            return null;

        return nodeImage.rectTransform;
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

        if (nodeId == puzzleManager.CurrentNodeId)
        {
            puzzleManager.UndoLastMove();
            return;
        }

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

    private void HandleRepeatPuzzleClicked()
    {
        HandleResetRouteClicked();
        if (completionPanel != null)
            completionPanel.SetActive(false);
    }

    private void HandleRouteCompleted(LongestPathPuzzleCompletionState completionState, float finalDistance)
    {
        if (completionPanel == null)
            return;

        completionPanel.SetActive(true);

        if (completionTitleText != null)
            completionTitleText.text = completionState == LongestPathPuzzleCompletionState.OptimalRoute
                ? "Route complete!"
                : "Route ends at B";

        if (completionResultText != null)
        {
            if (completionState == LongestPathPuzzleCompletionState.OptimalRoute)
            {
                completionResultText.text = $"Optimal route!\nDistance: {Mathf.RoundToInt(finalDistance)}\nBest: {Mathf.RoundToInt(puzzleManager.KnownOptimalDistance)}";
            }
            else
            {
                completionResultText.text = $"Not optimal.\nDistance: {Mathf.RoundToInt(finalDistance)}\nBest: {Mathf.RoundToInt(puzzleManager.KnownOptimalDistance)}";
            }
        }

        if (statusText != null)
            statusText.text = $"Distance: {Mathf.RoundToInt(finalDistance)}\nStart: {puzzleManager.StartNodeId}\nGoal: {puzzleManager.EndNodeId}\nOptimal: {Mathf.RoundToInt(puzzleManager.KnownOptimalDistance)}";
    }

    private void HandleBackToMenuClicked()
    {
        SceneLoader loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
            loader.LoadPuzzleMenu();
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

        if (repeatPuzzleButtonLabel != null)
            repeatPuzzleButtonLabel.text = "Repeat Puzzle";

        if (completionPanel != null && !puzzleManager.IsPuzzleFinished)
            completionPanel.SetActive(false);

        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        if (statusText == null || puzzleManager == null)
            return;

        statusText.text = $"Distance: {Mathf.RoundToInt(puzzleManager.TotalDistance)}\nStart: {puzzleManager.StartNodeId}\nGoal: {puzzleManager.EndNodeId}\nOptimal: {Mathf.RoundToInt(puzzleManager.KnownOptimalDistance)}";
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
