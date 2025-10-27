using System.Collections.Generic;

namespace FFramework
{
    /// <summary>
    /// 层次状态机
    /// </summary>
    public class HSMStateMachine
    {
        // 根节点
        public readonly HSMState rootState;
        // 状态转换序列器
        public readonly HSMTransitionSequencer sequencer;
        private bool started = false;

        public HSMStateMachine(HSMState rootState)
        {
            this.rootState = rootState;
            sequencer = new HSMTransitionSequencer(this);
        }

        public void Start()
        {
            if (started) return;
            started = true;
            rootState.Enter();
        }

        public void Tick(float deltaTime)
        {
            if (!started) Start();
            sequencer.Tick(deltaTime);
        }

        internal void InternalTick(float deltaTime) => rootState.Update(deltaTime);

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="from">要被切换的状态</param>
        /// <param name="to">要切换到的状态</param>
        public void ChangeState(HSMState from, HSMState to)
        {
            if (from == to || from == null || to == null) return;
            HSMState lca = HSMTransitionSequencer.LowestCommonAncestor(from, to);
            // 退出当前分支至（但不包括）LCA
            for (var state = from; state != lca; state = state.parent)
            {
                state.Exit();
            }
            // 从LCA向下输入目标分支 将目标状态压入栈中
            Stack<HSMState> stack = new Stack<HSMState>();
            for (HSMState state = to; state != lca; state = state.parent)
            {
                stack.Push(state);
            }
            // 依次进入目标状态
            while (stack.Count > 0)
            {
                stack.Pop().Enter();
            }
        }
    }
}