# FFramework.Kit UIKit 使用文档

---

## 一、简介

`UIKit` 是 FFramework 的 UI 管理工具类，专为 Unity 单 Canvas 架构设计，支持 UI 面板的打开、关闭、层级管理、组件查找等功能。通过泛型和栈结构，提供高效、类型安全的 UI 操作体验。

---

## 二、快速上手

### 1. 创建面板脚本

```csharp
using FFramework.Kit;

public class MainMenuPanel : UIPanel
{
    public override void Init()
    {
        // 初始化逻辑
    }
}
```

### 2. 准备面板预制体

- 在场景中创建 UI 面板 GameObject
- 挂载你的面板脚本
- 制作成预制体并放入 `Resources` 文件夹
- 预制体名称需与脚本类名一致

### 3. 打开/关闭面板

```csharp
// 打开面板（默认内容层）
var panel = UIKit.OpenPanel<MainMenuPanel>();

// 打开到指定层级
var popup = UIKit.OpenPanel<SettingsPanel>(UILayer.PopupLayer);

// 关闭面板
UIKit.ClosePanel<MainMenuPanel>();
```

---

## 三、核心功能

### 1. 面板管理

- 打开面板：`OpenPanel<T>(UILayer layer = UILayer.ContentLayer)`
- 关闭面板：`ClosePanel<T>()`
- 获取面板实例：`GetPanel<T>()`
- 获取栈顶面板：`GetTopPanel<T>()`
- 批量关闭层级面板：`CloseAllPanelsInLayer(UILayer layer)`
- 清理已销毁面板引用：`CleanupDestroyedPanels()`

### 2. 层级与排序

- UI 层级枚举：`BackgroundLayer`、`PostProcessingLayer`、`ContentLayer`、`PopupLayer`、`GuideLayer`、`DebugLayer`
- 设置面板显示顺序：`SetPanelSortOrder(UIPanel panel, int sortOrder)`
- 置前/置后显示：`BringPanelToFront(UIPanel panel)` / `SendPanelToBack(UIPanel panel)`
- 交换面板顺序：`SwapPanelOrder(UIPanel panel1, UIPanel panel2)`
- 查询面板顺序：`GetPanelSortOrder(UIPanel panel)`

### 3. 组件查找

- 查找子物体：`FindChildGameObject(GameObject panel, string childPath)`
- 获取或添加组件：`GetOrAddComponent<T>(GameObject panel, out T component)`
- 在子物体中获取或添加组件：`GetOrAddComponentInChildren<T>(GameObject panel, string childName, out T component)`

### 4. 预加载与性能

- 预加载面板：`PreloadPanel<T>(Action<T> callback = null)`
- 获取活跃面板名称列表：`GetActivePanelNames()`
- 获取层级活跃面板数量：`GetActivePanelCountInLayer(UILayer layer)`
- 清理所有面板：`ClearAllUIPanel(bool destroyGameObjects = true)`

---

## 四、示例代码

### 打开主菜单并置于最前

```csharp
var mainMenu = UIKit.OpenPanel<MainMenuPanel>();
UIKit.BringPanelToFront(mainMenu);
```

### 批量关闭弹窗层所有面板

```csharp
UIKit.CloseAllPanelsInLayer(UILayer.PopupLayer);
```

### 查找并操作组件

```csharp
var panel = UIKit.GetPanel<MainMenuPanel>();
UIKit.GetOrAddComponentInChildren<Button>(panel.gameObject, "StartButton", out Button startBtn);
if (startBtn != null) startBtn.onClick.AddListener(() => Debug.Log("开始游戏"));
```

---

## 五、常见问题

- 面板无法打开：检查预制体是否在 `Resources` 文件夹，名称是否与类名一致，脚本是否继承自 `UIPanel`
- 排序不生效：确认面板在同一父对象下，避免多 Canvas 冲突
- 内存占用高：定期调用 `CleanupDestroyedPanels()`，监控缓存数量

---

## 六、最佳实践

1. 预制体命名与脚本类名保持一致
2. 所有 UI 面板均继承自 `UIPanel`
3. 资源放在 `Resources` 文件夹
4. 合理分配 UI 层级，避免频繁排序
5. 场景切换时清理无效引用

---

如需详细 API 说明和高级用法，请参考源码注释或联系维护者。
