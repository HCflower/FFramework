using UnityEngine.EventSystems;
using UnityEngine;
using System;

namespace FFramework.Utility
{
    /// <summary>
    /// UIEventSystem类，用于处理Unity UI事件相关的功能
    /// 提供所有Unity EventSystem接口的便捷绑定方式
    /// </summary>
    public class UIEventSystem : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IScrollHandler, IUpdateSelectedHandler, ISelectHandler, IDeselectHandler,
        IMoveHandler, ISubmitHandler, ICancelHandler
    {
        #region 事件委托定义

        /// <summary>指针事件委托</summary>
        public Action<PointerEventData> OnPointerEnterEvent;
        public Action<PointerEventData> OnPointerExitEvent;
        public Action<PointerEventData> OnPointerDownEvent;
        public Action<PointerEventData> OnPointerUpEvent;
        public Action<PointerEventData> OnPointerClickEvent;

        /// <summary>拖拽事件委托</summary>
        public Action<PointerEventData> OnInitializePotentialDragEvent;
        public Action<PointerEventData> OnBeginDragEvent;
        public Action<PointerEventData> OnDragEvent;
        public Action<PointerEventData> OnEndDragEvent;
        public Action<PointerEventData> OnDropEvent;

        /// <summary>滚轮事件委托</summary>
        public Action<PointerEventData> OnScrollEvent;

        /// <summary>选择事件委托</summary>
        public Action<BaseEventData> OnUpdateSelectedEvent;
        public Action<BaseEventData> OnSelectEvent;
        public Action<BaseEventData> OnDeselectEvent;

        /// <summary>移动和输入事件委托</summary>
        public Action<AxisEventData> OnMoveEvent;
        public Action<BaseEventData> OnSubmitEvent;
        public Action<BaseEventData> OnCancelEvent;

        #endregion

        #region 静态便捷方法

        /// <summary>
        /// 为GameObject添加EventKit组件
        /// </summary>
        /// <param name="target">目标GameObject</param>
        /// <returns>EventKit组件实例</returns>
        public static UIEventSystem Get(GameObject target)
        {
            if (target == null)
            {
                Debug.LogError("EventKit.Get: target不能为空");
                return null;
            }

            var eventKit = target.GetComponent<UIEventSystem>();
            if (eventKit == null)
            {
                eventKit = target.AddComponent<UIEventSystem>();
            }

            return eventKit;
        }

        /// <summary>
        /// 为Component的GameObject添加EventKit组件
        /// </summary>
        /// <param name="target">目标Component</param>
        /// <returns>EventKit组件实例</returns>
        public static UIEventSystem Get(Component target)
        {
            return target != null ? Get(target.gameObject) : null;
        }

        #endregion

        #region 链式调用方法

        /// <summary>设置指针进入事件</summary>
        public UIEventSystem SetOnPointerEnter(Action<PointerEventData> callback)
        {
            OnPointerEnterEvent = callback;
            return this;
        }

        /// <summary>设置指针退出事件</summary>
        public UIEventSystem SetOnPointerExit(Action<PointerEventData> callback)
        {
            OnPointerExitEvent = callback;
            return this;
        }

        /// <summary>设置指针按下事件</summary>
        public UIEventSystem SetOnPointerDown(Action<PointerEventData> callback)
        {
            OnPointerDownEvent = callback;
            return this;
        }

        /// <summary>设置指针抬起事件</summary>
        public UIEventSystem SetOnPointerUp(Action<PointerEventData> callback)
        {
            OnPointerUpEvent = callback;
            return this;
        }

        /// <summary>设置指针点击事件</summary>
        public UIEventSystem SetOnPointerClick(Action<PointerEventData> callback)
        {
            OnPointerClickEvent = callback;
            return this;
        }

        /// <summary>设置拖拽初始化事件</summary>
        public UIEventSystem SetOnInitializePotentialDrag(Action<PointerEventData> callback)
        {
            OnInitializePotentialDragEvent = callback;
            return this;
        }

        /// <summary>设置开始拖拽事件</summary>
        public UIEventSystem SetOnBeginDrag(Action<PointerEventData> callback)
        {
            OnBeginDragEvent = callback;
            return this;
        }

        /// <summary>设置拖拽中事件</summary>
        public UIEventSystem SetOnDrag(Action<PointerEventData> callback)
        {
            OnDragEvent = callback;
            return this;
        }

        /// <summary>设置结束拖拽事件</summary>
        public UIEventSystem SetOnEndDrag(Action<PointerEventData> callback)
        {
            OnEndDragEvent = callback;
            return this;
        }

        /// <summary>设置放置事件</summary>
        public UIEventSystem SetOnDrop(Action<PointerEventData> callback)
        {
            OnDropEvent = callback;
            return this;
        }

        /// <summary>设置滚轮事件</summary>
        public UIEventSystem SetOnScroll(Action<PointerEventData> callback)
        {
            OnScrollEvent = callback;
            return this;
        }

        /// <summary>设置更新选择事件</summary>
        public UIEventSystem SetOnUpdateSelected(Action<BaseEventData> callback)
        {
            OnUpdateSelectedEvent = callback;
            return this;
        }

        /// <summary>设置选择事件</summary>
        public UIEventSystem SetOnSelect(Action<BaseEventData> callback)
        {
            OnSelectEvent = callback;
            return this;
        }

        /// <summary>设置取消选择事件</summary>
        public UIEventSystem SetOnDeselect(Action<BaseEventData> callback)
        {
            OnDeselectEvent = callback;
            return this;
        }

        /// <summary>设置移动事件</summary>
        public UIEventSystem SetOnMove(Action<AxisEventData> callback)
        {
            OnMoveEvent = callback;
            return this;
        }

        /// <summary>设置提交事件</summary>
        public UIEventSystem SetOnSubmit(Action<BaseEventData> callback)
        {
            OnSubmitEvent = callback;
            return this;
        }

        /// <summary>设置取消事件</summary>
        public UIEventSystem SetOnCancel(Action<BaseEventData> callback)
        {
            OnCancelEvent = callback;
            return this;
        }

        #endregion

        #region 添加事件监听方法（支持多个回调）

        /// <summary>添加指针进入事件监听</summary>
        public UIEventSystem AddOnPointerEnter(Action<PointerEventData> callback)
        {
            OnPointerEnterEvent += callback;
            return this;
        }

        /// <summary>添加指针退出事件监听</summary>
        public UIEventSystem AddOnPointerExit(Action<PointerEventData> callback)
        {
            OnPointerExitEvent += callback;
            return this;
        }

        /// <summary>添加指针按下事件监听</summary>
        public UIEventSystem AddOnPointerDown(Action<PointerEventData> callback)
        {
            OnPointerDownEvent += callback;
            return this;
        }

        /// <summary>添加指针抬起事件监听</summary>
        public UIEventSystem AddOnPointerUp(Action<PointerEventData> callback)
        {
            OnPointerUpEvent += callback;
            return this;
        }

        /// <summary>添加指针点击事件监听</summary>
        public UIEventSystem AddOnPointerClick(Action<PointerEventData> callback)
        {
            OnPointerClickEvent += callback;
            return this;
        }

        /// <summary>添加拖拽事件监听</summary>
        public UIEventSystem AddOnDrag(Action<PointerEventData> callback)
        {
            OnDragEvent += callback;
            return this;
        }

        /// <summary>添加滚轮事件监听</summary>
        public UIEventSystem AddOnScroll(Action<PointerEventData> callback)
        {
            OnScrollEvent += callback;
            return this;
        }

        #endregion

        #region 移除事件监听方法

        /// <summary>移除指针进入事件监听</summary>
        public UIEventSystem RemoveOnPointerEnter(Action<PointerEventData> callback)
        {
            OnPointerEnterEvent -= callback;
            return this;
        }

        /// <summary>移除指针退出事件监听</summary>
        public UIEventSystem RemoveOnPointerExit(Action<PointerEventData> callback)
        {
            OnPointerExitEvent -= callback;
            return this;
        }

        /// <summary>移除指针点击事件监听</summary>
        public UIEventSystem RemoveOnPointerClick(Action<PointerEventData> callback)
        {
            OnPointerClickEvent -= callback;
            return this;
        }

        /// <summary>清除所有事件监听</summary>
        public UIEventSystem ClearAllEvents()
        {
            OnPointerEnterEvent = null;
            OnPointerExitEvent = null;
            OnPointerDownEvent = null;
            OnPointerUpEvent = null;
            OnPointerClickEvent = null;
            OnInitializePotentialDragEvent = null;
            OnBeginDragEvent = null;
            OnDragEvent = null;
            OnEndDragEvent = null;
            OnDropEvent = null;
            OnScrollEvent = null;
            OnUpdateSelectedEvent = null;
            OnSelectEvent = null;
            OnDeselectEvent = null;
            OnMoveEvent = null;
            OnSubmitEvent = null;
            OnCancelEvent = null;
            return this;
        }

        #endregion

        #region Unity EventSystem接口实现

        /// <summary>当指针进入对象时调用</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnterEvent?.Invoke(eventData);
        }

