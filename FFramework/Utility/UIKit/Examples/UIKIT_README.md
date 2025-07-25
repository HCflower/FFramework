# UIKit 完整文档

本文档包含 UIKit 工具类的完整功能说明、使用教程和 API 参考。

## � 目录

1. [概述](#概述)
2. [快速开始](#快速开始)
3. [核心特性](#核心特性)
4. [API 参考](#api参考)
5. [面板排序指南](#面板排序指南)
6. [使用教程](#使用/程)
7. [示例代码](#示例代码)
8. [常见问题](#常见问题)
9. [性能优化](#性能优化)

---

## 概述

UIKit 是 FFramework 中的 UI 管理工具类，提供了完整的 UI 面板管理、层级控制、组件查找等功能。它采用单例模式和栈管理，确保 UI 面板的有序管理和高效操作。

### 设计理念

- **单 Canvas 架构**: 专为全局单 Canvas 设计，通过 Transform 层级控制显示顺序
- **高性能**: 智能缓存、预加载、批量操作，最大化性能
- **易用性**: 简化的 API 设计，类型安全的泛型支持
- **可调试**: 丰富的状态查询和调试功能

---

## 快速开始

### 1. 基础设置

#### 创建 UI 面板脚本

```csharp
using FFramework.Kit;

public class MainMenuPanel : UIPanel
{
    private void Awake()
    {
        // 面板初始化逻辑
        Debug.Log("MainMenuPanel 初始化");
    }

    private void OnEnable()
    {
        // 面板显示时的逻辑
        Debug.Log("MainMenuPanel 显示");
    }

    private void OnDisable()
    {
        // 面板隐藏时的逻辑
        Debug.Log("MainMenuPanel 隐藏");
    }
}
```

#### 准备面板预制体

1. 在场景中创建 UI 面板 GameObject
2. 添加你的面板脚本组件
3. 制作成预制体并放入`Resources`文件夹
4. **重要**: 确保预制体名称与脚本类名完全一致

### 2. 基本操作

```csharp
// 打开面板到默认层级（ContentLayer）
var panel = UIKit.OpenPanel<MainMenuPanel>();

// 打开面板到指定层级
var popup = UIKit.OpenPanel<SettingsPanel>(UILayer.PopupLayer);

// 关闭指定类型的面板
UIKit.ClosePanel<MainMenuPanel>();

// 获取已打开的面板实例
var mainMenu = UIKit.GetPanel<MainMenuPanel>();

// 获取栈顶的面板
var topPanel = UIKit.GetTopPanel<UIPanel>();
```

---

## 核心特性

### 🚀 高性能面板管理

- **面板缓存**: 智能缓存已加载的面板，避免重复实例化
- **栈式管理**: 使用栈结构管理面板显示顺序，支持返回操作
- **层级控制**: 支持多层级 UI 管理（背景层、内容层、弹窗层等）

### 🔍 强大的组件查找

- **路径查找**: 支持 "Parent/Child" 格式的路径查找
- **递归查找**: 深度搜索子组件
- **类型安全**: 泛型支持，编译时类型检查

### ⚡ 实用功能

- **批量操作**: 批量关闭指定层级的面板
- **预加载**: 支持面板预加载，提升用户体验
- **自动清理**: 自动清理已销毁的面板引用
- **性能监控**: 提供面板数量统计和状态查询

### 🎯 单 Canvas 优化

- **智能排序**: 基于 Transform.siblingIndex 的排序控制
- **快速操作**: 置前、置后、交换位置等快捷方法
- **状态查询**: 实时获取面板显示顺序信息

---

## API 参考

### 核心属性

```csharp
/// <summary>当前打开的UI面板数量</summary>
public static int OpenPanelCount { get; }

/// <summary>缓存的UI面板数量</summary>
public static int CachedPanelCount { get; }

/// <summary>是否有打开的面板</summary>
public static bool HasOpenPanels { get; }
```

### UI 层级

UIKit 支持以下 UI 层级：

- **BackgroundLayer**: 背景层 - 用于背景 UI
- **PostProcessingLayer**: 后处理层 - 用于特效处理
- **ContentLayer**: 内容层 - 主要游戏 UI（默认）
- **GuideLayer**: 引导层 - 新手引导 UI
- **PopupLayer**: 弹窗层 - 弹窗提示 UI
- **DebugLayer**: 调试层 - 调试信息 UI

### 面板管理方法

#### 打开面板

```csharp
// 简化方法（推荐）
public static T OpenPanel<T>(UILayer layer = UILayer.ContentLayer) where T : UIPanel

// 完整方法
public static T OpenUIPanelFromRes<T>(bool isCache = true, UILayer layer = UILayer.DebugLayer) where T : UIPanel
public static T OpenUIPanelFromAsset<T>(GameObject uiPrefab, bool isCache = true, UILayer layer = UILayer.DebugLayer) where T : UIPanel
```

#### 关闭面板

```csharp
// 简化方法（推荐）
public static void ClosePanel<T>() where T : UIPanel

// 完整方法
public static void CloseCurrentUIPanel()
public static void CloseUIPanel<T>()
public static void ClearAllUIPanel(bool destroyGameObjects = true)
```

#### 获取面板

```csharp
// 简化方法（推荐）
public static T GetPanel<T>() where T : UIPanel
public static T GetTopPanel<T>() where T : UIPanel

// 完整方法
public static T GetCurrentUIPanel<T>() where T : UIPanel
```

### 面板排序方法

```csharp
// 设置面板显示顺序（siblingIndex）
public static void SetPanelSortOrder(UIPanel panel, int sortOrder)

// 将面板置于最前面显示
public static void BringPanelToFront(UIPanel panel)

// 将面板置于最后面显示
public static void SendPanelToBack(UIPanel panel)

// 获取面板当前显示顺序
public static int GetPanelSortOrder(UIPanel panel)

// 交换两个面板的显示顺序
public static void SwapPanelOrder(UIPanel panel1, UIPanel panel2)
```

### 批量操作方法

```csharp
// 批量关闭指定层级的所有面板
public static void CloseAllPanelsInLayer(UILayer layer)

// 获取指定层级的活跃面板数量
public static int GetActivePanelCountInLayer(UILayer layer)

// 获取所有活跃面板的名称列表
public static List<string> GetActivePanelNames()
```

### 实用功能方法

```csharp
// 预加载UI面板
public static void PreloadPanel<T>(System.Action<T> callback = null) where T : UIPanel

// 清理所有已销毁的面板引用
public static void CleanupDestroyedPanels()
```

### 组件查找方法

```csharp
// 获取或添加组件
public static T GetOrAddComponent<T>(GameObject panel, out T component) where T : Component

// 在子物体中获取或添加组件
public static T GetOrAddComponentInChildren<T>(GameObject panel, string childName, out T component) where T : Component
```

---

## 面板排序指南

### 单 Canvas 架构原理

在单 Canvas 架构中，所有 UI 面板共享一个主 Canvas，通过 Transform 的 siblingIndex 控制显示顺序：

```
Canvas (Screen Space - Overlay)
├── BackgroundLayer    (siblingIndex: 0)
├── ContentLayer       (siblingIndex: 1)
│   ├── MenuPanel      (siblingIndex: 0) ← 最后显示
│   ├── InventoryPanel (siblingIndex: 1) ← 中间显示
│   └── ShopPanel      (siblingIndex: 2) ← 最前显示
├── PopupLayer         (siblingIndex: 2)
└── DebugLayer         (siblingIndex: 3)
```

### 排序规则

- **siblingIndex 越大** = **显示越靠前**
- `SetAsLastSibling()` = 显示在最前面
- `SetAsFirstSibling()` = 显示在最后面

### 排序方法使用

```csharp
// 方法1：设置具体位置
UIKit.SetPanelSortOrder(panel, 2);  // 设置到索引位置2

// 方法2：快速置前置后
UIKit.BringPanelToFront(panel);     // 置于最前面
UIKit.SendPanelToBack(panel);       // 置于最后面

// 方法3：交换位置
UIKit.SwapPanelOrder(panel1, panel2);

// 方法4：查询当前位置
int currentOrder = UIKit.GetPanelSortOrder(panel);
```

---

## 使用教程

### 实际使用案例

#### 案例 1：游戏主界面管理

```csharp
public class GameUIManager : MonoBehaviour
{
    private void Start()
    {
        // 预加载常用面板
        PreloadCommonPanels();

        // 打开主菜单
        UIKit.OpenPanel<MainMenuPanel>();
    }

    private void PreloadCommonPanels()
    {
        UIKit.PreloadPanel<InventoryPanel>();
        UIKit.PreloadPanel<SettingsPanel>();
        UIKit.PreloadPanel<ShopPanel>();
    }

    public void OnInventoryButtonClick()
    {
        var inventory = UIKit.OpenPanel<InventoryPanel>();
        // 确保背包面板在最前面
        UIKit.BringPanelToFront(inventory);
    }

    public void OnSettingsButtonClick()
    {
        UIKit.OpenPanel<SettingsPanel>(UILayer.PopupLayer);
    }
}
```

#### 案例 2：弹窗管理系统

```csharp
public class PopupManager : MonoBehaviour
{
    public void ShowConfirmDialog(string message, System.Action onConfirm)
    {
        var dialog = UIKit.OpenPanel<ConfirmDialogPanel>(UILayer.PopupLayer);
        if (dialog != null)
        {
            dialog.SetMessage(message);
            dialog.SetConfirmCallback(onConfirm);
            // 确保对话框在最前面
            UIKit.BringPanelToFront(dialog);
        }
    }

    public void ShowNotification(string text)
    {
        var notification = UIKit.OpenPanel<NotificationPanel>(UILayer.PopupLayer);
        if (notification != null)
        {
            notification.ShowNotification(text);
            // 通知应该在所有弹窗之上
            UIKit.BringPanelToFront(notification);
        }
    }

    public void CloseAllPopups()
    {
        UIKit.CloseAllPanelsInLayer(UILayer.PopupLayer);
    }
}
```

#### 案例 3：场景切换时的 UI 清理

```csharp
public class SceneManager : MonoBehaviour
{
    public void OnSceneUnload()
    {
        // 关闭所有UI面板
        UIKit.CloseAllPanelsInLayer(UILayer.ContentLayer);
        UIKit.CloseAllPanelsInLayer(UILayer.PopupLayer);

        // 清理已销毁的面板引用
        UIKit.CleanupDestroyedPanels();
    }

    public void OnSceneLoad()
    {
        // 预加载新场景需要的UI
        PreloadScenePanels();
    }
}
```

### 组件操作示例

```csharp
public class PanelComponentExample : MonoBehaviour
{
    public void SetupPanelComponents()
    {
        var panel = UIKit.GetPanel<MainMenuPanel>();
        if (panel != null)
        {
            // 获取或添加按钮组件
            UIKit.GetOrAddComponentInChildren<Button>(panel.gameObject, "StartButton", out Button startBtn);
            if (startBtn != null)
            {
                startBtn.onClick.AddListener(() => Debug.Log("开始游戏"));
            }

            // 使用路径查找
            UIKit.GetOrAddComponentInChildren<Text>(panel.gameObject, "Header/Title", out Text titleText);
            if (titleText != null)
            {
                titleText.text = "欢迎来到游戏";
            }
        }
    }
}
```

---

## 示例代码

项目包含两个完整的示例脚本：

### UIKitBasicExample.cs - 基础功能示例

演示基本的面板操作：

- 面板的打开、关闭、查找
- 状态查询功能
- 手动测试方法

### UIKitAdvancedExample.cs - 高级功能示例

演示高级功能：

- 组件查找和操作
- 面板预加载
- 单 Canvas 架构下的排序演示
- 性能监控和内存管理

### 运行示例的步骤

1. **创建测试场景**

   - 新建空场景
   - 添加 Canvas 和 EventSystem

2. **设置 UIRoot**

   - 确保场景中有 UIRoot 组件
   - 配置各个 UI 层级

3. **添加示例脚本**

   - 将示例脚本添加到 GameObject
   - 在 Inspector 中启用自动运行

4. **创建测试面板**
   - 在 Resources 文件夹创建面板预制体
   - 确保名称与脚本类名一致

---

## 常见问题

### Q1: 面板无法打开？

**可能原因：**

- 预制体不在 Resources 文件夹
- 预制体名称与类名不一致
- 面板脚本没有继承 UIPanel

**解决方案：**

```csharp
// 检查资源路径
var prefab = Resources.Load<GameObject>("面板名称");
if (prefab == null)
{
    Debug.LogError("面板预制体未找到，请检查Resources文件夹");
}
```

### Q2: 面板排序不生效？

**可能原因：**

- 面板在不同的父对象下
- 多个 Canvas 冲突

**解决方案：**

```csharp
// 检查面板的父对象
Debug.Log($"面板父对象: {panel.transform.parent.name}");

// 使用UIKit提供的排序方法
UIKit.SetPanelSortOrder(panel, sortOrder);
```

### Q3: 内存占用过高？

**解决方案：**

```csharp
// 定期清理
UIKit.CleanupDestroyedPanels();

// 监控缓存数量
if (UIKit.CachedPanelCount > maxCacheCount)
{
    // 手动清理一些不常用的面板
    UIKit.ClearAllUIPanel(false); // 不销毁GameObject，只清理缓存
}
```

### Q4: 如何调试面板状态？

```csharp
// 显示详细状态信息
Debug.Log($"打开面板数: {UIKit.OpenPanelCount}");
Debug.Log($"缓存面板数: {UIKit.CachedPanelCount}");
Debug.Log($"活跃面板: {string.Join(", ", UIKit.GetActivePanelNames())}");

// 显示面板排序信息
var panel = UIKit.GetPanel<MainMenuPanel>();
if (panel != null)
{
    Debug.Log($"面板排序: {UIKit.GetPanelSortOrder(panel)}");
}
```

---

## 性能优化

### 1. 预加载策略

```csharp
// 在游戏启动时预加载常用面板
public void PreloadCommonPanels()
{
    UIKit.PreloadPanel<MainMenuPanel>();
    UIKit.PreloadPanel<InventoryPanel>();
    UIKit.PreloadPanel<SettingsPanel>();
}

// 在场景加载时预加载场景专用面板
public void PreloadScenePanels()
{
    UIKit.PreloadPanel<GameHUDPanel>();
    UIKit.PreloadPanel<PausePanel>();
}
```

### 2. 内存管理

```csharp
// 定期清理（建议在场景切换时）
public void OnSceneUnload()
{
    UIKit.CleanupDestroyedPanels();
}

// 监控内存使用
public void MonitorMemoryUsage()
{
    if (UIKit.CachedPanelCount > 20) // 缓存面板过多
    {
        Debug.LogWarning("缓存面板数量过多，建议清理");
    }
}
```

### 3. 渲染优化

```csharp
// 合理使用UI层级
UIKit.OpenPanel<BackgroundPanel>(UILayer.BackgroundLayer);
UIKit.OpenPanel<GameUIPanel>(UILayer.ContentLayer);
UIKit.OpenPanel<DialogPanel>(UILayer.PopupLayer);

// 避免频繁排序
// ❌ 不要在Update中调用
void Update()
{
    UIKit.BringPanelToFront(somePanel); // 错误！
}

// ✅ 只在需要时调用
public void OnPanelClicked(UIPanel panel)
{
    UIKit.BringPanelToFront(panel); // 正确！
}
```

### 4. 最佳实践总结

1. **命名规范**: 确保预制体名称与脚本类名完全一致
2. **继承结构**: 所有 UI 面板都应继承自 UIPanel
3. **资源管理**: 将面板预制体放在 Resources 文件夹中
4. **性能监控**: 定期检查面板数量和内存使用
5. **错误处理**: 检查面板是否成功打开，处理 null 情况
6. **生命周期**: 正确使用 Unity 生命周期方法
7. **层级管理**: 根据功能合理分配 UI 层级
8. **内存清理**: 在合适的时机清理无效引用

---

📝 **注意**: 本文档基于 FFramework 框架，确保你的项目中已正确配置 UIPanel 基类和相关依赖。

🎯 **专为单 Canvas 架构设计**: UIKit 已针对全局单 Canvas 架构进行优化，提供最佳的性能和易用性。
