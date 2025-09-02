using System;

namespace FFramework.Kit
{
	/// <summary>
	/// 计时器集中管理器：
	/// - 使用单向链表管理，避免 List 扩容 GC
	/// - 每帧统一推进，支持按指定帧步长推进
	/// - 提供便捷创建/回收
	/// - 支持批量创建与管理计时器
	/// - 零GC设计，适合高性能场景
	/// </summary>
	public sealed class TimerManager
	{
		#region 字段

		/// <summary> 计时器链表头节点 </summary>
		private FrameTimer head;

		/// <summary> 默认的计时器更新间隔帧数 </summary>
		private int defaultTickInterval = 1;

		#endregion

		#region 计时器创建与配置

		/// <summary>
		/// 创建一个新的计时器并添加到管理器中
		/// </summary>
		/// <param name="totalFrames">计时器总帧数</param>
		/// <param name="onStart">计时器开始时的回调，可为null</param>
		/// <param name="onTick">计时器每帧更新时的回调，参数为(timer, remainingFrames, totalFrames)，可为null</param>
		/// <param name="onComplete">计时器完成时的回调，可为null</param>
		/// <param name="onCancel">计时器取消时的回调，可为null</param>
		/// <param name="onPause">计时器暂停时的回调，可为null</param>
		/// <param name="onResume">计时器恢复时的回调，可为null</param>
		/// <param name="tickInterval">Tick回调间隔帧数，null则使用默认值</param>
		/// <returns>创建的计时器实例</returns>
		public FrameTimer CreateTimer(int totalFrames,
			Action<FrameTimer> onStart = null,
			Action<FrameTimer, int, int> onTick = null,
			Action<FrameTimer> onComplete = null,
			Action<FrameTimer> onCancel = null,
			Action<FrameTimer> onPause = null,
			Action<FrameTimer> onResume = null,
			int? tickInterval = null)
		{
			int interval = tickInterval.HasValue ? (tickInterval.Value <= 0 ? 1 : tickInterval.Value) : defaultTickInterval;
			var timer = FrameTimer.Rent().Configure(totalFrames, onStart, onTick, onComplete, onCancel, onPause, onResume, interval);
			Add(timer);
			return timer;
		}

		/// <summary>
		/// 设置默认的Tick回调间隔帧数
		/// </summary>
		/// <param name="interval">间隔帧数，如果小于等于0则设为1</param>
		public void SetDefaultTickInterval(int interval)
		{
			defaultTickInterval = interval <= 0 ? 1 : interval;
		}

		/// <summary>
		/// 添加计时器到管理器中
		/// </summary>
		/// <param name="timer">要添加的计时器实例</param>
		/// <remarks>使用头插法，O(1)复杂度</remarks>
		public void Add(FrameTimer timer)
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
		public void Remove(FrameTimer timer)
		{
			if (timer == null) return;
			FrameTimer prev = null;
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
		/// 推进所有计时器一帧
		/// </summary>
		/// <remarks>等同于调用TickAll(1)</remarks>
		public void TickAll()
		{
			TickAll(1);
		}

		/// <summary>
		/// 推进所有计时器指定的帧数
		/// </summary>
		/// <param name="deltaFrames">要推进的帧数，必须大于0</param>
		/// <remarks>
		/// 会遍历所有计时器，只更新运行中的计时器。
		/// 先缓存next引用防止回调中修改链表结构导致遍历问题。
		/// </remarks>
		public void TickAll(int deltaFrames)
		{
			if (deltaFrames <= 0) return;
			var node = head;
			while (node != null)
			{
				var next = node.Next; // 先缓存 next，避免回调导致结构修改
				if (node.IsRunning)
				{
					node.Tick(deltaFrames);
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
				FrameTimer.Return(node);
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
		/// 直接调用FrameTimer.Warmup方法。
		/// </remarks>
		public void Warmup(int count)
		{
			FrameTimer.Warmup(count);
		}

		/// <summary>
		/// 批量创建计时器
		/// </summary>
		/// <param name="frames">每个计时器的帧数数组</param>
		/// <param name="onComplete">所有计时器完成时的统一回调</param>
		/// <param name="onTick">每帧更新时的回调，可为null</param>
		/// <param name="tickInterval">Tick回调间隔帧数，null则使用默认值</param>
		/// <param name="offset">frames数组的起始偏移，默认为0</param>
		/// <param name="length">要使用的frames数组长度，默认为-1表示全部</param>
		/// <remarks>
		/// 根据frames数组批量创建多个计时器，每个计时器帧数由数组元素决定。
		/// 所有计时器共用相同的回调，并在创建后自动启动。
		/// </remarks>
		public void CreateTimers(int[] frames, Action<FrameTimer> onComplete, Action<FrameTimer, int, int> onTick = null, int? tickInterval = null, int offset = 0, int length = -1)
		{
			if (frames == null || frames.Length == 0) return;
			if (offset < 0) offset = 0;
			if (length < 0 || offset + length > frames.Length) length = frames.Length - offset;
			int end = offset + length;
			int interval = tickInterval.HasValue ? (tickInterval.Value <= 0 ? 1 : tickInterval.Value) : defaultTickInterval;
			for (int i = offset; i < end; i++)
			{
				var timer = FrameTimer.Rent().Configure(frames[i], null, onTick, onComplete, null, null, null, interval);
				Add(timer);
				timer.Start();
			}
		}

		#endregion
	}
}


