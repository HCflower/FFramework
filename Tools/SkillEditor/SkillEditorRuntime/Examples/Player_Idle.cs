using FFramework.Kit;
using UnityEngine;

namespace SkillEditorExamples
{
    /// <summary>
    /// 编辑器示例-玩家待机状态
    /// </summary>
    public class Player_Idle : FSMStateBase<PlayerController>
    {
        public override async void OnEnter(FSMStateMachine<PlayerController> machine)
        {
            owner.canMove = true;
            await owner.playSmartAnima.ChangeAnima(owner.idle, owner.transitionTime);
        }

        public override async void OnUpdate(FSMStateMachine<PlayerController> machine)
        {
            // 技能切换
            if (Input.GetKeyDown(KeyCode.E))
            {
                await owner.playSmartAnima.ChangeAnima(owner.idle, owner.transitionTime); // 使用过渡
                machine.ChangeState<Player_Skill>();
                return;
            }

            // 移动切换到奔跑 - 使用过渡
            if (owner.velocity.magnitude > 0.01f)
            {
                await owner.playSmartAnima.ChangeAnima(owner.run, owner.transitionTime); // 使用过渡
                machine.ChangeState<Player_Run>();
                return;
            }
        }

        public override void OnExit(FSMStateMachine<PlayerController> machine)
        {

        }
    }
}
