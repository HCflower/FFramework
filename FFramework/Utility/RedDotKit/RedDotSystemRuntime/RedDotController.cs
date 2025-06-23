using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace FFramework.Kit
{
    /// <summary>
    /// 红点控制器
    /// </summary>
    [DisallowMultipleComponent]
    public class RedDotController : MonoBehaviour
    {
        [ShowOnly][Tooltip("父级红点List")] public List<RedDotKey> parentsRedDotKey = new List<RedDotKey>();
        [Tooltip("当前红点节点Key值,用于唯一标识")] public RedDotKey redDotKey;
        [Tooltip("红点显示数量"), Min(0)] public int redDotCount = 1;
        private int lastRedDotCount = 0; // 用于检测数值变化
        [Tooltip("当前节点是否显示红点数量")] public bool isShowRedDotCount = true;
        [Tooltip("红点数量显示的文本组件")][SerializeField] private TMP_Text tmpText;
        [Tooltip("红点数量显示的文本组件(传统UI)")][SerializeField] private Text uiText;
        private bool isInitialized;                      //是否初始化

        private void Start()
        {
            InitRedDotTree(this.redDotKey);
        }

        //初始化红点树
        private void InitRedDotTree(RedDotKey redDotKey)
        {
            if (isInitialized || redDotKey == RedDotKey.None) return;

            // 确保节点存在，如果不存在则创建
            var node = RedDotKit.GetOrCreateNode(redDotKey);

            isInitialized = true;

            // 从红点系统中同步当前节点的实际数值到Inspector显示
            SyncRedDotCountFromSystem();

            // 获取并缓存父节点列表
            RefreshParentsList();

            // 监听节点状态变化
            node.OnStateChanged += OnNodeStateChanged;

            // 初始化UI显示（使用系统中的实际数值）
            UpdateUI();
        }

        /// <summary>
        /// 从红点系统同步数值到Inspector显示
        /// </summary>
        private void SyncRedDotCountFromSystem()
        {
            var node = RedDotKit.GetNode(redDotKey);
            if (node != null)
            {
                // 同步BaseCount（节点自身设置的值）到Inspector显示
                redDotCount = node.BaseCount;
                lastRedDotCount = redDotCount;

                Debug.Log($"[RedDotController] Synced RedDotCount from system: {redDotKey} = {redDotCount}");
            }
            else
            {
                // 如果节点不存在，保持Inspector中的默认值
                lastRedDotCount = redDotCount;
            }
        }


        /// <summary>
        /// 刷新父节点列表
        /// </summary>
        private void RefreshParentsList()
        {
            var node = RedDotKit.GetNode(redDotKey);
            if (node != null)
            {
                parentsRedDotKey.Clear();
                foreach (var parent in node.Parents)
                {
                    parentsRedDotKey.Add(parent.Key);
                }
            }
        }

        private void OnNodeStateChanged(RedDotNode node)
        {
            UpdateUI();
        }

        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void UpdateUI()
        {
            var node = RedDotKit.GetNode(redDotKey);
            if (node == null) return;

            int displayCount = node.Count;
            bool shouldShowRedDot = displayCount > 0;

            Debug.Log($"[RedDotController] UpdateUI for {redDotKey}: Count={displayCount}, Show={shouldShowRedDot}");

            // 控制红点显示/隐藏
            gameObject.SetActive(shouldShowRedDot);

            // 更新数量文本
            if (shouldShowRedDot && isShowRedDotCount)
            {
                string countText = displayCount > 99 ? "99+" : displayCount.ToString();

                if (tmpText != null)
                {
                    tmpText.text = countText;
                    tmpText.gameObject.SetActive(true);
                }
                if (uiText != null)
                {
                    uiText.text = countText;
                    uiText.gameObject.SetActive(true);
                }
            }
            else
            {
                // 隐藏数量文本
                if (tmpText != null) tmpText.gameObject.SetActive(false);
                if (uiText != null) uiText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 更新红点状态（从外部调用）
        /// </summary>
        public void UpdateRedDotState()
        {
            if (!isInitialized) return;

            // 刷新父节点列表
            RefreshParentsList();

            // 更新UI显示
            UpdateUI();
        }

        /// <summary>
        /// 设置红点数量
        /// </summary>
        public void SetCount(int count)
        {
            if (!isInitialized) return;

            RedDotKit.ChangeRedDotCount(redDotKey, count);
            // UI会通过OnStateChanged事件自动更新，无需手动调用UpdateUI
        }

        /// <summary>
        /// 手动刷新显示
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateUI();
        }

        private void OnDestroy()
        {
            // 取消事件监听，防止内存泄漏
            var node = RedDotKit.GetNode(redDotKey);
            if (node != null)
            {
                node.OnStateChanged -= OnNodeStateChanged;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器下验证配置并检测RedDotCount变化
            if (Application.isPlaying && isInitialized)
            {
                // 确保数值在有效范围内
                redDotCount = Mathf.Max(0, redDotCount);

                // 检测RedDotCount是否发生变化
                if (lastRedDotCount != redDotCount)
                {
                    lastRedDotCount = redDotCount;

                    // 更新红点系统数据（只更新BaseCount，不影响子节点聚合值）
                    RedDotKit.ChangeRedDotCount(redDotKey, redDotCount);
                    Debug.Log($"[RedDotController] RedDotCount changed to {redDotCount} for {redDotKey}");
                }

                // 更新UI显示
                UpdateUI();
            }
        }
#endif
    }
}
