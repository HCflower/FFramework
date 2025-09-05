# TimerKit 计时器系统文档

## 目录

- [一、简介](#一简介)
- [二、优势](#二优势)
- [三、API 介绍](#三api介绍)
  - [1. FrameTimerManager](#1-frametimermanager)
  - [2. TimeTimerManager](#2-timetimermanager)
  - [3. FrameTimer](#3-frametimer)
  - [4. TimeTimer](#4-timetimer)
- [四、核心功能](#四核心功能)
- [五、快速上手](#五快速上手)
- [六、使用场景示例](#六使用场景示例)
- [七、性能优化](#七性能优化)

---

## 一简介

TimerKit 提供高性能、零 GC 的帧计时器和时间计时器管理器，适用于 Unity 或纯 C# 项目。支持批量创建、统一推进、对象池复用，适合高并发和高性能场景。

---

## 二优势

1. **零 GC 设计**：运行期不分配内存，所有计时器通过对象池复用。
2. **高性能链表管理**：避免 List 扩容和遍历带来的 GC 压力。
3. **灵活回调**：支持开始、每帧/每秒、完成、取消、暂停、恢复等多种回调。
4. **批量管理**：可批量创建多个计时器，统一推进和回收。
5. **与场景无关**：无需挂载到 GameObject，可在任意代码中使用。

---

## 三 api 介绍

### 1-frametimermanager

- `CreateTimer(int totalFrames, ...)`：创建帧计时器
- `TickAll()` / `TickAll(int deltaFrames)`：推进所有帧计时器
- `ClearAndReturnAll()`：清空并回收所有帧计时器
- `Warmup(int count)`：预热对象池
- `CreateTimers(int[] frames, ...)`：批量创建帧计时器

### 2-timetimermanager

- `CreateTimer(float totalTime, ...)`：创建时间计时器
- `TickAll(float deltaTime)`：推进所有时间计时器
- `ClearAndReturnAll()`：清空并回收所有时间计时器
- `Warmup(int count)`：预热对象池
- `CreateTimers(float[] times, ...)`：批量创建时间计时器

### 3-frametimer

- `Configure(...)`：配置参数和回调
- `Start()` / `Pause()` / `Resume()` / `Cancel()`：控制计时器
- `Tick(int deltaFrames)` / `Tick()`：推进计时器
- `IsRunning` / `IsPaused` / `IsCompleted`：状态属性

### 4-timetimer

- `Configure(...)`：配置参数和回调
- `Start()` / `Pause()` / `Resume()` / `Cancel()`：控制计时器
- `Tick(float deltaTime)`：推进计时器
- `IsRunning` / `IsPaused` / `IsCompleted`：状态属性

---

## 四核心功能

1. **帧/时间计时器统一管理**：支持按帧或按时间推进，灵活适配各种场景。
2. **对象池复用**：所有计时器通过对象池管理，避免频繁分配和 GC。
3. **批量创建与推进**：支持批量创建、统一推进和回收。
4. **多回调支持**：可绑定开始、Tick、完成、取消、暂停、恢复等回调。
5. **链表高效管理**：单向链表结构，遍历高效，适合高并发。

---

## 五快速上手

### 1. 创建管理器

```csharp
var frameManager = new FrameTimerManager();
var timeManager = new TimeTimerManager();
```

### 2. 预热对象池

```csharp
frameManager.Warmup(100);
timeManager.Warmup(100);
```

### 3. 创建计时器

```csharp
var frameTimer = frameManager.CreateTimer(60, onComplete: t => Debug.Log("帧计时器完成"));
frameTimer.Start();

var timeTimer = timeManager.CreateTimer(5.0f, onComplete: t => Debug.Log("时间计时器完成"));
timeTimer.Start();
```

### 4. 推进计时器

```csharp
void Update()
{
    frameManager.TickAll();
    timeManager.TickAll(Time.deltaTime);
}
```

### 5. 批量创建计时器

```csharp
frameManager.CreateTimers(new int[] {30, 60, 90}, onComplete: t => Debug.Log("批量帧计时器完成"));
timeManager.CreateTimers(new float[] {1.0f, 2.5f, 5.0f}, onComplete: t => Debug.Log("批量时间计时器完成"));
```

### 6. 清理所有计时器

```csharp
frameManager.ClearAndReturnAll();
timeManager.ClearAndReturnAll();
```

---

## 六使用场景示例

1. **技能冷却/BUFF 计时**：高并发计时器管理。
2. **动画帧驱动**：按帧推进动画或特效。
3. **UI 倒计时**：按时间推进 UI 显示。
4. **网络超时/逻辑延迟**：高性能逻辑计时。
5. **批量任务调度**：批量创建和管理多个计时器。

---

## 七性能优化

1. **对象池预热**：初始化时预热计时器池，减少运行时 GC。
2. **链表管理**：避免 List 扩容，提升遍历效率。
3. **批量推进**：统一推进所有计时器，减少分散调用。
4. **回调静态化**：回调建议用静态方法或预绑定，避免闭包和 GC。
5. **及时回收**：计时器完成或取消后及时归还对象池。
