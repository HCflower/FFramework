namespace FFramework.Kit
{
    //状态接口
    public interface IState<T> where T : class
    {
        void OnEnter(FSMStateMachine<T> machine);         //当状态进入时调用
        void OnUpdate(FSMStateMachine<T> machine);        //当状态更新时调用
        void OnFixedUpdate(FSMStateMachine<T> machine);   //当状态固定更新时调用
        void OnLateUpdate(FSMStateMachine<T> machine);    //当状态延迟更新时调用
        void OnExit(FSMStateMachine<T> machine);          //当状态退出时调用
    }
}