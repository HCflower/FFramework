using FFramework.Kit;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 攻击轨道项数据类
    /// 用于存储攻击轨道项的相关数据，与SkillConfig中的InjuryDetectionClip结构对应
    /// </summary>
    public class AttackTrackItemData : BaseTrackItemData
    {
        [Header("检测设置")]
        [Tooltip("目标层级")] public LayerMask targetLayers = -1;
        [Tooltip("是否是多段伤害检测")] public bool isMultiInjuryDetection = false;
        [Tooltip("多段伤害检测间隔")] public float multiInjuryDetectionInterval = 0.1f;

        [Header("碰撞体设置")]
        [Tooltip("碰撞体类型")] public ColliderType colliderType = ColliderType.Box;

        [Header("扇形碰撞体设置")]
        [Tooltip("扇形内圆半径")] public float innerCircleRadius = 0;
        [Tooltip("扇形外圆半径")] public float outerCircleRadius = 1;
        [Tooltip("扇形角度")] public float sectorAngle = 0;
        [Tooltip("扇形厚度")] public float sectorThickness = 0.1f;

        [Header("Transform")]
        [Tooltip("碰撞体位置")] public Vector3 position = Vector3.zero;
        [Tooltip("碰撞体旋转")] public Vector3 rotation = Vector3.zero;
        [Tooltip("碰撞体缩放")] public Vector3 scale = Vector3.one;
    }
}
