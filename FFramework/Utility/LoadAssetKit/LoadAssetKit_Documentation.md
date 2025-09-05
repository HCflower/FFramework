# LoadAssetKit 资源加载工具文档

## 目录

- [一、简介](#一简介)
- [二、优势](#二优势)
- [三、API 介绍](#三api介绍)
  - [1. Resources 加载方法](#1-resources-加载方法)
  - [2. AssetBundle 加载方法](#2-assetbundle-加载方法)
- [四、核心功能](#四核心功能)
- [五、快速上手](#五快速上手)
  - [1. Resources 文件夹加载](#1-resources-文件夹加载)
  - [2. AssetBundle 加载](#2-assetbundle-加载)
- [六、使用场景示例](#六使用场景示例)
- [七、性能优化](#七性能优化)

---

## 一简介

`LoadAssetKit` 是一个功能强大的 Unity 资源加载工具，支持从 Resources 文件夹和 AssetBundle 加载资源。它集成了 UniTask 提供高效的异步加载功能，并包含智能的资源缓存和引用计数管理系统，适用于多种资源加载场景。通过灵活的 API 和高性能的设计，`LoadAssetKit` 能够显著提升资源加载的效率和开发体验。

---

## 二优势

1. **多种加载方式**：支持同步加载和异步加载，兼容回调和 async/await 两种方式。
2. **高性能**：内置资源缓存和引用计数管理，避免重复加载资源，提升性能。
3. **依赖管理**：自动管理 AssetBundle 的依赖关系，简化开发流程。
4. **类型安全**：提供泛型支持，确保资源加载的类型安全，减少运行时错误。
5. **灵活性**：支持取消令牌（CancellationToken），便于任务管理和资源加载的中断控制。
6. **兼容性强**：支持传统回调方式和现代异步方式，适配不同开发需求。

---

## 三 api 介绍

### 1-resources-加载方法

- `LoadAssetFromRes<T>(string resPath, Action<T> callback = null, bool isCache = true)`
  - 同步或异步加载 Resources 文件夹中的资源。
  - **参数**：
    - `resPath`：资源路径。
    - `callback`：异步加载完成后的回调函数。
    - `isCache`：是否缓存加载的资源。
- `LoadAssetFromResAsync<T>(string resPath, bool isCache = true, CancellationToken cancellationToken = default)`
  - 异步加载 Resources 文件夹中的资源。
  - **参数**：
    - `resPath`：资源路径。
    - `isCache`：是否缓存加载的资源。
    - `cancellationToken`：取消令牌。
- `UnloadAsset(string resPath)`
  - 卸载指定资源。
  - **参数**：
    - `resPath`：资源路径。
- `ClearCache()`
  - 清理所有缓存资源。

### 2-assetbundle-加载方法

- `SetAssetBundleLoadPath(string path)`
  - 设置 AssetBundle 加载路径。
  - **参数**：
    - `path`：新的加载路径（需要以 `/` 结尾）。
- `GetAssetBundleLoadPath()`
  - 获取当前 AssetBundle 加载路径。
- `LoadAssetFromAssetBundle<T>(string abName, string resName)`
  - 同步加载 AssetBundle 中的资源。
  - **参数**：
    - `abName`：AssetBundle 包名。
    - `resName`：资源名。
- `LoadResAsync<T>(string abName, string resName, Action<T> callBack)`
  - 异步加载 AssetBundle 中的资源。
  - **参数**：
    - `abName`：AssetBundle 包名。
    - `resName`：资源名。
    - `callBack`：加载完成后的回调函数。
- `UnLoadAssetBundle(string abName)`
  - 卸载单个 AssetBundle。
  - **参数**：
    - `abName`：AssetBundle 包名。
- `UnLoadAllAssetBundle()`
  - 卸载所有 AssetBundle。
- `ClearAllCache()`
  - 清理所有缓存（包括 Resources 和 AssetBundle）。

---

## 四核心功能

1. **Resources 文件夹加载**：支持同步和异步加载，兼容回调和 async/await。
2. **AssetBundle 加载**：支持依赖管理和引用计数，提供同步和异步加载方法。
3. **资源缓存**：自动缓存加载的资源，减少重复加载。
4. **资源卸载**：提供资源卸载和缓存清理功能，释放内存。
5. **取消支持**：支持取消令牌，便于任务管理。

---

## 五快速上手

### 1-resources-文件夹加载

#### 同步加载

```csharp
var sprite = LoadAssetKit.LoadAssetFromRes<Sprite>("UI/Icons/player_icon");
```

#### 异步加载（回调方式）

```csharp
LoadAssetKit.LoadAssetFromRes<Sprite>("UI/Icons/player_icon", sprite => {
    if (sprite != null)
    {
        myImage.sprite = sprite;
    }
});
```

#### 异步加载（async/await）

```csharp
public async UniTask LoadPlayerIcon()
{
    var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>("UI/Icons/player_icon");
    if (sprite != null)
    {
        myImage.sprite = sprite;
    }
}
```

### 2-assetbundle-加载

#### 设置加载路径

```csharp
LoadAssetKit.SetAssetBundleLoadPath(Application.streamingAssetsPath + "/AssetBundles/");
```

#### 同步加载

```csharp
var prefab = LoadAssetKit.LoadAssetFromAssetBundle<GameObject>("ui_bundle", "PlayerUI");
```

#### 异步加载

```csharp
LoadAssetKit.LoadResAsync<GameObject>("ui_bundle", "PlayerUI", prefab => {
    if (prefab != null)
    {
        Instantiate(prefab);
    }
});
```

---

## 六使用场景示例

1. **UI 动态加载**：使用 Resources 文件夹加载 UI 资源。
2. **游戏关卡加载**：使用 AssetBundle 加载关卡资源。
3. **音效管理**：动态加载和卸载音效资源。
4. **资源优化**：使用缓存和引用计数减少内存占用。

---

## 七性能优化

1. **资源缓存**：合理使用缓存，避免重复加载。
2. **引用计数管理**：确保正确管理 AssetBundle 的引用计数，避免内存泄漏。
3. **异步加载**：使用异步加载减少主线程阻塞。
4. **资源卸载**：定期清理不再使用的资源，释放内存。
5. **调试工具**：使用日志和调试工具监控资源加载和卸载情况。
