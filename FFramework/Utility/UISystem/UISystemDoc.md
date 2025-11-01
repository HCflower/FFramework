# FFramework.UISystem 使用文档

## 概述

FFramework.UISystem 是一个简单易用的 Unity UI 管理框架，提供面板管理、事件绑定、组件查找等功能。

### 核心特性

- 🎯 简单的面板管理
- 🔗 自动事件绑定和清理
- 📱 多层级 UI 支持
- 🔍 便捷的组件查找
- 💾 面板缓存管理

---

## 快速开始

### 1. 创建 UIRoot

```csharp
// 在场景中创建UIRoot，右键选择"创建UI层级"
// 或者手动创建GameObject并添加UIRoot组件
```

### 2. 创建面板

```csharp
using FFramework.Utility;
using UnityEngine;

public class MainMenuPanel : UIPanel
{
    protected override void Initialize()
    {
        // 绑定按钮事件
        this.BindButton("StartBtn", OnStartGame);
        this.BindButton("SettingsBtn", OnSettings);
        this.BindButton("ExitBtn", OnExit);
    }

    private void OnStartGame()
    {
        UISystem.Instance.OpenPanel<GamePanel>();
        UISystem.Instance.ClosePanel<MainMenuPanel>();
    }

    private void OnSettings()
    {
        UISystem.Instance.OpenPanel<SettingsPanel>(UILayer.PopupLayer);
    }

    private void OnExit()
    {
        Application.Quit();
    }
}
```

### 3. 使用面板

```csharp
// 打开面板
UISystem.Instance.OpenPanel<MainMenuPanel>();

// 关闭面板
UISystem.Instance.ClosePanel<MainMenuPanel>();

// 获取面板
var panel = UISystem.Instance.GetPanel<MainMenuPanel>();
```

---

## 主要 API

### 面板管理

```csharp
// 打开面板
UISystem.Instance.OpenPanel<T>(UILayer layer = UILayer.ContentLayer, bool useCache = true)

// 关闭面板
UISystem.Instance.ClosePanel<T>()
UISystem.Instance.CloseCurrentPanel()

// 获取面板
UISystem.Instance.GetPanel<T>()
UISystem.Instance.GetTopPanel<T>()

// 清理
UISystem.Instance.ClearAllPanels()
UISystem.Instance.CleanupDestroyedPanels()
```

### 事件绑定

```csharp
protected override void Initialize()
{
    // Button事件
    this.BindButton("StartBtn", OnStart);

    // Toggle事件
    this.BindToggle("SoundToggle", OnSoundToggle);

    // Slider事件
    this.BindSlider("VolumeSlider", OnVolumeChange);

    // InputField事件
    this.BindInputField("NameInput", OnNameChanged);
    this.BindInputFieldEndEdit("NameInput", OnNameEndEdit);

    // Dropdown事件
    this.BindDropdown("QualityDropdown", OnQualityChanged);

    // EventTrigger事件
    this.BindPointerEnter("HoverArea", OnPointerEnter);
    this.BindPointerExit("HoverArea", OnPointerExit);
}
```

### 组件获取

```csharp
// 获取UI组件
Button btn = this.GetButton("StartBtn");
Toggle toggle = this.GetToggle("SoundToggle");
Slider slider = this.GetSlider("VolumeSlider");
Text text = this.GetText("TitleText");
Image image = this.GetImage("BackgroundImage");

// TMP组件
TextMeshProUGUI tmpText = this.GetTMPText("TMPTitle");
TMP_InputField tmpInput = this.GetTMPInputField("TMPInput");
```

### 便捷设置

```csharp
// 设置组件属性
this.SetText("ScoreText", "Score: 1000");
this.SetTMPText("TMPText", "Hello World");
this.SetButtonInteractable("StartBtn", false);
this.SetToggleValue("SoundToggle", true);
this.SetSliderValue("VolumeSlider", 0.8f);
this.SetImageSprite("Icon", newSprite);
this.SetImageColor("Background", Color.red);
```

