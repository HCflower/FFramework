using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// UIKit工具类 - 提供UI面板管理、层级控制、组件查找等功能
    /// </summary>
    public static class UIKit
    {
        #region 私有字段

        private static Dictionary<string, UIPanel> uiPanelDic = new Dictionary<string, UIPanel>();
        private static Stack<UIPanel> panelStack = new Stack<UIPanel>();

        #endregion

        #region 公共属性

        /// <summary>当前打开的UI面板数量</summary>
        public static int OpenPanelCount => panelStack.Count;

        /// <summary>缓存的UI面板数量</summary>
        public static int CachedPanelCount => uiPanelDic.Count;

        /// <summary>是否有打开的面板</summary>
        public static bool HasOpenPanels => panelStack.Count > 0;

        #endregion

        #region 私有方法


        private static UIRoot GetUIRoot()
        {
            return UIRoot.Instance;
        }

        //设置UI层级
        private static Transform SetUILayer(UILayer layer)
        {
            Transform uilayer;
            switch (layer)
            {
                case UILayer.BackgroundLayer:
                    uilayer = GetUIRoot().BackgroundLayer;
                    break;
                case UILayer.PostProcessingLayer:
                    uilayer = GetUIRoot().PostProcessingLayer;
                    break;
                case UILayer.ContentLayer:
                    uilayer = GetUIRoot().ContentLayer;
                    break;
                case UILayer.GuideLayer:
                    uilayer = GetUIRoot().GuideLayer;
                    break;
                case UILayer.PopupLayer:
                    uilayer = GetUIRoot().PopupLayer;
                    break;
                case UILayer.DebugLayer:
                    uilayer = GetUIRoot().DebugLayer;
                    break;
                default:
                    uilayer = GetUIRoot().DebugLayer;
                    break;
            }

            return uilayer;
        }

        /// <summary>
        /// 从栈中移除指定面板
        /// </summary>
        private static void RemovePanelFromStack(UIPanel targetPanel)
        {
            var tempStack = new Stack<UIPanel>();
            bool found = false;

            while (panelStack.Count > 0)
            {
                var panel = panelStack.Pop();
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
                panelStack.Push(tempStack.Pop());
            }

            if (!found)
            {
                Debug.LogWarning($"Panel {targetPanel.GetType().Name} not found in stack");
            }
        }

        /// <summary>
        /// 预加载UI面板（异步）
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="callback">预加载完成回调</param>
        public static void PreloadPanel<T>(System.Action<T> callback = null) where T : UIPanel
        {
            string panelName = typeof(T).Name;
            if (uiPanelDic.ContainsKey(panelName))
            {
                callback?.Invoke(uiPanelDic[panelName] as T);
                return;
            }

            // 使用Resources加载，因为Addressables可能未安装
            var panelPrefab = Resources.Load<GameObject>(panelName);
            if (panelPrefab != null)
            {
                var panel = Object.Instantiate(panelPrefab).GetComponent<T>();
                panel.gameObject.SetActive(false);
                uiPanelDic.Add(panelName, panel);
                callback?.Invoke(panel);
            }
            else
            {
                Debug.LogError($"无法预加载面板 {panelName}，请确保资源存在于Resources文件夹中");
                callback?.Invoke(null);
            }
        }

        /// <summary>
        /// 批量关闭指定层级的所有面板
        /// </summary>
        /// <param name="layer">UI层级</param>
        public static void CloseAllPanelsInLayer(UILayer layer)
        {
            Transform layerTransform = SetUILayer(layer);
            var panelsToClose = new List<UIPanel>();

            foreach (Transform child in layerTransform)
            {
                var panel = child.GetComponent<UIPanel>();
                if (panel != null && panel.gameObject.activeInHierarchy)
                {
                    panelsToClose.Add(panel);
                }
            }

            foreach (var panel in panelsToClose)
            {
                // 设置面板为非活跃状态
                panel.gameObject.SetActive(false);
                RemovePanelFromStack(panel);
            }
        }

        /// <summary>
        /// 获取指定层级的活跃面板数量
        /// </summary>
        /// <param name="layer">UI层级</param>
        /// <returns>活跃面板数量</returns>
        public static int GetActivePanelCountInLayer(UILayer layer)
        {
            Transform layerTransform = SetUILayer(layer);
            int count = 0;

            foreach (Transform child in layerTransform)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 设置面板的显示顺序（通过设置Transform的siblingIndex）
        /// </summary>
        /// <param name="panel">面板</param>
        /// <param name="sortOrder">显示顺序（在同层级中的索引位置）</param>
        public static void SetPanelSortOrder(UIPanel panel, int sortOrder)
        {
            if (panel == null) return;

            Transform parent = panel.transform.parent;
            if (parent != null)
            {
                // 将sortOrder转换为siblingIndex（确保在有效范围内）
                int targetIndex = Mathf.Clamp(sortOrder, 0, parent.childCount - 1);
                panel.transform.SetSiblingIndex(targetIndex);
                Debug.Log($"设置面板 {panel.name} 的层级索引为: {targetIndex}");
            }
            else
            {
                Debug.LogWarning($"面板 {panel.name} 没有父对象，无法设置显示顺序");
            }
        }

        /// <summary>
        /// 将面板置于最前面显示
        /// </summary>
        /// <param name="panel">面板</param>
        public static void BringPanelToFront(UIPanel panel)
        {
            if (panel == null) return;

            // 直接设置为最后一个子对象（最前显示）
            panel.transform.SetAsLastSibling();
            Debug.Log($"面板 {panel.name} 已置于最前面");
        }

        /// <summary>
        /// 将面板置于最后面显示
        /// </summary>
        /// <param name="panel">面板</param>
        public static void SendPanelToBack(UIPanel panel)
        {
            if (panel == null) return;

            // 直接设置为第一个子对象（最后显示）
            panel.transform.SetAsFirstSibling();
            Debug.Log($"面板 {panel.name} 已置于最后面");
        }

        /// <summary>
        /// 获取面板在同层级中的显示顺序
        /// </summary>
        /// <param name="panel">面板</param>
        /// <returns>siblingIndex（越大越靠前显示）</returns>
        public static int GetPanelSortOrder(UIPanel panel)
        {
            if (panel == null) return -1;
            return panel.transform.GetSiblingIndex();
        }

        /// <summary>
        /// 交换两个面板的显示顺序
        /// </summary>
        /// <param name="panel1">面板1</param>
        /// <param name="panel2">面板2</param>
        public static void SwapPanelOrder(UIPanel panel1, UIPanel panel2)
        {
            if (panel1 == null || panel2 == null) return;
            if (panel1.transform.parent != panel2.transform.parent)
            {
                Debug.LogWarning("两个面板不在同一父对象下，无法交换顺序");
                return;
            }

            int index1 = panel1.transform.GetSiblingIndex();
            int index2 = panel2.transform.GetSiblingIndex();

            panel1.transform.SetSiblingIndex(index2);
            panel2.transform.SetSiblingIndex(index1);

            Debug.Log($"已交换面板 {panel1.name} 和 {panel2.name} 的显示顺序");
        }

        /// <summary>
        /// 获取所有活跃面板的名称列表
        /// </summary>
        /// <returns>活跃面板名称列表</returns>
        public static List<string> GetActivePanelNames()
        {
            var activeNames = new List<string>();
            foreach (var kvp in uiPanelDic)
            {
                if (kvp.Value != null && kvp.Value.gameObject.activeInHierarchy)
                {
                    activeNames.Add(kvp.Key);
                }
            }
            return activeNames;
        }

        /// <summary>
        /// 清理所有已销毁的面板引用
        /// </summary>
        public static void CleanupDestroyedPanels()
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in uiPanelDic)
            {
                if (kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                uiPanelDic.Remove(key);
            }

            // 清理栈中已销毁的面板
            var tempStack = new Stack<UIPanel>();
            while (panelStack.Count > 0)
            {
                var panel = panelStack.Pop();
                if (panel != null)
                {
                    tempStack.Push(panel);
                }
            }

            while (tempStack.Count > 0)
            {
                panelStack.Push(tempStack.Pop());
            }
        }

        #endregion

        #region UI面板操作

        /// <summary>
        /// 从Resources/UI 中加载UI面板
        /// </summary>
        /// <param name="uiPanelName">UI面板名称</param>
        /// <param name="isCache">是否缓存面板(默认true)</param>
        public static T OpenUIPanelFromRes<T>(bool isCache = true, UILayer layer = UILayer.DebugLayer) where T : UIPanel
        {
            string typeName = typeof(T).Name;
            Debug.Log($"Open UIPanel : <color=green>{typeName}.</color>");
            if (!uiPanelDic.TryGetValue(typeName, out UIPanel uiPanel))
            {
                // 从Resources加载预设体
                GameObject prefab = Resources.Load<GameObject>($"UI/{typeName}");
                if (prefab == null)
                {
                    Debug.LogError($"UI prefabs don't exist: UI/{typeName}");
                    return null;
                }
                // 实例化UI
                GameObject ui = Object.Instantiate(prefab, SetUILayer(layer));
                ui.name = typeName;
                uiPanel = ui.GetComponent<T>();
                if (uiPanel == null)
                {
                    Debug.LogError($"UI prefabs are missing {typeof(UIPanel)} component: {typeName}");
                    Object.Destroy(ui);
                    return null;
                }
                if (isCache) uiPanelDic.Add(typeName, uiPanel);
            }

            // 锁定当前面板
            if (panelStack.Count > 0)
            {
                panelStack.Peek().OnLock();
            }

            uiPanel.Show();
            panelStack.Push(uiPanel);
            return uiPanel as T;
        }

        ///<summary>
        /// 从资产中加载UI
        /// </summary>
        /// <param name="uiPrefab">UI预制体</param>
        /// <param name="isCache">是否缓存面板(默认true)</param>
        public static T OpenUIPanelFromAsset<T>(GameObject uiPrefab, bool isCache = true, UILayer layer = UILayer.DebugLayer) where T : UIPanel
        {
            if (uiPrefab == null)
            {
                Debug.LogError("The UI prefab is empty");
                return null;
            }

            string panelName = uiPrefab.name;
            if (!uiPanelDic.TryGetValue(panelName, out UIPanel uiPanel))
            {
                // 实例化UI
                GameObject uiInstance = Object.Instantiate(uiPrefab, SetUILayer(layer));
                uiInstance.name = panelName;

                // 获取UIPanel组件
                uiPanel = uiInstance.GetComponent<T>();
                if (uiPanel == null)
                {
                    Debug.LogError($"UI prefabs are missing {typeof(T)} component: {panelName}");
                    Object.Destroy(uiInstance);
                    return null;
                }
                if (isCache) uiPanelDic.Add(panelName, uiPanel);
            }

            // 锁定当前面板
            if (panelStack.Count > 0)
            {
                panelStack.Peek().OnLock();
            }
            uiPanel.Show();
            panelStack.Push(uiPanel);
            return uiPanel as T;
        }

        /// <summary>
        /// 关闭当前UI面板
        /// </summary>
        public static void CloseCurrentUIPanel()
        {
            if (panelStack.Count == 0) return;

            UIPanel currentPanel = panelStack.Pop();
            Debug.Log($"Close UIPanel : <color=yellow>{currentPanel.GetType().Name}.</color>");
            currentPanel.Close();

            // 显示上一个面板
            if (panelStack.Count > 0)
            {
                panelStack.Peek().Show();
            }
        }

        /// <summary>
        /// 关闭指定UI面板
        /// </summary>
        public static void CloseUIPanel<T>()
        {
            string typeName = typeof(T).Name;
            Debug.Log($"Close UIPanel : <color=yellow>{typeName}.</color>");
            if (uiPanelDic.TryGetValue(typeName, out UIPanel ui))
            {
                // 从栈中移除该面板(如果存在)
                var tempStack = new Stack<UIPanel>();
                while (panelStack.Count > 0)
                {
                    var panel = panelStack.Pop();
                    if (panel != ui)
                    {
                        tempStack.Push(panel);
                    }
                }
                // 恢复栈
                while (tempStack.Count > 0)
                {
                    panelStack.Push(tempStack.Pop());
                }
                ui.Close();
            }
        }

        /// <summary>
        /// 清理所有UI面板
        /// </summary>
        /// <param name="destroyGameObjects">是否销毁GameObject对象</param>
        public static void ClearAllUIPanel(bool destroyGameObjects = true)
        {
            // 关闭并清理栈中的面板
            while (panelStack.Count > 0)
            {
                var panel = panelStack.Pop();
                panel.Close();
                if (destroyGameObjects && panel != null)
                {
                    Object.Destroy(panel.gameObject);
                }
            }
            // 处理字典中可能存在的未在栈中的面板
            foreach (var panel in uiPanelDic)
            {
                if (panel.Value != null)
                {
                    panel.Value.Close();
                    if (destroyGameObjects)
                    {
                        Object.Destroy(panel.Value.gameObject);
                    }
                }
            }
            // 清空字典
            uiPanelDic.Clear();
        }

        /// <summary>
        /// 获取当前显示的UIPanel
        /// </summary>
        public static T GetCurrentUIPanel<T>() where T : UIPanel
        {
            if (panelStack.Count == 0) return null;
            return panelStack.Peek() as T;
        }

        /// <summary>
        /// 简化方法：打开UI面板（默认从Resources加载）
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="layer">UI层级</param>
        /// <returns>打开的面板实例</returns>
        public static T OpenPanel<T>(UILayer layer = UILayer.ContentLayer) where T : UIPanel
        {
            return OpenUIPanelFromRes<T>(true, layer);
        }

        /// <summary>
        /// 简化方法：关闭UI面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        public static void ClosePanel<T>() where T : UIPanel
        {
            CloseUIPanel<T>();
        }

        /// <summary>
        /// 简化方法：获取指定类型的面板（如果已打开）
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <returns>面板实例</returns>
        public static T GetPanel<T>() where T : UIPanel
        {
            string typeName = typeof(T).Name;
            if (uiPanelDic.TryGetValue(typeName, out UIPanel panel) && panel.gameObject.activeInHierarchy)
            {
                return panel as T;
            }
            return null;
        }

        /// <summary>
        /// 简化方法：获取栈顶的指定类型面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <returns>栈顶面板实例</returns>
        public static T GetTopPanel<T>() where T : UIPanel
        {
            return GetCurrentUIPanel<T>();
        }

        #region Handle

        /// <summary>
        /// 查找子物体（优化版本，支持路径查找）
        /// </summary>
        /// <param name="panel">父物体</param>
        /// <param name="childPath">子物体路径，支持"Parent/Child"格式</param>
        private static GameObject FindChildGameObject(GameObject panel, string childPath)
        {
            if (string.IsNullOrEmpty(childPath))
            {
                Debug.LogError("Child path cannot be null or empty");
                return null;
            }

            // 支持路径查找
            if (childPath.Contains("/"))
            {
                Transform current = panel.transform;
                string[] pathSegments = childPath.Split('/');

                foreach (string segment in pathSegments)
                {
                    if (string.IsNullOrEmpty(segment)) continue;

                    Transform found = null;
                    for (int i = 0; i < current.childCount; i++)
                    {
                        if (current.GetChild(i).name == segment)
                        {
                            found = current.GetChild(i);
                            break;
                        }
                    }

                    if (found == null)
                    {
                        Debug.LogError($"Not find child {segment} in path {childPath} under {panel.name}");
                        return null;
                    }
                    current = found;
                }
                return current.gameObject;
            }
            else
            {
                // 原有的递归查找方式
                Transform[] children = panel.GetComponentsInChildren<Transform>(true);
                foreach (var item in children)
                {
                    if (item.name == childPath)
                    {
                        return item.gameObject;
                    }
                }
                Debug.LogError($"Not find child {childPath} in {panel.name}");
                return null;
            }
        }

        /// <summary>
        /// 根据名称获取组件,如果没有则添加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        public static T GetOrAddComponent<T>(GameObject panel, out T component) where T : Component
        {
            component = panel.GetComponent<T>();
            if (component == null)
                component = panel.AddComponent<T>();

            return component;
        }

        /// <summary>
        /// 根据名称获取子物体组件,如果没有则添加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        public static T GetOrAddComponentInChildren<T>(GameObject panel, string childName, out T component) where T : Component
        {
            GameObject child = FindChildGameObject(panel, childName);
            if (child == null)
            {
                component = null;
                return null;
            }
            component = child.GetComponent<T>();
            if (component == null)
                component = child.AddComponent<T>();

            return component;
        }

        #endregion

        #endregion
    }
}