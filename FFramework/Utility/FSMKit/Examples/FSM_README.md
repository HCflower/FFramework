# 高性能有限状态机使用说明

## 📖 目录

1. [概述](#概述)
2. [快速开始](#快速开始)
3. [高级功能](#高级功能)
4. [最佳实践](#最佳实践)
5. [注意事项](#注意事项)
6. [示例项目](#示例项目)

---

## 概述

这是一个针对 Unity 优化的高性能有限状态机系统，提供了以下特性：

### 🚀 性能优化特性

1. **状态缓存池** - 避免频繁创建销毁状态对象
2. **延迟状态切换** - 避免在 Update 中直接切换状态导致的问题
3. **条件转换表** - 高效的状态转换查找
4. **内存管理** - 实现 IDisposable，自动清理资源
5. **异常处理** - 状态切换过程中的异常保护

### 🛠️ 核心组件

- `FSMStateMachine` - 核心状态机类
- `IState` - 状态接口
- `BaseState` - 状态基类，提供常用功能
- `FSMStateMachineComponent` - Unity 组件，自动处理 Update 循环

## 快速开始

### 1. 创建状态类

```csharp
// 继承BaseState获得更多便利功能
public class IdleState : BaseState
{
    protected override void OnEnterState()
    {
        Debug.Log("进入空闲状态");
    }

    protected override void OnUpdateState()
    {
        // 检测输入或条件，切换状态
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ChangeStateDeferred<JumpState>();
        }
    }

    protected override void OnExitState()
    {
        Debug.Log("退出空闲状态");
    }
}

// 或者直接实现IState接口
public class CustomState : IState
{
    public void OnEnter() { }
    public void OnUpdate() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
    public void OnExit() { }
}
```

### 2. 使用状态机组件（推荐）

```csharp
public class PlayerController : FSMStateMachineComponent
{
    protected override void OnStateMachineInitialized()
    {
        // 配置状态转换条件
        var fsm = StateMachine;

        // 添加转换条件
        fsm.AddTransition<IdleState, MoveState>(() => Input.GetAxis("Horizontal") != 0);
        fsm.AddTransition<MoveState, IdleState>(() => Input.GetAxis("Horizontal") == 0);

        // 添加全局转换
        fsm.AddGlobalTransition<JumpState>(() => Input.GetKeyDown(KeyCode.Space));
    }

    protected override void OnAutoStart()
    {
        // 设置初始状态
        StartStateMachine<IdleState>();
    }
}
```

### 3. 手动使用状态机

```csharp
public class GameManager : MonoBehaviour
{
    private FSMStateMachine stateMachine;

    void Start()
    {
        // 创建状态机
        stateMachine = new FSMStateMachine();

        // 配置转换条件
        stateMachine.AddTransition<MenuState, GameState>(() => Input.GetKeyDown(KeyCode.Return));
        stateMachine.AddGlobalTransition<PauseState>(() => Input.GetKeyDown(KeyCode.Escape));

        // 启动状态机
        stateMachine.Start<MenuState>();
    }

    void Update()
    {
        stateMachine?.Update();
    }

    void OnDestroy()
    {
        stateMachine?.Dispose();
    }
}
```

## 高级功能

### 1. 状态转换条件

```csharp
// 条件转换
fsm.AddTransition<IdleState, MoveState>(() =>
{
    return Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
});

// 全局转换（任意状态都可以触发）
fsm.AddGlobalTransition<PauseState>(() => Input.GetKeyDown(KeyCode.Escape));
```

### 2. 延迟状态切换

```csharp
// 在状态的Update中使用延迟切换，避免立即切换导致的问题
protected override void OnUpdateState()
{
    if (someCondition)
    {
        ChangeStateDeferred<NextState>(); // 会在下一帧执行切换
    }
}
```

### 3. 状态查询和调试

```csharp
// 检查当前状态
if (fsm.IsInState<IdleState>())
{
    // 当前在空闲状态
}

// 获取状态信息
string currentState = fsm.GetCurrentStateName();
string statistics = fsm.GetStatistics();

// 检查是否可以转换
bool canJump = fsm.CanTransitionTo<JumpState>();
```

### 4. 性能监控

```csharp
// 获取性能统计
Debug.Log($"状态切换次数: {fsm.StateChangeCount}");
Debug.Log($"最后切换时间: {fsm.LastStateChangeTime}");
Debug.Log($"详细统计: {fsm.GetStatistics()}");
```

## 最佳实践

### 1. 状态设计原则

- **单一职责** - 每个状态只负责一种行为
- **状态独立** - 状态之间不应该直接依赖
- **条件明确** - 转换条件应该清晰明确

### 2. 性能优化建议

- 使用`ChangeStateDeferred`而不是`ChangeState`在 Update 中切换状态
- 利用状态缓存池，避免频繁创建状态对象
- 合理设置转换条件，避免不必要的状态检查
- 在不需要时禁用 FixedUpdate 和 LateUpdate

### 3. 内存管理

```csharp
// 在适当时候清理状态缓存
fsm.ClearStateCache();

// 确保释放资源
fsm.Dispose();
```

### 4. 调试技巧

- 在 Inspector 中启用`showDebugInfo`查看运行时状态
- 启用`logStateChanges`记录状态切换日志
- 使用`GetStatistics()`监控性能

## 注意事项

1. **线程安全** - 此状态机不是线程安全的，只能在主线程使用
2. **循环引用** - 避免状态之间的循环引用导致内存泄漏
3. **异常处理** - 状态切换过程中的异常会被捕获并记录，但状态机会继续运行
4. **状态生命周期** - 确保正确实现 OnEnter 和 OnExit 方法

## 示例项目

参考`ExampleStates.cs`中的完整示例，演示了如何创建一个简单的角色状态机。
