using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 时间计时器集中管理器：
    /// - 使用单向链表管理，避免 List 扩容 GC
    /// - 每帧统一推进，支持按指定时间步长推进
    /// - 提供便捷创建/回收
    /// - 支持批量创建与管理计时器
    /// - 零GC设计，适合高性能场景
    /// </summary>
    public sealed class TimeTimerManager
    {
        #region 字段

        /// <summary> 计时器链表头节点 </summary>
        private TimeTimer head;

        /// <summary> 默认的计时器更新间隔（秒） </summary>
        private float defaultTickInterval = 0.0f;

        #endregion

        #region 计时器创建与配置

        /// <summary>
        /// 创建一个新的计时器并添加到管理器中
        /// </summary>
        /// <param name="totalTime">计时器总时间（秒）</param>
        /// <param name="onStart">计时器开始时的回调，可为null</param>
        /// <param name="onTick">计时器每次更新时的回调，参数为(timer, remainingTime, totalTime)，可为null</param>
        /// <param name="onComplete">计时器完成时的回调，可为null</param>
        /// <param name="onCancel">计时器取消时的回调，可为null</param>
        /// <param name="onPause">计时器暂停时的回调，可为null</param>
        /// <param name="onResume">计时器恢复时的回调，可为null</param>
        /// <param name="tickInterval">Tick回调间隔（秒），null则使用默认值</param>
        /// <returns>创建的计时器实例</returns>
        public TimeTimer CreateTimer(float totalTime,
            Action<TimeTimer> onStart = null,
            Action<TimeTimer, float, float> onTick = null,
            Action<TimeTimer> onComplete = null,
            Action<TimeTimer> onCancel = null,
            Action<TimeTimer> onPause = null,
            Action<TimeTimer> onResume = null,
            float? tickInterval = null)
        {
            float interval = tickInterval.HasValue ? (tickInterval.Value < 0.0f ? 0.0f : tickInterval.Value) : defaultTickInterval;
            var timer = TimeTimer.Rent().Configure(totalTime, onStart, onTick, onComplete, onCancel, onPause, onResume, interval);
            Add(timer);
            return timer;
        }

        /// <summary>
        /// 设置默认的Tick回调间隔时间
        /// </summary>
        /// <param name="interval">间隔时间（秒），如果小于0则设为0</param>
        public void SetDefaultTickInterval(float interval)
        {
            defaultTickInterval = interval < 0.0f ? 0.0f : interval;
        }

        /// <summary>
        /// 添加计时器到管理器中
        /// </summary>
        /// <param name="timer">要添加的计时器实例</param>
        /// <remarks>使用头插法，O(1)复杂度</remarks>
        public void Add(TimeTimer timer)
        {
            if (timer == null) return;
            // 头插
            timer.Next = head;
            head = timer;
        }

        /// <summary>
        /// 从管理器中移除指定计时器
        /// </summary>
        /// <param name="timer">要移除的计时器实例</param>
        /// <remarks>需要遍历链表查找，O(n)复杂度</remarks>
        public void Remove(TimeTimer timer)
        {
            if (timer == null) return;
            TimeTimer prev = null;
            var node = head;
            while (node != null)
            {
                if (node == timer)
                {
                    if (prev == null) head = node.Next; else prev.Next = node.Next;
                    node.Next = null;
                    return;
                }
                prev = node;
                node = node.Next;
            }
        }

        #endregion

        #region 计时器更新与管理

        /// <summary>
        /// 推进所有计时器指定的时间
        /// </summary>
        /// <param name="deltaTime">要推进的时间（秒），通常为Time.deltaTime</param>
        /// <remarks>
        /// 会遍历所有计时器，只更新运行中的计时器。
        /// 先缓存next引用防止回调中修改链表结构导致遍历问题。
        /// </remarks>
        public void TickAll(float deltaTime)
        {
            if (deltaTime <= 0.0f) return;
            var node = head;
            while (node != null)
            {
                var next = node.Next; // 先缓存 next，避免回调导致结构修改
                if (node.IsRunning)
                {
                    node.Tick(deltaTime);
                }
                // 完成或取消的计时器可选择在外部回收。为安全，这里仅保持链表。
                node = next;
            }
        }

        /// <summary>
        /// 清空并回收所有计时器
        /// </summary>
        /// <remarks>
        /// 会遍历所有计时器，取消其运行，并将其归还到对象池。
        /// 此操作完成后，管理器将不包含任何计时器。
        /// </remarks>
        public void ClearAndReturnAll()
        {
            var node = head;
            while (node != null)
            {
                var next = node.Next;
                node.Cancel();
                TimeTimer.Return(node);
                node = next;
            }
            head = null;
        }

        /// <summary>
        /// 预热计时器对象池
        /// </summary>
        /// <param name="count">要预创建的计时器数量</param>
        /// <remarks>
        /// 在游戏初始化阶段调用，可以减少运行时的GC压力。
        /// 直接调用TimeTimer.Warmup方法。
        /// </remarks>
        public void Warmup(int count)
        {
            TimeTimer.Warmup(count);
        }

        /// <summary>
        /// 批量创建计时器
        /// </summary>
        /// <param name="times">每个计时器的时间数组（秒）</param>
        /// <param name="onComplete">所有计时器完成时的统一回调</param>
        /// <param name="onTick">每次更新时的回调，可为null</param>
        /// <param name="tickInterval">Tick回调间隔（秒），null则使用默认值</param>
        /// <param name="offset">times数组的起始偏移，默认为0</param>
        /// <param name="length">要使用的times数组长度，默认为-1表示全部</param>
        /// <remarks>
        /// 根据times数组批量创建多个计时器，每个计时器时间由数组元素决定。
        /// 所有计时器共用相同的回调，并在创建后自动启动。
        /// </remarks>
        public void CreateTimers(float[] times, Action<TimeTimer> onComplete, Action<TimeTimer, float, float> onTick = null, float? tickInterval = null, int offset = 0, int length = -1)
        {
            if (times == null || times.Length == 0) return;
            if (offset < 0) offset = 0;
            if (length < 0 || offset + length > times.Length) length = times.Length - offset;
            int end = offset + length;
            float interval = tickInterval.HasValue ? (tickInterval.Value < 0.0f ? 0.0f : tickInterval.Value) : defaultTickInterval;
            for (int i = offset; i < end; i++)
            {
                var timer = TimeTimer.Rent().Configure(times[i], null, onTick, onComplete, null, null, null, interval);
                Add(timer);
                timer.Start();
            }
        }

        /// <summary>
        /// 获取当前管理器中运行中的计时器数量
        /// </summary>
        /// <returns>运行中的计时器数量</returns>
        public int GetRunningTimerCount()
        {
            int count = 0;
            var node = head;
            while (node != null)
            {
                if (node.IsRunning && !node.IsPaused) count++;
                node = node.Next;
            }
            return count;
        }

        #endregion
    }
}
