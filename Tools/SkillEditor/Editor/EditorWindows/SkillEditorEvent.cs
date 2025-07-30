using FFramework.Kit;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器事件管理器
    /// 负责管理编辑器内部各组件之间的事件通信，实现松耦合的事件驱动架构
    /// </summary>
    public static class SkillEditorEvent
    {
        #region 事件定义

        /// <summary>当前帧变化事件 - 参数：新的帧数</summary>
        public static Action<int> OnCurrentFrameChanged;

        /// <summary>最大帧数变化事件 - 参数：新的最大帧数</summary>
        public static Action<int> OnMaxFrameChanged;

        /// <summary>全局控制面板显示状态切换事件 - 参数：是否显示</summary>
        public static Action<bool> OnGlobalControlToggled;

        /// <summary>请求刷新视图事件 - 无参数</summary>
        public static Action OnRefreshRequested;

        /// <summary>技能配置变化事件 - 参数：新的技能配置</summary>
        public static Action<SkillConfig> OnSkillConfigChanged;

        /// <summary>播放状态变化事件 - 参数：是否正在播放</summary>
        public static Action<bool> OnPlayStateChanged;

        /// <summary>时间轴缩放变化事件 - 无参数</summary>
        public static Action OnTimelineZoomChanged;

        #endregion

        #region 事件触发方法

        /// <summary>
        /// 触发当前帧变化事件
        /// 通知所有监听者当前帧数已更改，用于同步时间轴指示器和相关UI
        /// </summary>
        /// <param name="frame">新的当前帧数</param>
        public static void TriggerCurrentFrameChanged(int frame)
        {
            OnCurrentFrameChanged?.Invoke(frame);
        }

        /// <summary>
        /// 触发最大帧数变化事件
        /// 通知所有监听者时间轴长度已更改，用于更新时间轴显示和滚动范围
        /// </summary>
        /// <param name="maxFrame">新的最大帧数</param>
        public static void TriggerMaxFrameChanged(int maxFrame)
        {
            OnMaxFrameChanged?.Invoke(maxFrame);
        }

        /// <summary>
        /// 触发全局控制面板显示状态切换事件
        /// 通知UI管理器更新全局控制面板的可见性
        /// </summary>
        /// <param name="isShow">是否显示全局控制面板</param>
        public static void TriggerGlobalControlToggled(bool isShow)
        {
            OnGlobalControlToggled?.Invoke(isShow);
        }

        /// <summary>
        /// 触发刷新请求事件
        /// 通知所有相关组件刷新其显示内容，通常在数据变化后调用
        /// </summary>
        public static void TriggerRefreshRequested()
        {
            OnRefreshRequested?.Invoke();
        }

        /// <summary>
        /// 触发技能配置变化事件
        /// 通知所有组件当前编辑的技能配置已更改，用于同步配置相关的UI和数据
        /// </summary>
        /// <param name="config">新的技能配置对象</param>
        public static void TriggerSkillConfigChanged(SkillConfig config)
        {
            OnSkillConfigChanged?.Invoke(config);
        }

        /// <summary>
        /// 触发播放状态变化事件
        /// 通知相关组件技能播放状态已更改，用于更新播放控制UI和相关逻辑
        /// </summary>
        /// <param name="isPlaying">是否正在播放技能</param>
        public static void TriggerPlayStateChanged(bool isPlaying)
        {
            OnPlayStateChanged?.Invoke(isPlaying);
        }

        /// <summary>
        /// 触发时间轴缩放变化事件
        /// 通知时间轴和轨道相关组件更新显示比例，重新计算布局和宽度
        /// </summary>
        public static void TriggerTimelineZoomChanged()
        {
            OnTimelineZoomChanged?.Invoke();
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 清理所有事件监听器
        /// 将所有事件委托设置为null，防止内存泄漏和意外调用
        /// 通常在编辑器窗口关闭或重新初始化时调用
        /// </summary>
        public static void Cleanup()
        {
            OnCurrentFrameChanged = null;
            OnMaxFrameChanged = null;
            OnGlobalControlToggled = null;
            OnRefreshRequested = null;
            OnSkillConfigChanged = null;
            OnPlayStateChanged = null;
            OnTimelineZoomChanged = null;
        }

        /// <summary>
        /// 检查是否有任何事件监听器
        /// 用于调试和验证事件系统的状态
        /// </summary>
        /// <returns>是否存在活动的事件监听器</returns>
        public static bool HasActiveListeners()
        {
            return OnCurrentFrameChanged != null ||
                OnMaxFrameChanged != null ||
                OnGlobalControlToggled != null ||
                OnRefreshRequested != null ||
                OnSkillConfigChanged != null ||
                OnPlayStateChanged != null ||
                OnTimelineZoomChanged != null;
        }

        /// <summary>
        /// 获取活动监听器数量统计
        /// 返回每个事件的监听器数量，用于调试和性能监控
        /// </summary>
        /// <returns>监听器统计信息字符串</returns>
        public static string GetListenerStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("事件监听器统计:");
            stats.AppendLine($"  当前帧变化: {OnCurrentFrameChanged?.GetInvocationList()?.Length ?? 0}");
            stats.AppendLine($"  最大帧变化: {OnMaxFrameChanged?.GetInvocationList()?.Length ?? 0}");
            stats.AppendLine($"  控制面板切换: {OnGlobalControlToggled?.GetInvocationList()?.Length ?? 0}");
            stats.AppendLine($"  刷新请求: {OnRefreshRequested?.GetInvocationList()?.Length ?? 0}");
            stats.AppendLine($"  配置变化: {OnSkillConfigChanged?.GetInvocationList()?.Length ?? 0}");
            stats.AppendLine($"  播放状态变化: {OnPlayStateChanged?.GetInvocationList()?.Length ?? 0}");
            stats.AppendLine($"  时间轴缩放: {OnTimelineZoomChanged?.GetInvocationList()?.Length ?? 0}");
            return stats.ToString();
        }

        #endregion
    }
}
