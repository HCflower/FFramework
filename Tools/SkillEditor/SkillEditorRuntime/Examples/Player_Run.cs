using FFramework.Kit;
using UnityEngine;

namespace SkillEditorExamples
{
    /// <summary>
    /// 编辑器示例-玩家奔跑状态
    /// </summary>
    public class Player_Run : StateBase<PlayerController>
    {
        private float idleBufferTime = 0.15f; // 缓冲时间（秒）
        private float idleBufferCounter = 0f;

        public override void OnEnter(FSMStateMachine<PlayerController> machine)
        {
            owner.canMove = true;
            owner.playSmartAnima.ChangeAnima(owner.run, owner.transitionTime);
        }

        public override void OnExit(FSMStateMachine<PlayerController> machine)
        {

        }

        public override void OnUpdate(FSMStateMachine<PlayerController> machine)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                machine.ChangeState<Player_Skill>();
                return;
            }

            if (owner.velocity.magnitude <= 0.1f)
            {
                idleBufferCounter += Time.deltaTime;
                if (idleBufferCounter >= idleBufferTime)
                {
                    machine.ChangeState<Player_Idle>();
                    idleBufferCounter = 0f;
                }
            }
            else
            {
                idleBufferCounter = 0f;
            }
        }
    }
}
