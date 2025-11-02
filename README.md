# FFramework 使用指南

## 什么是 FFramework？

FFramework 是一个**简单易用**的 Unity 游戏开发框架，采用 **MVC 架构模式**，让你的代码更清晰、更好维护。

### 核心优势

- **简单分层**：Model 管数据，View 管界面，清晰明了
- **事件驱动**：模块间松耦合，扩展性强
- **自动管理**：生命周期自动处理，防止内存泄漏
- **丰富工具**：单例、对象池、定时器等开箱即用
- **快速开发**：减少重复工作，专注业务逻辑

---

## 三层架构

### 简单理解

```
┌─────────────────┐
│  View 视图层     │ ← 处理UI显示和用户交互
├─────────────────┤
│  Model 数据层    │ ← 管理游戏数据和状态
├─────────────────┤
│  Utility 工具层  │ ← 提供各种工具服务
└─────────────────┘
        ↕️
   EventSystem 事件系统
```

### 各层职责

#### View 层 (ViewController)

- **做什么**：UI 界面、按钮点击、界面逻辑
- **可以调用**：Model、工具、事件系统
- **不能做**：直接存储游戏数据

#### Model 层

- **做什么**：存储数据、处理业务逻辑
- **可以调用**：工具、事件系统
- **不能做**：直接操作 UI 界面

#### Utility 层

- **做什么**：提供工具函数和服务
- **特点**：独立模块，不依赖其他层

---

## 快速开始

### 第一步：创建数据模型 (Model)

```csharp
// 玩家数据模型
public class PlayerModel : BaseModel
{
    // 绑定属性 - 数据变化时自动通知UI
    public BindableProperty<int> Level = new BindableProperty<int>(1);
    public BindableProperty<int> Gold = new BindableProperty<int>(100);

    protected override void OnInitialize()
    {
        Debug.Log("玩家数据初始化完成");
    }

    // 业务逻辑方法
    public void AddGold(int amount)
    {
        Gold.Value += amount;
        SendEvent("GoldChanged", Gold.Value); // 发送事件通知其他模块
    }

    public void LevelUp()
    {
        Level.Value++;
        SendEvent("PlayerLevelUp", Level.Value);
    }
}
```

### 第二步：创建视图控制器 (ViewController)

```csharp
// UI控制器
public class PlayerUIController : BaseViewController
{
    [SerializeField] private Text levelText;
    [SerializeField] private Text goldText;
    [SerializeField] private Button addGoldButton;
    [SerializeField] private Button levelUpButton;

    private PlayerModel playerModel;

    protected override void OnInitialize()
    {
        // 获取数据模型
        playerModel = GetModel<PlayerModel>();

        // 数据绑定 - 数据变化自动更新UI
        playerModel.Level.Register(OnLevelChanged)
                  .UnRegisterWhenGameObjectDestroy(gameObject);

        playerModel.Gold.Register(OnGoldChanged)
                 .UnRegisterWhenGameObjectDestroy(gameObject);

        // 按钮事件
        addGoldButton.onClick.AddListener(() => playerModel.AddGold(10));
        levelUpButton.onClick.AddListener(() => playerModel.LevelUp());

        // 注册全局事件
        RegisterEvent("GameStart", OnGameStart);
        RegisterEvent<int>("PlayerLevelUp", OnPlayerLevelUpEvent);
    }

    private void OnLevelChanged(int newLevel)
    {
        levelText.text = $"等级: {newLevel}";
    }

    private void OnGoldChanged(int newGold)
    {
        goldText.text = $"金币: {newGold}";
    }

    private void OnGameStart()
    {
        Debug.Log("游戏开始！");
    }

    private void OnPlayerLevelUpEvent(int level)
    {
        Debug.Log($"恭喜升级到 {level} 级！");
        // 可以播放升级特效等
    }
}
```

### 第三步：注册组件

