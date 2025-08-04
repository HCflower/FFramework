using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 攻击轨道项数据类
    /// 用于存储攻击轨道项的相关数据，与SkillConfig中的InjuryDetectionClip结构对应
    /// </summary>
    public class InjuryDetectionTrackItemData : BaseTrackItemData
    {
        [Header("检测设置")]
        [Tooltip("目标层级")] public LayerMask targetLayers = -1;
        [Tooltip("启用所有碰撞体")] public bool enableAllCollisionGroups = false;
        [Tooltip("碰撞检测组ID")] public int collisionGroupId = 0;
    }
}
