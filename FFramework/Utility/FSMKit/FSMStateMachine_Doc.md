# FFramework.Kit FSM 状态机使用文档

## 1. 核心类说明

### FSMStateMachine<T>

- 泛型有限状态机，T 为状态机拥有者类型（如 PlayerController）。
- 负责状态切换、状态缓存、生命周期管理。

### IState<T>

- 状态接口，所有状态类需实现。
- 包含状态进入、更新、退出等方法。

### StateBase<T>

- 状态基类，继承自 IState<T>。
- 提供 owner 持有者引用和虚方法扩展。

---

## 2. 快速使用

### 1. 定义状态类

```csharp
public class Player_Idle : StateBase<PlayerController>
{
    public override void OnEnter(FSMStateMachine<PlayerController> machine)
    {
        // 进入Idle状态时的逻辑
    }
    public override void OnUpdate(FSMStateMachine<PlayerController> machine)
    {
        // Idle状态下的逻辑
    }
    public override void OnExit(FSMStateMachine<PlayerController> machine)
    {
        // 离开Idle状态时的逻辑
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

### 5. 状态自动初始化 owner

- 状态机会自动调用 `StateBase<T>.Init(owner)`，确保每个状态都持有正确的 owner 引用。

---

## 3. 生命周期方法

- `fsm.Update()`：手动调用，驱动当前状态的 OnUpdate。
- `fsm.FixedUpdate()`：驱动 OnFixedUpdate。
- `fsm.LateUpdate()`：驱动 OnLateUpdate。

---

## 4. 状态缓存机制

- 每种状态只会创建一次，后续切换直接复用，提升性能。
- 状态对象会自动初始化 owner，无需手动赋值。

---

## 5. 调试辅助

- `fsm.GetCurrentStateType()`：获取当前状态类型，便于调试。

---

## 6. 注意事项

- 每个 FSMStateMachine 只服务一个 owner。
- 状态类需继承 `StateBase<T>` 或实现 `IState<T>`。
- 状态切换时会自动调用 OnExit/OnEnter，无需手动管理。

---

如需扩展更多状态，只需新建类继承 `StateBase<T>` 并实现相关方法即可。
