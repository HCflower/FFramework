// 定义具体状态
public class IdleState : FSMStateBase
{
public override void OnEnter(FSMStateMachine machine)
{
// 获取强类型的持有者
var player = GetOwner<Player>();
Debug.Log($"{player.name} 进入闲置状态");
}

    public override void OnUpdate(FSMStateMachine machine)
    {
        var player = GetOwner<Player>();
        if (Input.GetKey(KeyCode.W))
        {
            machine.ChangeState<WalkState>();
        }
    }

    public override void OnExit(FSMStateMachine machine)
    {
        Debug.Log("离开闲置状态");
    }

}

public class WalkState : FSMStateBase
{
public override void OnEnter(FSMStateMachine machine)
{
var player = GetOwner<Player>();
Debug.Log($"{player.name} 进入行走状态");
}

    public override void OnUpdate(FSMStateMachine machine)
    {
        var player = GetOwner<Player>();
        if (!Input.GetKey(KeyCode.W))
        {
            machine.ChangeState<IdleState>();
        }
    }

    public override void OnExit(FSMStateMachine machine)
    {
        Debug.Log("离开行走状态");
    }

}

// 使用状态机
public class Player : MonoBehaviour
{
private FSMStateMachine stateMachine;

    void Start()
    {
        // 创建状态机时传入持有者
        stateMachine = new FSMStateMachine(this);

        // 设置默认状态（无需传入持有者类型）
        stateMachine.SetDefault<IdleState>();
    }

    void Update()
    {
        stateMachine.Update();
    }

}
