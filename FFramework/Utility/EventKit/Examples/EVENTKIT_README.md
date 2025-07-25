# EventKit 事件工具使用说明

## 概述

EventKit 是一个强大的 Unity UI 事件系统封装工具，提供了所有 Unity EventSystem 接口的便捷绑定方式，让事件处理变得更加简单和优雅。

## 核心特性

### 🎯 完整的事件支持

- **指针事件**: 进入、退出、按下、抬起、点击
- **拖拽事件**: 初始化、开始、拖拽中、结束、放置
- **输入事件**: 滚轮、选择、移动、提交、取消
- **高级拖拽**: DragKit 提供可视化效果和约束功能

### 🔧 便捷的 API 设计

- **链式调用**: 支持方法链式调用
- **扩展方法**: 为 GameObject 和 Component 提供扩展方法
- **静态便捷方法**: 一行代码绑定常用事件
- **事件数据封装**: 提供便捷的事件数据处理方法

## 快速开始

### 1. 基础事件绑定

```csharp
using FFramework.Kit;

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
// 使用EventKit进行复杂事件绑定
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

// 高级拖拽（使用DragKit）
DragKit.Get(gameObject)
    .SetDragConfig(enableDrag: true, returnToOriginal: true, returnSpeed: 3f)
    .SetVisualEffects(scaleOnDrag: true, fadeOnDrag: true)
    .SetConstraints(constrainToParent: true);
```

## 详细 API 文档

### EventKit 核心类

#### 静态方法

```csharp
// 获取或添加EventKit组件
EventKit eventKit = EventKit.Get(gameObject);
EventKit eventKit = EventKit.Get(component);
```

#### 事件设置方法（替换现有事件）

```csharp
EventKit SetOnPointerClick(Action<PointerEventData> callback)
EventKit SetOnPointerEnter(Action<PointerEventData> callback)
EventKit SetOnPointerExit(Action<PointerEventData> callback)
EventKit SetOnPointerDown(Action<PointerEventData> callback)
EventKit SetOnPointerUp(Action<PointerEventData> callback)
EventKit SetOnDrag(Action<PointerEventData> callback)
EventKit SetOnBeginDrag(Action<PointerEventData> callback)
EventKit SetOnEndDrag(Action<PointerEventData> callback)
EventKit SetOnScroll(Action<PointerEventData> callback)
// ... 更多事件方法
```

#### 事件添加方法（支持多个回调）

```csharp
EventKit AddOnPointerClick(Action<PointerEventData> callback)
EventKit AddOnPointerEnter(Action<PointerEventData> callback)
EventKit AddOnDrag(Action<PointerEventData> callback)
// ... 更多添加方法
```

#### 事件移除方法

```csharp
EventKit RemoveOnPointerClick(Action<PointerEventData> callback)
EventKit ClearAllEvents() // 清除所有事件
```

### EventKitExtensions 扩展方法

#### GameObject 扩展

```csharp
// 点击事件
gameObject.BindClick(Action<PointerEventData> callback)
gameObject.BindClick(Action callback) // 无参数版本

// 悬停事件
gameObject.BindHover(Action onEnter, Action onExit = null)
gameObject.BindHover(Action<PointerEventData> onEnter, Action<PointerEventData> onExit = null)

// 拖拽事件
gameObject.BindDrag(Action<PointerEventData> onBeginDrag, Action<PointerEventData> onDrag, Action<PointerEventData> onEndDrag)
gameObject.BindDrag(Action<Vector2> onDrag) // 简化版本
```

#### UI 组件特殊扩展

```csharp
// Button增强点击
button.BindEnhancedClick(Action<PointerEventData> callback)

// Image/Text启用射线检测并绑定点击
image.BindClickWithRaycast(Action<PointerEventData> callback)
text.BindClickWithRaycast(Action<PointerEventData> callback)

// ScrollRect滚动事件
scrollRect.BindScroll(Action<PointerEventData> callback)
```

#### 事件数据便捷方法

```csharp
// 检查鼠标按键
bool isLeft = eventData.IsLeftClick();
bool isRight = eventData.IsRightClick();
bool isMiddle = eventData.IsMiddleClick();

// 坐标转换
Vector3 worldPos = eventData.GetWorldPosition(camera);
Vector2 uiPos = eventData.GetUIPosition(rectTransform);
```

### DragKit 高级拖拽

#### 配置方法

```csharp
DragKit SetDragConfig(bool enableDrag, bool returnToOriginal, float returnSpeed)
DragKit SetVisualEffects(bool scaleOnDrag, Vector3 dragScale, bool fadeOnDrag, float dragAlpha)
DragKit SetConstraints(bool constrainToParent, bool constrainToScreen, Vector2 dragBounds)
DragKit SetCallbacks(Action<PointerEventData> onBeginDrag, Action<PointerEventData> onDrag, Action<PointerEventData> onEndDrag)
```

#### 公共方法

```csharp
void ResetToOriginalPosition(bool immediate = false)
void UpdateOriginalPosition()
```

#### 属性

```csharp
bool IsDragging { get; } // 是否正在拖拽
bool IsReturning { get; } // 是否正在返回原位
bool EnableDrag { get; set; } // 启用/禁用拖拽
```

## 使用场景示例

### 1. UI 按钮增强

```csharp
// 按钮点击音效
button.BindClick(() => AudioManager.PlayClickSound());

// 按钮悬停效果
button.BindHover(
    () => button.transform.DOScale(1.1f, 0.2f),
    () => button.transform.DOScale(1f, 0.2f)
);
```

### 2. 拖拽排序列表

```csharp
foreach (var item in listItems)
{
    DragKit.Get(item)
        .SetDragConfig(enableDrag: true, returnToOriginal: false)
        .SetVisualEffects(scaleOnDrag: true, fadeOnDrag: true)
        .SetCallbacks(
            onBeginDrag: data => OnItemDragStart(item),
            onEndDrag: data => OnItemDragEnd(item, data)
        );
}
```

### 3. 图片查看器

```csharp
image.BindClickWithRaycast(eventData =>
{
    if (eventData.clickCount == 2) // 双击
    {
        OpenFullScreenView();
    }
});

image.BindDrag(delta =>
{
    // 拖拽移动图片
    image.rectTransform.anchoredPosition += delta;
});
```

### 4. 右键菜单

```csharp
gameObject.BindClick(eventData =>
{
    if (eventData.IsRightClick())
    {
        ShowContextMenu(eventData.position);
    }
});
```

## 最佳实践

### 1. 性能优化

- 在对象销毁前调用`ClearAllEvents()`清理事件
- 对于临时 UI，使用`RemoveOnXXX`方法移除特定事件
- 避免在 Update 中频繁绑定/解绑事件

### 2. 代码组织

- 将相关事件绑定放在同一个方法中
- 使用链式调用提高代码可读性
- 为复杂交互创建专门的事件处理类

### 3. 调试技巧

- 在事件回调中添加 Debug.Log 确认事件触发
- 使用事件数据的详细信息进行问题排查
- 检查 UI 元素的 raycastTarget 设置

## 注意事项

1. **射线检测**: Image 和 Text 组件需要启用`raycastTarget`才能接收事件
2. **事件冲突**: 多个组件监听同一事件时要注意执行顺序
3. **内存泄漏**: 记得在适当时候清理事件监听
4. **坐标系统**: 注意世界坐标和 UI 坐标的转换
5. **Canvas 设置**: 确保 Canvas 有 GraphicRaycaster 组件
