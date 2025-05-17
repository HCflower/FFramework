namespace FFramework.Kit
{
    /// <summary>
    /// 有限状态机状态机
    /// </summary>
    public class FSMStateMachine
    {
        private IState currentState;

        // 状态切换方法
        public void ChangeState(IState newState)
        {
            if (currentState != null) currentState?.OnExit();
            currentState = newState;
            if (currentState != null) currentState?.OnEnter();
        }

        // 手动调用更新
        public void Update()
        {
            currentState?.OnUpdate();
        }

        // 获取当前状态类型（用于调试）
        public System.Type GetCurrentStateType()
        {
            return currentState?.GetType();
        }
    }
}