using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 运行时动画控制器
    /// </summary>
    [AddComponentMenu("Anima/RuntimeAnimaController")]
    public class RuntimeAnimaController : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip animationClip;
        public RuntimeAnimatorController runtimeAnimator;
        [Range(0f, 1f)] public float weight = 0.0f;
        public float playSpeed = 1.0f;
        private PlayableGraph playableGraph;
        private AnimationMixerPlayable mixerPlayable;

        private void Start()
        {
            playableGraph = PlayableGraph.Create();
            mixerPlayable = AnimationMixerPlayable.Create(playableGraph);
            var animationPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);
            var animatorControllerPlayable = AnimatorControllerPlayable.Create(playableGraph, runtimeAnimator);
            mixerPlayable.AddInput(animatorControllerPlayable, 0, 1 - weight);
            mixerPlayable.AddInput(animationPlayable, 0, weight);
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(mixerPlayable);

            playableGraph.Play();
        }

        private void OnDisable()
        {
            playableGraph.Destroy();
        }

        [Button("Play Animation")]
        private void PlayAnimation()
        {
            playableGraph.Play();
            mixerPlayable.SetSpeed(playSpeed);
        }

        [Button("Pause Animation")]
        private void PauseAnimation()
        {
            // 疑似有BUG
            // mixerPlayable.Pause();
            mixerPlayable.SetSpeed(0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mixerPlayable.IsValid())
            {
                mixerPlayable.SetInputWeight(0, 1 - weight);
                mixerPlayable.SetInputWeight(1, weight);

                mixerPlayable.SetSpeed(playSpeed);
            }
        }
#endif
    }
}