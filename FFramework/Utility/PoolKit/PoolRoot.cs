namespace FFramework.Kit
{
    /// <summary>
    /// 对象池根节点
    /// </summary>
    public class PoolRoot : SingletonMono<PoolRoot>
    {
        PoolRoot() => IsDontDestroyOnLoad = true;
        protected override void Awake()
        {
            base.Awake();
        }
    }
}