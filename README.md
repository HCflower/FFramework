# FFramework 框架使用文档

## 一、框架概述

FFramework 是一个基于 MVC 架构的 Unity 游戏开发框架，融合了 QFramework 设计理念，提供了完整的分层结构和丰富的工具集。框架旨在帮助开发者快速构建可维护、可扩展、高质量的游戏项目。

### 核心优势

- **分层架构**：明确的 MVC 层级划分，确保代码组织清晰
- **依赖注入**：IOC 容器管理组件生命周期与依赖关系
- **事件系统**：类型安全的事件通信机制，解耦系统间交互
- **单例管理**：支持普通类和 MonoBehaviour 的单例实现
- **丰富工具集**：UI、对象池、状态机等多种游戏开发必备工具

---

## 二、架构设计

### 层级结构

```
View 表现层 → Command → System 系统层 → Model 数据层 → Utility 工具层
     ↑           ↓          ↑           ↑             ↑
     └───────────┴──────────┴───────────┘─────────────┘
                     事件系统 (EventSystem)
```

#### 1. View 表现层（IViewController）

- 负责用户界面显示和交互
- 可使用：Command、Query、Model、System、Event

#### 2. System 系统层（ISystem）

- 处理游戏核心逻辑和规则
- 可使用：Model、System、Utility、Event

#### 3. Model 数据层（IModel）

- 管理游戏数据和状态
- 可使用：Utility、Event

#### 4. Utility 工具层（IUtility）

- 提供纯工具函数和服务
- 不依赖框架其他部分

---

## 三、核心功能

### 1. IOC 容器

IOC 容器负责管理组件的生命周期和依赖注入：

```csharp
// 注册类型
container.Register<IService>();

// 注册实例
container.RegisterInstance<IConfig>(config);

// 获取实例
var service = container.Get<IService>();

// 依赖注入
container.Inject(targetObject);
```

### 2. 事件系统

类型安全的事件通信：

```csharp
// 定义事件
public struct PlayerLevelUpEvent { public int Level; }

// 注册事件
this.RegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp);

// 发送事件
this.SendEvent(new PlayerLevelUpEvent { Level = 5 });

// 注销事件
this.UnRegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp);
```

### 3. 绑定属性

支持数据绑定和变化通知：

```csharp
// 创建绑定属性
public BindableProperty<int> Gold = new BindableProperty<int>(100);

// 注册变化回调
Gold.RegisterWithInitValue(value => Debug.Log($"Gold: {value}"));

// 更新属性触发回调
Gold.Value = 200;
```

### 4. 单例模式

两种单例实现：

```csharp
// 普通类单例
public class ConfigManager : Singleton<ConfigManager> { }
ConfigManager.Instance.Method();

// MonoBehaviour单例
public class AudioManager : SingletonMono<AudioManager> { }
AudioManager.Instance.Method();
```

---

## 四、工具套件 (Utility Kits)

FFramework 提供了丰富的工具套件，满足游戏开发各方面需求：

### 1. UIKit - UI 界面管理

UIKit 提供了完整的 UI 面板管理、层级控制、组件查找等功能：

```csharp
// 打开面板
var panel = UIKit.OpenPanel<MainMenuPanel>();

// 关闭面板
UIKit.ClosePanel<MainMenuPanel>();
```

[查看完整 UIKit 文档](FFramework/Utility/UIKit/UIKit_Documentation.md)

### 2. PoolKit - 对象池管理

PoolKit 用于高效管理和复用游戏对象：

```csharp
// 获取对象
GameObject obj = ObjectPoolKit.Spawn("PrefabName");

// 回收对象
ObjectPoolKit.Recycle(obj);
```

[查看完整 PoolKit 文档](FFramework/Utility/PoolKit/PoolKit_Documentation.md)

### 3. FSMKit - 有限状态机

FSMKit 提供了泛型有限状态机解决方案：

```csharp
// 创建状态机
var fsm = new FSMStateMachine<PlayerController>(player);

// 设置状态
fsm.SetDefault<PlayerIdleState>();
```

[查看完整 FSMKit 文档](FFramework/Utility/FSMKit/FSM_Documentation.md)

### 4. EventKit - 事件处理

EventKit 提供 Unity 事件系统的扩展：

```csharp
// 注册点击事件
button.OnClickEvent(() => Debug.Log("按钮点击"));

// 注册拖拽事件
image.OnBeginDragEvent(eventData => { /* 处理拖拽开始 */ });
```

[查看完整 EventKit 文档](FFramework/Utility/EventKit/EventKit_Documentation.md)

### 5. DataSaveKit - 数据存储

用于游戏数据的保存与加载：

```csharp
// 保存数据
DataSaveKit.SaveData("playerData", playerData);

// 加载数据
var data = DataSaveKit.LoadData<PlayerData>("playerData");
```

[查看完整 DataSaveKit 文档](FFramework/Utility/DataSaveKit/DataSaveKit_Documentation.md)

### 6. LoadAssetKit - 资源加载提供统一的资源加载接口：

```csharp
// 加载资源
var prefab = LoadAssetKit.LoadAsset<GameObject>("Prefabs/Character");

// 异步加载
LoadAssetKit.LoadAssetAsync<GameObject>("Prefabs/Effect", OnAssetLoaded);
```

[查看完整 LoadAssetKit 文档](FFramework/Utility/LoadAssetKit/LoadAssetKit_Documentation.md)

