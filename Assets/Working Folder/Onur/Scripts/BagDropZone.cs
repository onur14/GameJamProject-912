using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BagDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private bool isBagArea;
    [SerializeField] private RectTransform contentRoot;
    private Image picture;
    private Color restingColor;

    private void Awake()
    {
        picture = GetComponent<Image>();
        if (picture != null) restingColor = picture.color;
    }
    public void SetHighlighted(bool active)
    {
        if (picture != null) picture.color = active ? new Color(.82f, .94f, 1f) : restingColor;
    }

    public bool IsBagArea => isBagArea;
    public RectTransform ContentRoot => contentRoot != null ? contentRoot : (RectTransform)transform;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent(out BagDraggableItem item))
            item.DropInto(this);
    }
}
