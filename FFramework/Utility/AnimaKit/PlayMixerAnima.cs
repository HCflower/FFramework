using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 播放混合动画
    /// </summary>
    [AddComponentMenu("Anima/PlayMixerAnima")]
    public class PlayMixerAnima : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip animationClip1;
        public AnimationClip animationClip2;
        [Range(0f, 1f)] public float weight = 0.0f;
        private PlayableGraph playableGraph;
        private AnimationMixerPlayable mixerPlayable;
        private void Start()
        {
            playableGraph = PlayableGraph.Create();
            mixerPlayable = AnimationMixerPlayable.Create(playableGraph);
            var clip1Playable = AnimationClipPlayable.Create(playableGraph, animationClip1);
            var clip2Playable = AnimationClipPlayable.Create(playableGraph, animationClip2);
            mixerPlayable.AddInput(clip1Playable, 0, 1 - weight);
            mixerPlayable.AddInput(clip2Playable, 0, weight);
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(mixerPlayable);
        }

        private void OnDisable()
        {
            playableGraph.Destroy();
        }

        [Button("Play Animation")]
        private void PlayAnimation()
        {
            playableGraph.Play();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mixerPlayable.IsValid())
            {
                mixerPlayable.SetInputWeight(0, 1 - weight);
                mixerPlayable.SetInputWeight(1, weight);
            }
        }
#endif
    }
}