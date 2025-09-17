using FFramework.Kit;
using UnityEngine;

namespace SkillEditorExamples
{
    /// <summary>
    /// 编辑器示例-玩家奔跑状态
    /// </summary>
    public class Player_Run : FSMStateBase<PlayerController>
    {
        private float lastMovementTime;
        private const float MOVEMENT_BUFFER = 0.2f; // 200ms缓冲

        public override async void OnEnter(FSMStateMachine<PlayerController> machine)
        {
            owner.canMove = true;
            lastMovementTime = Time.time;
            await owner.playSmartAnima.ChangeAnima(owner.run, owner.transitionTime);
        }

        public override async void OnUpdate(FSMStateMachine<PlayerController> machine)
        {
            // 更新最后移动时间
            if (owner.velocity.magnitude > 0.01f)
            {
                lastMovementTime = Time.time;
            }

            // 技能切换
            if (Input.GetKeyDown(KeyCode.E))
            {
                await owner.playSmartAnima.ChangeAnima(owner.idle, owner.transitionTime); // 使用过渡
                machine.ChangeState<Player_Skill>();
                return;
            }

            // 使用时间缓冲检测真正的停止
            if (Time.time - lastMovementTime > MOVEMENT_BUFFER)
            {
                await owner.playSmartAnima.ChangeAnima(owner.idle, owner.transitionTime); // 使用过渡
                machine.ChangeState<Player_Idle>();
                return;
            }
        }

        public override void OnExit(FSMStateMachine<PlayerController> machine)
        {
            // 清理
        }
    }
}