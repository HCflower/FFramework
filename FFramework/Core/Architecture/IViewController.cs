using UnityEngine;

namespace FFramework.Architecture
{
    /// <summary>
    /// 视图控制器接口
    /// </summary>
    public interface IViewController
    {
        /// <summary>
        /// 初始化视图控制器
        /// </summary>
        void Initialize();

        /// <summary>
        /// 销毁视图控制器
        /// </summary>
        void Dispose();

        /// <summary>
        /// 绑定的GameObject
        /// </summary>
        GameObject GameObject { get; }
    }
}