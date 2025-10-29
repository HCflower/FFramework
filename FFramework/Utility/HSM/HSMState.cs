using System.Collections.Generic;

namespace FFramework.Utility
{
    /// <summary>
    /// 状态
    /// </summary>
    public abstract class HSMState
    {
        // 当前的层次状态机
        public readonly HSMStateMachine stateMachine;
        // 父状态
        public readonly HSMState parent;
        // 激活的子状态
        public HSMState activeChild;

        // 活动列表
        private readonly List<IActivity> activities = new List<IActivity>();
        public IReadOnlyList<IActivity> Activities => activities;

        public HSMState(HSMStateMachine stateMachine, HSMState parent)
        {
            this.stateMachine = stateMachine;
            this.parent = parent;
        }

        // 添加活动
        public void AddActivity(IActivity activity)
        {
            if (activity != null) activities.Add(activity);
        }

        // 初始化此状态进入时(null - 这是叶子)
        protected virtual HSMState GetInitialState() => null;
        // 目标状态切换（null - 保持在当前状态）
        protected virtual HSMState GetTransition() => null;

        // 生命周期方法
        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate(float deltaTime) { }
        // protected virtual void OnFixedUpdate() { }
        // protected virtual void OnLateUpdate() { }

        // 进入状态
        internal void Enter()
        {
            if (parent != null) parent.activeChild = this;

            OnEnter();

            HSMState init = GetInitialState();
            if (init != null) init.Enter();
        }

        // 离开状态
        internal void Exit()
        {
            if (activeChild != null) activeChild.Exit();

            activeChild = null;

            OnExit();
        }

        // 更新状态
        internal void Update(float deltaTime)
        {
            HSMState transition = GetTransition();
            if (transition != null)
            {
                stateMachine.sequencer.RequestTransition(this, transition);
                return;
            }
            else
            {
                if (activeChild != null) activeChild.Update(deltaTime);
                OnUpdate(deltaTime);
            }
        }

        /// <summary>
        /// TODO：返回当前活性最深的后代状态（活动路径的叶子）
        /// </summary>
        /// <returns>状态</returns>
        public HSMState Leaf()
        {
            HSMState state = this;
            while (state.activeChild != null) state = state.activeChild;
            return state;
        }

        /// <summary>
        /// TODO：列出此状态，然后将每个祖先提升到根部（自→父→...→根）。
        /// </summary>
        /// <returns>状态</returns>
        public IEnumerable<HSMState> PathToRoot()
        {
            for (HSMState state = this; state != null; state = state.parent) yield return state;
        }
    }
}