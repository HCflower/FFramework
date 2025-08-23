using Cysharp.Threading.Tasks;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 播放智能动画 - 支持双动画混合过渡
    /// </summary>
    [AddComponentMenu("Anima/PlaySmartAnima")]
    public class PlaySmartAnima : Anima
    {
        [SerializeField, Tooltip("当前播放的动画片段")]
        private AnimationClip currentAnimaClip;
        [SerializeField, Tooltip("动画片段1")]
        public AnimationClip animaClip1;
        [SerializeField, Tooltip("动画片段2")]
        public AnimationClip animaClip2;
        [Range(0f, 1f), Tooltip("混合权重")]
        public float weight = 0.0f;
        [SerializeField, Tooltip("过渡时间")]
        public float transitionTime = 0.15f;

        private AnimationMixerPlayable mixerPlayable;
        private AnimationClipPlayable[] clipPlayables = new AnimationClipPlayable[2];
        private bool isTransitioning = false;
        private UniTask currentTransitionTask;

        public AnimationClip CurrentAnimaClip
        {
            get => currentAnimaClip;
            private set => currentAnimaClip = value;
        }

        protected override void Awake()
        {
            base.Awake();
            InitializePlayableGraph();
        }

        private void InitializePlayableGraph()
        {
            // 设置初始当前动画
            CurrentAnimaClip = animaClip1 ?? animaClip2;

            // 创建混合器
            mixerPlayable = AnimationMixerPlayable.Create(playableGraph, 2);

            // 创建动画片段Playable
            clipPlayables[0] = CreateClipPlayable(animaClip1);
            clipPlayables[1] = CreateClipPlayable(animaClip2);

            // 连接输入
            mixerPlayable.ConnectInput(0, clipPlayables[0], 0);
            mixerPlayable.ConnectInput(1, clipPlayables[1], 0);

            // 设置初始权重
            UpdateMixerWeights();

            // 设置输出
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(mixerPlayable);

            isLoop = CurrentAnimaClip.isLooping;
            playableGraph.Play();
        }

        private AnimationClipPlayable CreateClipPlayable(AnimationClip clip)
        {
            var playable = AnimationClipPlayable.Create(playableGraph, clip ?? CreateEmptyClip());
            playable.SetSpeed(playSpeed);
            return playable;
        }

        private AnimationClip CreateEmptyClip()
        {
            var emptyClip = new AnimationClip();
            emptyClip.name = "EmptyClip";
            return emptyClip;
        }

        private void Update()
        {
            if (!mixerPlayable.IsValid() || !playableGraph.IsValid()) return;

            UpdatePlayProgress();
        }

        protected override void OnDestroy()
        {
            // 清理Playable
            for (int i = 0; i < 2; i++)
            {
                if (clipPlayables[i].IsValid())
                {
                    clipPlayables[i].Destroy();
                }
            }

            base.OnDestroy();
        }

        protected override void OnValidate()
        {
            if (mixerPlayable.IsValid())
            {
                UpdateMixerWeights();
            }
        }

        private void UpdatePlayProgress()
        {
            int primaryInputIndex = weight >= 0.5f ? 1 : 0;
            var primaryPlayable = clipPlayables[primaryInputIndex];

            if (!primaryPlayable.IsValid() || primaryPlayable.GetAnimationClip() == null) return;

            double currentTime = primaryPlayable.GetTime();
            float clipLength = primaryPlayable.GetAnimationClip().length;

            if (clipLength <= 0) return;

            if (isLoop)
            {
                playProgress = (float)(currentTime % clipLength) / clipLength;
            }
            else
            {
                playProgress = Mathf.Clamp01((float)currentTime / clipLength);
            }
        }

        private void UpdateMixerWeights()
        {
            mixerPlayable.SetInputWeight(0, 1 - weight);
            mixerPlayable.SetInputWeight(1, weight);
        }

        public override void PlayAnima()
        {
            SetPlayableSpeed(playSpeed);
        }

        public override void PauseAnima()
        {
            SetPlayableSpeed(0);
        }

        private void SetPlayableSpeed(float speed)
        {
            if (clipPlayables[0].IsValid()) clipPlayables[0].SetSpeed(speed);
            if (clipPlayables[1].IsValid()) clipPlayables[1].SetSpeed(speed);
        }

        public override void ChangeAnima()
        {
            var targetClip = CurrentAnimaClip == animaClip1 ? animaClip2 : animaClip1;
            ChangeAnima(targetClip, transitionTime).Forget();
        }

        private bool CanChangeAnima(AnimationClip newClip, float duration)
        {
            if (!playableGraph.IsValid())
            {
                Debug.LogError("PlayableGraph 未初始化或已销毁！");
                return false;
            }

            if (newClip == null)
            {
                Debug.LogWarning("目标动画片段为 null！");
                return false;
            }

            if (CurrentAnimaClip == newClip)
            {
                return false;
            }

            if (duration <= 0)
            {
                Debug.LogWarning("过渡时间必须大于0！");
                return false;
            }

            // 移除 isTransitioning 检查，允许中断正在进行的过渡
            // if (isTransitioning) 这行被移除

            return true;
        }

        public async UniTask ChangeAnima(AnimationClip newClip, float transitionDuration)
        {
            if (!CanChangeAnima(newClip, transitionDuration)) return;

            // 如果正在过渡，先标记为不过渡状态，然后立即开始新的过渡
            if (isTransitioning)
            {
                // Debug.Log("中断当前过渡，开始新的动画切换");
                isTransitioning = false; // 重置状态以允许新过渡
            }

            isTransitioning = true;

            try
            {
                await PerformAnimationTransition(newClip, transitionDuration);
                CompleteTransition(newClip);
            }
            finally
            {
                isTransitioning = false;
            }
        }

        private async UniTask PerformAnimationTransition(AnimationClip newClip, float duration)
        {
            int currentIndex = GetClipIndex(CurrentAnimaClip);
            int targetIndex = GetClipIndex(newClip, currentIndex);

            // 如果目标动画不在当前插槽中，先设置到空闲插槽
            if (targetIndex == -1)
            {
                targetIndex = currentIndex == 0 ? 1 : 0;
                SetupClipInSlot(targetIndex, newClip);
            }

            float startWeight = weight;
            float targetWeight = currentIndex == 0 ? 1f : 0f;
            float elapsedTime = 0f;

            // 移除 isTransitioning 检查，只根据时间判断
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);

                weight = Mathf.Lerp(startWeight, targetWeight, progress);
                UpdateMixerWeights();

                await UniTask.Yield();

                // 如果被外部中断（新的切换请求），立即退出
                if (!isTransitioning)
                {
                    // Debug.Log("过渡被新请求中断");
                    break;
                }
            }
        }

        private int GetClipIndex(AnimationClip clip, int excludeIndex = -1)
        {
            for (int i = 0; i < 2; i++)
            {
                if (i != excludeIndex && clipPlayables[i].IsValid() &&
                    clipPlayables[i].GetAnimationClip() == clip)
                {
                    return i;
                }
            }
            return -1;
        }

        private void SetupClipInSlot(int slotIndex, AnimationClip clip)
        {
            if (clipPlayables[slotIndex].IsValid())
            {
                mixerPlayable.DisconnectInput(slotIndex);
                clipPlayables[slotIndex].Destroy();
            }

            clipPlayables[slotIndex] = CreateClipPlayable(clip);
            mixerPlayable.ConnectInput(slotIndex, clipPlayables[slotIndex], 0);
            mixerPlayable.SetInputWeight(slotIndex, 0f);
        }

        private void CompleteTransition(AnimationClip newClip)
        {
            CurrentAnimaClip = newClip;
            weight = GetClipIndex(newClip) == 0 ? 0f : 1f;
            UpdateMixerWeights();

            // Debug.Log($"动画切换完成: {CurrentAnimaClip.name}");
        }

        // 立即切换动画（无过渡）
        public void ChangeAnimaImmediate(AnimationClip newClip)
        {
            // 移除 CanChangeAnima 检查，允许强制立即切换
            if (newClip == null || CurrentAnimaClip == newClip) return;

            // 中断任何正在进行的过渡
            isTransitioning = false;

            int targetIndex = GetClipIndex(newClip);
            if (targetIndex == -1)
            {
                targetIndex = GetClipIndex(CurrentAnimaClip) == 0 ? 1 : 0;
                SetupClipInSlot(targetIndex, newClip);
            }

            CurrentAnimaClip = newClip;
            weight = targetIndex == 0 ? 0f : 1f;
            UpdateMixerWeights();

            Debug.Log($"立即切换到: {CurrentAnimaClip.name}");
        }
    }
}