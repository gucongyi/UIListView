using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary> 为了让UIListView 同时支持滑动 和 拖拽 
/// 需要用IPointerDownHandler中的OnPointerDown 通知UIListView 点击了哪个UIListViewItem
/// </summary>
public interface UIListViewItemInterface : IPointerDownHandler
{
    /// <summary> 设置该UIListViewItem属于哪个UIListView
    /// 需要用IPointerDownHandler中的OnPointerDown 通知UIListView 点击了哪个UIListViewItem
    /// </summary>
    void SetUIListView(UIListView m_UIListView);

    bool IsGOActiveTrue();

    /// <summary> 模拟拖拽事件 OnBeginDrag </summary>
    void UIListViewItem_OnBeginDrag(PointerEventData eventData);
    /// <summary> 模拟拖拽事件 OnDrag </summary>
    void UIListViewItem_OnDrag(PointerEventData eventData);
    /// <summary> 模拟拖拽事件 OnEndDrag </summary>
    void UIListViewItem_OnEndDrag(PointerEventData eventData);
}
