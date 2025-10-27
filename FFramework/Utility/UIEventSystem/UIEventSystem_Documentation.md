# EventKit 事件工具文档

## 目录

- [一、简介](#一简介)
- [二、优势](#二优势)
- [三、API 介绍](#三api介绍)
  - [1. EventKit 核心类](#1-eventkit-核心类)
  - [2. EventKitExtensions 扩展方法](#2-eventkitextensions-扩展方法)
  - [3. DragKit 高级拖拽](#3-dragkit-高级拖拽)
- [四、核心功能](#四核心功能)
- [五、快速上手](#五快速上手)
  - [1. 基础事件绑定](#1-基础事件绑定)
  - [2. 高级事件绑定](#2-高级事件绑定)
  - [3. 拖拽功能](#3-拖拽功能)
- [六、使用场景示例](#六使用场景示例)
- [七、性能优化](#七性能优化)

---

## 一、简介

`EventKit` 是一个强大的 Unity UI 事件系统封装工具，提供了所有 Unity EventSystem 接口的便捷绑定方式。通过链式调用和扩展方法，开发者可以更高效地处理 UI 事件，提升代码的可读性和可维护性。

---

## 二、优势

1. **全面的事件支持**：涵盖指针事件、拖拽事件、输入事件等。
2. **便捷的 API**：支持链式调用和扩展方法，减少代码量。
3. **高级功能**：内置 `DragKit`，支持拖拽的视觉效果和约束。
4. **易用性**：提供静态便捷方法，一行代码即可绑定事件。
5. **高性能**：优化事件处理逻辑，减少性能开销。

---

## 三、API 介绍

### 1. EventKit 核心类

- `SetOnPointerEnter(Action<PointerEventData> callback)`
  - 设置指针进入事件。
- `SetOnPointerExit(Action<PointerEventData> callback)`
  - 设置指针退出事件。
- `SetOnPointerClick(Action<PointerEventData> callback)`
  - 设置指针点击事件。
- `SetOnDrag(Action<PointerEventData> callback)`
  - 设置拖拽中事件。
- `SetOnBeginDrag(Action<PointerEventData> callback)`
  - 设置开始拖拽事件。
- `SetOnEndDrag(Action<PointerEventData> callback)`
  - 设置结束拖拽事件。

### 2. EventKitExtensions 扩展方法

- `BindClick(Action callback)`
  - 为 GameObject 或 Component 绑定点击事件。
- `BindHover(Action onEnter, Action onExit)`
  - 绑定悬停事件。
- `BindDrag(Action<PointerEventData> onBeginDrag, Action<PointerEventData> onDrag, Action<PointerEventData> onEndDrag)`
  - 绑定拖拽事件。

### 3. DragKit 高级拖拽

- `SetDragConfig(bool enableDrag, bool returnToOriginal, float returnSpeed)`
  - 配置拖拽行为。
- `SetVisualEffects(bool scaleOnDrag, bool fadeOnDrag)`
  - 设置拖拽的视觉效果。
- `SetConstraints(bool constrainToParent, bool constrainToScreen)`
  - 设置拖拽的约束条件。

---

## 四、核心功能

1. **指针事件**：进入、退出、按下、抬起、点击。
2. **拖拽事件**：初始化、开始、拖拽中、结束、放置。
3. **输入事件**：滚轮、选择、移动、提交、取消。
4. **高级拖拽**：支持视觉效果（缩放、透明度）和位置约束。
5. **事件扩展**：便捷绑定常见事件，减少重复代码。

---

## 五、快速上手

### 1. 基础事件绑定

```csharp
// 最简单的点击事件
button.BindClick(() => Debug.Log("按钮被点击了！"));

// 带事件数据的点击事件
image.BindClickWithRaycast(eventData =>
{
    if (eventData.IsLeftClick())
        Debug.Log("左键点击");
});

// 悬停事件
gameObject.BindHover(
    () => Debug.Log("鼠标进入"),
    () => Debug.Log("鼠标离开")
);
```

### 2. 高级事件绑定

```csharp
// 使用 EventKit 进行复杂事件绑定
EventKit.Get(gameObject)
    .SetOnPointerClick(eventData => Debug.Log("点击"))
    .SetOnPointerEnter(eventData => Debug.Log("进入"))
    .SetOnPointerExit(eventData => Debug.Log("离开"))
    .SetOnDrag(eventData => Debug.Log("拖拽中"));
```

### 3. 拖拽功能

```csharp
// 基础拖拽
gameObject.BindDrag(
    onBeginDrag: eventData => Debug.Log("开始拖拽"),
    onDrag: eventData => Debug.Log("拖拽中"),
    onEndDrag: eventData => Debug.Log("结束拖拽")
);

// 高级拖拽（使用 DragKit）
DragKit.Get(gameObject)
    .SetDragConfig(enableDrag: true, returnToOriginal: true, returnSpeed: 3f)
    .SetVisualEffects(scaleOnDrag: true, fadeOnDrag: true)
    .SetConstraints(constrainToParent: true);
```

---

## 六、使用场景示例

1. **UI 按钮增强**：

   - 使用 `BindClick` 为按钮添加点击事件。

2. **拖拽排序列表**：

   - 使用 `DragKit` 实现拖拽排序功能。

3. **图片查看器**：

   - 使用拖拽事件实现图片的平移和缩放。

4. **右键菜单**：
   - 使用 `BindClick` 和 `BindHover` 实现右键菜单功能。

---

## 七、性能优化

1. **事件解绑**：

   - 在对象销毁时解绑事件，避免内存泄漏。

2. **减少事件监听器**：

   - 合理规划事件绑定，避免重复监听。

3. **优化拖拽逻辑**：

   - 使用约束条件减少无效计算。

4. **调试工具**：
   - 使用日志和断点调试事件逻辑，快速定位问题。