---

## UI 层级

```csharp
public enum UILayer
{
    BackgroundLayer,      // 背景层
    PostProcessingLayer,  // 后期处理层
    ContentLayer,         // 内容层（默认）
    PopupLayer,          // 弹窗层
    GuideLayer,          // 引导层
    DebugLayer           // 调试层
}
```

**使用示例：**

```csharp
// 主界面放在内容层
UISystem.Instance.OpenPanel<MainMenuPanel>(UILayer.ContentLayer);

// 弹窗放在弹窗层
UISystem.Instance.OpenPanel<MessageDialog>(UILayer.PopupLayer);

// 教程放在引导层
UISystem.Instance.OpenPanel<TutorialPanel>(UILayer.GuideLayer);
```

---

## 文件结构

```
Assets/
├── Resources/
│   └── UI/
│       ├── MainMenuPanel.prefab
│       ├── SettingsPanel.prefab
│       └── ...
└── Scripts/
    └── UI/
        ├── MainMenuPanel.cs
        ├── SettingsPanel.cs
        └── ...
```

**注意：** 预制体名称必须与脚本类名一致！

---

## 面板生命周期

```csharp
public class ExamplePanel : UIPanel
{
    protected override void Initialize()
    {
        // 面板初始化，只调用一次
        // 在这里绑定事件和初始化UI
    }

    protected override void OnShow()
    {
        // 面板显示时调用
    }

    protected override void OnHide()
    {
        // 面板隐藏时调用
    }

    protected override void OnLockPanel()
    {
        // 面板锁定时调用
    }

    protected override void OnUnlockPanel()
    {
        // 面板解锁时调用
    }
}
```

---

## 最佳实践

### 1. 事件绑定

```csharp
// ✅ 推荐：使用自动追踪
this.BindButton("StartBtn", OnStart);

// ❌ 避免：手动管理容易忘记清理
this.BindButton("StartBtn", OnStart, autoTrack: false);
```

### 2. 组件缓存

```csharp
public class OptimizedPanel : UIPanel
{
    private Button startButton;
    private Text scoreText;

    protected override void Initialize()
    {
        // 缓存常用组件
        startButton = this.GetButton("StartBtn");
        scoreText = this.GetText("ScoreText");

        startButton?.BindClick(OnStart, this);
    }

    public void UpdateScore(int score)
    {
        // 使用缓存的引用
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }
}
```

### 3. 批量绑定

```csharp
protected override void Initialize()
{
    var buttonEvents = new Dictionary<string, UnityAction>
    {
        ["StartBtn"] = OnStart,
        ["PauseBtn"] = OnPause,
        ["ExitBtn"] = OnExit
    };
    this.BindButtons(buttonEvents);
}
```

---

## 常见问题

### Q: 面板无法打开？

A: 检查预制体是否在 `Resources/UI/`文件夹下，名称是否与脚本类名一致。

### Q: 事件绑定失败？

A: 检查 GameObject 名称拼写，确保组件存在。

### Q: 面板关闭后事件还在触发？

A: 使用自动追踪绑定（默认开启），或手动调用 `UnbindAllEvents()`。

---

## 兼容接口

如果习惯静态调用，可以使用带 `S_`前缀的静态方法：

```csharp
// 新方式（推荐）
UISystem.Instance.OpenPanel<MainMenuPanel>();

// 兼容方式
UISystem.S_OpenPanel<MainMenuPanel>();
```

---

## 总结

FFramework.UISystem 让 UI 开发变得简单：

1. **继承 UIPanel** - 实现 Initialize 方法
2. **绑定事件** - 使用 this.BindXXX 方法
3. **管理面板** - 使用 UISystem.Instance 操作
4. **自动清理** - 系统自动处理事件和内存

就这么简单！🎉
