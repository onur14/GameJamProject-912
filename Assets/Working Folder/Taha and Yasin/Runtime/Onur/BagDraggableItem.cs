using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public sealed class BagDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
{
    [SerializeField] private string itemName;
    [SerializeField] private Image icon;
    [SerializeField] private Vector2 startingPosition;
    [SerializeField] private Vector2 bagPosition;
    [SerializeField] private BagDropZone startingArea;
    [SerializeField] private BagDropZone correctArea;
    [SerializeField] private BagMiniGame game;

    private RectTransform rect;
    private CanvasGroup group;
    private BagDropZone currentArea;
    private BagDropZone previousArea;
    private RectTransform dragLayer;
    private Vector3 pointerOffset;
    private int previousSibling;
    private bool dragging;
    private bool dropped;
    private bool draggedThisPress;

    public BagDropZone CurrentArea => currentArea;
    public bool IsCorrectlyPlaced => correctArea == null
        ? currentArea != null && !currentArea.IsBagArea
        : currentArea == correctArea;

    private void Awake()
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        // Keep an accessible hit rectangle even around thin/transparent sprite edges.
        if (icon != null) icon.alphaHitTestMinimumThreshold = 0f;
        if (currentArea == null) currentArea = startingArea;
    }

    public void ResetPlacement()
    {
        if (rect == null) rect = (RectTransform)transform;
        Place(startingArea);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (game == null || game.IsResolved || currentArea == null) return;
        dragging = true;
        draggedThisPress = true;
        dropped = false;
        previousArea = currentArea;
        previousSibling = transform.GetSiblingIndex();
        dragLayer = game.DragLayer;
        rect.SetParent(dragLayer, true);
        rect.SetAsLastSibling();
        rect.localScale = Vector3.one * 1.08f;
        game.HighlightBag(true);
        group.blocksRaycasts = false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position,
            eventData.pressEventCamera, out Vector2 pointer);
        pointerOffset = rect.localPosition - (Vector3)pointer;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position,
            eventData.pressEventCamera, out Vector2 pointer))
            rect.localPosition = (Vector3)pointer + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;
        if (!dropped)
        {
            Place(previousArea);
            transform.SetSiblingIndex(previousSibling);
        }
        group.blocksRaycasts = true;
        game.HighlightBag(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || draggedThisPress || dragging || game == null || game.IsResolved) return;
        Place(currentArea != null && currentArea.IsBagArea ? startingArea : game.BagArea);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) draggedThisPress = false;
    }

    public void DropInto(BagDropZone area)
    {
        if (!dragging || area == null) return;
        dropped = true;
        Place(area);
    }

    private void Place(BagDropZone area)
    {
        if (area == null) return;
        currentArea = area;
        rect.SetParent(area.ContentRoot, false);
        rect.localScale = area.IsBagArea ? Vector3.one * .58f : Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = area.IsBagArea ? bagPosition : startingPosition;
    }
    public void CancelDrag()
    {
        if (!dragging) return;
        dragging = false;
        Place(previousArea);
        if (group != null) group.blocksRaycasts = true;
        if (game != null) game.HighlightBag(false);
    }
}
