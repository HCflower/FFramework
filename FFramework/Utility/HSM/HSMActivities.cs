using System.Threading.Tasks;
using System.Threading;
using System;

namespace FFramework.Utility
{
    /// <summary>
    /// HSM状态机活动
    /// </summary>
    public enum HSMActivityMode
    {
        /// <summary>
        /// 未激活状态
        /// </summary>
        Inactive,

        /// <summary>
        /// 正在激活
        /// </summary>
        Activating,

        /// <summary>
        /// 已激活状态
        /// </summary>
        Active,

        /// <summary>
        /// 正在失效
        /// </summary>
        Deactivating,
    }

    // 活动接口，所有活动必须实现这个接口
    public interface IActivity
    {
        HSMActivityMode Mode { get; }
        // 激活
        Task ActivateAsync(CancellationToken token);
        // 使无效
        Task DeactivateAsync(CancellationToken token);
    }

    public abstract class Activity : IActivity
    {
        public HSMActivityMode Mode { get; protected set; } = HSMActivityMode.Inactive;

        public virtual async Task ActivateAsync(CancellationToken token)
        {
            if (Mode != HSMActivityMode.Inactive) return;

            Mode = HSMActivityMode.Activating;
            await Task.CompletedTask;
            Mode = HSMActivityMode.Active;
        }

        public virtual async Task DeactivateAsync(CancellationToken token)
        {
            if (Mode != HSMActivityMode.Active) return;

            Mode = HSMActivityMode.Deactivating;
            await Task.CompletedTask;
            Mode = HSMActivityMode.Inactive;
        }
    }

    /// <summary>
    /// 延迟激活活动
    /// </summary>
    public class DelayActivationActivity : Activity
    {
        public float seconds = 0.2f;
        public override async Task ActivateAsync(CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), token);
            await base.ActivateAsync(token);
        }
    }
}