```csharp
// 游戏管理器
public class GameManager : MonoBehaviour
{
    void Start()
    {
        // 注册数据模型
        ArchitectureManager.Instance.RegisterModel<PlayerModel>();

        // 注册视图控制器（如果这个GameObject上有PlayerUIController组件）
        var uiController = GetComponent<PlayerUIController>();
        if (uiController != null)
        {
            ArchitectureManager.Instance.RegisterViewController<PlayerUIController>(uiController);
        }

        // 触发游戏开始事件
        EventSystem.Instance.TriggerEvent("GameStart");
    }
}
```

---

## 核心功能详解

### 绑定属性 (BindableProperty)

让数据变化自动通知 UI 更新：

```csharp
// 声明绑定属性
public BindableProperty<int> Health = new BindableProperty<int>(100);

// 监听变化（自动注销版本，推荐使用）
Health.Register(value => healthBar.fillAmount = value / 100f)
      .UnRegisterWhenGameObjectDestroy(gameObject);

// 修改数据（自动触发UI更新）
Health.Value = 50; // healthBar 会自动更新显示
```

### 事件系统

模块间通信的最佳方式：

```csharp
// 发送事件
SendEvent("PlayerDied");                    // 无参数事件
SendEvent("ScoreChanged", 1000);           // 带参数事件
SendEvent("PlayerMove", new Vector3(1,0,0)); // 复杂参数事件

// 接收事件（推荐使用自动注销版本）
RegisterEvent("PlayerDied", OnPlayerDied);
RegisterEvent<int>("ScoreChanged", OnScoreChanged);
RegisterEvent<Vector3>("PlayerMove", OnPlayerMove);

// 事件处理方法
private void OnPlayerDied()
{
    Debug.Log("玩家死亡，游戏结束");
    // 显示游戏结束界面
}

private void OnScoreChanged(int score)
{
    Debug.Log($"分数更新: {score}");
    // 更新分数显示
}

private void OnPlayerMove(Vector3 position)
{
    Debug.Log($"玩家移动到: {position}");
    // 更新相机跟随等
}
```

### 单例模式

全局访问的便捷方式：

```csharp
// 普通类单例
public class GameConfig : Singleton<GameConfig>
{
    public float MasterVolume = 1.0f;
    public int Difficulty = 1;

    protected override void OnSingletonInit()
    {
        Debug.Log("游戏配置初始化");
    }
}

// MonoBehaviour 单例
public class AudioManager : SingletonMono<AudioManager>
{
    public void PlaySound(string soundName)
    {
        Debug.Log($"播放音效: {soundName}");
    }

    public void PlayMusic(string musicName)
    {
        Debug.Log($"播放背景音乐: {musicName}");
    }
}

// 使用方式
GameConfig.Instance.MasterVolume = 0.8f;
AudioManager.Instance.PlaySound("ButtonClick");
```

---

## 常用工具

框架内置了丰富的工具套件，让开发更高效：

### TimerKit - 定时器工具

```csharp
// 延迟执行
TimerKit.DelayInvoke(2.0f, () => Debug.Log("2秒后执行"));

// 循环执行
TimerKit.Loop(1.0f, () => Debug.Log("每秒执行一次"));

// 倒计时
TimerKit.CountDown(10.0f,
    timeLeft => Debug.Log($"剩余时间: {timeLeft}"),  // 每秒回调
    () => Debug.Log("倒计时结束"));                   // 结束回调
```

### EventKit - UI 事件扩展

```csharp
// 简化按钮事件
button.OnClickEvent(() => Debug.Log("按钮点击"));

// 拖拽事件
image.OnBeginDragEvent(data => Debug.Log("开始拖拽"));
image.OnDragEvent(data => Debug.Log("拖拽中"));
image.OnEndDragEvent(data => Debug.Log("结束拖拽"));

// 鼠标事件
image.OnPointerEnterEvent(data => Debug.Log("鼠标进入"));
image.OnPointerExitEvent(data => Debug.Log("鼠标离开"));
```

### ObjectPool - 对象池

```csharp
// 生成对象（从池中获取或创建新的）
GameObject bullet = ObjectPoolKit.Spawn("BulletPrefab");
bullet.transform.position = firePoint.position;

// 回收对象（返回池中复用）
ObjectPoolKit.Recycle(bullet);

// 预热对象池（提前创建对象）
ObjectPoolKit.Preload("BulletPrefab", 50);
```

