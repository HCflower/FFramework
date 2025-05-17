namespace FFramework.Kit
{
    /// <summary>
    /// 场景基类
    /// </summary>
    public interface IScene
    {
        /// <summary>
        /// 场景进入时调用
        /// </summary>
        public void OnEnter();

        /// <summary>
        /// 场景退出时调用
        /// </summary>
        public void OnExit();
    }
}