# FFramework.Kit TimerManager 使用文档

## 目录

- [简介](#简介)
- [主要特性](#主要特性)
- [快速上手](#快速上手)
  - [1. 创建 TimerManager 实例](#1-创建-timermanager-实例)
  - [2. 预热对象池（可选，减少运行时-gc）](#2-预热对象池可选减少运行时-gc)
  - [3. 创建计时器](#3-创建计时器)
  - [4. 推进计时器（通常在-update-中）](#4-推进计时器通常在-update-中)
  - [5. 批量创建计时器](#5-批量创建计时器)
  - [6. 清理所有计时器](#6-清理所有计时器)
- [完整示例代码](#完整示例代码)
- [FrameTimer API 说明](#frametimer-api-说明)
- [TimerManager API 说明](#timermanager-api-说明)
- [典型用法场景](#典型用法场景)
- [注意事项](#注意事项)

---

## 简介

`TimerManager` 是一个高性能、零 GC 的计时器集中管理器，适用于 Unity 或纯 C# 项目。它通过单向链表管理所有计时器，支持批量创建、统一推进、对象池复用，适合高并发和高性能场景。

`FrameTimer` 是单个计时器对象，支持按帧推进、暂停、恢复、取消、完成等操作，并可绑定多种回调。

---

## 主要特性

- **零 GC 设计**：运行期不分配内存，所有计时器通过对象池复用。
- **高性能**：链表管理，避免 List 扩容和遍历带来的 GC 压力。
- **灵活回调**：支持开始、每帧、完成、取消、暂停、恢复等多种回调。
- **批量管理**：可批量创建多个计时器，统一推进和回收。
- **与场景无关**：无需挂载到 GameObject，可在任意代码中使用。

---

## 快速上手

### 1. 创建 TimerManager 实例

```csharp
var timerManager = new TimerManager();
```

### 2. 预热对象池（可选，减少运行时 GC）

```csharp
timerManager.Warmup(100); // 预热100个计时器
```

### 3. 创建计时器

```csharp
var timer = timerManager.CreateTimer(
    totalFrames: 60,
    onStart: t => Debug.Log("计时器开始"),
    onTick: (t, remain, total) => Debug.Log($"剩余帧:{remain}"),
    onComplete: t => Debug.Log("计时器完成"),
    tickInterval: 1 // 每帧回调
);
timer.Start();
```

### 4. 推进计时器（通常在 Update 中）

```csharp
void Update()
{
    timerManager.TickAll(); // 每帧推进所有计时器
}
```

### 5. 批量创建计时器

```csharp
int[] frames = { 30, 60, 90 };
timerManager.CreateTimers(frames, onComplete: t => Debug.Log("批量计时器完成"));
```

### 6. 清理所有计时器

```csharp
timerManager.ClearAndReturnAll();
```

---

## 完整示例代码

下面是一个 Unity MonoBehaviour 示例，演示如何批量创建计时器并统计完成数量：

```csharp
using Game.GameCore.Countdown;
using UnityEngine;
using FFramework.Kit;

/// <summary>
/// 计时器Mono示例
/// </summary>
public class TimerMonoExample : MonoBehaviour
{
	public int timerCount = 1000;
	public int minFrames = 30;
	public int maxFrames = 600;
	public int fontSize = 28;
	public int margin = 12;

	private TimerManager timermanager;
	private static int s_completed;
	private static readonly System.Action<FrameTimer> s_onComplete = OnTimerCompleteStatic;
	private static GUIStyle s_labelStyle;

	private void Awake()
	{
		timermanager = new TimerManager();
		// 预热，减少运行期分配带来的碎片（容量可调）
		timermanager.Warmup(timerCount);
		s_completed = 0;
	}

	private void Start()
	{
		if (maxFrames < minFrames) maxFrames = minFrames;
		int range = maxFrames - minFrames + 1;
		for (int i = 0; i < timerCount; i++)
		{
			int frames = minFrames + (i % range);
			var timer = timermanager.CreateTimer(frames, onComplete: s_onComplete, tickInterval: 10);
			timer.Start();
		}
		Debug.Log($"[TimerMonoExample] Created {timerCount} timers. Frames [{minFrames},{maxFrames}] Range={range}");
	}

	private void Update()
	{
		timermanager.TickAll();
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

---

## FrameTimer API 说明

- `Configure(...)`：配置计时器参数和回调
- `Start()`：启动计时器
- `Pause()` / `Resume()`：暂停/恢复计时器
- `Cancel()`：取消计时器
- `Tick(int deltaFrames)`：推进计时器若干帧
- `IsRunning` / `IsPaused` / `IsCompleted`：状态属性

---

## TimerManager API 说明

- `CreateTimer(...)`：创建单个计时器
- `CreateTimers(...)`：批量创建计时器
- `TickAll()` / `TickAll(int deltaFrames)`：统一推进所有计时器
- `Add(FrameTimer)` / `Remove(FrameTimer)`：手动管理计时器
- `ClearAndReturnAll()`：清空并回收所有计时器
- `Warmup(int count)`：预热对象池

---

## 典型用法场景

- 技能冷却、Buff 计时、动画帧驱动
- UI 倒计时、网络超时、逻辑延迟
- 需要高并发、高性能的计时管理

---

## 注意事项

- 需要手动调用 `TickAll()` 推进计时器（建议放在 MonoBehaviour 的 Update 中）
- 计时器完成或取消后建议归还对象池（如 `FrameTimer.Return(timer)`）
- 回调建议用静态方法或预绑定，避免闭包和 GC

---

如需更详细的扩展用法，请参考源码注释或联系作者。
