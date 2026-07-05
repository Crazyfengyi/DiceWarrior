using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 用于拖拽移动目标面板的位置。
/// </summary>
public sealed class UIDragTargetPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform dragTarget;
    [SerializeField] private RectTransform clampRoot;

    private RectTransform targetParent;
    private Vector2 dragStartAnchoredPosition;
    private Vector2 dragStartPointerLocalPosition;

    /// <summary>
    /// 开始拖拽时缓存初始位置。
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TryPrepareDrag(eventData))
        {
            return;
        }

        dragStartAnchoredPosition = dragTarget.anchoredPosition;
    }

    /// <summary>
    /// 拖拽过程中更新目标面板位置。
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (dragTarget == null || targetParent == null)
        {
            return;
        }

        Vector2 currentPointerLocalPosition;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParent, eventData.position,
                eventData.pressEventCamera, out currentPointerLocalPosition))
        {
            return;
        }

        Vector2 nextPosition = dragStartAnchoredPosition + (currentPointerLocalPosition - dragStartPointerLocalPosition);
        dragTarget.anchoredPosition = ClampToRoot(nextPosition);
    }

    /// <summary>
    /// 结束拖拽时清理缓存引用。
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        targetParent = null;
    }

    /// <summary>
    /// 准备拖拽所需的父节点和起始指针位置。
    /// </summary>
    private bool TryPrepareDrag(PointerEventData eventData)
    {
        if (dragTarget == null)
        {
            return false;
        }

        targetParent = dragTarget.parent as RectTransform;
        if (targetParent == null)
        {
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParent, eventData.position,
            eventData.pressEventCamera, out dragStartPointerLocalPosition);
    }

    /// <summary>
    /// 将拖拽目标限制在指定区域内。
    /// </summary>
    private Vector2 ClampToRoot(Vector2 targetPosition)
    {
        RectTransform limitRoot = clampRoot != null ? clampRoot : targetParent;
        if (limitRoot == null || dragTarget == null)
        {
            return targetPosition;
        }

        Vector2 size = dragTarget.rect.size;
        Vector2 limitSize = limitRoot.rect.size;
        float halfWidth = size.x * dragTarget.localScale.x * 0.5f;
        float halfHeight = size.y * dragTarget.localScale.y * 0.5f;
        float minX = -limitSize.x * 0.5f + halfWidth;
        float maxX = limitSize.x * 0.5f - halfWidth;
        float minY = -limitSize.y * 0.5f + halfHeight;
        float maxY = limitSize.y * 0.5f - halfHeight;
        return new Vector2(Mathf.Clamp(targetPosition.x, minX, maxX), Mathf.Clamp(targetPosition.y, minY, maxY));
    }
}