        /// <summary>当指针退出对象时调用</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitEvent?.Invoke(eventData);
        }

        /// <summary>在对象上按下指针时调用</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerDownEvent?.Invoke(eventData);
        }

        /// <summary>松开指针时调用（在指针正在点击的游戏对象上调用）</summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpEvent?.Invoke(eventData);
        }

        /// <summary>在同一对象上按下再松开指针时调用</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            OnPointerClickEvent?.Invoke(eventData);
        }

        /// <summary>在找到拖动目标时调用，可用于初始化值</summary>
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            OnInitializePotentialDragEvent?.Invoke(eventData);
        }

        /// <summary>即将开始拖动时在拖动对象上调用</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragEvent?.Invoke(eventData);
        }

        /// <summary>发生拖动时在拖动对象上调用</summary>
        public void OnDrag(PointerEventData eventData)
        {
            OnDragEvent?.Invoke(eventData);
        }

        /// <summary>拖动完成时在拖动对象上调用</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragEvent?.Invoke(eventData);
        }

        /// <summary>在拖动目标对象上调用</summary>
        public void OnDrop(PointerEventData eventData)
        {
            OnDropEvent?.Invoke(eventData);
        }

        /// <summary>当鼠标滚轮滚动时调用</summary>
        public void OnScroll(PointerEventData eventData)
        {
            OnScrollEvent?.Invoke(eventData);
        }

        /// <summary>每次勾选时在选定对象上调用</summary>
        public void OnUpdateSelected(BaseEventData eventData)
        {
            OnUpdateSelectedEvent?.Invoke(eventData);
        }

        /// <summary>当对象成为选定对象时调用</summary>
        public void OnSelect(BaseEventData eventData)
        {
            OnSelectEvent?.Invoke(eventData);
        }

        /// <summary>取消选择选定对象时调用</summary>
        public void OnDeselect(BaseEventData eventData)
        {
            OnDeselectEvent?.Invoke(eventData);
        }

        /// <summary>发生移动事件（上、下、左、右等）时调用</summary>
        public void OnMove(AxisEventData eventData)
        {
            OnMoveEvent?.Invoke(eventData);
        }

        /// <summary>按下 Submit 按钮时调用</summary>
        public void OnSubmit(BaseEventData eventData)
        {
            OnSubmitEvent?.Invoke(eventData);
        }

        /// <summary>按下 Cancel 按钮时调用</summary>
        public void OnCancel(BaseEventData eventData)
        {
            OnCancelEvent?.Invoke(eventData);
        }

        #endregion

        #region 生命周期管理

        private void OnDestroy()
        {
            ClearAllEvents();
        }

        #endregion
    }
}