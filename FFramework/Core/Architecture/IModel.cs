// =============================================================
// 描述：数据模型接口
// 作者：HCFlower
// 创建时间：2025-11-15 18:49:00
// 版本：1.0.0
// =============================================================
namespace FFramework.Architecture
{
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