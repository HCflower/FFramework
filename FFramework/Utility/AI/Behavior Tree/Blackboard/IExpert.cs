// =============================================================
// 描述：专家接口
// 作者：HCFlower
// 创建时间：2025-11-17 00:22:00
// 版本：1.0.0
// =============================================================
public interface IExpert
{
    int GetInsistence(Blackboard blackboard);
    void Execute(Blackboard blackboard);
}
