namespace FFramework.Kit
{
    /// <summary>
    /// 对象池根节点
    /// </summary>
    public class PoolRoot : SingletonMono<PoolRoot>
    {
        protected override void Awake()
        {
            IsDontDestroyOnLoad = true;
            base.Awake();
        }
    }
}