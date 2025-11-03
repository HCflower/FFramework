using System.Collections.Generic;
using FFramework.Architecture;
using UnityEngine;

namespace FFramework.Utility
{
    /// <summary>
    /// UISystem管理器 - 提供UI面板管理、层级控制、组件查找等功能
    /// </summary>
    public class UISystem : SingletonMono<UISystem>
    {
        #region 私有字段

        // 缓存的UI面板字典（所有创建过的面板）
        private Dictionary<string, UIPanel> cachedPanels = new Dictionary<string, UIPanel>();

        // 当前活跃的面板栈（按打开顺序）
        private Stack<UIPanel> activeStack = new Stack<UIPanel>();

        // 每个层级的活跃面板列表
        private Dictionary<UILayer, List<UIPanel>> layerPanels = new Dictionary<UILayer, List<UIPanel>>();

        #endregion

        #region 属性

        /// <summary>当前打开的UI面板数量</summary>
        public int OpenPanelCount => activeStack.Count;

        /// <summary>缓存的UI面板数量</summary>
        public int CachedPanelCount => cachedPanels.Count;

        /// <summary>是否有打开的面板</summary>
        public bool HasOpenPanels => activeStack.Count > 0;

        /// <summary>获取当前栈顶面板</summary>
        public UIPanel CurrentPanel => activeStack.Count > 0 ? activeStack.Peek() : null;

        /// <summary>获取当前面板名称</summary>
        public string CurrentPanelName => CurrentPanel?.name ?? "无";

        /// <summary>获取当前面板类型名</summary>
        public string CurrentPanelTypeName => CurrentPanel?.GetType().Name ?? "无";

        #endregion

        #region 静态访问（兼容性）

        public static int S_OpenPanelCount => Instance.OpenPanelCount;
        public static int S_CachedPanelCount => Instance.CachedPanelCount;
        public static bool S_HasOpenPanels => Instance.HasOpenPanels;
        public static UIPanel S_CurrentPanel => Instance.CurrentPanel;
        public static string S_CurrentPanelName => Instance.CurrentPanelName;
        public static string S_CurrentPanelTypeName => Instance.CurrentPanelTypeName;

        #endregion

        #region Unity生命周期

        protected override void InitializeSingleton()
        {
            // 初始化层级面板列表
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                layerPanels[layer] = new List<UIPanel>();
            }

            Debug.Log("[UISystem] UI系统初始化完成");
        }

        protected override void OnDestroy()
        {
            ClearAllPanels(false);
            base.OnDestroy();
        }

        #endregion

        #region 内部方法

        private UIRoot GetUIRoot()
        {
            return UIRoot.Instance;
        }

        private Transform GetUILayer(UILayer layer)
        {
            var uiRoot = GetUIRoot();
            if (uiRoot == null)
            {
                Debug.LogError("[UISystem] UIRoot未找到，请确保场景中存在UIRoot");
                return null;
            }

            switch (layer)
            {
                case UILayer.BackgroundLayer: return uiRoot.BackgroundLayer;
                case UILayer.PostProcessingLayer: return uiRoot.PostProcessingLayer;
                case UILayer.ContentLayer: return uiRoot.ContentLayer;
                case UILayer.GuideLayer: return uiRoot.GuideLayer;
                case UILayer.PopupLayer: return uiRoot.PopupLayer;
                case UILayer.DebugLayer: return uiRoot.DebugLayer;
                default: return uiRoot.DebugLayer;
            }
        }

        private bool ShouldLockPreviousPanel(UILayer layer)
        {
            return layer != UILayer.PopupLayer && layer != UILayer.PostProcessingLayer;
        }

        private T CreatePanel<T>(string panelName, UILayer layer, GameObject prefab) where T : UIPanel
        {
            Transform layerTransform = GetUILayer(layer);
            if (layerTransform == null) return null;

            GameObject panelObject;
            if (prefab != null)
            {
                panelObject = Instantiate(prefab, layerTransform);
            }
            else
            {
                GameObject prefabRes = Resources.Load<GameObject>($"UI/{panelName}");
                if (prefabRes == null)
                {
                    Debug.LogError($"[UISystem] 无法加载UI预制体: UI/{panelName}");
                    return null;
                }
                panelObject = Instantiate(prefabRes, layerTransform);
            }

            panelObject.name = panelName;

            T panel = panelObject.GetComponent<T>();
            if (panel == null)
            {
                Debug.LogError($"[UISystem] 预制体缺少 {typeof(T)} 组件: {panelName}");
                Destroy(panelObject);
                return null;
            }

            return panel;
        }

