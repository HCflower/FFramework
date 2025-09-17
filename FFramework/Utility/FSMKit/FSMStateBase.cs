namespace FFramework.Kit
{
    /// <summary>
    /// 状态基类
    /// </summary>
    /// <typeparam name="T">状态持有者</typeparam>
    public abstract class FSMStateBase<T> : IFSMState<T> where T : class
    {
        public T owner;
        public void Init(T owner)
        {
            this.owner = owner;
        }

        // 状态机事件
        public abstract void OnEnter(FSMStateMachine<T> machine);
        public abstract void OnUpdate(FSMStateMachine<T> machine);
        public abstract void OnExit(FSMStateMachine<T> machine);
        // 虚方法
        public virtual void OnFixedUpdate(FSMStateMachine<T> machine) { }
        public virtual void OnLateUpdate(FSMStateMachine<T> machine) { }
    }
}