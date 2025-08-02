using FFramework.Kit;
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
        [Tooltip("是否是多段伤害检测")] public bool isMultiInjuryDetection = false;
        [Tooltip("多段伤害检测间隔"), Min(1)] public int multiInjuryDetectionInterval = 1;
        [Tooltip("启用所有碰撞体")] public bool enableAllCollider = false;
        [Tooltip("碰撞体索引值")] public int injuryDetectionIndex = 0;
    }
}
