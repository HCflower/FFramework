using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 动画播放队列
    /// </summary>
    [AddComponentMenu("Anima/PlayQueueAnima")]
    public class PlayQueueAnima : MonoBehaviour
    {
        public Animator animator;
        public float playSpeed = 1.0f;
        public bool isLoop = false; // 是否循环播放
        public AnimationClip[] animationClips;
        private PlayableGraph playableGraph;
        private ScriptPlayable<AnimaQueue> animaQueuePlayable;

        void Start()
        {
            playableGraph = PlayableGraph.Create();
            animaQueuePlayable = ScriptPlayable<AnimaQueue>.Create(playableGraph);
            animaQueuePlayable.GetBehaviour().Init(animationClips, animaQueuePlayable, playableGraph);
            animaQueuePlayable.GetBehaviour().SetPlaySpeed(playSpeed);
            // 创建动画输出
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(animaQueuePlayable);
        }

        void OnDisable()
        {
            playableGraph.Destroy();
        }

        [Button("Play Animation")]
        private void PlayAnimation()
        {
            playableGraph.Play();
            animaQueuePlayable.GetBehaviour().SetPlaySpeed(playSpeed);
        }

        [Button("Pause Animation")]
        private void PauseAnimation()
        {
            animaQueuePlayable.GetBehaviour().PauseAnimation();
        }
    }

    /// <summary>
    /// 动画队列项
    /// </summary>
    public class AnimaQueue : PlayableBehaviour
    {
        private AnimationMixerPlayable mixerPlayable;   // 混合动画播放
        private int currentAnimaIndex = 0;              // 当前动画索引
        private float currentAnimaLength = 0f;          // 当前动画长度
        private bool isLoop = false;                    // 是否循环播放

        public void Init(AnimationClip[] clips, Playable playable, PlayableGraph playableGraph)
        {
            mixerPlayable = AnimationMixerPlayable.Create(playableGraph);
            foreach (var clip in clips)
            {
                mixerPlayable.AddInput(AnimationClipPlayable.Create(playableGraph, clip), 0);
            }
            currentAnimaLength = clips[0].length;
            mixerPlayable.SetInputWeight(0, 1f);
            playable.AddInput(mixerPlayable, 0, 1f);
        }

        /// <summary>
        /// 设置播放速度
        /// </summary>
        public void SetPlaySpeed(float playSpeed)
        {
            mixerPlayable.SetSpeed(playSpeed);
        }

        /// <summary>
        /// 暂停动画
        /// </summary>
        public void PauseAnimation()
        {
            mixerPlayable.SetSpeed(0f);
        }

        /// <summary>
        /// 设置循环播放
        /// </summary>
        public void SetLoop(bool loop)
        {
            isLoop = loop;
        }

        // 前一帧调用
        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (mixerPlayable.IsValid())
            {
                base.PrepareFrame(playable, info);
                // 检查当前动画是否播放完毕
                if (mixerPlayable.GetInput(currentAnimaIndex).GetTime() >= currentAnimaLength)
                {
                    // 切换到下一个动画
                    if (currentAnimaIndex < mixerPlayable.GetInputCount() - 1)
                    {
                        mixerPlayable.SetInputWeight(currentAnimaIndex, 0f);
                        currentAnimaIndex++;
                        mixerPlayable.SetInputWeight(currentAnimaIndex, 1f);
                        var current = mixerPlayable.GetInput(currentAnimaIndex);
                        current.SetTime(0f); // 重置时间
                        currentAnimaLength = ((AnimationClipPlayable)current).GetAnimationClip().length;
                    }
                    else if (isLoop)
                    {
                        // 重置到第一个动画
                        mixerPlayable.SetInputWeight(currentAnimaIndex, 0f);
                        currentAnimaIndex = 0;
                        mixerPlayable.SetInputWeight(currentAnimaIndex, 1f);
                        var current = mixerPlayable.GetInput(currentAnimaIndex);
                        current.SetTime(0f); // 重置时间
                        currentAnimaLength = ((AnimationClipPlayable)current).GetAnimationClip().length;
                    }
                }
            }
        }
    }
}
