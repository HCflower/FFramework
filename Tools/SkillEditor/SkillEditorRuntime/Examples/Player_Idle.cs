using FFramework.Kit;
using UnityEngine;

namespace SkillEditorExamples
{
    /// <summary>
    /// 编辑器示例-玩家待机状态
    /// </summary>
    public class Player_Idle : StateBase<PlayerController>
    {
        public override void OnEnter(FSMStateMachine<PlayerController> machine)
        {

            owner.playSmartAnima.ChangeAnima(owner.idle, owner.transitionTime);
        }

        public override void OnUpdate(FSMStateMachine<PlayerController> machine)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                machine.ChangeState<Player_Skill>();
            }
            if (owner.velocity.magnitude > 0.1f)
            {
                machine.ChangeState<Player_Run>();
            }
        }

        public override void OnExit(FSMStateMachine<PlayerController> machine)
        {

        }
    }
}
