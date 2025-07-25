using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FFramework.Kit;

namespace FFramework.Examples
{
    /// <summary>
    /// UIKit 高级功能示例
    /// 演示组件查找、预加载、排序等高级功能
    /// </summary>
    public class UIKitAdvancedExample : MonoBehaviour
    {
        [Header("示例控制")]
        public bool runAdvancedExample = true;
        public float operationDelay = 1.5f;

        private void Start()
        {
            if (runAdvancedExample)
            {
                StartCoroutine(RunAdvancedExample());
            }
        }

        private IEnumerator RunAdvancedExample()
        {
            Debug.Log("=== UIKit 高级功能示例开始 ===");

            // 1. 预加载示例
            Debug.Log("1. 演示面板预加载功能");
            yield return StartCoroutine(DemoPreloading());
            yield return new WaitForSeconds(operationDelay);

            // 2. 组件查找和操作示例
            Debug.Log("2. 演示组件查找功能");
            yield return StartCoroutine(DemoComponentFinding());
            yield return new WaitForSeconds(operationDelay);

            // 3. 面板排序示例
            Debug.Log("3. 演示面板排序功能");
            yield return StartCoroutine(DemoPanelSorting());
            yield return new WaitForSeconds(operationDelay);

            // 4. 性能监控示例
            Debug.Log("4. 演示性能监控功能");
            DemoPerformanceMonitoring();
            yield return new WaitForSeconds(operationDelay);

            // 5. 内存管理示例
            Debug.Log("5. 演示内存管理功能");
            DemoMemoryManagement();

            Debug.Log("=== UIKit 高级功能示例结束 ===");
        }

        private IEnumerator DemoPreloading()
        {
            Debug.Log("开始预加载面板...");

            // 预加载示例面板
            bool preloadCompleted = false;
            UIKit.PreloadPanel<ExampleInventoryPanel>((panel) =>
            {
                if (panel != null)
                {
                    Debug.Log("背包面板预加载成功");
                }
                else
                {
                    Debug.LogWarning("背包面板预加载失败");
                }
                preloadCompleted = true;
            });

            // 等待预加载完成
            while (!preloadCompleted)
            {
                yield return null;
            }

            // 快速打开已预加载的面板
            Debug.Log("快速打开预加载的面板");
            var inventory = UIKit.OpenPanel<ExampleInventoryPanel>();
            if (inventory != null)
            {
                Debug.Log("预加载的面板打开速度更快！");
            }
        }

        private IEnumerator DemoComponentFinding()
        {
            // 打开一个包含子组件的面板
            var testPanel = UIKit.OpenPanel<ExampleComponentTestPanel>();
            if (testPanel != null)
            {
                yield return new WaitForSeconds(0.5f);

                // 演示组件查找和自动添加
                Debug.Log("查找并操作子组件...");

                // 查找按钮组件
                UIKit.GetOrAddComponentInChildren<Button>(testPanel.gameObject, "TestButton", out Button button);
                if (button != null)
                {
                    Debug.Log("找到按钮组件，设置点击事件");
                    button.onClick.AddListener(() => Debug.Log("按钮被点击！"));
                }

                // 查找文本组件
                UIKit.GetOrAddComponentInChildren<Text>(testPanel.gameObject, "TestText", out Text text);
                if (text != null)
                {
                    Debug.Log("找到文本组件，设置文本内容");
                    text.text = "UIKit 组件查找成功！";
                }

                // 演示路径查找（如果存在嵌套结构）
                var nestedComponent = testPanel.FindNestedComponent("Panel/SubPanel/NestedText");
                if (nestedComponent != null)
                {
                    Debug.Log("路径查找成功：找到嵌套组件");
                }
            }
        }

