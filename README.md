# FFramework Unity 游戏开发框架

轻量、模块化、可扩展的 Unity 游戏开发基础框架

---

# FFramework Unity 游戏开发框架

轻量、模块化、可扩展的 Unity 游戏开发基础框架

---

## 简介

FFramework 是一个基于 MVC 架构的 Unity 游戏开发框架，专注于清晰结构、快速迭代和易维护。框架内置数据绑定、生命周期调度、事件系统、单例、UI、状态机、对象池、计时器、资源加载等常用模块，助力高效开发。

## 主要特性

- MVC 架构，结构清晰
- 数据绑定（BindableProperty）
- 生命周期统一调度（GameMonoBehavior）
- 全局事件系统（EventSystem）
- 单例模式（普通/MonoBehaviour）
- 常用工具模块（UI、FSM、对象池、计时器、资源加载等）

## 快速开始

1. **导入框架**：将 `FFramework-main/FFramework` 拖入项目 `Assets` 目录。
2. **创建 Model/ViewController/启动架构**：参考下方“示例：玩家系统”完整代码。
3. **数据驱动 UI**：通过修改 Model 的数据，自动驱动 UI 响应。

   - 将 `FFramework-main/FFramework` 拖入项目 `Assets` 目录。

4. **创建 Model**

   ```csharp
   using FFramework.Architecture;
   public class PlayerModel : BaseModel {
       public BindableProperty<int> Health = new(100);
       public BindableProperty<int> MaxHealth = new(100);
       public BindableProperty<int> Level = new(1);
       public BindableProperty<int> Experience = new(0);
       public BindableProperty<string> Name = new("Hero");
       protected override void OnInitialize() {
           Health.Register(OnHealthChanged, false);
       }
       private void OnHealthChanged(int newHealth) {
           if (newHealth <= 0) SendEvent("PlayerDied");
       }
   }
   ```

5. **创建 ViewController**

   ```csharp
   using FFramework.Architecture;
   using UnityEngine;
   public class PlayerHUDViewController : BaseViewController {
       private PlayerModel playerModel;
       protected override void OnInitialize() {
           playerModel = GetModel<PlayerModel>();
           playerModel.Health.Register(UpdateHealthUI).UnRegisterWhenGameObjectDestroy(gameObject);
       }
       private void UpdateHealthUI(int hp) {
           Debug.Log($"HP => {hp}");
       }
   }
   ```

6. **启动架构**

   ```csharp
   using FFramework.Architecture;
   using UnityEngine;
   public class GameEntry : MonoBehaviour {
       public GameObject playerHUDPrefab;
       void Start() {
           ArchitectureManager.Instance.RegisterModel<PlayerModel>();
           ArchitectureManager.Instance.RegisterViewController<PlayerHUDViewController>(playerHUDPrefab);
       }
   }
   ```

7. **数据驱动 UI**

   ```csharp
   var model = ArchitectureManager.Instance.GetModel<PlayerModel>();
   model.Health.Value = 80; // 控制台输出：HP => 80
   ```

## 最佳实践

**推荐目录结构：**

```
Assets/
  Scripts/
    Architecture/   // 框架核心（只读）
    Models/
    ViewControllers/
    Systems/        // 业务系统（如背包/战斗等）
    Managers/
    UI/
    Utilities/
  Prefabs/
  Scenes/
  UI/
```

**建议：**

- Model 只放数据与纯逻辑，不引用具体 UI
- ViewController 只做显示与输入协调，不做重业务逻辑
- 尽量通过引用或事件传递，避免随意查找 GameObject
- 合理使用事件与数据绑定，避免双向强依赖
- 需要频繁帧更新的逻辑统一放入 GameMonoBehavior 注册，便于集中管理

## 常见问题（FAQ）

**Q: ViewController 必须和场景中的 GameObject 绑定吗？**
A: 是的，RegisterViewController 会基于传入的 GameObject 添加组件并初始化。

**Q: 可以跨场景共享 Model 吗？**
A: 可以。将 ArchitectureManager 所在对象设为 DontDestroyOnLoad，或在新场景重新注册并迁移数据。

