// =============================================================
// 描述：仲裁者
// 作者：HCFlower
// 创建时间：2025-11-17 00:22:00
// 版本：1.0.0
// =============================================================
using System;
using System.Collections.Generic;

public class Arbiter
{
    readonly List<IExpert> experts = new();
    public void RegisterExpert(IExpert expert)
    {
        if (expert == null)
            throw new System.ArgumentNullException(nameof(expert), "Expert 不能为 null");
        experts.Add(expert);
    }

    public List<Action> BlackboardIteration(Blackboard blackboard)
    {
        IExpert bestExpert = null;

        int highestInsistence = 0;
        foreach (IExpert expert in experts)
        {
            int insistence = expert.GetInsistence(blackboard);
            if (insistence > highestInsistence)
            {
                highestInsistence = insistence;
                bestExpert = expert;
            }
        }
        bestExpert?.Execute(blackboard);
        var actions = blackboard.Preconditions;
        blackboard.Preconditions.Clear();
        return actions;
    }
}