        private IEnumerator DemoPanelSorting()
        {
            Debug.Log("创建多个面板演示排序...");

            // 打开多个面板
            var panel1 = UIKit.OpenPanel<ExamplePanel1>();
            var panel2 = UIKit.OpenPanel<ExamplePanel2>();
            var panel3 = UIKit.OpenPanel<ExamplePanel3>();

            yield return new WaitForSeconds(0.5f);

            Debug.Log("演示单Canvas架构下的面板排序...");

            // 显示初始顺序
            Debug.Log($"Panel1 当前索引: {UIKit.GetPanelSortOrder(panel1)}");
            Debug.Log($"Panel2 当前索引: {UIKit.GetPanelSortOrder(panel2)}");
            Debug.Log($"Panel3 当前索引: {UIKit.GetPanelSortOrder(panel3)}");

            yield return new WaitForSeconds(0.5f);

            // 方式1：使用具体的索引位置
            if (panel1 != null)
            {
                UIKit.SetPanelSortOrder(panel1, 0);
                Debug.Log("Panel1 设置到索引位置 0（最后面）");
            }

            yield return new WaitForSeconds(0.5f);

            if (panel2 != null)
            {
                UIKit.BringPanelToFront(panel2);
                Debug.Log("Panel2 置于最前面");
            }

            yield return new WaitForSeconds(0.5f);

            // 方式2：交换面板顺序
            Debug.Log("演示面板顺序交换...");
            if (panel1 != null && panel3 != null)
            {
                UIKit.SwapPanelOrder(panel1, panel3);
                Debug.Log("Panel1 和 Panel3 的顺序已交换");
            }

            yield return new WaitForSeconds(0.5f);

            // 方式3：动态调整顺序
            Debug.Log("演示动态排序调整...");

            if (panel1 != null)
            {
                UIKit.BringPanelToFront(panel1);
                Debug.Log("Panel1 现在置于最前面");
            }

            yield return new WaitForSeconds(0.5f);

            if (panel2 != null)
            {
                UIKit.SendPanelToBack(panel2);
                Debug.Log("Panel2 现在置于最后面");
            }

            yield return new WaitForSeconds(0.5f);

            // 显示最终顺序
            Debug.Log("=== 最终排序结果 ===");
            Debug.Log($"Panel1 最终索引: {UIKit.GetPanelSortOrder(panel1)}");
            Debug.Log($"Panel2 最终索引: {UIKit.GetPanelSortOrder(panel2)}");
            Debug.Log($"Panel3 最终索引: {UIKit.GetPanelSortOrder(panel3)}");

            Debug.Log("面板排序演示完成！");
            Debug.Log("单Canvas架构：数值越大的siblingIndex越靠前显示");
        }
        private void DemoPerformanceMonitoring()
        {
            Debug.Log("=== 性能监控数据 ===");
            Debug.Log($"总打开面板数: {UIKit.OpenPanelCount}");
            Debug.Log($"总缓存面板数: {UIKit.CachedPanelCount}");
            Debug.Log($"有活跃面板: {UIKit.HasOpenPanels}");

            // 显示各层级的面板数量
            Debug.Log("各层级面板统计:");
            Debug.Log($"  背景层: {UIKit.GetActivePanelCountInLayer(UILayer.BackgroundLayer)}");
            Debug.Log($"  内容层: {UIKit.GetActivePanelCountInLayer(UILayer.ContentLayer)}");
            Debug.Log($"  弹窗层: {UIKit.GetActivePanelCountInLayer(UILayer.PopupLayer)}");
            Debug.Log($"  引导层: {UIKit.GetActivePanelCountInLayer(UILayer.GuideLayer)}");
            Debug.Log($"  调试层: {UIKit.GetActivePanelCountInLayer(UILayer.DebugLayer)}");

            // 显示活跃面板列表
            var activeNames = UIKit.GetActivePanelNames();
            Debug.Log($"活跃面板列表: [{string.Join(", ", activeNames)}]");
        }

