using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 播放单个动画
    /// </summary>
    [AddComponentMenu("Anima/PlaySingleAnima")]
    public class PlaySingleAnima : Anima
    {
        public AnimationClip animationClip;
        private AnimationClipPlayable animationPlayable;
        private AnimationPlayableOutput output;

        protected override void Awake()
        {
            base.Awake();
            animationPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);
            animationPlayable.SetSpeed(playSpeed);
            isLoop = animationClip.isLooping;
            output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(animationPlayable);
            playableGraph.Play();
        }

        private void Update()
        {
            if (!animationPlayable.IsValid()) return;

            // 获取动画剪辑引用
            AnimationClip clip = animationPlayable.GetAnimationClip();
            if (clip == null) return;

            // 获取当前播放时间(秒)
            double currentTime = animationPlayable.GetTime();

            // 获取动画长度
            float clipLength = clip.length;
            if (clipLength <= 0) return; // 避免除零错误

            // 计算标准化进度[0-1]
            if (isLoop)
            {
                // 循环动画：使用模运算确保始终在[0,1]范围内
                playProgress = (float)(currentTime % clipLength) / clipLength;
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

        public override void PlayAnima()
        {
            animationPlayable.SetSpeed(playSpeed);
            playableGraph.Play();
        }

        public override void PauseAnima()
        {
            animationPlayable.SetSpeed(0);
        }

        public override void ChangeAnima()
        {
            SetAndPlayAnimationClip(animationClip);
        }

        /// <summary>
        /// 替换动画并播放
        /// </summary>
        /// <param name="newClip">新的动画片段</param>
        public void SetAndPlayAnimationClip(AnimationClip newClip)
        {
            if (newClip == null)
            {
                Debug.LogError("新动画片段不能为空！");
                return;
            }

            // 停止当前播放
            playableGraph.Stop();

            // 清理旧的Playable
            if (output.GetSourcePlayable().IsValid())
            {
                output.GetSourcePlayable().Destroy();
            }

            // 创建新的 AnimationClipPlayable
            var newAnimationPlayable = AnimationClipPlayable.Create(playableGraph, newClip);
            animationPlayable = newAnimationPlayable;
            // 更新 isLoop 的值
            isLoop = newClip.isLooping;
            // 更新播放速度
            animationPlayable.SetSpeed(playSpeed);
            // 重置时间
            animationPlayable.SetTime(0);
            playProgress = 0.0f;
            // 替换输出的源
            output.SetSourcePlayable(animationPlayable);

            // 播放新的动画
            playableGraph.Play();
        }
    }
}
