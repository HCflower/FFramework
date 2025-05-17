/// <summary>
/// 场景基类
/// </summary>
public abstract class SceneBase
{
    /// <summary>
    /// 场景进入时调用
    /// </summary>
    public abstract void OnEnter();

    /// <summary>
    /// 场景退出时调用
    /// </summary>
    public abstract void OnExit();
}
