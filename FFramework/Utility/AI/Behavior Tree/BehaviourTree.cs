// =============================================================
// 描述：行为树
// 作者：HCFlower
// 创建时间：2025-11-16 16:30:00
// 版本：1.0.0
// =============================================================
namespace BehaviourTree
{
    public class BehaviourTree : Node
    {
        public BehaviourTree(string name = "BehaviourTree") : base(name) { }

        public override Status Process()
        {
            while (currentChildIndex < childrens.Count)
            {
                var status = childrens[currentChildIndex].Process();
                if (status != Status.Success)
                {
                    return status;
                }
                currentChildIndex++;
            }
            return Status.Success;
        }
    }
}