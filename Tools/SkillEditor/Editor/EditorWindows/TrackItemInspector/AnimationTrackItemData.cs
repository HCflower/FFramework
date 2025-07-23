using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 动画轨道项数据类
    /// 用于存储动画轨道项的相关数据
    /// </summary>
    public class AnimationTrackItemData : BaseTrackItemData
    {
        public AnimationClip animationClip;     // 动画片段
        public float playSpeed = 1f;            // 播放速度
        public bool isLoop;                     // 是否循环播放
        public bool applyRootMotion;            // 是应用根运动
    }
}
