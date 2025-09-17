using Cysharp.Threading.Tasks;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 播放智能动画 - 支持双动画混合过渡 (重构版本)
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

        public AnimationClip CurrentAnimaClip
        {
            get => currentAnimaClip;
            private set => currentAnimaClip = value;
        }

        public bool IsTransitioning => isTransitioning;

        // 新增：动画完成事件
        public System.Action<AnimationClip> OnAnimationComplete;

        // 新增：跟踪非循环动画的播放状态
        private bool isTrackingNonLoopAnimation = false;
        private float nonLoopAnimationStartTime = 0f;

        protected override void Awake()
        {
            base.Awake();
            InitializePlayableGraph();
        }

        private void Update()
        {
            if (!mixerPlayable.IsValid() || !playableGraph.IsValid()) return;
            UpdatePlayProgress();

            // 检查非循环动画是否播放完成
            CheckNonLoopAnimationComplete();
        }

        protected override void OnDestroy()
        {
            CleanupPlayables();
            base.OnDestroy();
        }

        private void InitializePlayableGraph()
        {
            CurrentAnimaClip = animaClip1 ?? animaClip2;
            mixerPlayable = AnimationMixerPlayable.Create(playableGraph, 2);

            clipPlayables[0] = CreateClipPlayable(animaClip1);
            clipPlayables[1] = CreateClipPlayable(animaClip2);

            mixerPlayable.ConnectInput(0, clipPlayables[0], 0);
            mixerPlayable.ConnectInput(1, clipPlayables[1], 0);
            UpdateMixerWeights();

            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(mixerPlayable);

            isLoop = CurrentAnimaClip.isLooping;
            playableGraph.Play();
        }

        private void CleanupPlayables()
        {
            for (int i = 0; i < clipPlayables.Length; i++)
            {
                if (clipPlayables[i].IsValid())
                {
                    clipPlayables[i].Destroy();
                }
            }
        }

        private AnimationClipPlayable CreateClipPlayable(AnimationClip clip)
        {
            var playable = AnimationClipPlayable.Create(playableGraph, clip ?? CreateEmptyClip());
            playable.SetSpeed(playSpeed);
            return playable;
        }

        private AnimationClip CreateEmptyClip()
        {
            var emptyClip = new AnimationClip { name = "EmptyClip" };
            return emptyClip;
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

        /// <summary>
        /// 检查非循环动画是否播放完成
        /// </summary>
        private void CheckNonLoopAnimationComplete()
        {
            if (!isTrackingNonLoopAnimation || CurrentAnimaClip == null || CurrentAnimaClip.isLooping)
                return;

            // 检查动画是否播放完成
            if (Time.time - nonLoopAnimationStartTime >= CurrentAnimaClip.length)
            {
                isTrackingNonLoopAnimation = false;
                OnAnimationComplete?.Invoke(CurrentAnimaClip);
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

            return true;
        }

        /// <summary>
        /// 切换到指定动画并设置过渡时间
        /// </summary>
        /// <param name="newClip">目标动画片段</param>
        /// <param name="transitionDuration">过渡时间</param>
        public async UniTask ChangeAnima(AnimationClip newClip, float transitionDuration)
        {
            if (!CanChangeAnima(newClip, transitionDuration)) return;

            // 如果正在过渡中，先停止当前过渡
            if (isTransitioning)
            {
                isTransitioning = false;
                await UniTask.Yield(); // 等待一帧确保状态更新
            }

            isTransitioning = true;

            try
            {
                await PerformAnimationTransition(newClip, transitionDuration);
                CompleteTransition(newClip);

                // 如果是非循环动画，开始跟踪其完成状态
                if (!newClip.isLooping)
                {
                    isTrackingNonLoopAnimation = true;
                    nonLoopAnimationStartTime = Time.time;
                }
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

            if (targetIndex == -1)
            {
                targetIndex = currentIndex == 0 ? 1 : 0;
                SetupClipInSlot(targetIndex, newClip);
            }

            float startWeight = weight;
            float targetWeight = targetIndex == 0 ? 0f : 1f; // 修复：正确的权重计算
            float elapsedTime = 0f;

            // 确保目标动画从时间0开始播放
            if (clipPlayables[targetIndex].IsValid())
            {
                clipPlayables[targetIndex].SetTime(0);
            }

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);

                weight = Mathf.Lerp(startWeight, targetWeight, progress);
                UpdateMixerWeights();

                await UniTask.Yield();

                if (!isTransitioning)
                {
                    break;
                }
            }

            // 确保最终权重设置正确
            weight = targetWeight;
            UpdateMixerWeights();
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
        }

        /// <summary>
        /// 立即切换动画（无过渡）
        /// </summary>
        /// <param name="newClip">目标动画片段</param>
        public void ChangeAnimaImmediate(AnimationClip newClip)
        {
            if (newClip == null || CurrentAnimaClip == newClip) return;

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

        /// <summary>
        /// 等待当前动画播放完成（仅对非循环动画有效）
        /// </summary>
        /// <returns></returns>
        public async UniTask WaitForAnimationComplete()
        {
            if (CurrentAnimaClip == null || CurrentAnimaClip.isLooping)
                return;

            bool animationCompleted = false;
            System.Action<AnimationClip> completeHandler = (clip) =>
            {
                if (clip == CurrentAnimaClip)
                    animationCompleted = true;
            };

            OnAnimationComplete += completeHandler;

            try
            {
                // 等待动画完成或超时
                float timeout = CurrentAnimaClip.length + 0.1f; // 添加0.1秒超时保护
                float startTime = Time.time;

                while (!animationCompleted && (Time.time - startTime) < timeout)
                {
                    await UniTask.Yield();
                }
            }
            finally
            {
                OnAnimationComplete -= completeHandler;
            }
        }

        /// <summary>
        /// 检查动画是否真的播放完成（基于播放时间）
        /// </summary>
        public bool IsCurrentAnimationComplete()
        {
            if (CurrentAnimaClip == null || CurrentAnimaClip.isLooping)
                return false;

            int primaryIndex = weight >= 0.5f ? 1 : 0;
            if (!clipPlayables[primaryIndex].IsValid())
                return false;

            double currentTime = clipPlayables[primaryIndex].GetTime();
            return currentTime >= CurrentAnimaClip.length;
        }
    }
}
