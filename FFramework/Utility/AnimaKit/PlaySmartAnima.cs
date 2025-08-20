using Cysharp.Threading.Tasks;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 播放智能动画
    /// </summary>
    [AddComponentMenu("Anima/PlaySmartAnima")]
    public class PlaySmartAnima : Anima
    {
        [ShowOnly] public AnimationClip currentAnimaClip;
        public AnimationClip animaClip1;
        public AnimationClip animaClip2;
        [Range(0f, 1f)] public float weight = 0.0f;
        // 用于平滑过渡的时间
        [ShowOnly] public float transitionTime = 0.15f;
        private AnimationMixerPlayable mixerPlayable;
        private AnimationClipPlayable currentPlayable;
        private AnimationClipPlayable targetPlayable;
        private bool isTransitioning = false; // 添加过渡状态标志

        protected override void Awake()
        {
            base.Awake();

            // 设置初始当前动画
            currentAnimaClip = animaClip1;

            // 初始化混合器
            mixerPlayable = AnimationMixerPlayable.Create(playableGraph, 2);

            // 创建初始动画
            currentPlayable = AnimationClipPlayable.Create(playableGraph, animaClip1);
            mixerPlayable.ConnectInput(0, currentPlayable, 0);
            mixerPlayable.SetInputWeight(0, 1f); // 当前动画初始权重为 1

            // 创建目标动画插槽
            targetPlayable = AnimationClipPlayable.Create(playableGraph, animaClip2);
            mixerPlayable.ConnectInput(1, targetPlayable, 0);
            mixerPlayable.SetInputWeight(1, 0f); // 目标动画初始权重为 0

            isLoop = currentAnimaClip.isLooping;
            // 输出设置
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(mixerPlayable);

            playableGraph.Play();
        }

        private void Update()
        {
            if (!mixerPlayable.IsValid()) return;
            if (!playableGraph.IsValid()) return;

            int primaryInputIndex = weight >= 0.5f ? 1 : 0;
            var primaryPlayable = (AnimationClipPlayable)mixerPlayable.GetInput(primaryInputIndex);
            double currentTime = primaryPlayable.GetTime();
            float clipLength = primaryPlayable.GetAnimationClip().length;
            if (clipLength <= 0) return;

            if (isLoop)
            {
                playProgress = (float)(currentTime % clipLength) / clipLength;
                if (playProgress == 1.0f) playProgress = 0f;
            }
            else
            {
                playProgress = Mathf.Clamp01((float)currentTime / clipLength);
                if (playProgress >= 1f) playProgress = 1f;
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
        }

        public override void PauseAnima()
        {
            mixerPlayable.SetSpeed(0);
        }

        public override void ChangeAnima()
        {
            ChangeAnima(animaClip2, transitionTime);
        }

        public async void ChangeAnima(AnimationClip newClip, float transitionTime)
        {
            if (!playableGraph.IsValid())
            {
                Debug.LogError("PlayableGraph 未初始化或已销毁，无法切换动画！");
                return;
            }
            if (isTransitioning) return;
            if (newClip == null)
            {
                Debug.LogWarning("目标动画片段为 null,无法切换动画！");
                return;
            }
            if (currentAnimaClip == newClip) return;

            isTransitioning = true;

            if (currentAnimaClip == animaClip1)
            {
                animaClip2 = newClip;
                if (targetPlayable.IsValid()) targetPlayable.Destroy();
                targetPlayable = AnimationClipPlayable.Create(playableGraph, animaClip2);
                targetPlayable.SetSpeed(playSpeed);
                targetPlayable.SetTime(0);
                mixerPlayable.ConnectInput(1, targetPlayable, 0);
            }
            else if (currentAnimaClip == animaClip2)
            {
                animaClip1 = newClip;
                if (currentPlayable.IsValid()) currentPlayable.Destroy();
                currentPlayable = AnimationClipPlayable.Create(playableGraph, animaClip1);
                currentPlayable.SetSpeed(playSpeed);
                currentPlayable.SetTime(0);
                mixerPlayable.ConnectInput(0, currentPlayable, 0);
            }
            else
            {
                animaClip2 = newClip;
                if (targetPlayable.IsValid()) targetPlayable.Destroy();
                targetPlayable = AnimationClipPlayable.Create(playableGraph, animaClip2);
                targetPlayable.SetSpeed(playSpeed);
                targetPlayable.SetTime(0);
                mixerPlayable.ConnectInput(1, targetPlayable, 0);
            }

            var sourcePlayable = currentAnimaClip == animaClip1 ? currentPlayable : targetPlayable;
            var targetPlayableToCheck = currentAnimaClip == animaClip1 ? targetPlayable : currentPlayable;

            if (!sourcePlayable.IsValid() || !targetPlayableToCheck.IsValid())
            {
                Debug.LogError("动画 Playable 创建失败！");
                isTransitioning = false;
                return;
            }

            float startWeight = weight;
            float targetWeight = currentAnimaClip == animaClip1 ? 1f : 0f;

            float elapsedTime = 0f;
            this.transitionTime = transitionTime;
            while (elapsedTime < transitionTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / transitionTime);

                weight = Mathf.Lerp(startWeight, targetWeight, progress);

                mixerPlayable.SetInputWeight(0, 1 - weight);
                mixerPlayable.SetInputWeight(1, weight);

                await UniTask.Yield();
            }

            weight = targetWeight;
            mixerPlayable.SetInputWeight(0, 1 - weight);
            mixerPlayable.SetInputWeight(1, weight);

            currentAnimaClip = newClip;
            isTransitioning = false;

            Debug.Log($"动画切换完成，当前播放:<color=yellow>{currentAnimaClip.name}</color>");
        }
    }
}