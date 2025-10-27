# FFramework.UISystem 使用文档

## 目录

- [一、简介](#一简介)
- [二、优势](#二优势)
- [三、API 介绍](#三api介绍)
- [四、核心功能](#四核心功能)
- [五、快速上手](#五快速上手)
- [六、使用场景示例](#六使用场景示例)
- [七、性能优化](#七性能优化)

---

## 一、简介

UIKit 是 FFramework 的 UI 管理工具，专为 Unity 单 Canvas 架构设计，支持 UI 面板的打开、关闭、层级管理、组件查找等功能。通过泛型和栈结构，提供高效、类型安全的 UI 操作体验。

---

## 二、优势

- 单 Canvas 架构，层级清晰，性能优越
- 泛型 API，类型安全，易于扩展
- 支持面板预加载与缓存，减少资源消耗
- 组件查找与自动添加，开发效率高
- 层级与排序灵活，满足复杂 UI 需求

---

## 三、API 介绍

**面板管理**

- `OpenPanel<T>(UILayer layer = UILayer.ContentLayer)`：打开面板
- `ClosePanel<T>()`：关闭面板
- `GetPanel<T>()`：获取面板实例
- `GetTopPanel<T>()`：获取栈顶面板
- `CloseAllPanelsInLayer(UILayer layer)`：批量关闭层级面板
- `CleanupDestroyedPanels()`：清理已销毁面板引用

**层级与排序**

- `SetPanelSortOrder(UIPanel panel, int sortOrder)`：设置面板显示顺序
- `BringPanelToFront(UIPanel panel)` / `SendPanelToBack(UIPanel panel)`：置前/置后显示
- `SwapPanelOrder(UIPanel panel1, UIPanel panel2)`：交换面板顺序
- `GetPanelSortOrder(UIPanel panel)`：查询面板顺序

**组件查找**

- `FindChildGameObject(GameObject panel, string childPath)`：查找子物体
- `GetOrAddComponent<T>(GameObject panel, out T component)`：获取或添加组件
- `GetOrAddComponentInChildren<T>(GameObject panel, string childName, out T component)`：在子物体中获取或添加组件

**预加载与性能**

- `PreloadPanel<T>(Action<T> callback = null)`：预加载面板
- `GetActivePanelNames()`：获取活跃面板名称列表
- `GetActivePanelCountInLayer(UILayer layer)`：获取层级活跃面板数量
- `ClearAllUIPanel(bool destroyGameObjects = true)`：清理所有面板

---

## 四、核心功能

1. 面板的打开、关闭、缓存与栈管理
2. 多层级 UI 管理（背景、内容、弹窗、引导、调试等）
3. 灵活的面板排序与层级调整
4. 组件查找与自动添加，简化 UI 操作
5. 支持面板预加载，提升响应速度
6. 批量关闭、清理无效面板，优化内存

---

## 五、快速上手

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

## 六、使用场景示例

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

## 七、性能优化

- 预加载常用面板，减少首次打开卡顿
- 定期调用 `CleanupDestroyedPanels()` 清理无效引用
- 合理分配 UI 层级，避免频繁排序和多 Canvas 冲突
- 场景切换时调用 `ClearAllUIPanel()` 释放资源
- 组件查找建议缓存引用，减少重复查找

---

如需详细 API 说明和高级用法，请参考源码注释或联系维护者。
