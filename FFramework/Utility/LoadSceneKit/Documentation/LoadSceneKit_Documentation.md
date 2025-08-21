# FFramework.Kit LoadSceneKit 场景加载工具文档

## 📖 目录

1. [概述](#概述)
2. [核心特性](#核心特性)
3. [API 参考](#api参考)
4. [使用教程](#使用教程)
5. [UniTask 使用指南](#unitask-使用指南) ⭐
6. [示例代码](#示例代码)
7. [最佳实践](#最佳实践)
8. [注意事项](#注意事项)

---

## 概述

LoadSceneKit 是一个优化的 Unity 场景加载工具类，专为高效的场景切换而设计。它提供了完整的同步/异步场景切换功能，自动处理旧场景卸载，并支持详细的进度监控和事件回调。

### 设计理念

- **简化 API**: 只暴露必要的场景切换方法，隐藏复杂的内部逻辑
- **智能管理**: 自动处理场景加载、激活和卸载的完整流程
- **进度监控**: 提供详细的加载和卸载进度信息
- **事件驱动**: 通过参数传入事件回调，支持跨场景的场景管理器

---

## 核心特性

### 🚀 智能场景切换

- **自动卸载**: 加载新场景后自动卸载旧场景
- **状态管理**: 自动设置新场景为激活状态
- **冲突检测**: 防止多个场景切换操作同时进行

### 📊 详细进度监控

- **分阶段进度**: 分别监控加载和卸载进度
- **总体进度**: 加载 50% + 卸载 50% = 总进度 100%
- **实时查询**: 随时获取当前操作状态和进度

### 🎯 灵活事件回调

- **参数化事件**: 事件通过参数传入，支持跨场景使用
- **开始回调**: 场景切换开始时触发（用于显示加载界面）
- **完成回调**: 场景切换完成时触发（返回成功/失败状态）

### ⚡ 高性能设计

- **异步优先**: 推荐使用异步加载避免卡顿
- **内存优化**: 及时清理操作引用，防止内存泄漏
- **错误处理**: 完善的异常处理和错误恢复机制

---

## API 参考

### 核心属性

```csharp
/// <summary>加载进度(0-1)</summary>
public static float LoadingProgress { get; }

/// <summary>卸载进度(0-1)</summary>
public static float UnloadingProgress { get; }

/// <summary>总进度(0-1) - 加载50% + 卸载50%</summary>
public static float TotalProgress { get; }

/// <summary>是否正在处理场景切换</summary>
public static bool IsProcessing { get; }
```

### 主要方法

#### LoadScene - 同步场景切换

```csharp
public static void LoadScene(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)
```

**参数说明：**

- `sceneName`: 目标场景名称
- `onChangeScene`: 场景切换开始时的回调（用于显示加载面板等）
- `onComplete`: 完成回调，参数表示是否成功

**使用示例：**

```csharp
LoadSceneKit.LoadScene("GameScene",
    onChangeScene: () => UIKit.OpenPanel<LoadingPanel>(),
    onComplete: (success) => {
        if (success) Debug.Log("场景切换成功");
        UIKit.ClosePanel<LoadingPanel>();
    });
```

#### LoadSceneAsync - 异步场景切换（协程版本）

```csharp
public static void LoadSceneAsync(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)
```

**参数说明：**

- `sceneName`: 目标场景名称
- `onChangeScene`: 场景切换开始时的回调（用于显示加载面板等）
- `onComplete`: 完成回调，参数表示是否成功

**使用示例：**

```csharp
LoadSceneKit.LoadSceneAsync("GameScene",
    onChangeScene: () => ShowLoadingUI(),
    onComplete: (success) => {
        if (success) Debug.Log("异步切换成功");
        HideLoadingUI();
    });
```

#### LoadSceneAsyncTask - UniTask 版本异步场景切换 ⭐ **推荐**

```csharp
public static async UniTask<bool> LoadSceneAsyncTask(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)
```

**参数说明：**

- `sceneName`: 目标场景名称
- `onChangeScene`: 场景切换开始时的回调（用于显示加载面板等）
- `onComplete`: 完成回调，参数表示是否成功
- **返回值**: UniTask`<bool>` - 可以 await 等待，返回操作是否成功

**优势：**

- 🚀 **性能更佳**: 比传统协程更高效
- 🧹 **代码更清洁**: 使用 async/await 语法
- 🛡️ **异常处理**: 原生支持 try-catch
- 🔧 **调试友好**: 更好的堆栈跟踪

**使用示例：**

```csharp
// 基础用法
bool success = await LoadSceneKit.LoadSceneAsyncTask("GameScene");

// 完整用法
try
{
    bool result = await LoadSceneKit.LoadSceneAsyncTask("GameScene",
        onChangeScene: () => ShowLoadingUI(),
        onComplete: (success) => {
            if (success) Debug.Log("UniTask切换成功");
            HideLoadingUI();
        });

    if (result)
    {
        Debug.Log("场景切换完成");
    }
}
catch (Exception ex)
{
    Debug.LogError($"场景切换出错: {ex.Message}");
}
```

### 状态查询方法

```csharp
/// <summary>获取当前场景名称</summary>
public static string GetCurrentSceneName()

/// <summary>获取加载阶段进度(0-0.5)</summary>
public static float GetLoadProgress()

/// <summary>获取卸载阶段进度(0-0.5)</summary>
public static float GetUnloadProgress()

/// <summary>获取总进度(0-1)</summary>
public static float GetTotalProgress()

/// <summary>获取进度详情</summary>
public static (float load, float unload, float total) GetProgressDetails()
```

---

## 使用教程

### 基础使用

#### 1. 简单场景切换

```csharp
// 协程版本 - 传统方式
LoadSceneKit.LoadSceneAsync("NextScene");

// UniTask版本 - 推荐方式
bool success = await LoadSceneKit.LoadSceneAsyncTask("NextScene");

// 带完成回调的协程版本
LoadSceneKit.LoadSceneAsync("NextScene", null, (success) => {
    Debug.Log(success ? "切换成功" : "切换失败");
});

// 带完成回调的UniTask版本
try
{
    bool result = await LoadSceneKit.LoadSceneAsyncTask("NextScene", null, (success) => {
        Debug.Log(success ? "切换成功" : "切换失败");
    });
}
catch (Exception ex)
{
    Debug.LogError($"场景切换异常: {ex.Message}");
}
```

#### 2. 带加载界面的场景切换

**协程版本：**

```csharp
LoadSceneKit.LoadSceneAsync("GameScene",
    onChangeScene: () => {
        // 显示加载界面
        UIKit.OpenPanel<LoadingPanel>();
    },
    onComplete: (success) => {
        // 隐藏加载界面
        UIKit.ClosePanel<LoadingPanel>();

        if (success) {
            Debug.Log("场景切换成功");
        } else {
            Debug.LogError("场景切换失败");
        }
    });
```

**UniTask 版本（推荐）：**

```csharp
public async void LoadGameScene()
{
    try
    {
        bool success = await LoadSceneKit.LoadSceneAsyncTask("GameScene",
            onChangeScene: () => {
                // 显示加载界面
                UIKit.OpenPanel<LoadingPanel>();
            },
            onComplete: (success) => {
                // 隐藏加载界面
                UIKit.ClosePanel<LoadingPanel>();
                Debug.Log(success ? "场景切换成功" : "场景切换失败");
            });

        // 在这里可以执行后续逻辑
        if (success)
        {
            Debug.Log("可以开始游戏了！");
        }
    }
    catch (Exception ex)
    {
        UIKit.ClosePanel<LoadingPanel>();
        Debug.LogError($"场景切换失败: {ex.Message}");
    }
}
```

### 进度监控

#### 1. 基础进度显示

```csharp
public class LoadingProgressDisplay : MonoBehaviour
{
    public Slider progressSlider;
    public Text progressText;

    private void Update()
    {
        if (LoadSceneKit.IsProcessing)
        {
            float progress = LoadSceneKit.TotalProgress;
            progressSlider.value = progress;
            progressText.text = $"{(progress * 100):F1}%";
        }
    }
}
```

#### 2. 详细进度监控

```csharp
private IEnumerator MonitorLoadingProgress()
{
    while (LoadSceneKit.IsProcessing)
    {
        var (loadProgress, unloadProgress, totalProgress) = LoadSceneKit.GetProgressDetails();

        // 显示不同阶段的进度
        if (loadProgress < 0.5f)
        {
            Debug.Log($"加载中... {(loadProgress * 200):F1}%");
        }
        else
        {
            Debug.Log($"卸载中... {(unloadProgress * 200):F1}%");
        }

        yield return null;
    }

    Debug.Log("场景切换完成！");
}
```

### 跨场景场景管理器

#### 场景管理器设计

```csharp
public class SceneManager : MonoBehaviour
{
    [Header("加载UI")]
    public GameObject loadingPanel;
    public Slider progressSlider;
    public Text statusText;

    private static SceneManager instance;

    private void Awake()
    {
        // 确保跨场景存在
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 切换到指定场景
    /// </summary>
    public void LoadScene(string sceneName)
    {
        LoadSceneKit.LoadSceneAsync(sceneName, OnSceneChangeStart, OnSceneLoadComplete);
    }

    /// <summary>
    /// UniTask版本场景切换（推荐）
    /// </summary>
    public async void LoadSceneUniTask(string sceneName)
    {
        try
        {
            bool success = await LoadSceneKit.LoadSceneAsyncTask(sceneName, OnSceneChangeStart, OnSceneLoadComplete);

            // 可以在这里执行后续逻辑
            if (success)
            {
                Debug.Log("UniTask场景切换完成");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"UniTask场景切换异常: {ex.Message}");
            HideLoadingUI();
        }
    }

    private void OnSceneChangeStart()
    {
        ShowLoadingUI();
        StartCoroutine(UpdateProgress());
    }

    private void OnSceneLoadComplete(bool success)
    {
        HideLoadingUI();

        if (success)
        {
            Debug.Log($"成功切换到场景: {LoadSceneKit.GetCurrentSceneName()}");
        }
        else
        {
            Debug.LogError("场景切换失败！");
        }
    }

    private void ShowLoadingUI()
    {
        loadingPanel.SetActive(true);
    }

    private void HideLoadingUI()
    {
        loadingPanel.SetActive(false);
    }

    private IEnumerator UpdateProgress()
    {
        while (LoadSceneKit.IsProcessing)
        {
            progressSlider.value = LoadSceneKit.TotalProgress;
            statusText.text = $"加载中... {(LoadSceneKit.TotalProgress * 100):F0}%";
            yield return null;
        }
    }
}
```

---

## UniTask 使用指南 ⭐

### 为什么选择 UniTask？

1. **性能优势**: 比传统协程减少约 20-30%的内存分配
2. **更好的语法**: 使用 async/await，代码更清晰
3. **异常处理**: 支持 try-catch，调试更容易
4. **功能丰富**: 支持取消、超时、进度报告等高级功能

### UniTask 最佳实践

#### 1. 基础用法对比

```csharp
// ❌ 传统协程版本
IEnumerator LoadSceneCoroutine()
{
    LoadSceneKit.LoadSceneAsync("NextScene", null, (success) => {
        if (success) Debug.Log("加载完成");
    });
    yield return null; // 无法直接等待加载完成
}

// ✅ UniTask版本
async UniTask LoadSceneUniTask()
{
    try
    {
        bool success = await LoadSceneKit.LoadSceneAsyncTask("NextScene");
        if (success) Debug.Log("加载完成");
        // 可以继续执行后续逻辑
    }
    catch (Exception ex)
    {
        Debug.LogError($"加载失败: {ex.Message}");
    }
}
```

#### 2. 高级用法示例

```csharp
public class AdvancedSceneManager : MonoBehaviour
{
    [SerializeField] private float timeoutSeconds = 30f;

    /// <summary>
    /// 带超时的场景切换
    /// </summary>
    public async UniTask<bool> LoadSceneWithTimeout(string sceneName)
    {
        try
        {
            // 设置超时限制
            var timeoutToken = this.GetCancellationTokenOnDestroy();
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken: timeoutToken);
            var loadTask = LoadSceneKit.LoadSceneAsyncTask(sceneName);

            // 等待任一任务完成
            var result = await UniTask.WhenAny(loadTask, timeoutTask);

            if (result.winArgumentIndex == 0)
            {
                // 加载任务完成
                return result.result1;
            }
            else
            {
                // 超时
                Debug.LogError($"场景加载超时: {sceneName}");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("场景加载被取消");
            return false;
        }
    }

    /// <summary>
    /// 连续加载多个场景
    /// </summary>
    public async UniTask LoadScenesSequentially(string[] sceneNames)
    {
        foreach (string sceneName in sceneNames)
        {
            Debug.Log($"开始加载场景: {sceneName}");

            bool success = await LoadSceneKit.LoadSceneAsyncTask(sceneName);

            if (!success)
            {
                Debug.LogError($"场景 {sceneName} 加载失败，停止后续加载");
                break;
            }

            // 可以在这里添加场景间的延迟
            await UniTask.Delay(1000); // 等待1秒
        }
    }

    /// <summary>
    /// 并行预加载场景（仅限单人游戏）
    /// </summary>
    public async UniTask PreloadScenes(string[] sceneNames)
    {
        var tasks = sceneNames.Select(sceneName =>
            LoadSceneKit.LoadSceneAsyncTask(sceneName)
        );

        var results = await UniTask.WhenAll(tasks);

        for (int i = 0; i < results.Length; i++)
        {
            Debug.Log($"场景 {sceneNames[i]} 预加载结果: {results[i]}");
        }
    }
}
```

---

## 示例代码

项目包含一个简单的示例脚本：

### SimpleLoadSceneExample.cs - 简单使用示例

- 基础的键盘控制场景切换
- 简化的事件处理
- 进度查询示例
- 适合快速上手和学习

### 运行示例的步骤

1. **准备测试场景**

   - 创建至少两个测试场景
   - 将场景添加到 Build Settings 中

2. **设置示例脚本**

   - 将 SimpleLoadSceneExample 脚本添加到 GameObject
   - 配置目标场景名称

3. **测试功能**

   - 运行场景，按空格键切换场景
   - 按 P 键查看当前进度信息
   - 观察控制台输出

4. **测试功能**

   - 运行场景，按空格键切换场景
   - 按 P 键查看当前进度信息
   - 观察控制台输出

---

## 最佳实践

### 1. 选择合适的加载方式

```csharp
// ⭐ 最推荐：UniTask异步加载（性能最佳，代码最清晰）
bool success = await LoadSceneKit.LoadSceneAsyncTask("LargeScene", ShowLoading, OnComplete);

// ✅ 推荐：传统异步加载（兼容旧代码）
LoadSceneKit.LoadSceneAsync("LargeScene", ShowLoading, OnComplete);

// ⚠️  谨慎：同步加载（可能卡顿，仅适用于小场景）
LoadSceneKit.LoadScene("SmallScene", ShowLoading, OnComplete);
```

### 2. UniTask vs 协程选择指南

```csharp
// ✅ 使用UniTask的场景
// - 需要等待场景加载完成后执行后续逻辑
// - 需要异常处理和错误恢复
// - 性能敏感的应用
// - 新项目开发

public async UniTask LoadAndInitialize()
{
    try
    {
        bool success = await LoadSceneKit.LoadSceneAsyncTask("GameScene");
        if (success)
        {
            await InitializeGameSystems(); // 可以继续等待其他异步操作
            StartGame();
        }
    }
    catch (Exception ex)
    {
        HandleLoadError(ex);
    }
}

// ✅ 使用协程的场景
// - 维护旧代码
// - 不需要等待加载完成
// - 简单的fire-and-forget操作

public void LoadSceneSimple()
{
    LoadSceneKit.LoadSceneAsync("NextScene");
}
```

### 3. 合理使用事件回调

```csharp
// ✅ 跨场景场景管理器
public class GlobalSceneManager : MonoBehaviour
{
    public void SwitchScene(string sceneName)
    {
        LoadSceneKit.LoadSceneAsync(sceneName,
            onChangeScene: () => ShowGlobalLoadingUI(),
            onComplete: (success) => HideGlobalLoadingUI());
    }
}

// ❌ 不要在即将销毁的对象中处理回调
public class LocalController : MonoBehaviour
{
    public void BadExample()
    {
        LoadSceneKit.LoadSceneAsync("NextScene",
            onComplete: (success) => {
                // 这个对象可能已经被销毁了！
                this.HandleComplete(success);
            });
    }
}
```

### 3. 进度监控最佳实践

```csharp
// ✅ 使用协程监控进度
private IEnumerator MonitorProgress()
{
    while (LoadSceneKit.IsProcessing)
    {
        UpdateProgressUI(LoadSceneKit.TotalProgress);
        yield return null; // 每帧更新一次就够了
    }
}

// ❌ 不要在Update中频繁查询
private void Update()
{
    // 避免每帧都执行复杂逻辑
    if (LoadSceneKit.IsProcessing)
    {
        // 复杂的UI更新逻辑...
    }
}
```

### 4. 错误处理与异常管理

**传统协程版本：**

```csharp
LoadSceneKit.LoadSceneAsync(sceneName,
    onChangeScene: () => ShowLoading(),
    onComplete: (success) => {
        HideLoading();

        if (!success)
        {
            // 处理加载失败
            ShowErrorDialog("场景加载失败，请重试");
        }
    });
```

**UniTask 版本（推荐）：**

```csharp
public async UniTask LoadSceneWithErrorHandling(string sceneName)
{
    try
    {
        ShowLoading();
        bool success = await LoadSceneKit.LoadSceneAsyncTask(sceneName);

        if (!success)
        {
            ShowErrorDialog("场景加载失败，请重试");
        }
    }
    catch (OperationCanceledException)
    {
        Debug.Log("场景加载被取消");
    }
    catch (Exception ex)
    {
        Debug.LogError($"场景加载异常: {ex.Message}");
        ShowErrorDialog($"场景加载出错: {ex.Message}");
    }
    finally
    {
        HideLoading(); // 确保UI被隐藏
    }
}
```

### 5. 性能优化建议

```csharp
// ✅ 推荐：预加载关键场景
public async UniTask PreloadCriticalScenes()
{
    string[] criticalScenes = { "MainMenu", "GameScene", "LoadingScene" };

    foreach (string sceneName in criticalScenes)
    {
        try
        {
            await LoadSceneKit.LoadSceneAsyncTask(sceneName);
            Debug.Log($"预加载完成: {sceneName}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"预加载失败: {sceneName}, 错误: {ex.Message}");
        }
    }
}

// ✅ 使用对象池管理UI
public class LoadingUIPool : MonoBehaviour
{
    private Queue<LoadingPanel> pool = new Queue<LoadingPanel>();

    public LoadingPanel GetLoadingPanel()
    {
        return pool.Count > 0 ? pool.Dequeue() : CreateNewPanel();
    }

    public void ReturnLoadingPanel(LoadingPanel panel)
    {
        panel.Reset();
        pool.Enqueue(panel);
    }
}
```

---

## 注意事项

1. **场景名称准确性**

   - 确保目标场景名称正确且已添加到 Build Settings
   - 区分大小写，确保名称完全匹配

2. **避免重复操作**

   - 在场景切换过程中不要重复调用加载方法
   - 使用 `IsProcessing`属性检查当前状态

3. **UniTask 依赖性**

   - 使用 UniTask 功能需要安装 Cysharp.UniTask 包
   - 在项目中添加 `using Cysharp.Threading.Tasks;`

4. **内存管理**

   - 系统会自动清理内部引用，无需手动管理
   - UniTask 版本具有更好的内存效率

5. **异常处理**

   - UniTask 版本支持原生 try-catch 异常处理
   - 协程版本的错误通过 onComplete 回调的 bool 参数表示

6. **取消操作**

   - UniTask 版本支持取消令牌(CancellationToken)
   - 组件销毁时会自动取消相关操作

7. **线程安全**

   - LoadSceneKit 在主线程上运行，是线程安全的
   - 回调和事件都在主线程中执行

8. **兼容性**

   - 同时提供协程和 UniTask 两种实现
   - 可以根据项目需求选择合适的版本
   - 两种版本可以在同一项目中混用
   - 确保回调中不会持有大量对象引用

9. **跨场景对象**

   - 使用 DontDestroyOnLoad 的对象来处理场景切换事件
   - 避免在即将销毁的对象中处理回调

10. **进度精确性**

    - Unity 的异步加载进度可能不是线性的
    - 90%以上的进度可能停留较长时间

11. **异常处理**

    - 系统内置了基础的异常处理
    - 建议在回调中添加额外的错误处理逻辑

---

📝 **注意**: 本工具基于 Unity SceneManager API，确保目标场景已正确添加到 Build Settings 中。推荐在实际项目中使用异步加载以获得最佳用户体验。
