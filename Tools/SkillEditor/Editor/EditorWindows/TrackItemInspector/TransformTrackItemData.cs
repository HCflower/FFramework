using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FFramework.Kit;

namespace SkillEditor
{
    /// <summary>
    /// 变换轨道项数据类
    /// 用于存储变换轨道项的相关数据，与SkillConfig中的TransformClip结构对应
    /// </summary>
    [CreateAssetMenu(fileName = "TransformTrackItemData", menuName = "SkillEditor/TransformTrackItemData")]
    public class TransformTrackItemData : BaseTrackItemData
    {
        [Header("变换类型")]
        public bool enablePosition = true;          // 是否启用位置变换
        public bool enableRotation = false;         // 是否启用旋转变换
        public bool enableScale = false;            // 是否启用缩放变换

        [Header("起始变换")]
        public Vector3 startPosition = Vector3.zero;       // 起始位置
        public Vector3 startRotation = Vector3.zero;       // 起始旋转
        public Vector3 startScale = Vector3.one;           // 起始缩放

        [Header("目标变换")]
        public Vector3 endPosition = Vector3.zero;         // 目标位置  
        public Vector3 endRotation = Vector3.zero;         // 目标旋转
        public Vector3 endScale = Vector3.one;             // 目标缩放

        [Header("动画设置")]
        public AnimationCurveType curveType = AnimationCurveType.Linear;        // 动画曲线类型
        public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);  // 自定义动画曲线
        public bool isRelative = false;                     // 是否相对于当前变换
        public GameObject targetObject;                     // 目标对象（为空则影响技能拥有者）
    }
}
