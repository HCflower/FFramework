namespace FFramework.Utility
{
    /// <summary>
    /// 状态接口 - 无泛型版本
    /// </summary>
    public interface IFSMState
    {
        void OnEnter(FSMStateMachine machine);         //当状态进入时调用
        void OnUpdate(FSMStateMachine machine);        //当状态更新时调用
        void OnFixedUpdate(FSMStateMachine machine);   //当状态固定更新时调用
        void OnLateUpdate(FSMStateMachine machine);    //当状态延迟更新时调用
        void OnExit(FSMStateMachine machine);          //当状态退出时调用
    }
}