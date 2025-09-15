using System;
using System.Threading;
using System.Threading.Tasks;

namespace FFramework.Kit
{
    /// <summary>
    /// HSM状态机活动
    /// </summary>
    public enum HSMActivityMode
    {
        /// <summary>
        /// 停止
        /// </summary>
        Inactive,

        /// <summary>
        /// 激活中
        /// </summary>
        Activating,

        /// <summary>
        /// 激活
        /// </summary>
        Active,

        /// <summary>
        /// 使无效
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
