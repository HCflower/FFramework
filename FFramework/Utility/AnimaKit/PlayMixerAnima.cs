using Cysharp.Threading.Tasks;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 播放混合动画
    /// </summary>
    [AddComponentMenu("Anima/PlayMixerAnima")]
    public class PlayMixerAnima : Anima
    {
        public AnimationClip animationClip1;
        public AnimationClip animationClip2;
        [Range(0f, 1f)] public float weight = 0.0f;
        // 用于平滑过渡的时间
        [Tooltip("过渡时间")] public float transitionTime = 0.15f;
        private AnimationMixerPlayable mixerPlayable;
        protected override void Start()
        {
            base.Start();
            mixerPlayable = AnimationMixerPlayable.Create(playableGraph);
            var clip1Playable = AnimationClipPlayable.Create(playableGraph, animationClip1);
            var clip2Playable = AnimationClipPlayable.Create(playableGraph, animationClip2);
            mixerPlayable.AddInput(clip1Playable, 0, 1 - weight);
            mixerPlayable.AddInput(clip2Playable, 0, weight);
            mixerPlayable.SetSpeed(playSpeed);
            isLoop = weight >= 0.5f ? animationClip2.isLooping : animationClip1.isLooping;
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(mixerPlayable);
            playableGraph.Play();
        }

        private void Update()
        {
            if (!mixerPlayable.IsValid()) return;

            // 根据权重判断当前主要播放的动画
            // 如果权重大于等于0.5，则主要播放动画2，否则播放动画1
            int primaryInputIndex = weight >= 0.5f ? 1 : 0;
            var primaryPlayable = (AnimationClipPlayable)mixerPlayable.GetInput(primaryInputIndex);
            // 获取当前播放时间(秒)
            double currentTime = primaryPlayable.GetTime();

            // 获取动画长度
            float clipLength = primaryPlayable.GetAnimationClip().length;
            if (clipLength <= 0) return; // 避免除零错误
            // 计算标准化进度[0-1]
            if (isLoop)
            {
                // 循环动画：使用模运算确保始终在[0,1]范围内
                playProgress = (float)(currentTime % clipLength) / clipLength;
                // 确保循环动画在结束时重置为0
                if (playProgress == 1.0f) playProgress = 0f;
            }
            else
            {
                // 非循环动画：限制在[0,1]范围内
                playProgress = Mathf.Clamp01((float)currentTime / clipLength);

                // 可选：添加动画结束检测
                if (playProgress >= 1f)
                {
                    playProgress = 1f;
                }
            }
        }

        protected override void OnValidate()
        {
            if (mixerPlayable.IsValid())
            {
                mixerPlayable.SetInputWeight(0, 1 - weight);
                mixerPlayable.SetInputWeight(1, weight);
            }
        }

        public override void PlayAnima()
        {
            mixerPlayable.SetSpeed(playSpeed);
            playableGraph.Play();
        }

        public override void PauseAnima()
        {
            mixerPlayable.SetSpeed(0);
        }

        public override async void ChangeAnima()
        {
            // 获取当前权重
            float initialWeight = weight;

            // 目标权重：如果当前主要播放的是动画1，则切换到动画2；反之亦然
            float targetWeight = initialWeight >= 0.5f ? 0f : 1f;
            isLoop = weight >= 0.5f ? animationClip2.isLooping : animationClip1.isLooping;

            // 过渡时间
            float transitionTime = this.transitionTime;

            // 平滑过渡
            float elapsedTime = 0f;
            while (elapsedTime < transitionTime)
            {
                elapsedTime += Time.deltaTime;
                weight = Mathf.Lerp(initialWeight, targetWeight, elapsedTime / transitionTime);

                // 更新混合器的权重
                mixerPlayable.SetInputWeight(0, 1 - weight);
                mixerPlayable.SetInputWeight(1, weight);

                // 等待下一帧
                await UniTask.Yield();
            }

            // 确保最终权重正确
            weight = targetWeight;
            mixerPlayable.SetInputWeight(0, 1 - weight);
            mixerPlayable.SetInputWeight(1, weight);
        }

    }
}