using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 特效轨道项数据类
    /// 用于存储特效轨道项的相关数据，与SkillConfig中的EffectClip结构对应
    /// </summary>
    public class EffectTrackItemData : BaseTrackItemData
    {
        [Header("特效设置")]
        [Tooltip("特效资源")] public GameObject effectPrefab;

        [Header("Transform")]
        [Tooltip("特效位置")] public Vector3 position = Vector3.zero;
        [Tooltip("特效旋转")] public Vector3 rotation = Vector3.zero;
        [Tooltip("特效缩放")] public Vector3 scale = Vector3.one;
    }
}
