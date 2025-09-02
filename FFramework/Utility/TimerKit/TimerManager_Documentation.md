# FFramework.Kit 计时器系统使用文档

## 目录

- [简介](#简介)
- [主要特性](#主要特性)
- [快速上手](#快速上手)
  - [1. 创建计时器管理器实例](#1-创建计时器管理器实例)
  - [2. 预热对象池（可选，减少运行时-gc）](#2-预热对象池可选减少运行时-gc)
  - [3. 创建计时器](#3-创建计时器)
  - [4. 推进计时器（通常在-update-中）](#4-推进计时器通常在-update-中)
  - [5. 批量创建计时器](#5-批量创建计时器)
  - [6. 清理所有计时器](#6-清理所有计时器)
- [完整示例代码](#完整示例代码)
- [FrameTimer API 说明](#frametimer-api-说明)
- [TimeTimer API 说明](#timetimer-api-说明)
- [FrameTimerManager API 说明](#frametimermanager-api-说明)
- [TimeTimerManager API 说明](#timetimermanager-api-说明)
- [典型用法场景](#典型用法场景)
- [注意事项](#注意事项)

---

## 简介

`FrameTimerManager` 和 `TimeTimerManager` 是高性能、零 GC 的计时器管理器，适用于 Unity 或纯 C# 项目。`FrameTimerManager` 通过帧数推进计时器，而 `TimeTimerManager` 则通过时间推进计时器。两者均支持批量创建、统一推进、对象池复用，适合高并发和高性能场景。

`FrameTimer` 和 `TimeTimer` 分别是帧计时器和时间计时器对象，支持暂停、恢复、取消、完成等操作，并可绑定多种回调。

---

## 主要特性

- **零 GC 设计**：运行期不分配内存，所有计时器通过对象池复用。
- **高性能**：链表管理，避免 List 扩容和遍历带来的 GC 压力。
- **灵活回调**：支持开始、每帧/每秒、完成、取消、暂停、恢复等多种回调。
- **批量管理**：可批量创建多个计时器，统一推进和回收。
- **与场景无关**：无需挂载到 GameObject，可在任意代码中使用。

---

## 快速上手

### 1. 创建计时器管理器实例

```csharp
var frameManager = new FrameTimerManager();
var timeManager = new TimeTimerManager();
```

### 2. 预热对象池（可选，减少运行时 GC）

```csharp
frameManager.Warmup(100); // 预热100个帧计时器
timeManager.Warmup(100); // 预热100个时间计时器
```

### 3. 创建计时器

#### 创建帧计时器

```csharp
var frameTimer = frameManager.CreateTimer(
    totalFrames: 60,
    onStart: t => Debug.Log("帧计时器开始"),
    onTick: (t, remain, total) => Debug.Log($"剩余帧:{remain}"),
    onComplete: t => Debug.Log("帧计时器完成"),
    tickInterval: 1 // 每帧回调
);
frameTimer.Start();
```

#### 创建时间计时器

```csharp
var timeTimer = timeManager.CreateTimer(
    totalTime: 5.0f,
    onStart: t => Debug.Log("时间计时器开始"),
    onTick: (t, remain, total) => Debug.Log($"剩余时间:{remain:F2}s"),
    onComplete: t => Debug.Log("时间计时器完成"),
    tickInterval: 1.0f // 每秒回调
);
timeTimer.Start();
```

### 4. 推进计时器

#### 推进帧计时器（通常在 Update 中）

```csharp
void Update()
{
    frameManager.TickAll(); // 每帧推进所有帧计时器
}
```

#### 推进时间计时器

```csharp
void Update()
{
    timeManager.TickAll(Time.deltaTime); // 使用 Time.deltaTime 推进所有时间计时器
}
```

### 5. 批量创建计时器

#### 批量创建帧计时器

```csharp
int[] frames = { 30, 60, 90 };
frameManager.CreateTimers(frames, onComplete: t => Debug.Log("批量帧计时器完成"));
```

#### 批量创建时间计时器

```csharp
float[] times = { 1.0f, 2.5f, 5.0f };
timeManager.CreateTimers(times, onComplete: t => Debug.Log("批量时间计时器完成"));
```

### 6. 清理所有计时器

```csharp
frameManager.ClearAndReturnAll();
timeManager.ClearAndReturnAll();
```

---

## 完整示例代码

### 帧计时器示例

```csharp
using UnityEngine;
using FFramework.Kit;

/// <summary>
/// 帧计时器Mono示例
/// </summary>
public class FrameTimerExample : MonoBehaviour
{
	public int timerCount = 1000;
	public int minFrames = 30;
	public int maxFrames = 600;
	public int fontSize = 28;
	public int margin = 12;

	private FrameTimerManager frameManager;
	private static int s_completed;
	private static readonly System.Action<FrameTimer> s_onComplete = OnTimerCompleteStatic;
	private static GUIStyle s_labelStyle;

	private void Awake()
	{
		frameManager = new FrameTimerManager();
		// 预热，减少运行期分配带来的碎片（容量可调）
		frameManager.Warmup(timerCount);
		s_completed = 0;
	}

	private void Start()
	{
		if (maxFrames < minFrames) maxFrames = minFrames;
		int range = maxFrames - minFrames + 1;
		for (int i = 0; i < timerCount; i++)
		{
			int frames = minFrames + (i % range);
			var timer = frameManager.CreateTimer(frames, onComplete: s_onComplete, tickInterval: 10);
			timer.Start();
		}
		Debug.Log($"[FrameTimerExample] Created {timerCount} timers. Frames [{minFrames},{maxFrames}] Range={range}");
	}

	private void Update()
	{
		frameManager.TickAll();
	}

	private static void OnTimerCompleteStatic(FrameTimer t)
	{
		s_completed++;
		FrameTimer.Return(t);
	}

	private void OnGUI()
	{
		if (s_labelStyle == null) s_labelStyle = new GUIStyle(GUI.skin.label);
		s_labelStyle.fontSize = fontSize;
		s_labelStyle.normal.textColor = Color.white;
		float width = Mathf.Max(600, fontSize * 18);
		float height = fontSize + 10;
		var rect = new Rect(margin, margin, width, height);
		GUI.Box(new Rect(rect.x - 6, rect.y - 6, rect.width + 12, rect.height + 12), GUIContent.none);
		GUI.Label(rect, $"Running timers completed: {s_completed}/{timerCount}", s_labelStyle);
	}
}
```

### 时间计时器示例

```csharp
using UnityEngine;
using FFramework.Kit;

/// <summary>
/// 时间计时器Mono示例
/// </summary>
public class TimeTimerExample : MonoBehaviour
{
    public int timerCount = 10;
    public float minTime = 1.0f;
    public float maxTime = 10.0f;
    public int fontSize = 28;
    public int margin = 12;

    private TimeTimerManager timeManager;
    private static int s_completed;
    private static readonly System.Action<TimeTimer> s_onComplete = OnTimerCompleteStatic;
    private static GUIStyle s_labelStyle;

    private void Awake()
    {
        timeManager = new TimeTimerManager();
        // 预热，减少运行期分配带来的碎片
        timeManager.Warmup(timerCount);
        s_completed = 0;
    }

    private void Start()
    {
        if (maxTime < minTime) maxTime = minTime;
        float range = maxTime - minTime;
        for (int i = 0; i < timerCount; i++)
        {
            float time = minTime + (range * (i / (float)timerCount));
            var timer = timeManager.CreateTimer(time,
                onComplete: s_onComplete,
                onTick: (t, remain, total) => Debug.Log($"Timer {i}: {remain:F2}s / {total:F2}s"),
                tickInterval: 1.0f); // 每秒回调一次
            timer.Start();
        }
        Debug.Log($"[TimeTimerExample] Created {timerCount} timers. Times [{minTime},{maxTime}]");
    }

    private void Update()
    {
        // 使用Time.deltaTime推进所有计时器
        timeManager.TickAll(Time.deltaTime);
    }

    private static void OnTimerCompleteStatic(TimeTimer t)
    {
        s_completed++;
        TimeTimer.Return(t);
    }

    private void OnGUI()
    {
        if (s_labelStyle == null) s_labelStyle = new GUIStyle(GUI.skin.label);
        s_labelStyle.fontSize = fontSize;
        s_labelStyle.normal.textColor = Color.white;
        float width = Mathf.Max(600, fontSize * 18);
        float height = fontSize + 10;
        var rect = new Rect(margin, margin, width, height);
        GUI.Box(new Rect(rect.x - 6, rect.y - 6, rect.width + 12, rect.height + 12), GUIContent.none);
        GUI.Label(rect, $"Running timers completed: {s_completed}/{timerCount}", s_labelStyle);
    }
}
```

---

## FrameTimer API 说明

- `Configure(...)`：配置计时器参数和回调
- `Start()`：启动计时器
- `Pause()` / `Resume()`：暂停/恢复计时器
- `Cancel()`：取消计时器
- `Tick(int deltaFrames)`：推进计时器若干帧
- `IsRunning` / `IsPaused` / `IsCompleted`：状态属性

---

## TimeTimer API 说明

- `Configure(...)`：配置计时器参数和回调
- `Start()`：启动计时器
- `Pause()` / `Resume()`：暂停/恢复计时器
- `Cancel()`：取消计时器
- `Tick(float deltaTime)`：推进计时器若干时间
- `IsRunning` / `IsPaused` / `IsCompleted`：状态属性

---

## TimeTimerManager API 说明

`TimeTimerManager` 是一个时间计时器的集中管理器，提供以下功能：

- **创建计时器**：通过 `CreateTimer` 方法创建单个计时器。
- **批量创建计时器**：通过 `CreateTimers` 方法批量创建多个计时器。
- **推进计时器**：通过 `TickAll` 方法推进所有计时器。
- **清理计时器**：通过 `ClearAndReturnAll` 方法清空并回收所有计时器。
- **对象池预热**：通过 `Warmup` 方法预热计时器对象池。

### 示例代码

```csharp
var timeManager = new TimeTimerManager();
timeManager.Warmup(50); // 预热50个计时器

var timer = timeManager.CreateTimer(
    totalTime: 10.0f,
    onStart: t => Debug.Log("计时器开始"),
    onTick: (t, remain, total) => Debug.Log($"剩余时间: {remain:F2}s"),
    onComplete: t => Debug.Log("计时器完成")
);

void Update()
{
    timeManager.TickAll(Time.deltaTime);
}
```

---

## FrameTimerManager API 说明

`FrameTimerManager` 是一个帧计时器的集中管理器，提供以下功能：

- **创建计时器**：通过 `CreateTimer` 方法创建单个计时器。
- **批量创建计时器**：通过 `CreateTimers` 方法批量创建多个计时器。
- **推进计时器**：通过 `TickAll` 方法推进所有计时器。
- **清理计时器**：通过 `ClearAndReturnAll` 方法清空并回收所有计时器。
- **对象池预热**：通过 `Warmup` 方法预热计时器对象池。

### 示例代码

```csharp
var frameManager = new FrameTimerManager();
frameManager.Warmup(100); // 预热100个计时器

var timer = frameManager.CreateTimer(
    totalFrames: 60,
    onStart: t => Debug.Log("计时器开始"),
    onTick: (t, remain, total) => Debug.Log($"剩余帧: {remain}"),
    onComplete: t => Debug.Log("计时器完成")
);

void Update()
{
    frameManager.TickAll();
}
```

---

## FrameTimerManager 与 TimeTimerManager 对比

| 特性       | FrameTimerManager | TimeTimerManager   |
| ---------- | ----------------- | ------------------ |
| 推进方式   | 按帧推进          | 按时间推进         |
| 适用场景   | 帧同步、动画驱动  | 时间同步、逻辑计时 |
| 回调间隔   | 帧数              | 时间（秒）         |
| 对象池支持 | 是                | 是                 |
| 高并发支持 | 是                | 是                 |

---

## 典型用法场景

- 技能冷却、Buff 计时、动画帧驱动
- UI 倒计时、网络超时、逻辑延迟
- 需要高并发、高性能的计时管理

---

## 注意事项

- 需要手动调用 `FrameTimerManager.TickAll()` 或 `TimeTimerManager.TickAll(deltaTime)` 推进计时器（建议放在 MonoBehaviour 的 Update 中）
- 计时器完成或取消后建议归还对象池（如 `FrameTimer.Return(timer)` 或 `TimeTimer.Return(timer)`）
- 回调建议用静态方法或预绑定，避免闭包和 GC

---

如需更详细的扩展用法，请参考源码注释或联系作者。
