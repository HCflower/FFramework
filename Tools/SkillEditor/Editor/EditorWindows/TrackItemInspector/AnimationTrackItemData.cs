using UnityEngine;

namespace SkillEditor
{
    public class AnimationTrackItemData : ScriptableObject
    {
        public string trackItemName;            // 轨道项名称
        public int frameCount;                  // 帧数
        public AnimationClip animationClip;     // 动画片段
        public int startFrame;                  // 动画片段起始帧
        public int durationFrame;               // 动画片段持续帧
        public float playSpeed = 1f;            // 播放速度
        public bool isLoop;                     // 是否循环播放
        public bool applyRootMotion;            // 是应用根运动
    }
}