        private void RemovePanelFromStack(UIPanel targetPanel)
        {
            if (targetPanel == null) return;

            var tempStack = new Stack<UIPanel>();
            bool found = false;

            while (activeStack.Count > 0)
            {
                var panel = activeStack.Pop();
                if (panel == targetPanel)
                {
                    found = true;
                    break;
                }
                tempStack.Push(panel);
            }

            while (tempStack.Count > 0)
            {
                activeStack.Push(tempStack.Pop());
            }

            if (!found)
            {
                Debug.LogWarning($"[UISystem] 面板 {targetPanel.GetType().Name} 不在活跃栈中");
            }
        }

        private GameObject FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child.gameObject;

                var result = FindChildRecursive(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        #endregion

        #region 核心面板管理

        /// <summary>
        /// 从Resources加载UI面板
        /// </summary>
        public T OpenPanel<T>(UILayer layer = UILayer.ContentLayer, bool useCache = true) where T : UIPanel
        {
            string panelName = typeof(T).Name;
            return OpenPanelInternal<T>(panelName, layer, useCache, null);
        }

        /// <summary>
        /// 从预制体加载UI面板
        /// </summary>
        public T OpenPanel<T>(GameObject prefab, UILayer layer = UILayer.ContentLayer, bool useCache = true) where T : UIPanel
        {
            if (prefab == null)
            {
                Debug.LogError("[UISystem] 预制体不能为空");
                return null;
            }
            return OpenPanelInternal<T>(prefab.name, layer, useCache, prefab);
        }

        /// <summary>
        /// 关闭当前顶层面板
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (activeStack.Count == 0)
            {
                Debug.LogWarning("[UISystem] 没有打开的面板可以关闭");
                return;
            }

            var currentPanel = activeStack.Pop();
            ClosePanelInternal(currentPanel);
        }

        /// <summary>
        /// 关闭指定类型的面板
        /// </summary>
        public void ClosePanel<T>() where T : UIPanel
        {
            string panelName = typeof(T).Name;

            if (cachedPanels.TryGetValue(panelName, out UIPanel panel) &&
                panel.gameObject.activeInHierarchy)
            {
                RemovePanelFromStack(panel);
                ClosePanelInternal(panel);
            }
            else
            {
                Debug.LogWarning($"[UISystem] 面板 {panelName} 未打开或不存在");
            }
        }

        /// <summary>
        /// 获取指定类型的面板（如果存在且活跃）
        /// </summary>
        public T GetPanel<T>() where T : UIPanel
        {
            string panelName = typeof(T).Name;
            if (cachedPanels.TryGetValue(panelName, out UIPanel panel) &&
                panel.gameObject.activeInHierarchy)
            {
                return panel as T;
            }
            return null;
        }

        /// <summary>
        /// 获取当前栈顶面板（指定类型）
        /// </summary>
        public T GetTopPanel<T>() where T : UIPanel
        {
            return CurrentPanel as T;
        }

        /// <summary>
        /// 检查当前面板是否为指定类型
        /// </summary>
        public bool IsCurrentPanel<T>() where T : UIPanel
        {
            return CurrentPanel is T;
        }

        #endregion

        #region 面板批量管理

