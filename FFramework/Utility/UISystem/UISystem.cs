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

        #endregion

        #region 静态属性（兼容性）

        /// <summary>当前打开的UI面板数量（静态访问）</summary>
        public static int S_OpenPanelCount => Instance.OpenPanelCount;

        /// <summary>缓存的UI面板数量（静态访问）</summary>
        public static int S_CachedPanelCount => Instance.CachedPanelCount;

        /// <summary>是否有打开的面板（静态访问）</summary>
        public static bool S_HasOpenPanels => Instance.HasOpenPanels;

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
            // 清理所有面板
            ClearAllPanels(false);
            base.OnDestroy();
        }

        #endregion

        #region 私有方法

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

        /// <summary>
        /// 判断层级是否需要锁定下层面板
        /// </summary>
        private bool ShouldLockPreviousPanel(UILayer layer)
        {
            // 弹窗层和后期处理层不锁定下层面板
            return layer != UILayer.PopupLayer && layer != UILayer.PostProcessingLayer;
        }

        /// <summary>
        /// 内部面板打开方法
        /// </summary>
        private T OpenPanelInternal<T>(string panelName, UILayer layer, bool useCache, GameObject prefab) where T : UIPanel
        {
            Debug.Log($"<color=green>[UISystem] 打开UI面板</color>: {panelName}");

            UIPanel panel = null;

            // 1. 检查缓存
            if (useCache && cachedPanels.TryGetValue(panelName, out panel))
            {
                // 如果面板已经激活，直接返回
                if (panel.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[UISystem] 面板 {panelName} 已经打开");
                    return panel as T;
                }
            }
            else
            {
                // 2. 创建新面板
                panel = CreatePanel<T>(panelName, layer, prefab);
                if (panel == null) return null;

                // 3. 缓存面板
                if (useCache)
                {
                    cachedPanels[panelName] = panel;
                }
            }

            // 4. 处理层级关系
            HandlePanelLayering(panel, layer);

            // 5. 显示面板
            ShowPanel(panel, layer);

            return panel as T;
        }

        /// <summary>
        /// 创建面板
        /// </summary>
        private T CreatePanel<T>(string panelName, UILayer layer, GameObject prefab) where T : UIPanel
        {
            GameObject panelObject;
            Transform layerTransform = GetUILayer(layer);

            if (layerTransform == null)
            {
                Debug.LogError($"[UISystem] 无法获取UI层级: {layer}");
                return null;
            }

            if (prefab != null)
            {
                // 从预制体创建
                panelObject = Instantiate(prefab, layerTransform);
            }
            else
            {
                // 从Resources加载
                GameObject prefabRes = Resources.Load<GameObject>($"UI/{panelName}");
                if (prefabRes == null)
                {
                    Debug.LogError($"[UISystem] 无法加载UI预制体: UI/{panelName}");
                    return null;
                }
                panelObject = Instantiate(prefabRes, layerTransform);
            }

            panelObject.name = panelName;

            // 获取UIPanel组件
            T panel = panelObject.GetComponent<T>();
            if (panel == null)
            {
                Debug.LogError($"[UISystem] 预制体缺少 {typeof(T)} 组件: {panelName}");
                Destroy(panelObject);
                return null;
            }

            return panel;
        }

        /// <summary>
        /// 处理面板层级关系
        /// </summary>
        private void HandlePanelLayering(UIPanel panel, UILayer layer)
        {
            // 锁定当前栈顶面板（如果需要）
            if (activeStack.Count > 0 && ShouldLockPreviousPanel(layer))
            {
                var topPanel = activeStack.Peek();
                topPanel.OnLock();
            }

            // 添加到层级列表
            if (!layerPanels[layer].Contains(panel))
            {
                layerPanels[layer].Add(panel);
            }
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        private void ShowPanel(UIPanel panel, UILayer layer)
        {
            panel.Show();
            activeStack.Push(panel);

            // 将面板置于同层级最前
            panel.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 内部关闭面板方法
        /// </summary>
        private void ClosePanelInternal(UIPanel panel)
        {
            if (panel == null) return;

            Debug.Log($"<color=yellow>[UISystem] 关闭UI面板</color>: {panel.GetType().Name}");

            // 从层级列表移除
            foreach (var layerList in layerPanels.Values)
            {
                layerList.Remove(panel);
            }

            // 关闭面板
            panel.Close();

            // 解锁新的栈顶面板
            if (activeStack.Count > 0)
            {
                var topPanel = activeStack.Peek();
                topPanel.OnUnLock();
            }
        }

        /// <summary>
        /// 从栈中移除指定面板
        /// </summary>
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

            // 恢复栈
            while (tempStack.Count > 0)
            {
                activeStack.Push(tempStack.Pop());
            }

            if (!found)
            {
                Debug.LogWarning($"[UISystem] 面板 {targetPanel.GetType().Name} 不在活跃栈中");
            }
        }

        #endregion

        #region 实例方法

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
                // 从栈中移除
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
        /// 获取当前栈顶面板
        /// </summary>
        public T GetTopPanel<T>() where T : UIPanel
        {
            if (activeStack.Count == 0) return null;
            return activeStack.Peek() as T;
        }

        /// <summary>
        /// 批量关闭指定层级的所有面板
        /// </summary>
        public void CloseAllPanelsInLayer(UILayer layer)
        {
            var panelsToClose = new List<UIPanel>(layerPanels[layer]);

            foreach (var panel in panelsToClose)
            {
                if (panel != null && panel.gameObject.activeInHierarchy)
                {
                    RemovePanelFromStack(panel);
                    ClosePanelInternal(panel);
                }
            }
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

            // 清理字典中的面板
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
        /// 清理已销毁的面板引用
        /// </summary>
        public void CleanupDestroyedPanels()
        {
            // 清理缓存字典
            var keysToRemove = new List<string>();
            foreach (var kvp in cachedPanels)
            {
                if (kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                cachedPanels.Remove(key);
            }

            // 清理栈
            var tempStack = new Stack<UIPanel>();
            while (activeStack.Count > 0)
            {
                var panel = activeStack.Pop();
                if (panel != null)
                {
                    tempStack.Push(panel);
                }
            }
            while (tempStack.Count > 0)
            {
                activeStack.Push(tempStack.Pop());
            }

            // 清理层级列表
            foreach (var layerList in layerPanels.Values)
            {
                layerList.RemoveAll(panel => panel == null);
            }

            Debug.Log("[UISystem] 清理已销毁的面板引用完成");
        }

        /// <summary>
        /// 将面板置于最前面显示
        /// </summary>
        public void BringPanelToFront(UIPanel panel)
        {
            if (panel == null) return;
            panel.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 将面板置于最后面显示
        /// </summary>
        public void SendPanelToBack(UIPanel panel)
        {
            if (panel == null) return;
            panel.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// 设置面板的显示顺序
        /// </summary>
        public void SetPanelSortOrder(UIPanel panel, int sortOrder)
        {
            if (panel == null) return;
            int targetIndex = Mathf.Clamp(sortOrder, 0, panel.transform.parent.childCount - 1);
            panel.transform.SetSiblingIndex(targetIndex);
        }

        /// <summary>
        /// 查找子物体（支持路径查找）
        /// </summary>
        public GameObject FindChildGameObject(GameObject panel, string childPath)
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
                // 递归查找
                Transform found = panel.transform.Find(childPath);
                if (found != null) return found.gameObject;

                foreach (Transform child in panel.transform)
                {
                    var result = FindChildGameObject(child.gameObject, childPath);
                    if (result != null) return result;
                }

                Debug.LogError($"[UISystem] 在 {panel.name} 中找不到子物体 {childPath}");
                return null;
            }
        }

        /// <summary>
        /// 获取或添加组件
        /// </summary>
        public T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            if (gameObject == null) return null;

            var component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();

            return component;
        }

        #endregion

        #region 静态方法（兼容性接口）

        /// <summary>
        /// 从Resources加载UI面板（静态访问）
        /// </summary>
        public static T S_OpenPanel<T>(UILayer layer = UILayer.ContentLayer, bool useCache = true) where T : UIPanel
        {
            return Instance.OpenPanel<T>(layer, useCache);
        }

        /// <summary>
        /// 从预制体加载UI面板（静态访问）
        /// </summary>
        public static T S_OpenPanel<T>(GameObject prefab, UILayer layer = UILayer.ContentLayer, bool useCache = true) where T : UIPanel
        {
            return Instance.OpenPanel<T>(prefab, layer, useCache);
        }

        /// <summary>
        /// 关闭当前顶层面板（静态访问）
        /// </summary>
        public static void S_CloseCurrentPanel()
        {
            Instance.CloseCurrentPanel();
        }

        /// <summary>
        /// 关闭指定类型的面板（静态访问）
        /// </summary>
        public static void S_ClosePanel<T>() where T : UIPanel
        {
            Instance.ClosePanel<T>();
        }

        /// <summary>
        /// 获取指定类型的面板（静态访问）
        /// </summary>
        public static T S_GetPanel<T>() where T : UIPanel
        {
            return Instance.GetPanel<T>();
        }

        /// <summary>
        /// 获取当前栈顶面板（静态访问）
        /// </summary>
        public static T S_GetTopPanel<T>() where T : UIPanel
        {
            return Instance.GetTopPanel<T>();
        }

        /// <summary>
        /// 批量关闭指定层级的所有面板（静态访问）
        /// </summary>
        public static void S_CloseAllPanelsInLayer(UILayer layer)
        {
            Instance.CloseAllPanelsInLayer(layer);
        }

        /// <summary>
        /// 获取指定层级的活跃面板数量（静态访问）
        /// </summary>
        public static int S_GetActivePanelCountInLayer(UILayer layer)
        {
            return Instance.GetActivePanelCountInLayer(layer);
        }

        /// <summary>
        /// 清理所有UI面板（静态访问）
        /// </summary>
        public static void S_ClearAllPanels(bool destroyGameObjects = true)
        {
            Instance.ClearAllPanels(destroyGameObjects);
        }

        /// <summary>
        /// 清理已销毁的面板引用（静态访问）
        /// </summary>
        public static void S_CleanupDestroyedPanels()
        {
            Instance.CleanupDestroyedPanels();
        }

        /// <summary>
        /// 将面板置于最前面显示（静态访问）
        /// </summary>
        public static void S_BringPanelToFront(UIPanel panel)
        {
            Instance.BringPanelToFront(panel);
        }

        /// <summary>
        /// 将面板置于最后面显示（静态访问）
        /// </summary>
        public static void S_SendPanelToBack(UIPanel panel)
        {
            Instance.SendPanelToBack(panel);
        }

        /// <summary>
        /// 设置面板的显示顺序（静态访问）
        /// </summary>
        public static void S_SetPanelSortOrder(UIPanel panel, int sortOrder)
        {
            Instance.SetPanelSortOrder(panel, sortOrder);
        }

        /// <summary>
        /// 查找子物体（静态访问）
        /// </summary>
        public static GameObject S_FindChildGameObject(GameObject panel, string childPath)
        {
            return Instance.FindChildGameObject(panel, childPath);
        }

        /// <summary>
        /// 获取或添加组件（静态访问）
        /// </summary>
        public static T S_GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            return Instance.GetOrAddComponent<T>(gameObject);
        }

        #endregion
    }
}