**Q: BindableProperty 会不会有性能问题？**
A: 仅在值变化时触发回调，内部有相等性判断。大批量写入时可合并更新。

**Q: 事件名如何管理？**
A: 建议集中在一个静态类中常量化，避免魔法字符串。

**Q: 如何与 ScriptableObject 配合？**
A: 可用 ScriptableObject 保存初始配置，Model 初始化时读取注入。

## 示例：玩家系统

```csharp
// 1. PlayerModel
using FFramework.Architecture;
public class PlayerModel : BaseModel {
    public BindableProperty<int> Health = new(100);
    public BindableProperty<int> MaxHealth = new(100);
    public BindableProperty<int> Level = new(1);
    public BindableProperty<int> Experience = new(0);
    public BindableProperty<string> PlayerName = new("玩家");
    protected override void OnInitialize() {
        Health.Register(OnHealthChanged, false);
    }
    private void OnHealthChanged(int newHealth) {
        if (newHealth <= 0) SendEvent("PlayerDied");
    }
    public void TakeDamage(int damage) {
        Health.Value = Mathf.Max(0, Health.Value - damage);
    }
    public void Heal(int amount) {
        Health.Value = Mathf.Min(MaxHealth.Value, Health.Value + amount);
    }
    public void GainExperience(int exp) {
        Experience.Value += exp;
        CheckLevelUp();
    }
    private void CheckLevelUp() {
        int requiredExp = Level.Value * 100;
        if (Experience.Value >= requiredExp) {
            Level.Value++;
            Experience.Value -= requiredExp;
            SendEvent("PlayerLevelUp", Level.Value);
        }
    }
}

// 2. PlayerViewController
using FFramework.Architecture;
using UnityEngine;
using UnityEngine.UI;
public class PlayerViewController : BaseViewController {
    public Slider healthBar;
    public Text healthText;
    public Text levelText;
    public Text nameText;
    private PlayerModel playerModel;
    protected override void OnInitialize() {
        playerModel = GetModel<PlayerModel>();
        playerModel.Health.Register(UpdateHealthUI).UnRegisterWhenGameObjectDestroy(gameObject);
        playerModel.MaxHealth.Register(UpdateMaxHealthUI).UnRegisterWhenGameObjectDestroy(gameObject);
        playerModel.Level.Register(UpdateLevelUI).UnRegisterWhenGameObjectDestroy(gameObject);
        playerModel.PlayerName.Register(UpdateNameUI).UnRegisterWhenGameObjectDestroy(gameObject);
        RegisterEvent("PlayerDied", OnPlayerDied);
        RegisterEvent<int>("PlayerLevelUp", OnPlayerLevelUp);
    }
    private void UpdateHealthUI(int health) {
        if (healthBar != null)
            healthBar.value = (float)health / playerModel.MaxHealth.Value;
        if (healthText != null)
            healthText.text = $"{health}/{playerModel.MaxHealth.Value}";
    }
    private void UpdateMaxHealthUI(int maxHealth) {
        UpdateHealthUI(playerModel.Health.Value);
    }
    private void UpdateLevelUI(int level) {
        if (levelText != null)
            levelText.text = $"等级: {level}";
    }
    private void UpdateNameUI(string playerName) {
        if (nameText != null)
            nameText.text = playerName;
    }
    private void OnPlayerDied() {
        Debug.Log("玩家死亡!");
    }
    private void OnPlayerLevelUp(int newLevel) {
        Debug.Log($"恭喜升级到{newLevel}级!");
    }
}

// 3. GameManager
using FFramework.Architecture;
using UnityEngine;
public class GameManager : MonoBehaviour {
    public GameObject playerUIPanel;
    void Start() {
        InitializeArchitecture();
        StartGame();
    }
    private void InitializeArchitecture() {
        ArchitectureManager.Instance.RegisterModel<PlayerModel>();
        ArchitectureManager.Instance.RegisterViewController<PlayerViewController>(playerUIPanel);
    }
    private void StartGame() {
        var playerModel = ArchitectureManager.Instance.GetModel<PlayerModel>();
        playerModel.PlayerName.Value = "勇敢的冒险者";
    }
    [ContextMenu("测试受伤")]
    public void TestTakeDamage() {
        var playerModel = ArchitectureManager.Instance.GetModel<PlayerModel>();
        playerModel.TakeDamage(20);
    }
    [ContextMenu("测试治疗")]
    public void TestHeal() {
        var playerModel = ArchitectureManager.Instance.GetModel<PlayerModel>();
        playerModel.Heal(30);
    }
    [ContextMenu("测试获得经验")]
    public void TestGainExp() {
        var playerModel = ArchitectureManager.Instance.GetModel<PlayerModel>();
        playerModel.GainExperience(150);
    }
}

// PlayerModel、PlayerViewController、GameManager 示例请参考框架源码或上方“快速开始”部分。
```

