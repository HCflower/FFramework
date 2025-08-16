using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 播放单个动画
    /// </summary>
    [AddComponentMenu("Anima/PlaySingleAnima")]
    public class PlaySingleAnima : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip animationClip;
        private PlayableGraph playableGraph;

        private void Start()
        {
            playableGraph = PlayableGraph.Create();
            var animationPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(animationPlayable);
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
    }
}
