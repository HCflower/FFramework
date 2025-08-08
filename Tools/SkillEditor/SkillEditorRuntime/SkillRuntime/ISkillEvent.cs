using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能编辑器事件接口
    /// </summary>    
    public interface ISkillEvent
    {
        /// <summary>
        /// 技能开始事件
        /// </summary>
        void OnSkillStart();

        /// <summary>
        /// 技能结束事件
        /// </summary>
        void OnSkillEnd();

        /// <summary>
        /// 伤害检测事件
        /// </summary>
        /// <param name="groupId">碰撞组ID</param>
        /// <param name="frame">当前帧</param>
        void OnInjuryDetection(GameObject target);

        /// <summary>
        /// 技能事件
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="parameters">事件参数</param>
        void OnSkillEvent(string eventType, string parameters);
    }

    [DisallowMultipleComponent]
    public abstract class SkillEvent : MonoBehaviour, ISkillEvent
    {
        public abstract void OnSkillStart();

        public abstract void OnSkillEnd();

        public abstract void OnInjuryDetection(GameObject target);

        public abstract void OnSkillEvent(string eventType, string parameters);
    }

}