````

## 核心概念概览

| 名称                      | 作用                         | 典型调用                 | 备注                      |
| ------------------------- | ---------------------------- | ------------------------ | ------------------------- |
| BaseModel                 | 业务与数据封装               | 继承并重写 OnInitialize  | 可通过 SendEvent 发送事件 |
| BaseViewController        | 视图/交互控制                | GetModel / RegisterEvent | 持有 GameObject           |
| ArchitectureManager       | 管理 Model 与 ViewController | Register / Get           | 单例 Mono                 |
| BindableProperty          | 数据绑定响应                 | Value 赋值触发回调       | 支持自动注销扩展          |
| GameMonoBehavior          | 全局 Update 分发             | RegisterUpdate 等        | 自动/手动注销两种         |
| EventSystem               | 全局事件通信                 | Trigger / Register       | 解耦模块                  |
| Singleton / SingletonMono | 单例基类                     | Instance                 | 普通/MonoBehaviour        |

---

## MVC 工作流示例（完整）

```csharp
// 1. Model
public class InventoryModel : BaseModel
{
    public BindableProperty<int> Coin = new BindableProperty<int>(0);
    public void AddCoin(int amount) => Coin.Value += amount;
}

// 2. ViewController
public class InventoryViewController : BaseViewController
{
    private InventoryModel model;
    protected override void OnInitialize()
    {
        model = GetModel<InventoryModel>();
        model.Coin.Register(UpdateCoin).UnRegisterWhenGameObjectDestroy(gameObject);
    }
    void UpdateCoin(int c) => Debug.Log($"Coin:{c}");
}

// 3. 入口
public class GameStartup : MonoBehaviour
{
    public GameObject inventoryViewPrefab;
    void Start()
    {
        ArchitectureManager.Instance.RegisterModel<InventoryModel>();
        ArchitectureManager.Instance.RegisterViewController<InventoryViewController>(inventoryViewPrefab);
        ArchitectureManager.Instance.GetModel<InventoryModel>().AddCoin(10);
    }
}
````

---

## 核心 API 速查表

| 分类                | 方法                                | 说明                               |
| ------------------- | ----------------------------------- | ---------------------------------- |
| ArchitectureManager | RegisterModel `<T>`()               | 注册并初始化一个 Model             |
|                     | GetModel `<T>`()                    | 获取已注册 Model                   |
|                     | RegisterViewController `<T>`(go)    | 绑定 GameObject 并初始化视图控制器 |
|                     | UnRegisterModel `<T>`()             | 注销 Model（释放资源）             |
| BindableProperty    | Register(callback, invokeNow=true)  | 监听值变化                         |
|                     | Value                               | 赋值触发通知                       |
|                     | UnRegisterWhenGameObjectDestroy(go) | 自动随 GameObject 销毁注销         |
| GameMonoBehavior    | RegisterUpdate/Fixed/Late           | 自动注销版本（传入 Component）     |
|                     | UnRegisterUpdate/Fixed/Late         | 手动注销                           |
| EventSystem         | TriggerEvent(name, obj?)            | 触发事件                           |
|                     | RegisterEvent(name, cb)             | 注册无参事件                       |
|                     | RegisterEvent `<T>`(name, cb)       | 注册有参事件                       |

---

## 数据绑定（BindableProperty）

### 监听与自动注销

```csharp
playerModel.Health.Register(OnHealthChanged) // 默认立即回调一次当前值
             .UnRegisterWhenGameObjectDestroy(gameObject);

