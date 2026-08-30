using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ConstraintPuzzleUI : MonoBehaviour
{
    [SerializeField] private ConstraintPuzzleManager manager;
    [SerializeField] private SceneLoader sceneLoader;

    private string selectedItemId;
    private Text statusText;
    private Text titleText;
    private readonly Dictionary<int, Button> positionButtons = new Dictionary<int, Button>();
    private readonly Dictionary<string, Button> elementButtons = new Dictionary<string, Button>();

    private void Awake()
    {
        EnsureEventSystemModule();

        if (manager == null)
            manager = GetComponent<ConstraintPuzzleManager>();
        if (manager == null)
            manager = gameObject.AddComponent<ConstraintPuzzleManager>();

        if (sceneLoader == null)
            sceneLoader = GetComponentInParent<SceneLoader>();

        if (manager != null)
        {
            manager.LayoutChanged += RefreshUi;
            manager.PuzzleSolved += HandlePuzzleSolved;
        }

        BuildUi();
        RefreshUi();
    }

    private void EnsureEventSystemModule()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
            return;

        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.LayoutChanged -= RefreshUi;
            manager.PuzzleSolved -= HandlePuzzleSolved;
        }
    }

    private void BuildUi()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        Transform existingRoot = transform.Find("ConstraintPuzzleRoot");
        if (existingRoot != null)
            Destroy(existingRoot.gameObject);

        GameObject root = new GameObject("ConstraintPuzzleRoot", typeof(RectTransform));
        root.transform.SetParent(transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        CreateTitle(root.transform);
        CreateClueText(root.transform);
        CreatePositions(root.transform);
        CreateElements(root.transform);
        CreateControls(root.transform);
        CreateStatusText(root.transform);
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(parent, false);

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -80f);
        titleRect.sizeDelta = new Vector2(700f, 80f);

        titleText = titleObject.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.text = "COLOCA LOS ELEMENTOS";
    }

    private void CreateClueText(Transform parent)
    {
        GameObject clueObject = new GameObject("Clues", typeof(RectTransform), typeof(Text));
        clueObject.transform.SetParent(parent, false);

        RectTransform clueRect = clueObject.GetComponent<RectTransform>();
        clueRect.anchorMin = new Vector2(0.5f, 1f);
        clueRect.anchorMax = new Vector2(0.5f, 1f);
        clueRect.pivot = new Vector2(0.5f, 1f);
        clueRect.anchoredPosition = new Vector2(0f, -180f);
        clueRect.sizeDelta = new Vector2(700f, 180f);

        Text clueText = clueObject.GetComponent<Text>();
        clueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        clueText.fontSize = 22;
        clueText.alignment = TextAnchor.MiddleCenter;
        clueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        clueText.verticalOverflow = VerticalWrapMode.Overflow;
        clueText.color = new Color(0.9f, 0.92f, 0.96f, 1f);
        clueText.text = "PISTAS\n• A está en la posición 3 o 4.\n• B está en un extremo.\n• D está a la izquierda de E.\n• E está a la izquierda de F.\n• B está exactamente a 3 posiciones de D.";
    }

    private void CreatePositions(Transform parent)
    {
        GameObject positionsRoot = new GameObject("PositionsRoot", typeof(RectTransform));
        positionsRoot.transform.SetParent(parent, false);

        RectTransform positionsRect = positionsRoot.GetComponent<RectTransform>();
        positionsRect.anchorMin = new Vector2(0.5f, 0.5f);
        positionsRect.anchorMax = new Vector2(0.5f, 0.5f);
        positionsRect.pivot = new Vector2(0.5f, 0.5f);
        positionsRect.anchoredPosition = new Vector2(0f, 85f);
        positionsRect.sizeDelta = new Vector2(660f, 90f);

        for (int i = 1; i <= ConstraintPuzzleManager.PositionCount; i++)
        {
            CreatePositionSlot(positionsRoot.transform, i);
        }
    }

    private void CreatePositionSlot(Transform parent, int positionIndex)
    {
        GameObject slot = new GameObject($"Position{positionIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
        slot.transform.SetParent(parent, false);

        RectTransform slotRect = slot.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0f, 0.5f);
        slotRect.anchorMax = new Vector2(0f, 0.5f);
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.sizeDelta = new Vector2(90f, 70f);
        slotRect.anchoredPosition = new Vector2(-300f + (positionIndex - 1) * 100f, 0f);

        Image slotImage = slot.GetComponent<Image>();
        slotImage.color = new Color(0.18f, 0.18f, 0.21f, 1f);

        Button slotButton = slot.GetComponent<Button>();
        slotButton.targetGraphic = slotImage;
        int capturedIndex = positionIndex;
        slotButton.onClick.AddListener(() => HandlePositionClicked(capturedIndex));

        ConstraintPuzzleDropArea dropArea = slot.AddComponent<ConstraintPuzzleDropArea>();
        dropArea.Initialize(this, capturedIndex);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(slot.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 32;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = positionIndex.ToString();

        positionButtons[positionIndex] = slotButton;
    }

    private void CreateElements(Transform parent)
    {
        GameObject elementsRoot = new GameObject("ElementsRoot", typeof(RectTransform));
        elementsRoot.transform.SetParent(parent, false);

        RectTransform elementsRect = elementsRoot.GetComponent<RectTransform>();
        elementsRect.anchorMin = new Vector2(0.5f, 0.5f);
        elementsRect.anchorMax = new Vector2(0.5f, 0.5f);
        elementsRect.pivot = new Vector2(0.5f, 0.5f);
        elementsRect.anchoredPosition = new Vector2(0f, -30f);
        elementsRect.sizeDelta = new Vector2(660f, 90f);

        for (int i = 0; i < ConstraintPuzzleManager.ItemIds.Length; i++)
        {
            CreateElementButton(elementsRoot.transform, ConstraintPuzzleManager.ItemIds[i], i);
        }
    }

    private void CreateElementButton(Transform parent, string itemId, int index)
    {
        GameObject itemObject = new GameObject($"Item_{itemId}", typeof(RectTransform), typeof(Image), typeof(Button));
        itemObject.transform.SetParent(parent, false);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(0f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.sizeDelta = new Vector2(80f, 70f);
        itemRect.anchoredPosition = new Vector2(-300f + index * 100f, 0f);

        Image itemImage = itemObject.GetComponent<Image>();
        itemImage.color = new Color(0.14f, 0.25f, 0.42f, 1f);

        Button itemButton = itemObject.GetComponent<Button>();
        itemButton.targetGraphic = itemImage;
        itemButton.onClick.AddListener(() => HandleElementClicked(itemId));

        CanvasGroup itemGroup = itemObject.AddComponent<CanvasGroup>();
        itemGroup.blocksRaycasts = true;

        ConstraintPuzzleDraggableItem draggableItem = itemObject.AddComponent<ConstraintPuzzleDraggableItem>();
        draggableItem.Initialize(this, itemId);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(itemObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 28;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = itemId;

        elementButtons[itemId] = itemButton;
    }

    private void CreateControls(Transform parent)
    {
        GameObject controlsRoot = new GameObject("ControlsRoot", typeof(RectTransform));
        controlsRoot.transform.SetParent(parent, false);

        RectTransform controlsRect = controlsRoot.GetComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
        controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
        controlsRect.pivot = new Vector2(0.5f, 0.5f);
        controlsRect.anchoredPosition = new Vector2(0f, -170f);
        controlsRect.sizeDelta = new Vector2(600f, 120f);

        CreateButton(controlsRoot.transform, "CheckButton", "CHECK", new Vector2(-140f, 0f), HandleCheckClicked);
        CreateButton(controlsRoot.transform, "ResetButton", "RESET", new Vector2(0f, 0f), HandleResetClicked);
        CreateButton(controlsRoot.transform, "BackButton", "BACK TO PUZZLES", new Vector2(170f, 0f), HandleBackClicked);
    }

    private void CreateButton(Transform parent, string name, string labelText, Vector2 anchoredPosition, UnityAction callback)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(160f, 54f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.22f, 0.22f, 0.25f, 1f);

        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(callback);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelGO.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = labelText;
        label.raycastTarget = false;
    }

    private void CreateStatusText(Transform parent)
    {
        GameObject statusObject = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
        statusObject.transform.SetParent(parent, false);

        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = new Vector2(0f, -255f);
        statusRect.sizeDelta = new Vector2(700f, 90f);

        statusText = statusObject.GetComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 20;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(0.9f, 0.92f, 0.96f, 1f);
        statusText.text = "Selecciona un elemento y luego una posición.";
    }

    private void RefreshUi()
    {
        if (manager == null || statusText == null)
            return;

        for (int position = 1; position <= ConstraintPuzzleManager.PositionCount; position++)
        {
            Button slotButton = positionButtons[position];
            if (slotButton == null)
                continue;

            Text slotText = slotButton.GetComponentInChildren<Text>();
            string itemAtPosition = manager.GetItemAtPosition(position);
            if (slotText != null)
                slotText.text = itemAtPosition ?? position.ToString();

            Image slotImage = slotButton.GetComponent<Image>();
            if (slotImage != null)
                slotImage.color = itemAtPosition == null ? new Color(0.18f, 0.18f, 0.21f, 1f) : new Color(0.28f, 0.38f, 0.52f, 1f);
        }

        foreach (KeyValuePair<string, Button> entry in elementButtons)
        {
            Button button = entry.Value;
            if (button == null)
                continue;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                bool isSelected = string.Equals(selectedItemId, entry.Key, System.StringComparison.Ordinal);
                image.color = isSelected ? new Color(0.66f, 0.82f, 1f, 1f) : new Color(0.14f, 0.25f, 0.42f, 1f);
            }
        }

        if (manager.IsSolved)
        {
            statusText.text = "Puzzle Complete!";
            return;
        }

        statusText.text = selectedItemId == null
            ? "Elige un elemento y luego un hueco."
            : $"Elemento seleccionado: {selectedItemId}";
    }

    private void HandlePuzzleSolved()
    {
        selectedItemId = null;
        titleText.text = "PUZZLE COMPLETADO";
        statusText.text = "Puzzle Complete!";
    }

    public void BeginDragItem(string itemId, PointerEventData eventData)
    {
        if (manager != null && manager.IsSolved)
            return;

        selectedItemId = itemId;
        RefreshUi();

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
            return;

        CanvasGroup itemGroup = draggedObject.GetComponent<CanvasGroup>();
        if (itemGroup != null)
            itemGroup.blocksRaycasts = false;

        draggedObject.transform.SetAsLastSibling();
    }

    public void UpdateDragItemPosition(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
            return;

        RectTransform draggedRect = draggedObject.GetComponent<RectTransform>();
        if (draggedRect == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            draggedRect.anchoredPosition = localPoint;
    }

    public void EndDragItem(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
            return;

        CanvasGroup itemGroup = draggedObject.GetComponent<CanvasGroup>();
        if (itemGroup != null)
            itemGroup.blocksRaycasts = true;

        eventData.pointerDrag = null;
    }

    public void TryResolveDrop(int positionIndex, PointerEventData eventData)
    {
        if (manager == null || manager.IsSolved)
            return;

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
            return;

        ConstraintPuzzleDraggableItem dragItem = draggedObject.GetComponent<ConstraintPuzzleDraggableItem>();
        if (dragItem == null)
            return;

        string itemId = dragItem.ItemId;
        if (string.IsNullOrEmpty(itemId))
            return;

        manager.PlaceItemAtPosition(itemId, positionIndex);
        selectedItemId = null;
        manager.TryCompleteIfValid();
        RefreshUi();
    }

    private void HandleElementClicked(string itemId)
    {
        if (manager != null && manager.IsSolved)
            return;

        selectedItemId = itemId;
        RefreshUi();
    }

    private void HandlePositionClicked(int positionIndex)
    {
        if (manager != null && manager.IsSolved)
            return;

        if (string.IsNullOrEmpty(selectedItemId))
            return;

        manager.PlaceItemAtPosition(selectedItemId, positionIndex);
        selectedItemId = null;
        manager.TryCompleteIfValid();
        RefreshUi();
    }

    private void HandleCheckClicked()
    {
        if (manager == null)
            return;

        if (manager.IsSolved)
        {
            titleText.text = "PUZZLE COMPLETADO";
            statusText.text = "Puzzle Complete!";
            return;
        }

        if (manager.TryCompleteIfValid())
        {
            titleText.text = "PUZZLE COMPLETADO";
            statusText.text = "Puzzle Complete!";
            return;
        }

        bool isValid = manager.IsValidArrangement(out string message);
        statusText.text = message;
        titleText.text = "COLOCA LOS ELEMENTOS";
    }

    private void HandleResetClicked()
    {
        selectedItemId = null;
        manager.ResetBoard();
        titleText.text = "COLOCA LOS ELEMENTOS";
        statusText.text = "Elige un elemento y luego un hueco.";
    }

    private void HandleBackClicked()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadPuzzleMenu();
            return;
        }

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("PuzzleMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}

public sealed class ConstraintPuzzleDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ConstraintPuzzleUI ui;
    private string itemId;

    public string ItemId => itemId;

    public void Initialize(ConstraintPuzzleUI owner, string value)
    {
        ui = owner;
        itemId = value;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ui == null)
            return;

        ui.BeginDragItem(itemId, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ui == null)
            return;

        ui.UpdateDragItemPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ui == null)
            return;

        ui.EndDragItem(eventData);
    }
}

public sealed class ConstraintPuzzleDropArea : MonoBehaviour, IDropHandler
{
    private ConstraintPuzzleUI ui;
    private int positionIndex;

    public void Initialize(ConstraintPuzzleUI owner, int slotIndex)
    {
        ui = owner;
        positionIndex = slotIndex;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (ui == null)
            return;

        ui.TryResolveDrop(positionIndex, eventData);
    }
}
