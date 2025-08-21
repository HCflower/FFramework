using FFramework.Kit;

namespace SkillEditorExamples
{
    /// <summary>
    /// 编辑器示例-玩家技能状态
    /// </summary>
    public class Player_Skill : StateBase<PlayerController>
    {
        public override void OnEnter(FSMStateMachine<PlayerController> machine)
        {
            owner.canMove = false;
            owner.skillRuntime.PlaySkill();
        }

        public override void OnUpdate(FSMStateMachine<PlayerController> machine)
        {
            //TODO:添加技能可打断
            if (!owner.skillRuntime.IsPlaying && owner.skillRuntime.isOver)
            {
                // 如果没有移动输入，切回Idle
                if (owner.velocity.magnitude <= 0.1f)
                {
                    machine.ChangeState<Player_Idle>();
                }
                else
                {
                    machine.ChangeState<Player_Run>();
                }
            }
        }

        public override void OnExit(FSMStateMachine<PlayerController> machine)
        {

        }
    }
}