### DataSave - 数据存储

```csharp
// 保存数据
var playerData = new PlayerData { Level = 5, Gold = 1000 };
DataSaveKit.SaveData("PlayerSave", playerData);

// 加载数据
var loadedData = DataSaveKit.LoadData<PlayerData>("PlayerSave");
if (loadedData != null)
{
    Debug.Log($"加载玩家数据: 等级{loadedData.Level}, 金币{loadedData.Gold}");
}

// 检查存档是否存在
if (DataSaveKit.HasData("PlayerSave"))
{
    // 存档存在，可以继续游戏
}
```

### SceneLoader - 场景加载

```csharp
// 异步加载场景（带进度显示）
LoadSceneKit.LoadSceneAsync("GameLevel",
    progress => Debug.Log($"加载进度: {progress * 100}%"),
    () => Debug.Log("场景加载完成"));

// 简单场景切换
LoadSceneKit.LoadScene("MainMenu");
```

---

## 最佳实践

### 推荐做法

```csharp
// 使用自动注销，避免内存泄漏
playerModel.Health.Register(OnHealthChanged)
          .UnRegisterWhenGameObjectDestroy(gameObject);

// Model只负责数据处理，不直接操作UI
public class PlayerModel : BaseModel
{
    public void TakeDamage(int damage)
    {
        Health.Value = Mathf.Max(0, Health.Value - damage);
        if (Health.Value <= 0)
        {
            SendEvent("PlayerDied");  // 通过事件通知，不直接操作UI
        }
    }
}

// ViewController只负责UI逻辑
public class HealthUIController : BaseViewController
{
    private void OnHealthChanged(int health)
    {
        healthSlider.value = health / 100f;  // 只处理UI更新
        if (health <= 20)
        {
            healthBar.color = Color.red;  // 低血量警告
        }
    }
}

// 使用事件进行模块通信
SendEvent("ItemPickup", itemData);     // 发送事件
RegisterEvent<ItemData>("ItemPickup", OnItemPickup); // 接收事件
```

### 避免做法

```csharp
// ❌ 忘记注销事件导致内存泄漏
EventSystem.Instance.RegisterEvent("GameStart", OnGameStart); // 没有自动注销

// ❌ Model直接操作UI
public class PlayerModel : BaseModel
{
    public void TakeDamage(int damage)
    {
        Health.Value -= damage;
        healthSlider.value = Health.Value; // Model不应该知道UI组件
    }
}

// ❌ 层级混乱
public class PlayerModel : BaseModel
{
    public void UpdateHealthBar() { } // Model不应该有UI相关方法
}

// ❌ 直接引用导致紧耦合
public class PlayerController : BaseViewController
{
    public EnemyController enemy; // 应该通过事件通信，不要直接引用
}
```

### 推荐项目结构

```
Assets/Scripts/
├── Models/              # 数据层
│   ├── PlayerModel.cs
│   ├── GameModel.cs
│   └── InventoryModel.cs
├── Views/               # 视图层
│   ├── UI/
│   │   ├── PlayerUIController.cs
│   │   └── GameUIController.cs
│   └── Game/
│       ├── PlayerController.cs
│       └── EnemyController.cs
├── Managers/            # 管理器
│   ├── GameManager.cs
│   └── AudioManager.cs
├── Data/                # 数据定义
│   ├── PlayerData.cs
│   └── ItemData.cs
└── Events/              # 事件定义
    └── GameEvents.cs
```

---

## 完整示例：简单 RPG 系统

以下是使用 FFramework 实现 RPG 升级系统的完整示例：

### 数据层 (Model)

