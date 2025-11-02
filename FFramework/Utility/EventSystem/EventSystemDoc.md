# 事件系统使用说明

## 基本功能

本事件系统支持**无参数**和**泛型参数**事件的注册、注销和触发，适用于解耦各类模块间的消息通信。

---

## 快速上手

### 1. 注册事件

```csharp
// 无参数事件
EventSystem.Instance.RegisterEvent("GameStart", OnGameStart);

// 泛型事件
EventSystem.Instance.RegisterEvent<int>("ScoreChanged", OnScoreChanged);
EventSystem.Instance.RegisterEvent<string>("PlayerNameChanged", OnPlayerNameChanged);
```

### 2. 触发事件

```csharp
EventSystem.Instance.TriggerEvent("GameStart");
EventSystem.Instance.TriggerEvent<int>("ScoreChanged", 100);
```

### 3. 注销事件

```csharp
EventSystem.Instance.UnregisterEvent("GameStart", OnGameStart);
EventSystem.Instance.UnregisterEvent<int>("ScoreChanged", OnScoreChanged);
```

---

## 自动注销（推荐）

通过扩展方法，可让事件在 GameObject 销毁时自动注销，无需手动管理：

```csharp
// 注册并自动注销
EventSystem.Instance.RegisterEvent("GameStart", OnGameStart, gameObject);
EventSystem.Instance.RegisterEvent<int>("ScoreChanged", OnScoreChanged, gameObject);
```

---

## 进阶用法

- 支持链式注册、一次性事件、调试信息打印等。
- 可通过 `EventSystem.Instance.GetAllEventNames()` 查询所有事件名。
- 支持事件触发位置追踪，便于调试。

---

## 注意事项

- 建议所有事件注册都配合自动注销，避免内存泄漏。
- 支持多参数类型事件，但事件名需唯一。

---

## 示例

```csharp
void Start()
{
    EventSystem.Instance.RegisterEvent("GameStart", OnGameStart, gameObject);
    EventSystem.Instance.RegisterEvent<int>("ScoreChanged", OnScoreChanged, gameObject);

    EventSystem.Instance.TriggerEvent("GameStart");
    EventSystem.Instance.TriggerEvent<int>("ScoreChanged", 100);
}

void OnGameStart() { Debug.Log("游戏开始！"); }
void OnScoreChanged(int score) { Debug.Log($"分数变化: {score}"); }
```

---

## 相关 API

- `RegisterEvent` / `UnregisterEvent` / `TriggerEvent`
- 自动注销扩展：`RegisterEvent(..., gameObject)`
- 查询事件监听者：`GetEventListeners(eventName)`
- 查询所有事件名：`GetAllEventNames()`
- 打印事件触发位置：`DebugPrintTriggerLocation(eventName)`

---

如需更多高级用法，请参考源码注释。
