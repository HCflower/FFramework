// =============================================================
// 描述：黑板控制器
// 作者：HCFlower
// 创建时间：2025-11-17 00:37:00
// 版本：1.0.0
// =============================================================
using UnityEngine;
public class BlackboardController : MonoBehaviour
{
    [SerializeField] private BlackboardData blackboardData;
    readonly Blackboard blackboard = new Blackboard();
    readonly Arbiter arbiter = new Arbiter();

    void Awake()
    {
        blackboardData.SetValuesOnBlackboard(blackboard);
        blackboard.Debug();
    }

    void Update()
    {
        foreach (var action in arbiter.BlackboardIteration(blackboard))
        {
            action();
        }
    }

    public Blackboard GetBlackboard() => blackboard;

    public void RegisterExpert(IExpert expert) => arbiter.RegisterExpert(expert);
}
