using UnityEngine;

namespace FFramework.Kit.Examples
{
    /// <summary>
    /// 示例：空闲状态
    /// </summary>
    public class IdleState : BaseState
    {
        private float idleTime = 0f;
        private const float MAX_IDLE_TIME = 3f;

        protected override void OnEnterState()
        {
            Debug.Log("进入空闲状态");
            idleTime = 0f;
        }

        protected override void OnUpdateState()
        {
            idleTime += Time.deltaTime;

            // 示例：空闲3秒后自动切换到移动状态
            if (idleTime >= MAX_IDLE_TIME)
            {
                ChangeStateDeferred<MoveState>();
            }

            // 示例：检测输入切换状态
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ChangeStateDeferred<JumpState>();
            }
        }

        protected override void OnExitState()
        {
            Debug.Log("退出空闲状态");
        }
    }

    /// <summary>
    /// 示例：移动状态
    /// </summary>
    public class MoveState : BaseState
    {
        private Vector3 moveDirection;
        private float moveSpeed = 5f;

        protected override void OnEnterState()
        {
            Debug.Log("进入移动状态");
            moveDirection = Random.insideUnitSphere.normalized;
        }

        protected override void OnUpdateState()
        {
            // 示例移动逻辑
            var transform = Camera.main?.transform;
            if (transform != null)
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }

            // 检测停止移动的条件
            if (Input.GetKeyDown(KeyCode.S))
            {
                ChangeStateDeferred<IdleState>();
            }

            // 检测跳跃
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ChangeStateDeferred<JumpState>();
            }
        }

        protected override void OnExitState()
        {
            Debug.Log("退出移动状态");
        }
    }

    /// <summary>
    /// 示例：跳跃状态
    /// </summary>
    public class JumpState : BaseState
    {
        private float jumpDuration = 1f;

        protected override void OnEnterState()
        {
            Debug.Log("进入跳跃状态");
        }

        protected override void OnUpdateState()
        {
            // 跳跃持续1秒后返回空闲
            if (StateDuration >= jumpDuration)
            {
                ChangeStateDeferred<IdleState>();
            }
        }

        protected override void OnExitState()
        {
            Debug.Log("退出跳跃状态");
        }
    }

    /// <summary>
    /// 示例状态机控制器
    /// </summary>
    public class ExampleStateMachineController : FSMStateMachineComponent
    {
        protected override void OnStateMachineInitialized()
        {
            // 配置状态转换条件
            var fsm = StateMachine;

            // 添加转换条件（可选，也可以在状态内部直接切换）
            fsm.AddTransition<IdleState, MoveState>(() => Input.GetKeyDown(KeyCode.W));
            fsm.AddTransition<MoveState, IdleState>(() => Input.GetKeyDown(KeyCode.S));

            // 添加全局转换（任意状态都可以触发）
            fsm.AddGlobalTransition<JumpState>(() => Input.GetKeyDown(KeyCode.Space));
        }

        protected override void OnAutoStart()
        {
            // 设置初始状态为空闲状态
            StartStateMachine<IdleState>();
        }
    }
}
