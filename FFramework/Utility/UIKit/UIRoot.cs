namespace FFramework.Kit
{
    ///<summary>
    /// UI根节点
    /// </summary>
    public class UIRoot : SingletonMono<UIRoot>
    {
        protected override void Awake()
        {
            IsDontDestroyOnLoad = true;
            base.Awake();
        }
    }
}