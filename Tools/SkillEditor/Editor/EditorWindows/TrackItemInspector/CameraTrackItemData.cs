using FFramework.Kit;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 摄像机轨道项数据类
    /// 用于存储摄像机轨道项的相关数据，与SkillConfig中的CameraClip结构对应
    /// </summary>
    public class CameraTrackItemData : BaseTrackItemData
    {
        [Header("摄像机类型")]
        public bool enablePosition = true;          // 是否启用位置变换
        public bool enableRotation = true;          // 是否启用旋转变换
        public bool enableFieldOfView = false;      // 是否启用视野变换

        [Header("起始状态")]
        public Vector3 startPosition = Vector3.zero;       // 起始位置
        public Vector3 startRotation = Vector3.zero;       // 起始旋转
        public float startFieldOfView = 60f;                // 起始视野角度

        [Header("目标状态")]
        public Vector3 endPosition = Vector3.zero;         // 目标位置
        public Vector3 endRotation = Vector3.zero;         // 目标旋转
        public float endFieldOfView = 60f;                  // 目标视野角度

        [Header("动画设置")]
        public AnimationCurveType curveType = AnimationCurveType.Linear;        // 动画曲线类型
        public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);  // 自定义动画曲线
        public bool isRelative = false;                     // 是否相对于当前状态
        public Camera targetCamera;                         // 目标摄像机（为空则使用主摄像机）
        public string cameraPath;                           // 摄像机路径（用于运行时查找）
    }
}