### 7. LoadSceneKit - 场景加载

封装场景加载逻辑：

```csharp
// 异步加载场景
LoadSceneKit.LoadSceneAsync("GameLevel", LoadSceneMode.Single, progress => {
    // 更新加载进度
}, onComplete => {
    // 加载完成
});
```

[查看完整 LoadSceneKit 文档](FFramework/Utility/LoadSceneKit/LoadSceneKit_Documentation.md)

### 8. TimerKit - 定时器

提供丰富的定时器功能：

```csharp
// 延迟执行
TimerKit.DelayInvoke(2.0f, () => Debug.Log("两秒后执行"));

// 循环定时器
TimerKit.Loop(1.0f, () => Debug.Log("每秒执行一次"));
```

[查看完整 TimerKit 文档](FFramework/Utility/TimerKit/TimerManager_Documentation.md)

---

## 五、最佳实践

### 推荐的项目结构

```
Assets/
├── Scripts/
│   ├── Architecture/   # 架构定义
│   ├── Models/         # 数据层
│   ├── Systems/        # 系统层
│   ├── Views/          # 表现层
│   ├── Commands/       # 命令
│   ├── Queries/        # 查询
│   └── Events/         # 事件定义
├── Resources/          # 资源文件
│   ├── UI/             # UI预制体
│   └── Configs/        # 配置文件
```

### 开发流程建议

1. **架构设计**：明确项目分层和职责
2. **数据设计**：设计 Model 层数据结构
3. **系统实现**：开发核心 System 业务逻辑
4. **界面开发**：构建 View 层用户界面
5. **事件通信**：通过事件系统进行交互

### 注意事项

- **遵循层级规则**：严格按照分层架构进行开发
- **避免循环依赖**：谨慎处理层级间的引用关系
- **善用工具套件**：充分利用框架提供的工具提高效率
- **单一职责**：每个组件只负责单一功能
- **数据封装**：避免直接暴露数据，使用 BindableProperty

---

## 六、示例：角色升级功能

以下是使用 FFramework 实现角色升级功能的完整示例：

### 1. 定义事件

```csharp
public struct PlayerLevelUpEvent
{
    public int Level;
    public int Exp;
}
```

### 2. 定义数据模型

```csharp
public interface IPlayerModel : IModel
{
    BindableProperty<int> Level { get; }
    BindableProperty<int> Exp { get; }
    BindableProperty<int> MaxExp { get; }
}

public class PlayerModel : AbstractModel, IPlayerModel
{
    public BindableProperty<int> Level { get; } = new BindableProperty<int>(1);
    public BindableProperty<int> Exp { get; } = new BindableProperty<int>(0);
    public BindableProperty<int> MaxExp { get; } = new BindableProperty<int>(100);
}
```

### 3. 实现系统层逻辑

```csharp
public interface IPlayerSystem : ISystem
{
    void AddExp(int amount);
}

public class PlayerSystem : AbstractSystem, IPlayerSystem
{
    public void AddExp(int amount)
    {
        var playerModel = this.GetModel<IPlayerModel>();
        playerModel.Exp.Value += amount;

        // 检查是否升级
        while (playerModel.Exp.Value >= playerModel.MaxExp.Value)
        {
            playerModel.Exp.Value -= playerModel.MaxExp.Value;
            playerModel.Level.Value++;
            playerModel.MaxExp.Value = CalculateNextLevelExp(playerModel.Level.Value);

            // 发送升级事件
            this.SendEvent(new PlayerLevelUpEvent {
                Level = playerModel.Level.Value,
                Exp = playerModel.Exp.Value
            });
        }
    }

    private int CalculateNextLevelExp(int level)
    {
        return 100 * level;
    }
}
```

### 4. 定义命令

```csharp
public class AddExpCommand : AbstractCommand
{
    public int ExpAmount;

    protected override void OnExecute()
    {
        this.GetSystem<IPlayerSystem>().AddExp(ExpAmount);
    }
}
```

### 5. 实现界面

```csharp
public class PlayerUI : MonoBehaviour, IController
{
    [SerializeField] private Text levelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Button addExpButton;

    private void Start()
    {
        var playerModel = this.GetModel<IPlayerModel>();

        // 绑定UI更新
        playerModel.Level.RegisterWithInitValue(level =>
            levelText.text = $"Lv.{level}");

        playerModel.Exp.Register(UpdateExpSlider);
        playerModel.MaxExp.Register(UpdateExpSlider);

        // 注册事件监听
        this.RegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp);

        // 按钮点击
        addExpButton.OnClickEvent(() => {
            this.SendCommand(new AddExpCommand { ExpAmount = 50 });
        });
    }

    private void UpdateExpSlider()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        expSlider.maxValue = playerModel.MaxExp.Value;
        expSlider.value = playerModel.Exp.Value;
    }

    private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
    {
        // 播放升级特效
        var effect = ObjectPoolKit.Spawn("LevelUpEffect");
        effect.transform.position = this.transform.position;

        // 5秒后回收特效
        TimerKit.DelayInvoke(5.0f, () => {
            ObjectPoolKit.Recycle(effect);
        });
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
```

---

## 七、资源与支持

- 完整的 API 文档请参考各工具套件的详细文档
- 示例工程请参考仓库中的 Examples 目录
- 问题反馈请提交到项目 Issues

---

_FFramework - 让游戏开发更简单、更高效_
