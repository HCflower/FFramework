# FFramework.Kit FSMKit 有限状态机模块文档

---

## 一、模块简介

FSMKit 提供了泛型有限状态机（FSM）解决方案，适用于角色、AI、流程等多种场景。通过接口和基类规范，支持灵活扩展和高效状态切换。

---

## 二、核心类结构

- **FSMStateMachine`<T>`**：有限状态机管理器，T 为状态机拥有者类型。
- **IState`<T>`**：状态接口，定义状态生命周期方法。
- **StateBase`<T>`**：状态基类，持有 owner 引用，简化状态开发。

---

## 三、快速上手

### 1. 定义状态类

```csharp
public class Player_Idle : StateBase<PlayerController>
{
    public override void OnEnter(FSMStateMachine<PlayerController> machine)
    {
        // 进入Idle状态逻辑
    }
    public override void OnUpdate(FSMStateMachine<PlayerController> machine)
    {
        // Idle状态更新逻辑
    }
    public override void OnExit(FSMStateMachine<PlayerController> machine)
    {
        // 离开Idle状态逻辑
    }
}
```

### 2. 创建状态机

```csharp
PlayerController player = ...;
var fsm = new FSMStateMachine<PlayerController>(player);
```

### 3. 设置默认状态

```csharp
fsm.SetDefault<Player_Idle>();
// 或
fsm.SetDefault(new Player_Idle());
```

### 4. 切换状态

```csharp
fsm.ChangeState<Player_Run>();
// 或
fsm.ChangeState(new Player_Run());
```

---

## 四、生命周期方法

- `OnEnter(FSMStateMachine<T> machine)`：进入状态时调用
- `OnUpdate(FSMStateMachine<T> machine)`：每帧更新时调用
- `OnFixedUpdate(FSMStateMachine<T> machine)`：物理帧更新时调用
- `OnLateUpdate(FSMStateMachine<T> machine)`：延迟帧更新时调用
- `OnExit(FSMStateMachine<T> machine)`：离开状态时调用

状态机需手动调用 `fsm.Update()`、`fsm.FixedUpdate()`、`fsm.LateUpdate()` 以驱动状态逻辑。

---

## 五、状态缓存与 owner 初始化

- 每种状态只会创建一次，后续切换直接复用，提升性能。
- 状态对象会自动初始化 owner 引用，无需手动赋值。

---

## 六、调试辅助

- `fsm.GetCurrentStateType()`：获取当前状态类型，便于调试和日志输出。

---

## 七、注意事项

1. 每个 FSMStateMachine 只服务一个 owner。
2. 状态类需继承 `StateBase<T>` 或实现 `IState<T>`。
3. 状态切换时自动调用 OnExit/OnEnter，无需手动管理。
4. 状态机不自动驱动 Update，请在合适时机手动调用。

---

## 八、示例代码

```csharp
// 定义状态
public class Player_Run : StateBase<PlayerController>
{
    public override void OnEnter(FSMStateMachine<PlayerController> machine) { ... }
    public override void OnUpdate(FSMStateMachine<PlayerController> machine) { ... }
    public override void OnExit(FSMStateMachine<PlayerController> machine) { ... }
}

// 创建状态机并切换状态
var fsm = new FSMStateMachine<PlayerController>(player);
fsm.SetDefault<Player_Idle>();
fsm.ChangeState<Player_Run>();
fsm.Update();
```

---

如需扩展更多状态，只需新建类继承 `StateBase<T>` 并实现相关方法即可。
