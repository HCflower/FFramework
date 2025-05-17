//状态接口
public interface IState
{
    void OnEnter();         //当状态进入时调用
    void OnUpdate();        //当状态更新时调用
    void OnExit();          //当状态退出时调用
}