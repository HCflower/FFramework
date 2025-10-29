using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace FFramework.Utility
{
    /// <summary>
    /// UIEventSystemExtensions扩展方法，提供更便捷的事件绑定方式
    /// </summary>
    public static class UIEventSystemExtensions
    {
        #region GameObject扩展

        /// <summary>
        /// 为GameObject绑定点击事件
        /// </summary>
        public static UIEventSystem BindClick(this GameObject obj, Action<PointerEventData> callback)
        {
            return UIEventSystem.Get(obj).SetOnPointerClick(callback);
        }

        /// <summary>
        /// 为GameObject绑定点击事件（无参数版本）
        /// </summary>
        public static UIEventSystem BindClick(this GameObject obj, Action callback)
        {
            return UIEventSystem.Get(obj).SetOnPointerClick(_ => callback?.Invoke());
        }

        /// <summary>
        /// 为GameObject绑定悬停事件
        /// </summary>
        public static UIEventSystem BindHover(this GameObject obj, Action<PointerEventData> onEnter, Action<PointerEventData> onExit = null)
        {
            var eventKit = UIEventSystem.Get(obj);
            eventKit.SetOnPointerEnter(onEnter);
            if (onExit != null)
                eventKit.SetOnPointerExit(onExit);
            return eventKit;
        }

        /// <summary>
        /// 为GameObject绑定悬停事件（无参数版本）
        /// </summary>
        public static UIEventSystem BindHover(this GameObject obj, Action onEnter, Action onExit = null)
        {
            var eventKit = UIEventSystem.Get(obj);
            eventKit.SetOnPointerEnter(_ => onEnter?.Invoke());
            if (onExit != null)
                eventKit.SetOnPointerExit(_ => onExit?.Invoke());
            return eventKit;
        }

        /// <summary>
        /// 为GameObject绑定拖拽事件
        /// </summary>
        public static UIEventSystem BindDrag(this GameObject obj,
            Action<PointerEventData> onBeginDrag = null,
            Action<PointerEventData> onDrag = null,
            Action<PointerEventData> onEndDrag = null)
        {
            var eventKit = UIEventSystem.Get(obj);
            if (onBeginDrag != null) eventKit.SetOnBeginDrag(onBeginDrag);
            if (onDrag != null) eventKit.SetOnDrag(onDrag);
            if (onEndDrag != null) eventKit.SetOnEndDrag(onEndDrag);
            return eventKit;
        }

        /// <summary>
        /// 为GameObject绑定拖拽事件（简化版本）
        /// </summary>
        public static UIEventSystem BindDrag(this GameObject obj, Action<Vector2> onDrag)
        {
            return UIEventSystem.Get(obj).SetOnDrag(eventData => onDrag?.Invoke(eventData.delta));
        }

        #endregion

        #region Component扩展

        /// <summary>
        /// 为Component绑定点击事件
        /// </summary>
        public static UIEventSystem BindClick(this Component component, Action<PointerEventData> callback)
        {
            return component.gameObject.BindClick(callback);
        }

        /// <summary>
        /// 为Component绑定点击事件（无参数版本）
        /// </summary>
        public static UIEventSystem BindClick(this Component component, Action callback)
        {
            return component.gameObject.BindClick(callback);
        }

        /// <summary>
        /// 为Component绑定悬停事件
        /// </summary>
        public static UIEventSystem BindHover(this Component component, Action<PointerEventData> onEnter, Action<PointerEventData> onExit = null)
        {
            return component.gameObject.BindHover(onEnter, onExit);
        }

        /// <summary>
        /// 为Component绑定悬停事件（无参数版本）
        /// </summary>
        public static UIEventSystem BindHover(this Component component, Action onEnter, Action onExit = null)
        {
            return component.gameObject.BindHover(onEnter, onExit);
        }

        #endregion

        #region UI组件特殊扩展

        /// <summary>
        /// 为Button添加增强点击事件（保留原有onClick，添加EventKit支持）
        /// </summary>
        public static UIEventSystem BindEnhancedClick(this Button button, Action<PointerEventData> callback)
        {
            return button.gameObject.BindClick(callback);
        }

        /// <summary>
        /// 为Image绑定点击事件并启用射线检测
        /// </summary>
        public static UIEventSystem BindClickWithRaycast(this Image image, Action<PointerEventData> callback)
        {
            image.raycastTarget = true;
            return image.gameObject.BindClick(callback);
        }

        /// <summary>
        /// 为Text绑定点击事件并启用射线检测
        /// </summary>
        public static UIEventSystem BindClickWithRaycast(this Text text, Action<PointerEventData> callback)
        {
            text.raycastTarget = true;
            return text.gameObject.BindClick(callback);
        }

        /// <summary>
        /// 为ScrollRect绑定滚动事件
        /// </summary>
        public static UIEventSystem BindScroll(this ScrollRect scrollRect, Action<PointerEventData> callback)
        {
            return UIEventSystem.Get(scrollRect).SetOnScroll(callback);
        }

        #endregion

        #region 事件数据便捷方法

        /// <summary>
        /// 检查是否为左键点击
        /// </summary>
        public static bool IsLeftClick(this PointerEventData eventData)
        {
            return eventData.button == PointerEventData.InputButton.Left;
        }

        /// <summary>
        /// 检查是否为右键点击
        /// </summary>
        public static bool IsRightClick(this PointerEventData eventData)
        {
            return eventData.button == PointerEventData.InputButton.Right;
        }

        /// <summary>
        /// 检查是否为中键点击
        /// </summary>
        public static bool IsMiddleClick(this PointerEventData eventData)
        {
            return eventData.button == PointerEventData.InputButton.Middle;
        }

        /// <summary>
        /// 获取世界空间中的位置
        /// </summary>
        public static Vector3 GetWorldPosition(this PointerEventData eventData, Camera camera = null)
        {
            if (camera == null)
                camera = Camera.main;

            return camera.ScreenToWorldPoint(eventData.position);
        }

        /// <summary>
        /// 获取UI空间中的位置
        /// </summary>
        public static Vector2 GetUIPosition(this PointerEventData eventData, RectTransform rectTransform)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);
            return localPoint;
        }

        #endregion
    }
}