        private void DemoMemoryManagement()
        {
            Debug.Log("演示内存管理功能...");

            // 模拟一些面板被意外销毁的情况
            Debug.Log("执行内存清理前的状态:");
            DemoPerformanceMonitoring();

            // 执行清理
            UIKit.CleanupDestroyedPanels();
            Debug.Log("已执行面板引用清理");

            Debug.Log("清理后的状态:");
            DemoPerformanceMonitoring();

            // 建议的内存管理最佳实践
            Debug.Log("内存管理建议:");
            Debug.Log("- 在场景切换时调用 CleanupDestroyedPanels()");
            Debug.Log("- 定期监控 CachedPanelCount，避免内存泄漏");
            Debug.Log("- 对于临时面板，考虑及时销毁而不是缓存");
        }

        #region 手动测试功能

        [ContextMenu("运行预加载测试")]
        public void TestPreloading()
        {
            StartCoroutine(DemoPreloading());
        }

        [ContextMenu("运行组件查找测试")]
        public void TestComponentFinding()
        {
            StartCoroutine(DemoComponentFinding());
        }

        [ContextMenu("运行排序测试")]
        public void TestPanelSorting()
        {
            StartCoroutine(DemoPanelSorting());
        }

        [ContextMenu("显示性能数据")]
        public void ShowPerformanceData()
        {
            DemoPerformanceMonitoring();
        }

        [ContextMenu("执行内存清理")]
        public void CleanupMemory()
        {
            DemoMemoryManagement();
        }

        [ContextMenu("测试面板置前")]
        public void TestBringToFront()
        {
            var panel = UIKit.OpenPanel<ExamplePanel1>();
            if (panel != null)
            {
                UIKit.BringPanelToFront(panel);
                Debug.Log("Panel1 已置于最前面");
            }
        }

        [ContextMenu("测试面板置后")]
        public void TestSendToBack()
        {
            var panel = UIKit.GetPanel<ExamplePanel1>();
            if (panel != null)
            {
                UIKit.SendPanelToBack(panel);
                Debug.Log("Panel1 已置于最后面");
            }
        }

        [ContextMenu("测试面板交换")]
        public void TestSwapPanels()
        {
            var panel1 = UIKit.GetPanel<ExamplePanel1>();
            var panel2 = UIKit.GetPanel<ExamplePanel2>();
            if (panel1 != null && panel2 != null)
            {
                UIKit.SwapPanelOrder(panel1, panel2);
                Debug.Log("Panel1 和 Panel2 顺序已交换");
            }
        }

        [ContextMenu("显示面板顺序")]
        public void ShowPanelOrder()
        {
            var panel1 = UIKit.GetPanel<ExamplePanel1>();
            var panel2 = UIKit.GetPanel<ExamplePanel2>();
            var panel3 = UIKit.GetPanel<ExamplePanel3>();

            Debug.Log("=== 当前面板顺序 ===");
            if (panel1 != null) Debug.Log($"Panel1 索引: {UIKit.GetPanelSortOrder(panel1)}");
            if (panel2 != null) Debug.Log($"Panel2 索引: {UIKit.GetPanelSortOrder(panel2)}");
            if (panel3 != null) Debug.Log($"Panel3 索引: {UIKit.GetPanelSortOrder(panel3)}");
        }

        #endregion
    }

    #region 示例面板类

    public class ExampleInventoryPanel : UIPanel
    {
        private void Awake()
        {
            Debug.Log("ExampleInventoryPanel 初始化");
        }
    }

    public class ExampleComponentTestPanel : UIPanel
    {
        private void Awake()
        {
            Debug.Log("ExampleComponentTestPanel 初始化");
        }

        public GameObject FindNestedComponent(string path)
        {
            // 这里可以实现自定义的组件查找逻辑
            Debug.Log($"查找嵌套组件: {path}");
            return null; // 示例中返回null
        }
    }

    public class ExamplePanel1 : UIPanel
    {
        private void Awake()
        {
            Debug.Log("ExamplePanel1 初始化");
        }
    }

    public class ExamplePanel2 : UIPanel
    {
        private void Awake()
        {
            Debug.Log("ExamplePanel2 初始化");
        }
    }

    public class ExamplePanel3 : UIPanel
    {
        private void Awake()
        {
            Debug.Log("ExamplePanel3 初始化");
        }
    }

    #endregion
}
