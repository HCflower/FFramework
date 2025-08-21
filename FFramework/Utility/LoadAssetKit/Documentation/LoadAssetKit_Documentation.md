# FFramework.Kit LoadAssetKit 资源加载工具文档

## 📋 目录

- [📖 简介](#-简介)
- [✨ 主要特性](#-主要特性)
- [🚀 快速开始](#-快速开始)
  - [Resources 文件夹资源加载](#resources-文件夹资源加载)
    - [同步加载](#同步加载)
    - [异步加载（回调方式）](#异步加载回调方式)
    - [异步加载（UniTask 方式）](#异步加载unitask-方式)
  - [AssetBundle 资源加载](#assetbundle-资源加载)
    - [初始化主包](#初始化主包)
    - [同步加载](#同步加载-1)
    - [异步加载（回调方式）](#异步加载回调方式-1)
    - [异步加载（UniTask 方式）](#异步加载unitask-方式-1)
- [🗂️ 资源管理](#️-资源管理)
  - [缓存管理](#缓存管理)
  - [AssetBundle 管理](#assetbundle-管理)
- [📚 API 参考](#-api-参考)
  - [Resources 加载方法](#resources-加载方法)
  - [AssetBundle 加载方法](#assetbundle-加载方法)
- [💡 最佳实践](#-最佳实践)
- [⚠️ 注意事项](#️-注意事项)
- [📦 依赖要求](#-依赖要求)

## 📖 简介

LoadAssetKit 是一个强大的 Unity 资源加载工具，支持从 Resources 文件夹和 AssetBundle 加载资源。它使用 UniTask 提供高效的异步加载功能，并包含智能的资源缓存和引用计数管理系统。

## ✨ 主要特性

- ✅ **Resources 支持**: 完整的 Resources 文件夹同步/异步加载
- ✅ **AssetBundle 支持**: 完整的 AssetBundle 同步/异步加载
- ✅ **UniTask 集成**: 基于 UniTask 的高性能异步操作
- ✅ **智能缓存**: 自动资源缓存系统，提升加载性能
- ✅ **依赖管理**: AssetBundle 依赖自动管理和引用计数
- ✅ **取消支持**: 支持取消令牌（CancellationToken）
- ✅ **向后兼容**: 支持传统回调方式和现代异步方式
- ✅ **类型安全**: 强类型泛型支持，减少运行时错误
- ✅ **Lua 友好**: 提供 Lua 脚本友好的 Type 参数接口

## 🚀 快速开始

### Resources 文件夹资源加载

#### 同步加载

```csharp
// 同步加载精灵图片
var sprite = LoadAssetKit.LoadAssetFromRes<Sprite>("UI/Icons/player_icon");

// 同步加载音频剪辑
var audioClip = LoadAssetKit.LoadAssetFromRes<AudioClip>("Audio/BGM/main_theme");

// 同步加载预制件
var prefab = LoadAssetKit.LoadAssetFromRes<GameObject>("Prefabs/Player");
```

#### 异步加载（回调方式）

```csharp
// 使用回调方式异步加载
LoadAssetKit.LoadAssetFromRes<Sprite>("UI/Icons/player_icon", sprite => {
    if (sprite != null)
    {
        // 使用加载的精灵
        myImage.sprite = sprite;
    }
});
```

#### 异步加载（UniTask 方式）

```csharp
// 使用 async/await 方式
public async UniTask LoadPlayerIcon()
{
    var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>("UI/Icons/player_icon");
    if (sprite != null)
    {
        myImage.sprite = sprite;
    }
}

// 带取消令牌的异步加载
public async UniTask LoadPlayerIconWithCancellation()
{
    var cts = new CancellationTokenSource();
    try
    {
        var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>(
            "UI/Icons/player_icon",
            true,
            cts.Token
        );
        myImage.sprite = sprite;
    }
    catch (OperationCanceledException)
    {
        Debug.Log("资源加载已取消");
    }
}
```

### AssetBundle 资源加载

#### 初始化主包

```csharp
// 首先需要加载主包
bool success = LoadAssetKit.LoadMainAssetBundle("main_bundle");
if (!success)
{
    Debug.LogError("主包加载失败");
    return;
}
```

#### 同步加载

```csharp
// 同步加载资源
var prefab = LoadAssetKit.LoadAssetFromAssetBundle<GameObject>("ui_bundle", "MainMenuPrefab");

// 使用类型参数的同步加载（适用于 Lua）
var obj = LoadAssetKit.LoadAssetFromAssetBundle("ui_bundle", "MainMenuPrefab", typeof(GameObject));
```

#### 异步加载（回调方式）

```csharp
// 使用回调方式
LoadAssetKit.LoadResAsync<GameObject>("ui_bundle", "MainMenuPrefab", prefab => {
    if (prefab != null)
    {
        Instantiate(prefab);
    }
});

// 使用类型参数的异步加载
LoadAssetKit.LoadResAsync("ui_bundle", "MainMenuPrefab", typeof(GameObject), obj => {
    if (obj != null)
    {
        Instantiate(obj as GameObject);
    }
});
```

#### 异步加载（UniTask 方式）

```csharp
// 使用 async/await 方式
public async UniTask LoadMainMenuPrefab()
{
    var prefab = await LoadAssetKit.LoadResAsync<GameObject>("ui_bundle", "MainMenuPrefab");
    if (prefab != null)
    {
        Instantiate(prefab);
    }
}

// 带取消令牌的异步加载
public async UniTask LoadMainMenuPrefabWithCancellation()
{
    var cts = new CancellationTokenSource();
    try
    {
        var prefab = await LoadAssetKit.LoadResAsync<GameObject>(
            "ui_bundle",
            "MainMenuPrefab",
            cts.Token
        );
        Instantiate(prefab);
    }
    catch (OperationCanceledException)
    {
        Debug.Log("资源加载已取消");
    }
}
```

## 🗂️ 资源管理

### 缓存管理

```csharp
// 卸载指定资源
LoadAssetKit.UnloadAsset("UI/Icons/player_icon");

// 清理所有缓存
LoadAssetKit.ClearCache();
```

### AssetBundle 管理

```csharp
// 卸载单个 AssetBundle（自动处理依赖）
LoadAssetKit.UnLoadAssetBundle("ui_bundle");

// 卸载所有 AssetBundle
LoadAssetKit.UnLoadAllAssetBundle();
```

## 📚 API 参考

### Resources 加载方法

| 方法                                             | 描述              | 返回类型     |
| ------------------------------------------------ | ----------------- | ------------ |
| `LoadAssetFromRes<T>(path, callback, isCache)`   | 同步/异步加载资源 | `T`          |
| `LoadAssetFromResAsync<T>(path, isCache, token)` | 纯异步加载资源    | `UniTask<T>` |
| `UnloadAsset(path)`                              | 卸载指定资源      | `void`       |
| `ClearCache()`                                   | 清理所有缓存      | `void`       |

### AssetBundle 加载方法

| 方法                                        | 描述                | 返回类型     |
| ------------------------------------------- | ------------------- | ------------ |
| `LoadMainAssetBundle(name)`                 | 加载主包            | `bool`       |
| `LoadAssetFromAssetBundle<T>(bundle, name)` | 同步加载资源        | `T`          |
| `LoadResAsync<T>(bundle, name, callback)`   | 异步加载（回调）    | `void`       |
| `LoadResAsync<T>(bundle, name, token)`      | 异步加载（UniTask） | `UniTask<T>` |
| `UnLoadAssetBundle(name)`                   | 卸载单个包          | `void`       |
| `UnLoadAllAssetBundle()`                    | 卸载所有包          | `void`       |

### 路径管理方法

| 方法                           | 描述                 | 返回类型 |
| ------------------------------ | -------------------- | -------- |
| `SetAssetBundleLoadPath(path)` | 设置 AB 加载路径     | `void`   |
| `GetAssetBundleLoadPath()`     | 获取当前 AB 加载路径 | `string` |

### 缓存管理方法

| 方法              | 描述                         | 返回类型 |
| ----------------- | ---------------------------- | -------- |
| `ClearCache()`    | 清理 Resources 缓存          | `void`   |
| `ClearAllCache()` | 清理所有缓存（Resources+AB） | `void`   |

## 🎯 最佳实践

### 1. 资源路径管理

```csharp
// 建议使用常量管理资源路径
public static class ResourcePaths
{
    // UI 图标路径
    public const string PLAYER_ICON = "UI/Icons/player_icon";
    public const string ENEMY_ICON = "UI/Icons/enemy_icon";

    // 预制件路径
    public const string PLAYER_PREFAB = "Prefabs/Player";
    public const string BULLET_PREFAB = "Prefabs/Bullet";

    // 音频路径
    public const string BGM_MAIN = "Audio/BGM/main_theme";
    public const string SFX_SHOOT = "Audio/SFX/shoot";
}
```

### 2. 异步加载模式

```csharp
// 推荐使用异步加载，避免阻塞主线程
public class ResourceManager : MonoBehaviour
{
    private readonly Dictionary<string, Object> _loadedAssets = new();

    public async UniTask<T> GetAssetAsync<T>(string path) where T : Object
    {
        if (_loadedAssets.ContainsKey(path))
            return _loadedAssets[path] as T;

        var asset = await LoadAssetKit.LoadAssetFromResAsync<T>(path);
        if (asset != null)
            _loadedAssets[path] = asset;

        return asset;
    }
}
```

### 3. AssetBundle 路径配置

```csharp
// 配置AssetBundle加载路径
public class AssetBundleConfig : MonoBehaviour
{
    private void Start()
    {
        // 设置自定义加载路径
        string customPath = Application.streamingAssetsPath + "/AssetBundles/";
        LoadAssetKit.SetAssetBundleLoadPath(customPath);

        // 获取当前路径
        string currentPath = LoadAssetKit.GetAssetBundleLoadPath();
        Debug.Log($"当前AssetBundle加载路径: {currentPath}");
    }
}
```

### 4. 内存管理

```csharp
// 及时清理不需要的资源
public class SceneResourceManager : MonoBehaviour
{
    private readonly List<string> _loadedPaths = new();

    private void OnDestroy()
    {
        // 场景销毁时清理加载的资源
        foreach (var path in _loadedPaths)
        {
            LoadAssetKit.UnloadAsset(path);
        }
        _loadedPaths.Clear();

        // 清理所有缓存（可选）
        LoadAssetKit.ClearAllCache();
    }
};
    }
}
```

### 5. 错误处理

```csharp
// 完善的错误处理机制
public async UniTask<T> SafeLoadAsset<T>(string path) where T : Object
{
    try
    {
        var asset = await LoadAssetKit.LoadAssetFromResAsync<T>(path);
        if (asset == null)
        {
            Debug.LogWarning($"资源加载失败: {path}");
            return null;
        }
        return asset;
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"资源加载异常 {path}: {ex.Message}");
        return null;
    }
}
```

### 6. AssetBundle 最佳实践

```csharp
// AssetBundle 初始化管理
public class BundleManager : MonoBehaviour
{
    [SerializeField] private string[] requiredBundles;

    private async void Start()
    {
        // 初始化主包
        if (!LoadAssetKit.LoadMainAssetBundle("main"))
        {
            Debug.LogError("主包初始化失败");
            return;
        }

        // 预加载关键资源包
        foreach (var bundleName in requiredBundles)
        {
            await PreloadBundle(bundleName);
        }
    }

    private async UniTask PreloadBundle(string bundleName)
    {
        try
        {
            // 预加载包中的关键资源
            var manifest = await LoadAssetKit.LoadResAsync<AssetBundleManifest>(
                bundleName, "manifest");
            // 处理预加载逻辑
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"包预加载失败 {bundleName}: {ex.Message}");
        }
    }
}
```

## ⚠️ 注意事项

1. **Resources 文件夹限制**：Resources 中的资源会被打包到主包中，影响包体大小
2. **异步操作**：使用异步加载时要处理好生命周期，避免组件销毁后继续操作
3. **内存管理**：及时卸载不需要的资源，特别是大型资源如纹理和音频
4. **路径规范**：保持资源路径的一致性和可维护性
5. **错误处理**：始终检查加载结果，处理资源不存在的情况

## 📝 示例代码

完整的示例代码请参考 `LoadAssetKitExample.cs` 文件，其中包含了图片和预制件的加载演示。

---

**LoadAssetKit** 提供了统一的资源加载接口，支持 Resources 和 AssetBundle 两种加载方式，结合 UniTask 实现高性能的异步加载，是 Unity 项目中资源管理的理想选择。
{
try
{
var sprite = await LoadAssetKit.LoadAssetFromResAsync`<Sprite>`("UI/Icons/player_icon");
if (sprite != null)
{
// 使用资源
}
else
{
Debug.LogWarning("资源加载失败");
}
}
catch (OperationCanceledException)
{
Debug.Log("加载被取消");
}
catch (Exception ex)
{
Debug.LogError($"加载异常: {ex.Message}");
}
}

````

### 3. 资源清理

```csharp
void OnDestroy()
{
    // 清理缓存
    LoadAssetKit.ClearCache();

    // 清理实例化的对象
    if (instantiatedObjects != null)
    {
        foreach (var obj in instantiatedObjects)
        {
            if (obj != null) Destroy(obj);
        }
    }
}
````

## ⚠️ 注意事项

1. **路径格式**: Resources 路径不需要文件扩展名和 "Resources/" 前缀
2. **内存管理**: 及时清理不需要的资源，避免内存泄漏
3. **异常处理**: 异步操作建议使用 try-catch 包装
4. **预制件实例化**: 加载预制件后记得实例化才能在场景中使用
5. **缓存策略**: 合理使用 isCache 参数控制缓存行为

## � 示例文件说明

- `LoadAssetKitExample.cs`: 主要示例文件，包含图片和预制件加载的完整示例
- 资源路径配置已内置在示例文件中，便于统一管理

### 1. Resources 文件夹资源加载

#### 同步加载

```csharp
// 同步加载精灵图片
var sprite = LoadAssetKit.LoadAssetFromRes<Sprite>("UI/Icons/player_icon");

// 同步加载音频剪辑
var audioClip = LoadAssetKit.LoadAssetFromRes<AudioClip>("Audio/BGM/main_theme");
```

#### 异步加载（回调方式）

```csharp
// 使用回调方式异步加载
LoadAssetKit.LoadAssetFromRes<Sprite>("UI/Icons/player_icon", sprite => {
    if (sprite != null)
    {
        // 使用加载的精灵
        myImage.sprite = sprite;
    }
});
```

#### 异步加载（UniTask 方式）

```csharp
// 使用 async/await 方式
public async UniTask LoadPlayerIcon()
{
    var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>("UI/Icons/player_icon");
    if (sprite != null)
    {
        myImage.sprite = sprite;
    }
}

// 带取消令牌的异步加载
public async UniTask LoadPlayerIconWithCancellation()
{
    var cts = new CancellationTokenSource();
    try
    {
        var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>(
            "UI/Icons/player_icon",
            true,
            cts.Token
        );
        myImage.sprite = sprite;
    }
    catch (OperationCanceledException)
    {
        Debug.Log("资源加载已取消");
    }
}
```

### 2. AssetBundle 资源加载

#### 初始化主包

```csharp
// 首先需要加载主包
bool success = LoadAssetKit.LoadMainAssetBundle("main_bundle");
if (!success)
{
    Debug.LogError("主包加载失败");
    return;
}
```

#### 同步加载

```csharp
// 同步加载资源
var prefab = LoadAssetKit.LoadAssetFromAssetBundle<GameObject>("ui_bundle", "MainMenuPrefab");

// 使用类型参数的同步加载（适用于 Lua）
var obj = LoadAssetKit.LoadAssetFromAssetBundle("ui_bundle", "MainMenuPrefab", typeof(GameObject));
```

#### 异步加载（回调方式）

```csharp
// 使用回调方式
LoadAssetKit.LoadResAsync<GameObject>("ui_bundle", "MainMenuPrefab", prefab => {
    if (prefab != null)
    {
        Instantiate(prefab);
    }
});

// 使用类型参数的异步加载
LoadAssetKit.LoadResAsync("ui_bundle", "MainMenuPrefab", typeof(GameObject), obj => {
    if (obj != null)
    {
        Instantiate(obj as GameObject);
    }
});
```

#### 异步加载（UniTask 方式）

```csharp
// 使用 async/await 方式
public async UniTask LoadMainMenuPrefab()
{
    var prefab = await LoadAssetKit.LoadResAsync<GameObject>("ui_bundle", "MainMenuPrefab");
    if (prefab != null)
    {
        Instantiate(prefab);
    }
}

// 带取消令牌的异步加载
public async UniTask LoadMainMenuPrefabWithCancellation()
{
    var cts = new CancellationTokenSource();
    try
    {
        var prefab = await LoadAssetKit.LoadResAsync<GameObject>(
            "ui_bundle",
            "MainMenuPrefab",
            cts.Token
        );
        Instantiate(prefab);
    }
    catch (OperationCanceledException)
    {
        Debug.Log("资源加载已取消");
    }
}
```

## 🗂️ 资源管理

### 缓存管理

```csharp
// 卸载指定资源
LoadAssetKit.UnloadAsset("UI/Icons/player_icon");

// 清理所有缓存
LoadAssetKit.ClearCache();
```

### AssetBundle 管理

```csharp
// 卸载单个 AssetBundle（自动处理依赖）
LoadAssetKit.UnLoadAssetBundle("ui_bundle");

// 卸载所有 AssetBundle
LoadAssetKit.UnLoadAllAssetBundle();
```

## 📚 API 参考

### Resources 加载方法

| 方法                                             | 描述              | 返回类型     |
| ------------------------------------------------ | ----------------- | ------------ |
| `LoadAssetFromRes<T>(path, callback, isCache)`   | 同步/异步加载资源 | `T`          |
| `LoadAssetFromResAsync<T>(path, isCache, token)` | 纯异步加载资源    | `UniTask<T>` |
| `UnloadAsset(path)`                              | 卸载指定资源      | `void`       |
| `ClearCache()`                                   | 清理所有缓存      | `void`       |

### AssetBundle 加载方法

| 方法                                        | 描述                | 返回类型     |
| ------------------------------------------- | ------------------- | ------------ |
| `LoadMainAssetBundle(name)`                 | 加载主包            | `bool`       |
| `LoadAssetFromAssetBundle<T>(bundle, name)` | 同步加载资源        | `T`          |
| `LoadResAsync<T>(bundle, name, callback)`   | 异步加载（回调）    | `void`       |
| `LoadResAsync<T>(bundle, name, token)`      | 异步加载（UniTask） | `UniTask<T>` |
| `UnLoadAssetBundle(name)`                   | 卸载单个包          | `void`       |
| `UnLoadAllAssetBundle()`                    | 卸载所有包          | `void`       |

## 💡 最佳实践

### 1. 资源路径管理

```csharp
// 建议使用常量管理资源路径
public static class ResourcePaths
{
    // UI 资源路径
    public const string UI_PLAYER_ICON = "UI/Icons/player_icon";
    public const string UI_ENEMY_ICON = "UI/Icons/enemy_icon";
    public const string UI_ITEM_ICON = "UI/Icons/item_icon";

    // 音频资源路径
    public const string AUDIO_BGM_MAIN = "Audio/BGM/main_theme";
    public const string AUDIO_SFX_CLICK = "Audio/SFX/click_sound";

    // 预制体资源路径
    public const string PREFAB_PLAYER = "Prefabs/Player";
    public const string PREFAB_ENEMY = "Prefabs/Enemy";
}

// 使用常量
var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>(ResourcePaths.UI_PLAYER_ICON);
```

### 2. 异常处理

```csharp
public async UniTask LoadResourceSafely()
{
    try
    {
        var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>("UI/Icons/player_icon");
        if (sprite == null)
        {
            Debug.LogWarning("资源加载失败，使用默认资源");
            sprite = GetDefaultSprite(); // 提供默认资源
            return;
        }

        // 使用资源...
        ApplySprite(sprite);
    }
    catch (OperationCanceledException)
    {
        Debug.Log("资源加载被取消");
    }
    catch (Exception ex)
    {
        Debug.LogError($"加载资源时发生异常: {ex.Message}");
        // 记录错误或进行其他错误处理
    }
}
```

### 3. 生命周期管理

```csharp
public class ResourceManager : MonoBehaviour
{
    private CancellationTokenSource cts;

    void Start()
    {
        cts = new CancellationTokenSource();
    }

    void OnDestroy()
    {
        // 取消所有正在进行的加载操作
        cts?.Cancel();
        cts?.Dispose();

        // 清理缓存
        LoadAssetKit.ClearCache();
    }

    public async UniTask LoadGameResources()
    {
        try
        {
            var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>(
                "UI/loading",
                true,
                cts.Token
            );
            // 使用资源...
        }
        catch (OperationCanceledException)
        {
            // 处理取消操作
        }
    }
}
```

## ⚠️ 注意事项

1. **主包加载**: 使用 AssetBundle 前必须先调用 `LoadMainAssetBundle`
2. **路径格式**: Resources 路径不需要文件扩展名和 "Resources/" 前缀
3. **内存管理**: 及时卸载不需要的资源，避免内存泄漏
4. **异常处理**: 异步操作建议使用 try-catch 包装
5. **取消令牌**: 长时间运行的加载操作建议传入 CancellationToken

## 📦 依赖要求

- Unity 2020.3 或更高版本
- UniTask 插件（已包含在项目中）
