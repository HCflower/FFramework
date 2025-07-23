using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 轨道项数据基类
    /// 包含所有轨道项类型的公共属性
    /// </summary>
    public abstract class BaseTrackItemData : ScriptableObject
    {
        public string trackItemName;            // 轨道项名称
        public int frameCount;                  // 帧数
        public int startFrame;                  // 起始帧
        public int durationFrame;               // 持续帧数
    }
}