```csharp
// 玩家数据模型
public class PlayerModel : BaseModel
{
    public BindableProperty<string> Name = new BindableProperty<string>("勇者");
    public BindableProperty<int> Level = new BindableProperty<int>(1);
    public BindableProperty<int> Exp = new BindableProperty<int>(0);
    public BindableProperty<int> MaxExp = new BindableProperty<int>(100);
    public BindableProperty<int> Gold = new BindableProperty<int>(50);

    protected override void OnInitialize()
    {
        Debug.Log("玩家数据初始化完成");
    }

    public void AddExp(int amount)
    {
        Exp.Value += amount;
        CheckLevelUp();
        SendEvent("ExpGained", amount);
    }

    public void AddGold(int amount)
    {
        Gold.Value += amount;
        SendEvent("GoldChanged", Gold.Value);
    }

    private void CheckLevelUp()
    {
        if (Exp.Value >= MaxExp.Value)
        {
            Level.Value++;
            Exp.Value = Exp.Value - MaxExp.Value;
            MaxExp.Value = Level.Value * 100; // 每级需要更多经验

            SendEvent("PlayerLevelUp", Level.Value);
            Debug.Log($"恭喜升级到 {Level.Value} 级！");
        }
    }
}

// 游戏状态模型
public class GameModel : BaseModel
{
    public BindableProperty<bool> GameRunning = new BindableProperty<bool>(false);
    public BindableProperty<int> Score = new BindableProperty<int>(0);
    public BindableProperty<float> GameTime = new BindableProperty<float>(0);

    public void StartGame()
    {
        GameRunning.Value = true;
        Score.Value = 0;
        GameTime.Value = 0;
        SendEvent("GameStarted");
    }

    public void EndGame()
    {
        GameRunning.Value = false;
        SendEvent("GameEnded", Score.Value);
    }

    public void AddScore(int points)
    {
        if (GameRunning.Value)
        {
            Score.Value += points;
            SendEvent("ScoreChanged", Score.Value);
        }
    }
}
```

### 视图层 (ViewController)

```csharp
// 玩家UI控制器
public class PlayerUIController : BaseViewController
{
    [Header("玩家信息显示")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text goldText;

    [Header("经验条")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private Text expText;

    [Header("操作按钮")]
    [SerializeField] private Button fightButton;
    [SerializeField] private Button workButton;

    private PlayerModel playerModel;
    private GameModel gameModel;

    protected override void OnInitialize()
    {
        // 获取数据模型
        playerModel = GetModel<PlayerModel>();
        gameModel = GetModel<GameModel>();

        // 绑定玩家数据变化
        BindPlayerData();

        // 绑定按钮事件
        SetupButtons();

        // 注册全局事件
        RegisterGameEvents();
    }

    private void BindPlayerData()
    {
        // 玩家基础信息
        playerModel.Name.Register(name => nameText.text = name)
                  .UnRegisterWhenGameObjectDestroy(gameObject);

        playerModel.Level.Register(level => levelText.text = $"Lv.{level}")
                  .UnRegisterWhenGameObjectDestroy(gameObject);

        playerModel.Gold.Register(gold => goldText.text = $"金币: {gold}")
                 .UnRegisterWhenGameObjectDestroy(gameObject);

        // 经验条
        playerModel.Exp.Register(UpdateExpDisplay)
                .UnRegisterWhenGameObjectDestroy(gameObject);
        playerModel.MaxExp.Register(UpdateExpDisplay)
                   .UnRegisterWhenGameObjectDestroy(gameObject);
    }

    private void SetupButtons()
    {
        fightButton.OnClickEvent(() => {
            // 战斗获得经验和金币
            playerModel.AddExp(25);
            playerModel.AddGold(10);
            gameModel.AddScore(100);
        });

        workButton.OnClickEvent(() => {
            // 工作只获得金币
            playerModel.AddGold(5);
            gameModel.AddScore(20);
        });
    }

    private void RegisterGameEvents()
    {
        RegisterEvent<int>("PlayerLevelUp", OnPlayerLevelUp);
        RegisterEvent<int>("ExpGained", OnExpGained);
        RegisterEvent("GameStarted", OnGameStarted);
        RegisterEvent<int>("GameEnded", OnGameEnded);
    }

    private void UpdateExpDisplay()
    {
        expSlider.maxValue = playerModel.MaxExp.Value;
        expSlider.value = playerModel.Exp.Value;
        expText.text = $"{playerModel.Exp.Value}/{playerModel.MaxExp.Value}";
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        // 播放升级特效
        Debug.Log($"🎉 升级到 {newLevel} 级!");

        // 可以在这里播放升级动画、音效等
        ShowLevelUpEffect();
    }

    private void OnExpGained(int exp)
    {
        Debug.Log($"获得经验: +{exp}");
        // 可以显示经验获得的UI提示
    }

    private void OnGameStarted()
    {
        Debug.Log("游戏开始！");
        fightButton.interactable = true;
        workButton.interactable = true;
    }

    private void OnGameEnded(int finalScore)
    {
        Debug.Log($"游戏结束！最终分数: {finalScore}");
        fightButton.interactable = false;
        workButton.interactable = false;
    }

    private void ShowLevelUpEffect()
    {
        // 简单的升级效果
        transform.localScale = Vector3.one * 1.2f;

        // 0.5秒后恢复原始大小
        TimerKit.DelayInvoke(0.5f, () => {
            transform.localScale = Vector3.one;
        });
    }
}

// 游戏控制器
public class GameController : BaseViewController
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button endButton;

    private GameModel gameModel;

    protected override void OnInitialize()
    {
        gameModel = GetModel<GameModel>();

        // 绑定游戏数据
        gameModel.Score.Register(score => scoreText.text = $"分数: {score}")
                .UnRegisterWhenGameObjectDestroy(gameObject);

        gameModel.GameTime.Register(time => timeText.text = $"时间: {time:F1}s")
                  .UnRegisterWhenGameObjectDestroy(gameObject);

        // 按钮事件
        startButton.OnClickEvent(() => gameModel.StartGame());
        endButton.OnClickEvent(() => gameModel.EndGame());

        // 游戏运行时更新时间
        this.RegisterUpdate(() => {
            if (gameModel.GameRunning.Value)
            {
                gameModel.GameTime.Value += Time.deltaTime;
            }
        });
    }
}
```

