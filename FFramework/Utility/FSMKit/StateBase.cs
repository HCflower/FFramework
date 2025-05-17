/// <summary>
/// 状态基类
/// </summary>
/// <typeparam name="T">状态持有者</typeparam>
public class StateBase<T> : IState where T : class
{
    protected FSMStateMachine stateMachine;     // 状态机引用
    protected T owner;                          // 状态持有者

    /// <summary>
    /// 状态初始化方法
    /// </summary>
    /// <param name="stateMachine">状态机控制器</param>
    /// <param name="owner">状态所属</param>
    public virtual void Init(FSMStateMachine stateMachine, T owner)
    {
        this.stateMachine = stateMachine;
        this.owner = owner;
    }

    // 默认空实现（子类按需重写）
    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}
