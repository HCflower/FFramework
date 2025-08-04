using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能运行时控制器 - 负责执行技能配置中定义的各种轨道和片段
    /// </summary>
    [DisallowMultipleComponent]
    public class SkillRuntimeController : MonoBehaviour
    {
        [Header("设置")]
        [Tooltip("技能配置文件")] public SkillConfig skillConfig;
        [Tooltip("技能动画状态机")] public Animator skillAnimator;
        [Tooltip("技能动画状态名")] public string animationStateName;
        [Tooltip("技能控制的摄像机")] public Camera skillCamera;
        // 自动获取
        public ISkillEvent skillEvent => GetComponent<ISkillEvent>();

        [Header("伤害检测")]
        [Tooltip("伤害检测碰撞器")] public List<CollisionGroup> collisionGroup = new List<CollisionGroup>();

    }

    [Serializable]
    public class CollisionGroup
    {
        [Min(0)][Tooltip("碰撞组ID")] public int collisionGroupId;
        public List<CustomCollider> colliders = new List<CustomCollider>();
    }
}
