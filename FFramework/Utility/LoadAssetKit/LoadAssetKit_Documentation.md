# LoadAssetKit 资源加载工具文档

## 目录

- [简介](#简介)
- [主要功能](#主要功能)
- [API 说明](#api-说明)
  - [Resources 加载](#resources-加载)
  - [AssetBundle 加载](#assetbundle-加载)
- [快速入门](#快速入门)
- [典型场景](#典型场景)
- [性能建议](#性能建议)

---

## 简介

**LoadAssetKit** 是一套高效的 Unity 资源加载工具，支持同步与异步加载 Resources 和 AssetBundle 资源，集成 UniTask 异步方案，具备缓存、依赖管理和引用计数等功能，适合多种项目资源管理需求。

---

## 主要功能

- **同步/异步加载**：支持回调和 async/await 两种异步方式。
- **资源缓存**：自动缓存已加载资源，避免重复加载。
- **依赖管理**：AssetBundle 自动加载依赖包。
- **引用计数**：自动管理资源包引用，安全卸载。
- **类型安全**：泛型接口，类型检查更安全。
- **灵活卸载与清理**：支持单资源、单包和全部资源的卸载与清理。

---

## API 说明

### Resources 加载

- `LoadAssetFromRes<T>(string resPath, Action<T> callback = null, bool isCache = true)`
  - 同步加载资源，或异步加载（带回调）。
- `LoadAssetFromResAsync<T>(string resPath, bool isCache = true, CancellationToken cancellationToken = default)`
  - 异步加载资源，支持 async/await。
- `UnloadAsset(string resPath)`
  - 卸载指定资源。
- `ClearCache()`
  - 清理所有缓存资源。

### AssetBundle 加载（需配合 LoadAssetBundleHandler 使用）

- `LoadAsset<T>(string bundleName, string assetName)`
  - 同步加载 AssetBundle 资源，自动加载依赖。
- `LoadAssetAsync<T>(string bundleName, string assetName, Action<bool> isSuccess = null, Action<float> progress = null)`
  - 异步加载 AssetBundle 资源，自动加载依赖，支持进度与完成回调。
- `UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)`
  - 卸载指定资源包及其依赖。
- `UnloadBundleAsync(string bundleName, bool unloadAllLoadedObjects = false)`
  - 异步卸载指定资源包及其依赖。
- `ClearAllBundles(bool unloadAllLoadedObjects = false)`
  - 清理所有已加载的资源包。
- `ClearAllBundlesAsync(bool unloadAllLoadedObjects = false)`
  - 异步清理所有已加载的资源包。

---

## 快速入门

### Resources 加载

**同步加载：**

```csharp
var sprite = LoadAssetKit.LoadAssetFromRes<Sprite>("UI/Icons/player_icon");
```

**异步加载（回调）：**

```csharp
LoadAssetKit.LoadAssetFromRes<Sprite>("UI/Icons/player_icon", sprite => {
    if (sprite != null) myImage.sprite = sprite;
});
```

**异步加载（async/await）：**

```csharp
var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>("UI/Icons/player_icon");
```

### AssetBundle 加载

**初始化加载器：**

```csharp
var handler = new LoadAssetBundleHandler(Application.streamingAssetsPath, "AB_Group1", "AB_Group1");
```

**同步加载资源：**

```csharp
var prefab = handler.LoadAsset<GameObject>("uigameinfopanel", "UIGameInfoPanel");
```

**异步加载资源：**

```csharp
var prefab = await handler.LoadAssetAsync<GameObject>("uigameinfopanel", "UIGameInfoPanel",
    progress: p => Debug.Log("进度:" + p),
    isDone: done=> Debug.Log("加载结果:" + done));
```

**卸载资源包：**

```csharp
handler.UnloadBundle("uigameinfopanel");
await handler.UnloadBundleAsync("uigameinfopanel");
```

**清理所有资源包：**

```csharp
handler.ClearAllBundles();
await handler.ClearAllBundlesAsync(false,
    progress: p => Debug.Log("进度:" + p),
    isDone: done=> Debug.Log("卸载结果:" + done));
```

---

## 典型场景

- UI 动态加载与卸载
- 关卡/场景资源按需加载
- 音效、特效等大资源异步加载
- 资源包依赖自动管理
- 内存优化与资源回收

---

## 性能建议

- 合理使用缓存，避免重复加载资源。
- 及时卸载不再使用的资源和资源包，释放内存。
- 使用异步加载减少主线程阻塞，提升运行流畅度。
- 关注引用计数，防止资源泄漏。
- 利用进度回调优化加载体验。

---
