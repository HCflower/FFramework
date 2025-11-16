// =============================================================
// 描述：行为树节点基类
// 作者：HCFlower
// 创建时间：2025-11-16 16:30:00
// 版本：1.0.0
// =============================================================
using System.Collections.Generic;
using System.Linq;

namespace BehaviourTree
{
    /// <summary>
    /// 行为树节点基类
    /// </summary>
    /// <remarks>
    /// Node（基类）：定义节点名称、优先级、子节点列表及当前子节点索引，提供添加子节点、处理逻辑和重置状态的基础方法。
    /// </remarks>
    public class Node
    {
        public enum Status
        {
            Success,
            Failure,
            Running
        }

        public readonly string name;                    // 名称
        public readonly int priority;                   // 优先级
        public readonly List<Node> childrens = new();   // 子节点列表
        protected int currentChildIndex = 0;            // 当前子节点索引

        public Node(string name = "Node", int priority = 0)
        {
            this.name = name;
            this.priority = priority;
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddChild(Node child) => childrens.Add(child);

        /// <summary>
        /// 处理节点逻辑
        /// </summary>
        public virtual Status Process()
        {
            if (childrens.Count == 0)
                return Status.Failure; // 或者 Success，看你想要的默认行为

            if (currentChildIndex < 0 || currentChildIndex >= childrens.Count)
                currentChildIndex = 0;

            return childrens[currentChildIndex].Process();
        }

        /// <summary>
        /// 重置节点状态
        /// </summary>
        public virtual void Reset()
        {
            currentChildIndex = 0;
            foreach (var child in childrens)
            {
                child.Reset();
            }
        }
    }

    /// <summary>
    /// 叶子节点
    /// </summary>
    /// <remarks>
    /// LeafNode（叶子节点）：包装具体策略（IStrategy），用于行为树的最底层执行单元。
    /// </remarks>
    public class LeafNode : Node
    {
        readonly IStrategy strategy;

        public LeafNode(IStrategy strategy, string name = "LeafNode", int priority = 0) : base(name, priority)
        {
            this.strategy = strategy ?? throw new System.ArgumentNullException(nameof(strategy));
        }

        public override Status Process() => strategy.Process();

        public override void Reset()
        {
            base.Reset();
            strategy.Reset();
        }
    }

    /// <summary>
    /// 序列节点
    /// </summary>
    /// <remarks>
    /// SequenceNode（序列节点）：按顺序依次执行子节点，遇到失败或运行中即停止，全部成功则返回成功。
    /// </remarks>
    public class SequenceNode : Node
    {
        public SequenceNode(string name = "SequenceNode", int priority = 0) : base(name, priority) { }

        public override Status Process()
        {
            while (currentChildIndex < childrens.Count)
            {
                var status = childrens[currentChildIndex].Process();
                if (status == Status.Running)
                    return Status.Running;
                if (status == Status.Failure)
                {
                    Reset();
                    return Status.Failure;
                }
                currentChildIndex++;
            }
            Reset();
            return Status.Success;
        }
    }

    /// <summary>
    /// 选择节点
    /// </summary>
    /// <remarks>
    /// SelectorNode（选择节点）：依次尝试子节点，遇到成功或运行中即停止，全部失败则返回失败。
    /// </remarks>
    public class SelectorNode : Node
    {
        public SelectorNode(string name = "SelectorNode", int priority = 0) : base(name, priority) { }

        public override Status Process()
        {
            while (currentChildIndex < childrens.Count)
            {
                var status = childrens[currentChildIndex].Process();
                if (status == Status.Running)
                    return Status.Running;
                if (status == Status.Success)
                {
                    Reset();
                    return Status.Success;
                }
                currentChildIndex++;
            }
            Reset();
            return Status.Failure;
        }
    }

    /// <summary>
    /// 优先级选择节点
    /// </summary>
    /// <remarks>
    /// PrioritySelectorNode（优先级选择节点）：按优先级高低排序子节点，每次都从最高优先级开始尝试，遇到运行中或成功即停止，全部失败则返回失败。
    /// </remarks>
    public class PrioritySelectorNode : SelectorNode
    {
        List<Node> sortedChildren = new();
        List<Node> SortedChildren => sortedChildren ??= SortChildren();
        protected virtual List<Node> SortChildren() => childrens.OrderByDescending(child => child.priority).ToList();

        public PrioritySelectorNode(string name = "PrioritySelectorNode", int priority = 0) : base(name, priority) { }
        public override Status Process()
        {
            foreach (var child in SortedChildren)
            {
                switch (child.Process())
                {
                    case Status.Running:
                        return Status.Running;
                    case Status.Success:
                        return Status.Success;
                    default:
                        continue;
                }
            }
            return Status.Failure;
        }

        public override void Reset()
        {
            base.Reset();
            sortedChildren = null;
        }
    }

    /// <summary>
    /// 随机选择节点
    /// </summary>
    /// <remarks>
    /// RandomSelectorNode（随机选择节点）：每次处理时随机选择一个子节点执行，返回其状态。
    /// </remarks>
    public class RandomSelectorNode : PrioritySelectorNode
    {
        private static System.Random random = new System.Random();

        protected override List<Node> SortChildren()
        {
            var list = childrens.ToList();
            int count = list.Count;
            while (count > 1)
            {
                --count;
                int index = random.Next(count + 1);
                (list[index], list[count]) = (list[count], list[index]);
            }
            return list;
        }

        public RandomSelectorNode(string name = "RandomSelectorNode", int priority = 0) : base(name, priority) { }
    }

    /// <summary>
    /// 反转节点
    /// </summary>
    /// <remarks>
    /// InverterMode（反转节点）：反转子节点的返回状态，成功变失败，失败变成功，运行中不变。
    /// </remarks>
    public class InverterMode : Node
    {
        public InverterMode(string name = "InverterMode", int priority = 0) : base(name, priority) { }

        public override Status Process()
        {
            if (childrens.Count == 0)
                return Status.Failure;

            var status = childrens[0].Process();
            return status switch
            {
                Status.Success => Status.Failure,
                Status.Failure => Status.Success,
                _ => Status.Running,
            };
        }
    }

    /// <summary>
    /// 直到失败节点
    /// </summary>
    /// <remarks>
    /// UntilFailureNode（直到失败节点）：持续执行子节点直到其返回指定的失败状态，返回成功；否则返回运行中。
    /// </remarks>
    public class UntilFailNode : Node
    {
        public UntilFailNode(string name = "UntilFailureNode", int priority = 0) : base(name, priority) { }
        public override Status Process()
        {
            if (childrens.Count == 0)
                return Status.Failure;

            var status = childrens[0].Process();
            if (status == Status.Failure)
            {
                Reset();
                return Status.Success;
            }
            return Status.Running;
        }
    }
}