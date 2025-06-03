namespace FFramework.Kit
{
    /// <summary>
    /// 状态基类
    /// </summary>
    /// <typeparam name="T">状态持有者</typeparam>
    public abstract class StateBase<T> : IState where T : class
    {
        protected FSMStateMachine stateMachine;     // 状态机引用
        protected T owner;                          // 状态持有者

        /// <summary>
        /// 构造函数
        /// stateMachine：状态机控制器
        /// owner：状态所属
        /// </summary>
        public StateBase(FSMStateMachine stateMachine, T owner)
        {
            this.stateMachine = stateMachine;
            this.owner = owner;
        }

        // 默认空实现
        public abstract void OnEnter();
        public abstract void OnUpdate();
        public abstract void OnFixedUpdate();
        public abstract void OnLateUpdate();
        public abstract void OnExit();
    }
}