# FFramework 框架文档

## 概述

FFramework 是一个基于 MVC 架构模式设计的 Unity 游戏开发框架，采用 QFramework 的设计理念，提供了完整的分层架构和依赖注入系统。框架旨在帮助开发者构建可维护、可扩展的游戏项目。

## 核心特性

- **分层架构**：清晰的 MVC 分层设计
- **依赖注入**：IOC 容器管理组件生命周期
- **事件系统**：类型安全的事件通信机制
- **命令模式**：解耦的业务逻辑处理
- **查询系统**：统一的数据查询接口
- **单例模式**：普通类和 MonoBehaviour 单例支持
- **绑定属性**：数据绑定和变化通知

## 架构层级

### 1. View 表现层（IViewController）

- 负责用户界面显示和交互
- 接收用户输入，显示数据
- 通过命令与其他层通信
- **可以使用**：Command、Query、Model、System、Event

### 2. System 系统层（ISystem）

- 处理游戏逻辑和规则
- 协调 Model 层的数据操作
- **可以使用**：Model、System、Utility、Event

### 3. Model 数据层（IModel）

- 管理游戏数据和状态
- 提供数据的存储和访问
- **可以使用**：Utility、Event

### 4. Utility 工具层（IUtility）

- 提供纯函数工具和算法
- 不依赖框架其他部分
- **不可使用任何框架功能**

## 核心组件详解

### Architecture 架构基类

```csharp
// 定义项目架构
public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册 Model
        this.RegisterModel<IPlayerModel>(new PlayerModel());

        // 注册 System
        this.RegisterSystem<IGameSystem>(new GameSystem());

        // 注册 Utility
        this.RegisterUtility<ITimeUtility>(new TimeUtility());
    }
}
```

### IOC 容器

IOC 容器负责管理组件的生命周期和依赖注入：

```csharp
// 注册类型
Register<IService>();

// 注册实例
RegisterInstance<IConfig>(config);

// 获取实例
var service = Get<IService>();

// 依赖注入
Inject(targetObject);
```

### 事件系统

类型安全的事件通信机制：

```csharp
// 1. 首先定义事件结构
public struct PlayerLevelUpEvent
{
    public int Level;
    public int NewExp;
}

// 2. 定义事件处理方法
private void OnPlayerLevelUp(PlayerLevelUpEvent levelUpEvent)
{
    Debug.Log($"Player leveled up to {levelUpEvent.Level}!");
    // 处理升级逻辑...
}

// 3. 注册事件监听
this.RegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp);

// 4. 发送事件
this.SendEvent<PlayerLevelUpEvent>();  // 发送默认事件
this.SendEvent(new PlayerLevelUpEvent { Level = 10, NewExp = 1000 });  // 发送具体数据

// 5. 注销事件（通常在 OnDestroy 中调用）
this.UnRegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp);
```

### 命令模式

用于封装业务逻辑：

```csharp
public class BuyItemCommand : AbstractCommand
{
    public int ItemId { get; set; }
    public int Price { get; set; }

    protected override void OnExecute()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var inventorySystem = this.GetSystem<IInventorySystem>();

        if (playerModel.Gold >= Price)
        {
            playerModel.Gold -= Price;
            inventorySystem.AddItem(ItemId);
            this.SendEvent(new ItemPurchasedEvent());
        }
    }
}

// 使用命令
this.SendCommand(new BuyItemCommand { ItemId = 1, Price = 100 });
```

### 查询系统

统一的数据查询接口：

```csharp
public class GetPlayerInfoQuery : AbstractQuery<PlayerInfo>
{
    protected override PlayerInfo OnDo()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        return new PlayerInfo
        {
            Name = playerModel.Name,
            Level = playerModel.Level,
            Gold = playerModel.Gold
        };
    }
}

// 使用查询
var playerInfo = this.SendQuery(new GetPlayerInfoQuery());
```

### 绑定属性

支持数据绑定和变化通知：

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(1000);
    public BindableProperty<int> Level { get; } = new BindableProperty<int>(1);

    protected override void OnInit()
    {
        Gold.RegisterWithInitValue(value => Debug.Log($"Gold changed to: {value}"));
    }
}
```

### 单例模式

框架提供两种单例实现：

#### 普通类单例

```csharp
public class ConfigManager : Singleton<ConfigManager>
{
    private ConfigManager() { } // 私有构造函数

