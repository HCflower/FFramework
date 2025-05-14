using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework
{
    ///<summary>
    /// 时间系统接口
    /// TODO:优化,对象池父节点
    /// </summary>
    public interface ITimeSystem : ISystem
    {
        public float currentTime { get; }
        public void AddDelayTask(float delayTime, Action onDelayFinished);
    }

    /// <summary>
    /// 计时器类
    /// </summary>
    public class Timer : MonoBehaviour
    {
        public event Action OnUpdate;

        private void Update()
        {
            OnUpdate?.Invoke();
        }
    }

    /// <summary>
    /// 时间系统
    /// </summary>
    public class TimeSystem : AbstractSystem, ITimeSystem
    {
        public float currentTime { get; private set; }
        public LinkedList<DelayTask> delayTasks = new LinkedList<DelayTask>();
        private Queue<DelayTask> delayTaskPool = new Queue<DelayTask>();
        protected override void OnInit()
        {
            currentTime = 0;
            GameObject timer = new GameObject(nameof(Timer));
            timer.AddComponent<Timer>().OnUpdate += () =>
            {
                OnUpdate();
            };
        }

        private void OnUpdate()
        {
            currentTime += Time.deltaTime;
            if (delayTasks.Count > 0)
            {
                var currentTimer = delayTasks.First;
                while (currentTimer != null)
                {
                    var nextTimer = currentTimer.Next;
                    var delayTask = currentTimer.Value;
                    if (delayTask.state == DelayTaskState.NotStart)
                    {
                        delayTask.state = DelayTaskState.Started;
                        delayTask.startTime = currentTime;
                        delayTask.endTime = currentTime + delayTask.delayTime;
                    }
                    else if (delayTask.state == DelayTaskState.Started)
                    {
                        if (currentTime >= delayTask.endTime)
                        {
                            delayTask.state = DelayTaskState.Finished;
                            delayTask.onFinished?.Invoke();

                            delayTask.onFinished = null;
                            delayTasks.Remove(currentTimer);
                            // 回收对象到池中
                            delayTaskPool.Enqueue(delayTask);
                        }
                    }
                    currentTimer = nextTimer;
                }
            }
        }

        //添加延时任务
        public void AddDelayTask(float delayTime, Action onDelayFinished)
        {
            DelayTask delayTask = delayTaskPool.Count > 0 ? delayTaskPool.Dequeue() : new DelayTask();
            delayTask.delayTime = delayTime;
            delayTask.onFinished = onDelayFinished;
            delayTask.state = DelayTaskState.NotStart;
            delayTasks.AddLast(delayTask);
        }

        //取消延时任务
        public bool CancelDelayTask(Action onDelayFinished)
        {
            if (delayTasks.Count == 0 || onDelayFinished == null)
                return false;

            var current = delayTasks.First;
            while (current != null)
            {
                if (current.Value.onFinished == onDelayFinished)
                {
                    var task = current.Value;
                    delayTasks.Remove(current);
                    task.onFinished = null;
                    // 回收到对象池
                    delayTaskPool.Enqueue(task);
                    return true;
                }
                current = current.Next;
            }
            return false;
        }
    }

    /// <summary>
    /// 延时任务
    /// </summary>
    public class DelayTask
    {
        public float delayTime { get; set; }
        public Action onFinished { get; set; }
        public float startTime { get; set; }
        public float endTime { get; set; }
        public DelayTaskState state { get; set; }
    }

    //延时任务状态
    public enum DelayTaskState
    {
        NotStart,
        Started,
        Finished
    }
}
