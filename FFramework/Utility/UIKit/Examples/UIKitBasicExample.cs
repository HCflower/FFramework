using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FFramework.Kit;

namespace FFramework.Examples
{
    /// <summary>
    /// UIKit 基础使用示例
    /// 演示了UIKit的基本功能：面板打开、关闭、查找等
    /// </summary>
    public class UIKitBasicExample : MonoBehaviour
    {
        [Header("示例控制")]
        public bool autoRunExample = true;
        public float delayBetweenOperations = 2f;

        private void Start()
        {
            if (autoRunExample)
            {
                StartCoroutine(RunBasicExample());
            }
        }

        private IEnumerator RunBasicExample()
        {
            Debug.Log("=== UIKit 基础示例开始 ===");

            // 1. 显示当前状态
            ShowCurrentStatus("初始状态");
            yield return new WaitForSeconds(delayBetweenOperations);

            // 2. 打开主菜单面板
            Debug.Log("2. 打开主菜单面板");
            var mainMenu = UIKit.OpenPanel<ExampleMainMenuPanel>();
            if (mainMenu != null)
            {
                Debug.Log("主菜单面板打开成功");
            }
            ShowCurrentStatus("打开主菜单后");
            yield return new WaitForSeconds(delayBetweenOperations);

            // 3. 打开设置面板（弹窗层）
            Debug.Log("3. 打开设置面板");
            var settings = UIKit.OpenPanel<ExampleSettingsPanel>(UILayer.PopupLayer);
            if (settings != null)
            {
                Debug.Log("设置面板打开成功");
            }
            ShowCurrentStatus("打开设置面板后");
            yield return new WaitForSeconds(delayBetweenOperations);

            // 4. 获取当前顶层面板
            Debug.Log("4. 获取顶层面板");
            var topPanel = UIKit.GetTopPanel<UIPanel>();
            if (topPanel != null)
            {
                Debug.Log($"当前顶层面板: {topPanel.GetType().Name}");
            }
            yield return new WaitForSeconds(delayBetweenOperations);

            // 5. 关闭设置面板
            Debug.Log("5. 关闭设置面板");
            UIKit.ClosePanel<ExampleSettingsPanel>();
            ShowCurrentStatus("关闭设置面板后");
            yield return new WaitForSeconds(delayBetweenOperations);

            // 6. 获取指定层级的面板数量
            Debug.Log("6. 查询各层级面板数量");
            Debug.Log($"内容层面板数量: {UIKit.GetActivePanelCountInLayer(UILayer.ContentLayer)}");
            Debug.Log($"弹窗层面板数量: {UIKit.GetActivePanelCountInLayer(UILayer.PopupLayer)}");
            yield return new WaitForSeconds(delayBetweenOperations);

            // 7. 批量关闭内容层面板
            Debug.Log("7. 批量关闭内容层面板");
            UIKit.CloseAllPanelsInLayer(UILayer.ContentLayer);
            ShowCurrentStatus("批量关闭后");
            yield return new WaitForSeconds(delayBetweenOperations);

            Debug.Log("=== UIKit 基础示例结束 ===");
        }

        private void ShowCurrentStatus(string context)
        {
            Debug.Log($"--- {context} ---");
            Debug.Log($"打开的面板数量: {UIKit.OpenPanelCount}");
            Debug.Log($"缓存的面板数量: {UIKit.CachedPanelCount}");
            Debug.Log($"是否有打开的面板: {UIKit.HasOpenPanels}");

            var activeNames = UIKit.GetActivePanelNames();
            if (activeNames.Count > 0)
            {
                Debug.Log($"活跃面板: {string.Join(", ", activeNames)}");
            }
            else
            {
                Debug.Log("没有活跃面板");
            }
        }

        #region 手动测试方法

        [ContextMenu("打开主菜单")]
        public void OpenMainMenu()
        {
            UIKit.OpenPanel<ExampleMainMenuPanel>();
        }

        [ContextMenu("打开设置面板")]
        public void OpenSettings()
        {
            UIKit.OpenPanel<ExampleSettingsPanel>(UILayer.PopupLayer);
        }

        [ContextMenu("关闭所有面板")]
        public void CloseAllPanels()
        {
            UIKit.CloseAllPanelsInLayer(UILayer.ContentLayer);
            UIKit.CloseAllPanelsInLayer(UILayer.PopupLayer);
        }

        [ContextMenu("显示状态")]
        public void ShowStatus()
        {
            ShowCurrentStatus("手动查询");
        }

        [ContextMenu("清理已销毁面板")]
        public void CleanupPanels()
        {
            UIKit.CleanupDestroyedPanels();
            Debug.Log("已清理销毁的面板引用");
        }

        #endregion
    }

    /// <summary>
    /// 示例主菜单面板
    /// </summary>
    public class ExampleMainMenuPanel : UIPanel
    {
        private void Awake()
        {
            Debug.Log("ExampleMainMenuPanel 初始化");
        }

        private void OnEnable()
        {
            Debug.Log("ExampleMainMenuPanel 显示");
        }

        private void OnDisable()
        {
            Debug.Log("ExampleMainMenuPanel 隐藏");
        }
    }

    /// <summary>
    /// 示例设置面板
    /// </summary>
    public class ExampleSettingsPanel : UIPanel
    {
        private void Awake()
        {
            Debug.Log("ExampleSettingsPanel 初始化");
        }

        private void OnEnable()
        {
            Debug.Log("ExampleSettingsPanel 显示");
        }

        private void OnDisable()
        {
            Debug.Log("ExampleSettingsPanel 隐藏");
        }
    }
}
