using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 特效轨道项数据类
    /// 用于存储特效轨道项的相关数据，与SkillConfig中的EffectClip结构对应
    /// </summary>
    public class EffectTrackItemData : BaseTrackItemData
    {
        public GameObject effectPrefab;             //特效资源
        public float effectPlaySpeed;               //特效播放速度
        public Vector3 position = Vector3.zero;     //特效位置
        public Vector3 rotation = Vector3.zero;     //特效旋转
        public Vector3 scale = Vector3.one;         //特效缩放
    }
}
