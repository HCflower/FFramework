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

        protected override void Start()
        {
            base.Start();

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
            // 检查是否正在过渡中
            if (isTransitioning)
            {
                Debug.LogWarning("正在过渡中，无法切换动画！");
                return;
            }

            // 检查新动画片段是否为空
            if (newClip == null)
            {
                Debug.LogWarning("目标动画片段为 null,无法切换动画！");
                return;
            }

            // 如果新动画与当前动画相同，则不需要切换
            if (currentAnimaClip == newClip)
            {
                Debug.Log("目标动画与当前动画相同，无需切换！");
                return;
            }

            isTransitioning = true;

            // 根据当前动画片段决定替换策略
            if (currentAnimaClip == animaClip1)
            {
                // 当前播放的是 animaClip1，将新动画替换到 animaClip2 插槽
                animaClip2 = newClip;

                // 销毁旧的 targetPlayable 并创建新的
                if (targetPlayable.IsValid())
                {
                    targetPlayable.Destroy();
                }

                targetPlayable = AnimationClipPlayable.Create(playableGraph, animaClip2);
                targetPlayable.SetSpeed(playSpeed);
                targetPlayable.SetTime(0);

                // 连接到混合器的输入1
                mixerPlayable.ConnectInput(1, targetPlayable, 0);

                // Debug.Log($"将新动画 {newClip.name} 替换到 animaClip2 插槽");
            }
            else if (currentAnimaClip == animaClip2)
            {
                // 当前播放的是 animaClip2，将新动画替换到 animaClip1 插槽
                animaClip1 = newClip;

                // 销毁旧的 currentPlayable 并创建新的
                if (currentPlayable.IsValid())
                {
                    currentPlayable.Destroy();
                }

                currentPlayable = AnimationClipPlayable.Create(playableGraph, animaClip1);
                currentPlayable.SetSpeed(playSpeed);
                currentPlayable.SetTime(0);

                // 连接到混合器的输入0
                mixerPlayable.ConnectInput(0, currentPlayable, 0);

                // Debug.Log($"将新动画 {newClip.name} 替换到 animaClip1 插槽");
            }
            else
            {
                // currentAnimaClip 既不是 animaClip1 也不是 animaClip2
                // 默认替换到 animaClip2 插槽
                animaClip2 = newClip;

                if (targetPlayable.IsValid())
                {
                    targetPlayable.Destroy();
                }

                targetPlayable = AnimationClipPlayable.Create(playableGraph, animaClip2);
                targetPlayable.SetSpeed(playSpeed);
                targetPlayable.SetTime(0);

                mixerPlayable.ConnectInput(1, targetPlayable, 0);

                // Debug.Log($"当前动画未知，将新动画 {newClip.name} 默认替换到 animaClip2 插槽");
            }

            // 检查 Playable 是否创建成功
            var sourcePlayable = currentAnimaClip == animaClip1 ? currentPlayable : targetPlayable;
            var targetPlayableToCheck = currentAnimaClip == animaClip1 ? targetPlayable : currentPlayable;

            if (!sourcePlayable.IsValid() || !targetPlayableToCheck.IsValid())
            {
                Debug.LogError("动画 Playable 创建失败！");
                isTransitioning = false;
                return;
            }

            // 确定过渡方向：从当前权重过渡到目标权重
            float startWeight = weight;
            float targetWeight = currentAnimaClip == animaClip1 ? 1f : 0f;

            // 平滑过渡
            float elapsedTime = 0f;
            this.transitionTime = transitionTime;
            while (elapsedTime < transitionTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / transitionTime);

                weight = Mathf.Lerp(startWeight, targetWeight, progress);

                // 更新混合器的权重
                mixerPlayable.SetInputWeight(0, 1 - weight); // animaClip1 的权重
                mixerPlayable.SetInputWeight(1, weight);     // animaClip2 的权重

                await UniTask.Yield(); // 等待下一帧
            }

            // 确保过渡完成，设置最终权重
            weight = targetWeight;
            mixerPlayable.SetInputWeight(0, 1 - weight);
            mixerPlayable.SetInputWeight(1, weight);

            // 更新当前动画片段引用
            currentAnimaClip = newClip;

            isTransitioning = false;

            Debug.Log($"动画切换完成，当前播放:<color=yellow> {currentAnimaClip.name}</color>");
        }
    }
}