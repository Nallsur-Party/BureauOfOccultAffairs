using UnityEngine;
using UnityEngine.EventSystems;

public class UIDraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform dragTarget;
    [SerializeField] private Canvas parentCanvas;

    private Vector2 dragOffset;

    private void Awake()
    {
        if (dragTarget == null)
        {
            dragTarget = GetComponent<RectTransform>();
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag() || !TryGetLocalPointerPosition(eventData, out Vector2 localPointerPosition))
        {
            return;
        }

        dragTarget.SetAsLastSibling();
        dragOffset = dragTarget.anchoredPosition - localPointerPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanDrag() || !TryGetLocalPointerPosition(eventData, out Vector2 localPointerPosition))
        {
            return;
        }

        dragTarget.anchoredPosition = ClampToBounds(localPointerPosition + dragOffset);
    }

    private bool CanDrag()
    {
        return isActiveAndEnabled
            && dragTarget != null
            && parentCanvas != null
            && gameObject.activeInHierarchy;
    }

    private bool TryGetLocalPointerPosition(PointerEventData eventData, out Vector2 localPointerPosition)
    {
        localPointerPosition = default;

        if (dragTarget == null || parentCanvas == null)
        {
            return false;
        }

        RectTransform referenceRect = dragTarget.parent as RectTransform;
        if (referenceRect == null)
        {
            referenceRect = parentCanvas.transform as RectTransform;
        }

        Camera eventCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            referenceRect,
            eventData.position,
            eventCamera,
            out localPointerPosition
        );
    }

    private Vector2 ClampToBounds(Vector2 anchoredPosition)
    {
        RectTransform referenceRect = dragTarget.parent as RectTransform;
        if (referenceRect == null)
        {
            referenceRect = parentCanvas.transform as RectTransform;
        }

        if (referenceRect == null)
        {
            return anchoredPosition;
        }

        Vector2 targetSize = dragTarget.rect.size;
        Vector2 targetPivot = dragTarget.pivot;
        Vector2 referenceSize = referenceRect.rect.size;

        float minX = -referenceSize.x * referenceRect.pivot.x + targetSize.x * targetPivot.x;
        float maxX = referenceSize.x * (1f - referenceRect.pivot.x) - targetSize.x * (1f - targetPivot.x);
        float minY = -referenceSize.y * referenceRect.pivot.y + targetSize.y * targetPivot.y;
        float maxY = referenceSize.y * (1f - referenceRect.pivot.y) - targetSize.y * (1f - targetPivot.y);

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);
        return anchoredPosition;
    }
}
