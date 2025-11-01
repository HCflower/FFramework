using System.Collections.Generic;
using UnityEngine;

namespace FFramework.Utility
{
    /// <summary>
    /// UI面板基类 - 提供完整的面板生命周期和事件管理
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        #region 私有字段

        private CanvasGroup canvasGroup;
        private bool isInitialized = false;
        private bool isShowing = false;
        private bool isLocked = false;

        // 事件追踪列表，用于自动注销
        private List<System.Action> eventCleanupActions = new List<System.Action>();

        // 运行时事件统计
        private Dictionary<string, int> runtimeEventCounts = new Dictionary<string, int>();

        #endregion

        #region 属性

        /// <summary>面板是否已初始化</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>面板是否正在显示</summary>
        public bool IsShowing => isShowing;

        /// <summary>面板是否被锁定</summary>
        public bool IsLocked => isLocked;

        /// <summary>面板层级</summary>
        public UILayer Layer { get; internal set; }

        #endregion

        #region Unity生命周期

        protected virtual void Awake()
        {
            OnAwake();
        }

        protected virtual void Start()
        {
            OnStart();
        }

        protected virtual void OnEnable()
        {
            if (!isInitialized)
            {
                Initialize();
                isInitialized = true;
            }
            OnPanelEnable();
        }

        protected virtual void OnDisable()
        {
            OnPanelDisable();
        }

        protected virtual void OnDestroy()
        {
            // 先调用子类的销毁逻辑
            OnPanelDestroy();

            // 然后清理资源
            CleanupAll();
        }

        #endregion

        #region 抽象和虚方法 - 子类可重写

        /// <summary>
        /// 面板初始化 - 只调用一次
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Awake时调用
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// Start时调用
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// 面板启用时调用
        /// </summary>
        protected virtual void OnPanelEnable() { }

        /// <summary>
        /// 面板禁用时调用
        /// </summary>
        protected virtual void OnPanelDisable() { }

        /// <summary>
        /// 面板销毁时调用
        /// </summary>
        protected virtual void OnPanelDestroy() { }

        /// <summary>
        /// 面板显示时调用
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// 面板隐藏时调用
        /// </summary>
        protected virtual void OnHide() { }

        /// <summary>
        /// 面板锁定时调用
        /// </summary>
        protected virtual void OnLockPanel() { }

        /// <summary>
        /// 面板解锁时调用
        /// </summary>
        protected virtual void OnUnlockPanel() { }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示面板
        /// </summary>
        public void Show()
        {
            if (isShowing) return;

            GetOrSetCanvasGroup();

            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = !isLocked;
            isShowing = true;

            OnShow();

            Debug.Log($"<color=green>显示面板</color>: {GetType().Name}");
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide()
        {
            if (!isShowing) return;

            GetOrSetCanvasGroup();

            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            isShowing = false;

            OnHide();

            Debug.Log($"<color=yellow>隐藏面板</color>: {GetType().Name}");
        }

        /// <summary>
        /// 关闭面板（隐藏的别名，语义更清晰）
        /// </summary>
        public void Close()
        {
            Hide();
        }

        /// <summary>
        /// 锁定面板（禁用交互）
        /// </summary>
        public void OnLock()
        {
            if (isLocked) return;

            GetOrSetCanvasGroup();
            canvasGroup.blocksRaycasts = false;
            isLocked = true;

            OnLockPanel();

            Debug.Log($"<color=orange>锁定面板</color>: {GetType().Name}");
        }

        /// <summary>
        /// 解锁面板（启用交互）
        /// </summary>
        public void OnUnLock()
        {
            if (!isLocked) return;

            GetOrSetCanvasGroup();
            if (isShowing)
            {
                canvasGroup.blocksRaycasts = true;
            }
            isLocked = false;

            OnUnlockPanel();

            Debug.Log($"<color=cyan>解锁面板</color>: {GetType().Name}");
        }

        /// <summary>
        /// 设置面板透明度
        /// </summary>
        public void SetAlpha(float alpha)
        {
            GetOrSetCanvasGroup();
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        /// <summary>
        /// 设置面板可交互性
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            GetOrSetCanvasGroup();
            canvasGroup.interactable = interactable;
        }

        #endregion

        #region 事件管理

        /// <summary>
        /// 添加事件清理动作
        /// </summary>
        public void AddEventCleanup(System.Action cleanupAction, string componentName = "Unknown")
        {
            if (cleanupAction != null)
            {
                eventCleanupActions.Add(cleanupAction);

                // 统计运行时事件
                if (runtimeEventCounts.ContainsKey(componentName))
                    runtimeEventCounts[componentName]++;
                else
                    runtimeEventCounts[componentName] = 1;
            }
        }

        /// <summary>
        /// 移除指定的事件清理动作
        /// </summary>
        public void RemoveEventCleanup(System.Action cleanupAction)
        {
            eventCleanupActions.Remove(cleanupAction);
        }

        /// <summary>
        /// 获取运行时事件统计
        /// </summary>
        public Dictionary<string, int> GetRuntimeEventCounts()
        {
            return new Dictionary<string, int>(runtimeEventCounts);
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
            runtimeEventCounts.Clear();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取或设置CanvasGroup组件
        /// </summary>
        private void GetOrSetCanvasGroup()
        {
            if (canvasGroup == null && !TryGetComponent<CanvasGroup>(out canvasGroup))
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// 清理所有资源
        /// </summary>
        private void CleanupAll()
        {
            // 先清理追踪的事件
            CleanupTrackedEvents();

            // 然后使用扩展方法清理所有UI事件（双重保险）
            try
            {
                if (this != null && gameObject != null)
                {
                    this.UnbindAllEvents();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UIPanel] 清理UI事件扩展方法时出错: {e.Message}");
            }

            Debug.Log($"[UIPanel] 面板 {name} 已清理完成");
        }

        #endregion

        #region 调试和工具方法

        /// <summary>
        /// 打印面板状态信息
        /// </summary>
        [ContextMenu("打印面板状态")]
        public void PrintPanelStatus()
        {
            Debug.Log($"面板状态 - {GetType().Name}:\n" +
                     $"已初始化: {isInitialized}\n" +
                     $"正在显示: {isShowing}\n" +
                     $"已锁定: {isLocked}\n" +
                     $"事件数量: {eventCleanupActions.Count}\n" +
                     $"层级: {Layer}");
        }

        /// <summary>
        /// 打印事件统计
        /// </summary>
        [ContextMenu("打印事件统计")]
        public void PrintEventStatistics()
        {
            Debug.Log($"面板事件统计 - {GetType().Name}:");
            foreach (var kvp in runtimeEventCounts)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}个事件");
            }
        }

        #endregion
    }
}