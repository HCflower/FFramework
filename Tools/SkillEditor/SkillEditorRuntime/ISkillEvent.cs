using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能编辑器事件接口
    /// </summary>    
    public interface ISkillEvent { }

    [DisallowMultipleComponent]
    public class SkillEvent : MonoBehaviour, ISkillEvent { }

}