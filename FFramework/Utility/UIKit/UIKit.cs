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

        private static Transform GetUIRoot()
        {
            return UIRoot.Instance.transform;
        }

        /// <summary>
        /// 从Resources/UI 中加载UI面板
        /// </summary>
        /// <param name="uiPanelName">UI面板名称</param>
        /// <param name="isCache">是否缓存面板(默认true)</param>
        public static T OpenUIFromRes<T>(string uiPanelName, bool isCache = true) where T : UIPanel
        {
            if (!uiPanelDic.TryGetValue(uiPanelName, out UIPanel uiPanel))
            {
                // 从Resources加载预设体
                GameObject prefab = Resources.Load<GameObject>($"UI/{uiPanelName}");
                if (prefab == null)
                {
                    Debug.LogError($"UI预制体不存在: UI/{uiPanelName}");
                    return null;
                }
                // 实例化UI
                GameObject ui = Object.Instantiate(prefab, GetUIRoot());
                ui.name = uiPanelName;
                uiPanel = ui.GetComponent<T>();
                if (uiPanel == null)
                {
                    Debug.LogError($"UI预制体缺少{typeof(UIPanel)}组件: {uiPanelName}");
                    Object.Destroy(ui);
                    return null;
                }
                if (isCache) uiPanelDic.Add(uiPanelName, uiPanel);
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
        public static T OpenUIFromAsset<T>(GameObject uiPrefab, bool isCache = true) where T : UIPanel
        {
            if (uiPrefab == null)
            {
                Debug.LogError("UI预制体为空");
                return null;
            }

            string panelName = uiPrefab.name;
            if (!uiPanelDic.TryGetValue(panelName, out UIPanel uiPanel))
            {
                // 实例化UI
                GameObject uiInstance = Object.Instantiate(uiPrefab, GetUIRoot());
                uiInstance.name = panelName;

                // 获取UIPanel组件
                uiPanel = uiInstance.GetComponent<T>();
                if (uiPanel == null)
                {
                    Debug.LogError($"UI预制体缺少{typeof(T)}组件: {panelName}");
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
        /// 关闭当前UI
        /// </summary>
        public static void CloseCurrentUI()
        {
            if (panelStack.Count == 0) return;

            var currentPanel = panelStack.Pop();
            currentPanel.Close();

            // 显示上一个面板
            if (panelStack.Count > 0)
            {
                panelStack.Peek().Show();
            }
        }

        /// <summary>
        /// 关闭指定UI
        /// </summary>
        public static void CloseUI(string uiName)
        {
            if (uiPanelDic.TryGetValue(uiName, out UIPanel ui))
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
        public static void ClearAllUIPanel(bool destroyGameObjects = false)
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