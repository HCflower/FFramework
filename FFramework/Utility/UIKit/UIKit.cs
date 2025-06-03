using System.Collections.Generic;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// UIKit工具类
    /// </summary>
    public static class UIKit
    {
        private static Dictionary<string, UIPanel> uiPanelDic = new Dictionary<string, UIPanel>();
        private static Stack<UIPanel> panelStack = new Stack<UIPanel>();

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

        #region Handle

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
        /// 查找子物体
        /// </summary>
        /// <param name="name">子物体名称</param>
        private static GameObject FindChildGameObject(GameObject panel, string childName)
        {
            Transform[] children = panel.GetComponentsInChildren<Transform>(true);
            foreach (var item in children)
            {
                if (item.name == childName)
                {
                    return item.gameObject;
                }
            }
            Debug.LogError($"Not find child {childName} in {panel.name}");
            return null;
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
    }
}