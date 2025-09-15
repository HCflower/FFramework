using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 转换序列器
    /// </summary>
    public class HSMTransitionSequencer
    {
        public readonly HSMStateMachine stateMachine;
        private ISequence sequencer;                      // 当前序列
        private Action nextPhase;                         // 下一个序列
        private (HSMState from, HSMState to)? pending;    // 待处理的状态转换
        private HSMState lastFrom, lastTo;                // 最后一个转换的状态

        public HSMTransitionSequencer(HSMStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        /// <summary>
        /// 请求一个状态过渡到另一个状态
        /// </summary>
        /// <param name="from">要被切换的状态</param>
        /// <param name="to">要切换到的状态</param>
        public void RequestTransition(HSMState from, HSMState to)
        {
            if (to == null || to == from) return;
            if (sequencer != null)
            {
                pending = (from, to);
                return;
            }
            BeginTransition(from, to);
        }

        // 获取状态转换序列
        private List<PhaseStep> GetHerPhaseSteps(List<HSMState> chain, bool deactivate)
        {
            var steaps = new List<PhaseStep>();
            for (var i = 0; i < chain.Count; i++)
            {
                var activities = chain[i].Activities;
                for (var j = 0; j < activities.Count; j++)
                {
                    var activity = activities[j];
                    if (deactivate)
                    {
                        if (activity.Mode == HSMActivityMode.Active)
                        {
                            steaps.Add(token => activity.DeactivateAsync(token));
                        }
                        else
                        {
                            steaps.Add(token => activity.ActivateAsync(token));
                        }
                    }
                }
            }
            return steaps;
        }

        // 退出分支状态 从下向上停用状态
        private static List<HSMState> StatesToExit(HSMState from, HSMState lca)
        {
            var list = new List<HSMState>();
            for (var state = from; state != null && state != lca; state = state.parent)
            {
                list.Add(state);
            }
            return list;
        }

        // 进入分支状态 从上向下激活状态
        private static List<HSMState> StatesToEnter(HSMState to, HSMState lca)
        {
            // 使用栈保证顺序
            var stack = new Stack<HSMState>();
            for (var state = to; state != lca; state = state.parent)
            {
                stack.Push(state);
            }
            return new List<HSMState>(stack);
        }

        CancellationTokenSource source;
        // 设置false以并行使用
        public readonly bool UseSequential = true;

        // 开始转换状态之前
        private void BeginTransition(HSMState from, HSMState to)
        {
            var lca = LowestCommonAncestor(from, to);
            var exitChain = StatesToExit(from, lca);
            var enterChain = StatesToEnter(to, lca);

            // 停用“旧分支”
            var exitSteps = GetHerPhaseSteps(exitChain, deactivate: true);
            sequencer = UseSequential
            ? new SequentialPhase(exitSteps, source.Token)
            : new ParallelPhase(exitSteps, source.Token);
            sequencer.Start();

            nextPhase = () =>
            {
                // 切换状态
                stateMachine.ChangeState(from, to);
                // 激活“新分支”
                var enterSteps = GetHerPhaseSteps(enterChain, deactivate: false);
                sequencer = UseSequential
                ? new SequentialPhase(enterSteps, source.Token)
                : new ParallelPhase(enterSteps, source.Token);
                sequencer.Start();
            };
        }

        // 转换状态之后
        private void EndTransition()
        {
            // 清理当前序列器
            sequencer = null;
            if (pending.HasValue)
            {
                (HSMState from, HSMState to) p = pending.Value; // 如果有挂起的状态转换，请开始它
                pending = null;
                BeginTransition(p.from, p.to);
            }
        }

        public void Tick(float deltaTime)
        {
            // 检查是否存在过渡
            if (sequencer != null)
            {
                // 检查当前序列是否完成
                if (sequencer.Update())
                {
                    // 如果完成，请开始下一个序列
                    if (nextPhase != null)
                    {
                        Action next = nextPhase;
                        nextPhase = null;
                        next();
                    }
                    else
                    {
                        // 否则，结束转换
                        EndTransition();
                    }
                }
                // 过渡时，我们不运行正常更新
                return;
            }
            stateMachine.InternalTick(deltaTime);
        }

        /// <summary>
        /// 寻找两个状态的最近公共祖先
        /// </summary>
        /// <param name="stateA">状态A</param>
        /// <param name="stateB">状态B</param>
        /// <returns></returns>
        public static HSMState LowestCommonAncestor(HSMState stateA, HSMState stateB)
        {
            // 创建一组“A”的父母
            var stateAParent = new HashSet<HSMState>();
            for (var s = stateA; s != null; s = s.parent)
            {
                stateAParent.Add(s);
            }

            // 找到“B”的第一位父母，也是“A”的父母
            for (var s = stateB; s != null; s = s.parent)
            {
                if (stateAParent.Contains(s)) return s;
            }

            // 如果找不到共同的祖先，请退回null
            return null;
        }
    }

    // 序列接口 
    public interface ISequence
    {
        bool isDone { get; }  // 是否完成
        void Start();         // 开始
        bool Update();        // 更新
    }

    // 空序列
    public class NoopPhase : ISequence
    {
        public bool isDone { get; private set; }
        // 立即完成
        public void Start() => isDone = true;
        public bool Update() => isDone;
    }

    // 一个活动操作（激活或停用）以在此阶段运行。
    public delegate Task PhaseStep(CancellationToken token);

    /// <summary>
    /// 顺序序列
    /// </summary>
    public class SequentialPhase : ISequence
    {
        private readonly List<PhaseStep> steps;
        private readonly CancellationToken token;
        int index = -1;
        private Task current;
        public bool isDone { get; private set; }

        public SequentialPhase(List<PhaseStep> steps, CancellationToken token)
        {
            this.steps = steps;
            this.token = token;
        }

        public void Start()
        {
            NextStep();
        }

        public bool Update()
        {
            if (isDone) return true;
            if (current == null || current.IsCompleted) NextStep();
            return isDone;
        }

        void NextStep()
        {
            index++;
            if (index >= steps.Count)
            {
                isDone = true;
                return;
            }
            current = steps[index](token);
        }
    }

    ///<summary>
    /// 并行序列
    /// </summary>
    public class ParallelPhase : ISequence
    {
        private readonly List<PhaseStep> steps;
        private readonly CancellationToken token;
        private List<Task> tasks;
        public bool isDone { get; private set; }

        public ParallelPhase(List<PhaseStep> steps, CancellationToken token)
        {
            this.steps = steps;
            this.token = token;
        }

        public void Start()
        {
            if (steps == null || steps.Count == 0)
            {
                isDone = true;
                return;
            }
            tasks = new List<Task>(steps.Count);
            foreach (var step in steps)
            {
                tasks.Add(step(token));
            }
        }

        public bool Update()
        {
            if (isDone) return true;
            isDone = tasks == null || tasks.TrueForAll(t => t.IsCompleted);
            return isDone;
        }
    }
}