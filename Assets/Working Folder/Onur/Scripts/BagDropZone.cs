using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BagDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private bool isBagArea;
    [SerializeField] private RectTransform contentRoot;

    public bool IsBagArea => isBagArea;
    public RectTransform ContentRoot => contentRoot != null ? contentRoot : (RectTransform)transform;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent(out BagDraggableItem item))
            item.DropInto(this);
    }
}
