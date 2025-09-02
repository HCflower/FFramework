using System.Collections.Generic;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 高级按时间计时器：
    /// - 不依赖 Unity
    /// - 按时间推进（Tick）
    /// - 0GC（运行期不分配；回调引用在启动前绑定）
    /// - 自动停止（到时完成）
    /// - 高复用（对象池复用 Reset）
    /// - 隐藏业务细节（仅暴露必要状态与回调）
    /// </summary>
    public sealed class TimeTimer
    {
        #region 对象池管理

        // 计时器对象池，避免频繁创建实例导致GC
        private static readonly Stack<TimeTimer> timerPool = new Stack<TimeTimer>(64);

        /// <summary>
        /// 从对象池获取计时器实例
        /// </summary>
        /// <returns>可复用的计时器实例</returns>
        public static TimeTimer Rent()
        {
            return timerPool.Count > 0 ? timerPool.Pop() : new TimeTimer();
        }

        /// <summary>
        /// 将计时器归还对象池
        /// </summary>
        /// <param name="timer">待回收的计时器实例</param>
        public static void Return(TimeTimer timer)
        {
            if (timer == null) return;
            timer.InternalReset();
            timerPool.Push(timer);
        }

        #endregion

        #region 状态字段

        //  计时器总时间（秒）
        private float totalTime;

        // 剩余时间（秒）
        private float remainingTime;

        // 是否正在运行中
        private bool isRunning;

        // 是否已暂停
        private bool isPaused;

        // 每多少秒触发一次 OnTick 回调，默认每帧(0.0f)
        private float tickInterval = 0.0f;

        // 上次通知回调时的剩余时间，用于计算是否需要触发 OnTick
        private float lastNotifiedRemaining;

        /// <summary>
        /// 管理器用的单向链表指针（避免 List 扩容产生 GC）
        /// </summary>
        public TimeTimer Next;

        #endregion

        #region 回调委托

        /// <summary>
        /// 计时器启动时回调
        /// </summary>
        public Action<TimeTimer> OnStart;

        /// <summary>
        /// 计时器每次更新时回调，根据 tickInterval 可能跳过部分更新
        /// </summary>
        /// <remarks>参数为: (timer, remainingTime, totalTime)</remarks>
        public Action<TimeTimer, float, float> OnTick;

        /// <summary>
        /// 计时器暂停时回调
        /// </summary>
        public Action<TimeTimer> OnPause;

        /// <summary>
        /// 计时器恢复时回调
        /// </summary>
        public Action<TimeTimer> OnResume;

        /// <summary>
        /// 计时器正常完成时回调（倒数到0）
        /// </summary>
        public Action<TimeTimer> OnComplete;

        /// <summary>
        /// 计时器被取消时回调
        /// </summary>
        public Action<TimeTimer> OnCancel;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取计时器总时间（秒）
        /// </summary>
        public float TotalTime => totalTime;

        /// <summary>
        /// 获取计时器剩余时间（秒）
        /// </summary>
        public float RemainingTime => remainingTime;

        /// <summary>
        /// 获取计时器是否正在运行中
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        /// 获取计时器是否已暂停
        /// </summary>
        public bool IsPaused => isPaused;

        /// <summary>
        /// 获取计时器是否已完成计时
        /// </summary>
        public bool IsCompleted => isRunning == false && remainingTime <= 0.0f;

        #endregion

        // 私有构造函数，通过对象池获取实例
        private TimeTimer() { }

        /// <summary>
        /// 配置计时器参数
        /// </summary>
        /// <param name="totalTime">计时器总时间（秒），如果小于0会被设为0</param>
        /// <param name="onStart">开始计时时回调，可为null</param>
        /// <param name="onTick">每次更新时回调，可为null</param>
        /// <param name="onComplete">计时完成时回调，可为null</param>
        /// <param name="onCancel">计时取消时回调，可为null</param>
        /// <param name="onPause">计时暂停时回调，可为null</param>
        /// <param name="onResume">计时恢复时回调，可为null</param>
        /// <param name="tickInterval">Tick回调间隔（秒），默认为0表示每次Tick都回调</param>
        /// <returns>当前计时器实例，支持链式调用</returns>
        public TimeTimer Configure(float totalTime,
            Action<TimeTimer> onStart = null,
            Action<TimeTimer, float, float> onTick = null,
            Action<TimeTimer> onComplete = null,
            Action<TimeTimer> onCancel = null,
            Action<TimeTimer> onPause = null,
            Action<TimeTimer> onResume = null,
            float tickInterval = 0.0f)
        {
            this.totalTime = totalTime < 0.0f ? 0.0f : totalTime;
            remainingTime = this.totalTime;
            this.tickInterval = tickInterval < 0.0f ? 0.0f : tickInterval;
            lastNotifiedRemaining = remainingTime;
            OnStart = onStart;
            OnTick = onTick;
            OnComplete = onComplete;
            OnCancel = onCancel;
            OnPause = onPause;
            OnResume = onResume;
            return this;
        }

        #region 公开控制方法

        /// <summary>
        /// 启动计时器
        /// </summary>
        /// <remarks>
        /// 如果计时器已在运行则忽略。
        /// 启动时会立即触发OnStart回调和首次OnTick回调。
        /// 如果总时间为0，会立即触发OnComplete回调。
        /// </remarks>
        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            isPaused = false;
            SafeInvokeStart();
            // 起始时间按策略回调
            MaybeNotifyTick(force: true);
            // 若 total 为 0，直接完成
            if (remainingTime <= 0.0f)
            {
                CompleteInternal();
            }
        }

        /// <summary>
        /// 取消计时器
        /// </summary>
        /// <remarks>
        /// 只有在运行状态才能取消。
        /// 取消后会触发OnCancel回调。
        /// </remarks>
        public void Cancel()
        {
            if (!isRunning) return;
            isRunning = false;
            isPaused = false;
            remainingTime = 0.0f;
            SafeInvokeCancel();
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <remarks>
        /// 只有在运行且未暂停状态才能暂停。
        /// 暂停后会触发OnPause回调。
        /// </remarks>
        public void Pause()
        {
            if (!isRunning || isPaused) return;
            isPaused = true;
            SafeInvokePause();
        }

        /// <summary>
        /// 恢复已暂停的计时器
        /// </summary>
        /// <remarks>
        /// 只有在运行且已暂停状态才能恢复。
        /// 恢复后会触发OnResume回调。
        /// </remarks>
        public void Resume()
        {
            if (!isRunning || !isPaused) return;
            isPaused = false;
            SafeInvokeResume();
        }

        /// <summary>
        /// 推进计时器一定时间
        /// </summary>
        /// <param name="deltaTime">要推进的时间（秒），通常为Time.deltaTime</param>
        /// <remarks>
        /// 只有在运行且未暂停状态下才会推进。
        /// 如果deltaTime小于等于0，则不执行任何操作。
        /// 推进后可能会触发OnTick回调（取决于tickInterval设置）。
        /// 如果剩余时间减至0或以下，会触发OnComplete回调。
        /// </remarks>
        public void Tick(float deltaTime)
        {
            if (!isRunning || isPaused || remainingTime <= 0.0f) return;
            if (deltaTime <= 0.0f) return;

            // 扣减
            float before = remainingTime;
            float after = before - deltaTime;
            remainingTime = after > 0.0f ? after : 0.0f;

            // 根据 tickInterval 判定是否回调
            MaybeNotifyTick(force: false);

            if (remainingTime <= 0.0f)
            {
                CompleteInternal();
            }
        }

        #endregion

        #region 内部方法

        // 计时器完成时的内部处理
        private void CompleteInternal()
        {
            isRunning = false;
            isPaused = false;
            SafeInvokeComplete();
        }

        // 安全调用Start回调
        private void SafeInvokeStart()
        {
            var callback = OnStart; if (callback != null) callback(this);
        }

        // 安全调用Tick回调
        private void SafeInvokeTick()
        {
            var callback = OnTick; if (callback != null) callback(this, remainingTime, totalTime);
        }

        // 根据条件决定是否触发Tick回调
        private void MaybeNotifyTick(bool force)
        {
            if (force)
            {
                SafeInvokeTick();
                lastNotifiedRemaining = remainingTime;
                return;
            }

            if (tickInterval <= 0.0f)
            {
                // 每次都回调
                SafeInvokeTick();
                lastNotifiedRemaining = remainingTime;
                return;
            }

            // 计算距离上次通知过了多少时间
            float delta = lastNotifiedRemaining - remainingTime;
            if (delta >= tickInterval || remainingTime <= 0.0f)
            {
                // 满足间隔条件或计时完成时触发回调
                SafeInvokeTick();
                lastNotifiedRemaining = remainingTime;
            }
        }

        // 安全调用Pause回调
        private void SafeInvokePause()
        {
            var callback = OnPause; if (callback != null) callback(this);
        }

        // 安全调用Resume回调
        private void SafeInvokeResume()
        {
            var callback = OnResume; if (callback != null) callback(this);
        }

        // 安全调用Complete回调
        private void SafeInvokeComplete()
        {
            var callback = OnComplete; if (callback != null) callback(this);
        }

        // 安全调用Cancel回调
        private void SafeInvokeCancel()
        {
            var callback = OnCancel; if (callback != null) callback(this);
        }

        // 重置计时器内部状态，用于对象池回收
        private void InternalReset()
        {
            totalTime = 0.0f;
            remainingTime = 0.0f;
            isRunning = false;
            isPaused = false;
            tickInterval = 0.0f;
            lastNotifiedRemaining = 0.0f;
            Next = null;
            // 清空所有回调引用，避免内存泄漏
            OnStart = null;
            OnTick = null;
            OnPause = null;
            OnResume = null;
            OnComplete = null;
            OnCancel = null;
        }

        #endregion

        /// <summary>
        /// 预热计时器对象池，减少运行时分配
        /// </summary>
        /// <param name="count">要预创建的计时器数量</param>
        /// <remarks>
        /// 在游戏初始化阶段调用，可以减少运行时的GC压力。
        /// </remarks>
        public static void Warmup(int count)
        {
            if (count <= 0) return;
            var temp = new TimeTimer[count];
            for (int i = 0; i < count; i++) temp[i] = Rent();
            for (int i = 0; i < count; i++) Return(temp[i]);
        }
    }
}
