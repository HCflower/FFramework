# LoadSceneKit 场景加载工具文档

## 目录

- [一、简介](#一简介)
- [二、优势](#二优势)
- [三、API 介绍](#三api介绍)
  - [1. 核心属性](#1-核心属性)
  - [2. 主要方法](#2-主要方法)
  - [3. 状态查询方法](#3-状态查询方法)
- [四、核心功能](#四核心功能)
- [五、快速上手](#五快速上手)
- [六、使用场景示例](#六使用场景示例)
- [七、性能优化](#七性能优化)

---

## 一简介

`LoadSceneKit` 是一个优化的 Unity 场景加载工具类，专为高效场景切换而设计。支持同步/异步场景切换，自动卸载旧场景，进度监控和事件回调，适用于各种游戏和工具项目。

---

## 二优势

1. **智能场景切换**：自动卸载旧场景，激活新场景，防止冲突。
2. **进度监控**：分阶段进度与总进度实时查询。
3. **事件驱动**：支持切换开始/完成回调，便于 UI 管理。
4. **高性能设计**：异步优先，内存优化，异常处理完善。
5. **UniTask 集成**：推荐 async/await 语法，性能更佳。

---

## 三 api 介绍

### 1-核心属性

- `LoadingProgress`：加载进度(0-1)
- `UnloadingProgress`：卸载进度(0-1)
- `TotalProgress`：总进度(0-1)
- `IsProcessing`：是否正在处理场景切换

### 2-主要方法

- `LoadScene(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)`
  - 同步场景切换，自动卸载旧场景。
- `LoadSceneAsync(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)`
  - 异步场景切换（协程/回调）。
- `LoadSceneAsyncTask(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)`
  - 异步场景切换（UniTask/async await）。

### 3-状态查询方法

- `GetCurrentSceneName()`：获取当前场景名称
- `GetLoadProgress()`：获取加载阶段进度(0-0.5)
- `GetUnloadProgress()`：获取卸载阶段进度(0-0.5)
- `GetTotalProgress()`：获取总进度(0-1)
- `GetProgressDetails()`：获取进度详情（load, unload, total）

---

## 四核心功能

1. **同步/异步场景切换**：支持多种切换方式，灵活适配项目需求。
2. **自动卸载旧场景**：无需手动管理，减少内存占用。
3. **进度监控**：实时获取加载/卸载/总进度，便于 UI 展示。
4. **事件回调**：切换开始/完成均可自定义回调。
5. **异常处理**：完善的错误处理机制，提升稳定性。

---

## 五快速上手

### 1. 同步场景切换

```csharp
LoadSceneKit.LoadScene("GameScene",
    onChangeScene: () => ShowLoadingUI(),
    onComplete: (success) => HideLoadingUI());
```

### 2. 异步场景切换（协程/回调）

```csharp
LoadSceneKit.LoadSceneAsync("GameScene",
    onChangeScene: () => ShowLoadingUI(),
    onComplete: (success) => HideLoadingUI());
```

### 3. 异步场景切换（UniTask/async await）

```csharp
bool success = await LoadSceneKit.LoadSceneAsyncTask("GameScene",
    onChangeScene: () => ShowLoadingUI(),
    onComplete: (success) => HideLoadingUI());
```

### 4. 进度监控

```csharp
float progress = LoadSceneKit.TotalProgress;
(bool load, bool unload, bool total) = LoadSceneKit.GetProgressDetails();
```

---

## 六使用场景示例

1. **加载界面管理**：切换场景时自动显示/隐藏加载界面。
2. **关卡切换**：游戏关卡场景的高效切换。
3. **多场景协作**：编辑器工具或多场景项目的场景管理。
4. **进度条展示**：实时进度反馈，提升用户体验。

---

## 七性能优化

1. **异步优先**：推荐使用 UniTask 异步切换，减少卡顿。
2. **自动资源释放**：场景切换自动卸载旧场景，降低内存占用。
3. **进度分阶段**：合理展示加载/卸载进度，优化 UI 体验。
4. **事件回调管理**：避免重复绑定，及时解绑无用回调。
5. **异常处理**：统一 try-catch，保证切换流程稳定。
