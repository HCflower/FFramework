using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 动画轨道项数据类
    /// 用于存储动画轨道项的相关数据
    /// </summary>
    public class AnimationTrackItemData : TrackItemDataBase
    {
        public AnimationClip animationClip;                 // 动画片段
        public float animationPlaySpeed = 1f;               // 播放速度
        [Min(0.0f)] public int transitionDurationFrame = 0; // 过渡时间
        public bool applyRootMotion;                        // 是应用根运动
    }
}