### 游戏初始化

```csharp
public class GameManager : MonoBehaviour
{
    void Start()
    {
        InitializeFramework();
    }

    private void InitializeFramework()
    {
        // 注册数据模型
        ArchitectureManager.Instance.RegisterModel<PlayerModel>();
        ArchitectureManager.Instance.RegisterModel<GameModel>();

        // 注册视图控制器（自动查找场景中的组件）
        var playerUI = FindObjectOfType<PlayerUIController>();
        if (playerUI != null)
        {
            ArchitectureManager.Instance.RegisterViewController<PlayerUIController>(playerUI);
        }

        var gameController = FindObjectOfType<GameController>();
        if (gameController != null)
        {
            ArchitectureManager.Instance.RegisterViewController<GameController>(gameController);
        }

        Debug.Log("游戏框架初始化完成！");
    }
}
```

---

## 更多资源

### 详细文档

- **事件系统**：[EventSystem 使用指南](./FFramework/Utility/EventSystem/EventSystemDoc.md)
- **UI 系统**：[UIKit 完整文档](./FFramework/Utility/UIKit/UIKit_Documentation.md)
- **对象池**：[PoolKit 使用说明](./FFramework/Utility/PoolKit/PoolKit_Documentation.md)
- **状态机**：[FSMKit 状态机指南](./FFramework/Utility/FSMKit/FSM_Documentation.md)
- **定时器**：[TimerKit 定时器工具](./FFramework/Utility/TimerKit/TimerManager_Documentation.md)

### 示例项目

- 查看 `Examples` 目录获取更多完整示例
- 每个工具套件都包含独立的使用示例

### 技术支持

- 遇到问题请查看文档或提交 Issue
- 欢迎贡献代码和改进建议

---

## 总结

**FFramework 让游戏开发变得更简单：**

- **清晰架构**：MVC 分层，职责明确
- **高效开发**：丰富工具，减少重复工作
- **稳定可靠**：自动管理生命周期，避免内存泄漏
- **易于扩展**：模块化设计，方便定制和扩展
- **完善文档**：详细说明，快速上手

**立即开始使用 FFramework，让你的游戏开发更轻松！**

---

_FFramework - 简单、高效、可靠的 Unity 游戏开发框架_
