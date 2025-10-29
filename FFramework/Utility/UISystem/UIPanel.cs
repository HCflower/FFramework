using UnityEngine;
using System.Collections.Generic;

namespace FFramework.Utility
{
    /// <summary>
    /// UI基类
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;

        // 事件追踪列表，用于自动注销
        private List<System.Action> eventCleanupActions = new List<System.Action>();

        protected virtual void OnEnable()
        {
            Init();
        }

        /// <summary>
        /// 初始化UI 
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// 添加事件清理动作
        /// </summary>
        /// <param name="cleanupAction">清理动作</param>
        public void AddEventCleanup(System.Action cleanupAction)
        {
            if (cleanupAction != null)
            {
                eventCleanupActions.Add(cleanupAction);
            }
        }

        /// <summary>
        /// 清理所有追踪的事件
        /// </summary>
        private void CleanupTrackedEvents()
        {
            foreach (var cleanupAction in eventCleanupActions)
            {
                try
                {
                    cleanupAction?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"清理事件时发生错误: {e.Message}");
                }
            }
            eventCleanupActions.Clear();
        }

        //显示UI
        public virtual void Show()
        {
            GetOrSetCanvasGroup();
            //显示UI时，允许点击
            canvasGroup.blocksRaycasts = true;
            gameObject.SetActive(true);
        }

        //锁定UI
        public virtual void OnLock()
        {
            GetOrSetCanvasGroup();
            //锁定UI时，不允许点击
            canvasGroup.blocksRaycasts = false;
        }

        //解锁UI
        public virtual void OnUnLock()
        {
            GetOrSetCanvasGroup();
            //解锁UI时，允许点击
            canvasGroup.blocksRaycasts = true;
        }

        //关闭UI
        public virtual void Close()
        {
            GetOrSetCanvasGroup();
            //关闭UI时，不允许点击
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        //获取或设置CanvasGroup
        private void GetOrSetCanvasGroup()
        {
            if (!TryGetComponent<CanvasGroup>(out canvasGroup))
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// 面板销毁时自动清理事件
        /// </summary>
        protected virtual void OnDestroy()
        {
            // 清理追踪的事件
            CleanupTrackedEvents();

            // 使用扩展方法清理所有UI事件
            this.UnbindAllEvents();

            Debug.Log($"面板 {name} 已销毁，所有事件已清理");
        }
    }
}