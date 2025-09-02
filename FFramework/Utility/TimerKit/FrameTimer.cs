using System.Collections.Generic;
using System;

namespace FFramework.Kit
{
	/// <summary>
	/// 高级按帧计时器：
	/// - 不依赖 Unity
	/// - 按帧推进（Tick）
	/// - 0GC（运行期不分配；回调引用在启动前绑定）
	/// - 自动停止（到时完成）
	/// - 高复用（对象池复用 Reset）
	/// - 隐藏业务细节（仅暴露必要状态与回调）
	/// </summary>
	public sealed class FrameTimer
	{
		#region 对象池管理

		// 计时器对象池，避免频繁创建实例导致GC
		private static readonly Stack<FrameTimer> timerPool = new Stack<FrameTimer>(64);

		/// <summary>
		/// 从对象池获取计时器实例
		/// </summary>
		/// <returns>可复用的计时器实例</returns>
		public static FrameTimer Rent()
		{
			return timerPool.Count > 0 ? timerPool.Pop() : new FrameTimer();
		}

		/// <summary>
		/// 将计时器归还对象池
		/// </summary>
		/// <param name="timer">待回收的计时器实例</param>
		public static void Return(FrameTimer timer)
		{
			if (timer == null) return;
			timer.InternalReset();
			timerPool.Push(timer);
		}

		#endregion

		#region 状态字段

		//  计时器总帧数
		private int totalFrames;

		// 剩余帧数
		private int remainingFrames;

		// 是否正在运行中
		private bool isRunning;

		// 是否已暂停
		private bool isPaused;

		// 每多少帧触发一次 OnTick 回调，默认每帧(1)
		private int tickInterval = 1;

		// 上次通知回调时的剩余帧数，用于计算是否需要触发 OnTick
		private int lastNotifiedRemaining;

		/// <summary>
		/// 管理器用的单向链表指针（避免 List 扩容产生 GC）
		/// </summary>
		public FrameTimer Next;

		#endregion

		#region 回调委托

		/// <summary>
		/// 计时器启动时回调
		/// </summary>
		/// <remarks>回调由上层在 Start 前绑定，Tick 期间不修改，避免闭包与装箱</remarks>
		public Action<FrameTimer> OnStart;

		/// <summary>
		/// 计时器每帧更新时回调，根据 tickInterval 可能跳过部分帧
		/// </summary>
		/// <remarks>参数为: (timer, remainingFrames, totalFrames)</remarks>
		public Action<FrameTimer, int, int> OnTick;

		/// <summary>
		/// 计时器暂停时回调
		/// </summary>
		public Action<FrameTimer> OnPause;

		/// <summary>
		/// 计时器恢复时回调
		/// </summary>
		public Action<FrameTimer> OnResume;

		/// <summary>
		/// 计时器正常完成时回调（倒数到0）
		/// </summary>
		public Action<FrameTimer> OnComplete;

		/// <summary>
		/// 计时器被取消时回调
		/// </summary>
		public Action<FrameTimer> OnCancel;

		#endregion

		#region 公开属性

		/// <summary>
		/// 获取计时器总帧数
		/// </summary>
		public int TotalFrames => totalFrames;

		/// <summary>
		/// 获取计时器剩余帧数
		/// </summary>
		public int RemainingFrames => remainingFrames;

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
		public bool IsCompleted => isRunning == false && remainingFrames == 0;

		#endregion

		// 私有构造函数，通过对象池获取实例
		private FrameTimer() { }

		/// <summary>
		/// 配置计时器参数
		/// </summary>
		/// <param name="totalFrames">计时器总帧数，如果小于0会被设为0</param>
		/// <param name="onStart">开始计时时回调，可为null</param>
		/// <param name="onTick">每帧更新时回调，可为null</param>
		/// <param name="onComplete">计时完成时回调，可为null</param>
		/// <param name="onCancel">计时取消时回调，可为null</param>
		/// <param name="onPause">计时暂停时回调，可为null</param>
		/// <param name="onResume">计时恢复时回调，可为null</param>
		/// <param name="tickInterval">Tick回调间隔帧数，默认为1表示每帧都回调</param>
		/// <returns>当前计时器实例，支持链式调用</returns>
		public FrameTimer Configure(int totalFrames,
			Action<FrameTimer> onStart = null,
			Action<FrameTimer, int, int> onTick = null,
			Action<FrameTimer> onComplete = null,
			Action<FrameTimer> onCancel = null,
			Action<FrameTimer> onPause = null,
			Action<FrameTimer> onResume = null,
			int tickInterval = 1)
		{
			this.totalFrames = totalFrames < 0 ? 0 : totalFrames;
			remainingFrames = this.totalFrames;
			this.tickInterval = tickInterval <= 0 ? 1 : tickInterval;
			lastNotifiedRemaining = remainingFrames;
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
		/// 启动时会立即触发OnStart回调和首帧OnTick回调。
		/// 如果总帧数为0，会立即触发OnComplete回调。
		/// </remarks>
		public void Start()
		{
			if (isRunning) return;
			isRunning = true;
			isPaused = false;
			SafeInvokeStart();
			// 起始帧按策略回调
			MaybeNotifyTick(force: true);
			// 若 total 为 0，直接完成
			if (remainingFrames <= 0)
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
			remainingFrames = 0;
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
		/// 推进计时器若干帧
		/// </summary>
		/// <param name="deltaFrames">要推进的帧数，通常每帧调一次，传1</param>
		/// <remarks>
		/// 只有在运行且未暂停状态下才会推进。
		/// 如果deltaFrames小于等于0，则不执行任何操作。
		/// 推进后可能会触发OnTick回调（取决于tickInterval设置）。
		/// 如果剩余帧数减至0，会触发OnComplete回调。
		/// </remarks>
		public void Tick(int deltaFrames)
		{
			if (!isRunning || isPaused || remainingFrames <= 0) return;
			if (deltaFrames <= 0) return;

			// 扣减
			int before = remainingFrames;
			int after = before - deltaFrames;
			remainingFrames = after > 0 ? after : 0;

			// 根据 tickInterval 判定是否回调
			MaybeNotifyTick(force: false);

			if (remainingFrames == 0)
			{
				CompleteInternal();
			}
		}

		/// <summary>
		/// 推进计时器一帧（便捷方法）
		/// </summary>
		/// <remarks>等同于调用Tick(1)</remarks>
		public void Tick()
		{
			Tick(1);
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
			var callback = OnTick; if (callback != null) callback(this, remainingFrames, totalFrames);
		}

		// 根据条件决定是否触发Tick回调
		private void MaybeNotifyTick(bool force)
		{
			if (force)
			{
				SafeInvokeTick();
				lastNotifiedRemaining = remainingFrames;
				return;
			}

			if (tickInterval <= 1)
			{
				// 每帧都回调
				SafeInvokeTick();
				lastNotifiedRemaining = remainingFrames;
				return;
			}

			// 计算距离上次通知过了多少帧
			int delta = lastNotifiedRemaining - remainingFrames;
			if (delta >= tickInterval || remainingFrames == 0)
			{
				// 满足间隔条件或计时完成时触发回调
				SafeInvokeTick();
				lastNotifiedRemaining = remainingFrames;
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
			totalFrames = 0;
			remainingFrames = 0;
			isRunning = false;
			isPaused = false;
			tickInterval = 1;
			lastNotifiedRemaining = 0;
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
			var temp = new FrameTimer[count];
			for (int i = 0; i < count; i++) temp[i] = Rent();
			for (int i = 0; i < count; i++) Return(temp[i]);
		}
	}
}


