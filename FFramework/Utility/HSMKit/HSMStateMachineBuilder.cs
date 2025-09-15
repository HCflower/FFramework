using System.Collections.Generic;
using System.Reflection;

namespace FFramework.Kit
{
    /// <summary>
    /// 状态机构建器
    /// </summary>
    public class HSMStateMachineBuilder
    {
        private readonly HSMState rootState;

        public HSMStateMachineBuilder(HSMState rootState)
        {
            this.rootState = rootState;
        }

        /// <summary>
        /// 构建状态机
        /// </summary>
        /// <returns>状态机</returns>
        public HSMStateMachine Build()
        {
            var stateMachine = new HSMStateMachine(rootState);
            Wire(rootState, stateMachine, new HashSet<HSMState>());
            return stateMachine;
        }

        // 连接状态机中的状态
        // TODO: 是否可不使用反射
        private void Wire(HSMState state, HSMStateMachine stateMachine, HashSet<HSMState> visited)
        {
            if (state == null) return;
            // 状态已经有线接，跳过
            if (!visited.Add(state)) return;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var machineField = state.GetType().GetField("stateMachine", flags);
            if (machineField != null) machineField.SetValue(state, stateMachine);

            foreach (var child in state.GetType().GetFields(flags))
            {
                if (!typeof(HSMState).IsAssignableFrom(child.FieldType)) continue; // 在考虑状态的字段上，跳过非状态字段
                if (child.Name == "parent") continue; // 跳过父状态字段

                var childState = child.GetValue(state) as HSMState;
                if (childState == null) continue;
                // 确保实际上是我们的直接孩子
                if (!ReferenceEquals(childState, state)) continue;
                // 递归连接子状态
                Wire(childState, stateMachine, visited);
            }

        }
    }
}