    public void LoadConfig() { /* 实现 */ }
}

// 使用
ConfigManager.Instance.LoadConfig();
```

#### MonoBehaviour 单例

```csharp
public class AudioManager : SingletonMono<AudioManager>
{
    protected override void Awake()
    {
        base.Awake();
        // 初始化代码
    }

    public void PlaySound(string soundName) { /* 实现 */ }
}

// 使用
AudioManager.Instance.PlaySound("click");
```

## 使用规则和最佳实践

### 层级访问规则

```
View    → Command、Query、Model、System、Event
System  → Model、System、Utility、Event
Model   → Utility、Event
Utility → 无依赖
```

### 推荐的项目结构

```
Scripts/
├── Architecture/           # 架构定义
│   └── GameArchitecture.cs
├── Models/                # 数据层
│   ├── IPlayerModel.cs
│   └── PlayerModel.cs
├── Systems/               # 系统层
│   ├── IGameSystem.cs
│   └── GameSystem.cs
├── Views/                 # 表现层
│   ├── UI/
│   └── Game/
├── Commands/              # 命令
├── Queries/               # 查询
├── Events/                # 事件定义
└── Utilities/             # 工具类
```

### 开发流程建议

1. **定义架构**：创建项目的 Architecture 类
2. **设计数据模型**：定义 Model 接口和实现
3. **实现业务系统**：创建 System 处理业务逻辑
4. **编写命令**：封装具体的业务操作
5. **构建界面**：实现 View 层展示逻辑
6. **事件通信**：使用事件进行跨层通信

### 注意事项

- **避免循环依赖**：严格遵循层级访问规则
- **合理使用事件**：事件应该表达"已经发生的事情"
- **命令职责单一**：每个命令只做一件事
- **数据封装**：Model 中的数据应该通过属性暴露
- **工具类纯净**：Utility 不应依赖框架任何功能

## 示例：完整的功能实现

以下是一个完整的商店购买功能实现示例：

### 1. 定义事件

```csharp
public struct ItemPurchasedEvent
{
    public int ItemId;
    public int Price;
}
```

### 2. 定义 Model

```csharp
public interface IPlayerModel : IModel
{
    BindableProperty<int> Gold { get; }
    BindableProperty<List<int>> Inventory { get; }
}

public class PlayerModel : AbstractModel, IPlayerModel
{
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(1000);
    public BindableProperty<List<int>> Inventory { get; } = new BindableProperty<List<int>>(new List<int>());

    protected override void OnInit() { }
}
```

### 3. 定义命令

```csharp
public class PurchaseItemCommand : AbstractCommand
{
    public int ItemId;
    public int Price;

    protected override void OnExecute()
    {
        var playerModel = this.GetModel<IPlayerModel>();

        if (playerModel.Gold.Value >= Price)
        {
            playerModel.Gold.Value -= Price;
            playerModel.Inventory.Value.Add(ItemId);

            this.SendEvent(new ItemPurchasedEvent
            {
                ItemId = ItemId,
                Price = Price
            });
        }
    }
}
```

### 4. 实现 View

```csharp
public class ShopView : MonoBehaviour, IViewController
{
    [SerializeField] private Button buyButton;
    [SerializeField] private Text goldText;

    private void Start()
    {
        var playerModel = this.GetModel<IPlayerModel>();

        // 绑定数据显示
        playerModel.Gold.RegisterWithInitValue(gold => goldText.text = $"Gold: {gold}");

        // 绑定按钮事件
        buyButton.onClick.AddListener(() =>
        {
            this.SendCommand(new PurchaseItemCommand { ItemId = 1, Price = 100 });
        });

        // 监听购买完成事件
        this.RegisterEvent<ItemPurchasedEvent>(OnItemPurchased);
    }

    private void OnItemPurchased(ItemPurchasedEvent e)
    {
        Debug.Log($"Successfully purchased item {e.ItemId} for {e.Price} gold!");
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
```
