using FFramework.Kit;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 变换轨道项数据类
    /// 用于存储变换轨道项的相关数据，与SkillConfig中的TransformClip结构对应
    /// </summary>
    public class TransformTrackItemData : TrackItemDataBase
    {
        public Vector3 positionOffset = Vector3.zero;      // 目标位置  
        public Vector3 targetRotation = Vector3.zero;      // 目标旋转
        public Vector3 targetScale = Vector3.one;          // 目标缩放

        public AnimationCurveType curveType = AnimationCurveType.Linear;        // 动画曲线类型
        public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);  // 自定义动画曲线

        public bool enablePosition = true;          // 是否启用位置变换
        public bool enableRotation = false;         // 是否启用旋转变换
        public bool enableScale = false;            // 是否启用缩放变换
    }
}
