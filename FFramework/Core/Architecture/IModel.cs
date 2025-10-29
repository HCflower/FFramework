namespace FFramework.Architecture
{
    /// <summary>
    /// 数据模型接口
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// 初始化模型
        /// </summary>
        void Initialize();

        /// <summary>
        /// 销毁模型
        /// </summary>
        void Dispose();
    }
}