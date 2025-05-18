namespace FFramework.Kit
{
    ///<summary>
    /// UI根节点
    /// </summary>
    public class UIRoot : SingletonMono<UIRoot>
    {
        UIRoot() => IsDontDestroyOnLoad = true;
        protected override void Awake()
        {
            base.Awake();
        }
    }
}