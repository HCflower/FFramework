# FSMKit 有限状态机模块文档

## 目录

- [一、简介](#一简介)
- [二、优势](#二优势)
- [三、API 介绍](#三api介绍)
  - [1. FSMStateMachine 核心类](#1-fsmstatemachine-核心类)
  - [2. IFSMState 接口](#2-istate-接口)
  - [3. FSMStateBase 基类](#3-statebase-基类)
- [四、核心功能](#四核心功能)
- [五、快速上手](#五快速上手)
  - [1. 定义状态类](#1-定义状态类)
  - [2. 创建状态机](#2-创建状态机)
  - [3. 设置默认状态](#3-设置默认状态)
  - [4. 切换状态](#4-切换状态)
  - [5. 更新状态机](#5-更新状态机)
- [六、使用场景示例](#六使用场景示例)
- [七、性能优化](#七性能优化)

---

## 一、简介

`FSMKit` 是一个泛型有限状态机（FSM）解决方案，适用于角色、AI、流程等多种场景。通过接口和基类规范，支持灵活扩展和高效状态切换，帮助开发者轻松实现复杂的状态管理。

---

## 二、优势

1. **高扩展性**：支持泛型，适配多种类型的状态机拥有者。
2. **性能优化**：状态缓存机制，避免重复创建状态实例。
3. **易用性**：提供基类 `FSMStateBase<T>` 和接口 `IFSMState<T>`，简化状态开发。
4. **灵活性**：支持动态状态切换和生命周期管理。
5. **调试友好**：提供当前状态类型查询，便于调试。

---

## 三、API 介绍

### 1. FSMStateMachine 核心类

- `SetDefault<TState>()`
  - 设置默认状态。
- `SetDefault(IState<T> defaultState)`
  - 设置默认状态（实例化状态对象）。
- `ChangeState<TState>()`
  - 切换到指定类型的状态。
- `ChangeState(IState<T> newState)`
  - 切换到指定的状态实例。
- `Update()`
  - 手动调用更新逻辑。
- `FixedUpdate()`
  - 手动调用物理帧更新逻辑。
- `LateUpdate()`
  - 手动调用延迟帧更新逻辑。
- `GetCurrentStateType()`
  - 获取当前状态类型。

### 2. IFSMState 接口

- `OnEnter(FSMStateMachine<T> machine)`
  - 进入状态时调用。
- `OnUpdate(FSMStateMachine<T> machine)`
  - 每帧更新时调用。
- `OnFixedUpdate(FSMStateMachine<T> machine)`
  - 物理帧更新时调用。
- `OnLateUpdate(FSMStateMachine<T> machine)`
  - 延迟帧更新时调用。
- `OnExit(FSMStateMachine<T> machine)`
  - 离开状态时调用。

### 3. FSMStateBase 基类

- `Init(T owner)`
  - 初始化状态拥有者。
- `OnEnter(FSMStateMachine<T> machine)`
  - 抽象方法，进入状态时调用。
- `OnUpdate(FSMStateMachine<T> machine)`
  - 抽象方法，每帧更新时调用。
- `OnExit(FSMStateMachine<T> machine)`
  - 抽象方法，离开状态时调用。
- `OnFixedUpdate(FSMStateMachine<T> machine)`
  - 虚方法，物理帧更新时调用。
- `OnLateUpdate(FSMStateMachine<T> machine)`
  - 虚方法，延迟帧更新时调用。

---

## 四、核心功能

1. **状态管理**：

   - 支持状态的动态切换和生命周期管理。
2. **状态缓存**：

   - 每种状态只会创建一次，后续切换直接复用。
3. **生命周期方法**：

   - 提供 `OnEnter`、`OnUpdate`、`OnExit` 等完整的生命周期方法。
4. **调试辅助**：

   - 提供当前状态类型查询，便于调试和日志输出。

---

## 五、快速上手

### 1. 定义状态类

```csharp
public class Player_Idle : StateBase<PlayerController>
{
    public override void OnEnter(FSMStateMachine<PlayerController> machine)
    {
        Debug.Log("进入 Idle 状态");
    }
    public override void OnUpdate(FSMStateMachine<PlayerController> machine)
    {
        Debug.Log("Idle 状态更新");
    }
    public override void OnExit(FSMStateMachine<PlayerController> machine)
    {
        Debug.Log("离开 Idle 状态");
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

### 5. 更新状态机

```csharp
fsm.Update();
fsm.FixedUpdate();
fsm.LateUpdate();
```

---

## 六、使用场景示例

1. **角色状态管理**：

   - 使用状态机管理角色的移动、攻击、跳跃等状态。
2. **AI 行为树**：

   - 将状态机作为行为树的基础单元，管理 AI 的行为逻辑。
3. **流程控制**：

   - 使用状态机管理游戏的关卡流程或 UI 流程。
4. **任务系统**：

   - 使用状态机管理任务的不同阶段。

---

## 七、性能优化

1. **状态缓存**：

   - 避免重复创建状态实例，提升性能。
2. **手动更新**：

   - 状态机不自动驱动更新，开发者可根据需求手动调用更新方法。
3. **减少无效切换**：

   - 在切换状态前检查是否与当前状态相同，避免重复切换。
4. **调试工具**：

   - 使用 `GetCurrentStateType` 方法输出当前状态类型，便于调试。
