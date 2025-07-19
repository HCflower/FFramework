using UnityEngine;

namespace SkillEditor
{
    [CreateAssetMenu(menuName = "SkillEditor/AnimationTrackItemData")]
    public class AnimationTrackItemData : ScriptableObject
    {
        public string trackName;
        public float frameCount;
        public AnimationClip animationClip;
        public bool isLoop;
        // 可扩展更多动画相关属性
    }
}