        /// <summary>
        /// 清理所有UI面板
        /// </summary>
        public void ClearAllPanels(bool destroyGameObjects = true)
        {
            // 清空栈
            while (activeStack.Count > 0)
            {
                var panel = activeStack.Pop();
                if (panel != null)
                {
                    panel.Close();
                    if (destroyGameObjects)
                    {
                        Destroy(panel.gameObject);
                    }
                }
            }

            // 清理缓存
            foreach (var kvp in cachedPanels)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Close();
                    if (destroyGameObjects)
                    {
                        Destroy(kvp.Value.gameObject);
                    }
                }
            }

            // 清空所有容器
            cachedPanels.Clear();
            foreach (var layerList in layerPanels.Values)
            {
                layerList.Clear();
            }

            Debug.Log("[UISystem] 清理所有UI面板完成");
        }

        /// <summary>
        /// 清理指定层级的所有面板
        /// </summary>
        public void ClearPanelsInLayer(UILayer layer, bool destroyGameObjects = true)
        {
            Debug.Log($"<color=orange>[UISystem] 清理层级面板</color>: {layer}");

            var panelsToRemove = new List<UIPanel>(layerPanels[layer]);

            foreach (var panel in panelsToRemove)
            {
                if (panel != null)
                {
                    RemovePanelFromStack(panel);

                    // 从缓存中移除
                    var keysToRemove = new List<string>();
                    foreach (var kvp in cachedPanels)
                    {
                        if (kvp.Value == panel)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                    foreach (var key in keysToRemove)
                    {
                        cachedPanels.Remove(key);
                    }

                    panel.Close();
                    if (destroyGameObjects && panel.gameObject != null)
                    {
                        Destroy(panel.gameObject);
                    }
                }
            }

            layerPanels[layer].Clear();
            Debug.Log($"[UISystem] 层级 {layer} 清理完成，共清理 {panelsToRemove.Count} 个面板");
        }

        /// <summary>
        /// 获取指定层级的活跃面板数量
        /// </summary>
        public int GetActivePanelCountInLayer(UILayer layer)
        {
            int count = 0;
            foreach (var panel in layerPanels[layer])
            {
                if (panel != null && panel.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 检查指定层级是否有活跃面板
        /// </summary>
        public bool HasActivePanelsInLayer(UILayer layer)
        {
            return GetActivePanelCountInLayer(layer) > 0;
        }

        #endregion

        #region 组件查找

        /// <summary>
        /// 查找子物体（支持路径查找和递归查找）
        /// </summary>
        public GameObject FindChildGameObject(GameObject panel, string childPath, bool recursive = true)
        {
            if (string.IsNullOrEmpty(childPath) || panel == null)
            {
                Debug.LogError("[UISystem] 参数不能为空");
                return null;
            }

            if (childPath.Contains("/"))
            {
                // 路径查找
                Transform current = panel.transform;
                string[] pathSegments = childPath.Split('/');

                foreach (string segment in pathSegments)
                {
                    if (string.IsNullOrEmpty(segment)) continue;

                    Transform found = current.Find(segment);
                    if (found == null)
                    {
                        Debug.LogError($"[UISystem] 路径 {childPath} 中找不到 {segment}");
                        return null;
                    }
                    current = found;
                }
                return current.gameObject;
            }
            else
            {
                // 直接查找
                Transform found = panel.transform.Find(childPath);
                if (found != null) return found.gameObject;

                // 递归查找（如果启用）
                if (recursive)
                {
                    return FindChildRecursive(panel.transform, childPath);
                }

                Debug.LogError($"[UISystem] 在 {panel.name} 中找不到子物体 {childPath}");
                return null;
            }
        }

        /// <summary>
        /// 获取指定名称的子物体组件
        /// </summary>
        public T GetChildComponent<T>(GameObject panel, string objectName, bool recursive = true) where T : Component
        {
            if (panel == null || string.IsNullOrEmpty(objectName))
            {
                Debug.LogError("[UISystem] 参数不能为空");
                return null;
            }

            GameObject targetObj = FindChildGameObject(panel, objectName, recursive);
            if (targetObj == null) return null;

            return targetObj.GetComponent<T>();
        }

        /// <summary>
        /// 获取所有指定类型的子组件
        /// </summary>
        public T[] GetAllChildComponents<T>(GameObject panel, bool includeInactive = true) where T : Component
        {
            if (panel == null)
            {
                Debug.LogError("[UISystem] panel不能为空");
                return new T[0];
            }

            return panel.GetComponentsInChildren<T>(includeInactive);
        }

        /// <summary>
        /// 通过组件类型查找第一个匹配的子物体
        /// </summary>
        public T GetFirstChildComponent<T>(GameObject panel, bool includeInactive = true) where T : Component
        {
            if (panel == null)
            {
                Debug.LogError("[UISystem] panel不能为空");
                return null;
            }

            return panel.GetComponentInChildren<T>(includeInactive);
        }

        #endregion

        #region 内部实现

        private T OpenPanelInternal<T>(string panelName, UILayer layer, bool useCache, GameObject prefab) where T : UIPanel
        {
            Debug.Log($"<color=green>[UISystem] 打开UI面板</color>: {panelName}");

            UIPanel panel = null;

            // 检查缓存
            if (useCache && cachedPanels.TryGetValue(panelName, out panel))
            {
                if (panel.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[UISystem] 面板 {panelName} 已经打开");
                    return panel as T;
                }
            }
            else
            {
                // 创建新面板
                panel = CreatePanel<T>(panelName, layer, prefab);
                if (panel == null) return null;

                if (useCache)
                {
                    cachedPanels[panelName] = panel;
                }
            }

            // 处理层级关系
            if (activeStack.Count > 0 && ShouldLockPreviousPanel(layer))
            {
                var topPanel = activeStack.Peek();
                topPanel.OnLock();
            }

            if (!layerPanels[layer].Contains(panel))
            {
                layerPanels[layer].Add(panel);
            }

            // 显示面板
            panel.Show();
            activeStack.Push(panel);
            panel.transform.SetAsLastSibling();

            return panel as T;
        }

        private void ClosePanelInternal(UIPanel panel)
        {
            if (panel == null) return;

            Debug.Log($"<color=yellow>[UISystem] 关闭UI面板</color>: {panel.GetType().Name}");

            // 从层级列表移除
            foreach (var layerList in layerPanels.Values)
            {
                layerList.Remove(panel);
            }

            panel.Close();

            // 解锁新的栈顶面板
            if (activeStack.Count > 0)
            {
                var topPanel = activeStack.Peek();
                topPanel.OnUnLock();
            }
        }

        #endregion
    }
}