void OnHealthChanged(int hp)
{
    // 更新 UI / 播放特效
}
```

### 手动注销

```csharp
var token = playerModel.Health.Register(OnHealthChanged);
token.UnRegister();
```

### 链式组合（示例：同时监听多个）

```csharp
playerModel.Health.Register(UpdateAll).UnRegisterWhenGameObjectDestroy(gameObject);
playerModel.MaxHealth.Register(UpdateAll).UnRegisterWhenGameObjectDestroy(gameObject);
```

---

## 生命周期与更新分发（GameMonoBehavior）

### 自动注销写法（推荐）

```csharp
public class EnemyAI : MonoBehaviour
{
    void Start()
    {
        this.RegisterUpdate(OnTick);
        this.RegisterFixedUpdate(OnFixed);
    }
    void OnTick(){ }
    void OnFixed(){ }
}
```

### 手动注册/注销

```csharp
GameMonoBehavior.Instance.RegisterUpdate(OnTick);
GameMonoBehavior.Instance.UnRegisterUpdate(OnTick);
```

---

## 单例模式

```csharp
public class ConfigManager : Singleton<ConfigManager>
{
    public string Version => "1.0.0";
}

public class AudioManager : SingletonMono<AudioManager>
{
    public void Play(string name){ /* ... */ }
}
```

---

## 典型模块速览

| 模块        | 作用                  | 入口类（示例）                    | 备注           |
| ----------- | --------------------- | --------------------------------- | -------------- |
| EventSystem | 全局事件发布/订阅     | EventSystem                       | 支持泛型参数   |
| UI System   | 面板管理 / 根节点     | UISystem / UIRoot / UIPanel       | Inspector 支持 |
| FSM / HSM   | 状态机 / 分层状态机   | FSMStateMachine / HSMStateMachine | 行为驱动逻辑   |
| ObjectPool  | 复用对象减少 GC       | ObjectPool                        | 支持预热       |
| Timer       | 延时/循环操作         | Timer                             | 支持取消与回调 |
| AssetLoad   | 资源加载（AB / 直读） | LoadAssetKit / ResLoad            | 可扩展策略     |
| Shake       | 摄像机抖动            | SmoothShake                       | 预设支持       |

> 建议为每个模块拆出独立文档详述（见“后续扩展”）。

---

## 最佳实践与目录建议

### 推荐目录

```
Assets/
  Scripts/
    Architecture/   // 框架核心（保持只读）
    Models/
    ViewControllers/
    Systems/        // 业务系统（背包/战斗/任务等）
    Managers/
    UI/
    Utilities/
```

### 建议

- Model 只放数据与纯逻辑，不引用具体 UI
- ViewController 不做重业务逻辑，只协调显示与输入
- 避免在任意代码随意查找 GameObject，尽量通过引用或事件
- 合理使用事件与数据绑定，避免双向强依赖
- 需要频繁帧更新的逻辑统一放入 GameMonoBehavior 注册，便于集中管理

---

## 常见问题（FAQ）

**Q: ViewController 必须和场景中的 GameObject 绑定吗？**
A: 是的，RegisterViewController 会基于传入的 GameObject 添加组件并初始化。

**Q: 可以跨场景共享 Model 吗？**
A: 可以。将 `ArchitectureManager` 所在对象设为 `DontDestroyOnLoad`（可在其 Awake 中处理），或在新场景重新注册并迁移数据。

**Q: BindableProperty 会不会产生性能问题？**
A: 仅在值变化时触发回调，内部做了相等性判断，频繁大批量写入时可考虑合并更新。

**Q: 事件名如何管理？**
A: 建议集中在一个静态类中常量化，避免魔法字符串。

**Q: 和 Unity 原生 ScriptableObject 配合方式？**
A: 可用 ScriptableObject 保存初始配置，Model 初始化时读取注入。

---
