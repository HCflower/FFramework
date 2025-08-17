namespace FFramework.Kit
{
    /// <summary>
    /// 动画接口
    /// </summary>
    public interface IAnima
    {
        public abstract void PlayAnima();
        public abstract void PauseAnima();
        public abstract void ChangeAnima();
        public void SetAnimaPlayProgress();
    }
}