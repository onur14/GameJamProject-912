using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public sealed class BagDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    public BagDropZone CurrentArea => currentArea;
    public bool IsCorrectlyPlaced => correctArea == null
        ? currentArea != null && !currentArea.IsBagArea
        : currentArea == correctArea;

    private void Awake()
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (icon != null) icon.alphaHitTestMinimumThreshold = .1f;
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
        dropped = false;
        previousArea = currentArea;
        previousSibling = transform.GetSiblingIndex();
        dragLayer = game.DragLayer;
        rect.SetParent(dragLayer, true);
        rect.SetAsLastSibling();
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
        rect.anchoredPosition = area.IsBagArea ? bagPosition : startingPosition;
    }
}
