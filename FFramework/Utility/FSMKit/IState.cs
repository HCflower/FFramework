namespace FFramework.Kit
{
    //状态接口
    public interface IState
    {
        void OnEnter();         //当状态进入时调用
        void OnUpdate();        //当状态更新时调用
        void OnFixedUpdate();   //当状态固定更新时调用
        void OnLateUpdate();    //当状态延迟更新时调用
        void OnExit();          //当状态退出时调用
    }
}