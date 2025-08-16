using UnityEngine.Playables;
using UnityEngine;
using UnityEngine.Animations;

namespace FFramework.Kit
{
    /// <summary>
    /// 动画IK控制器
    /// </summary>
    [AddComponentMenu("Anima/AnimaHumanIK")]
    public class AnimaHumanIK : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip animationClip;
        public AvatarIKGoal avatarIKGoal;
        public Transform avatarIkEffector;
        [Range(0f, 1f)] public float weight = 0.0f;
        private PlayableGraph playableGraph;
        private AnimationScriptPlayable jobPlayable;
        private void Start()
        {
            playableGraph = PlayableGraph.Create();
            // 设置IK数据
            AnimaIKJob job = new AnimaIKJob();
            job.avatarIKGoal = avatarIKGoal;
            job.avatarIKHandle = animator.BindSceneTransform(avatarIkEffector);
            job.weight = weight;
            jobPlayable = AnimationScriptPlayable.Create(playableGraph, job);

            AnimationClipPlayable anim = AnimationClipPlayable.Create(playableGraph, animationClip);
            // 设置动画不处理IK-手动处理
            anim.SetApplyFootIK(false);
            anim.SetApplyPlayableIK(false);
            jobPlayable.AddInput(anim, 0, 1.0f);
            // 创建动画输出
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(jobPlayable);
            playableGraph.Play();
        }

        void OnDisable()
        {
            playableGraph.Destroy();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (jobPlayable.IsValid())
            {
                AnimaIKJob job = jobPlayable.GetJobData<AnimaIKJob>();
                job.avatarIKGoal = avatarIKGoal;
                job.weight = weight;
                jobPlayable.SetJobData(job);
            }
        }
#endif
    }

    // 动画IK工作
    public struct AnimaIKJob : IAnimationJob
    {
        public TransformSceneHandle avatarIKHandle;
        public AvatarIKGoal avatarIKGoal;
        public float weight;
        // 处理动画
        public void ProcessAnimation(AnimationStream stream)
        {
            if (stream.isValid && avatarIKHandle.IsValid(stream))
            {
                var human = stream.AsHuman();
                human = SetAvatarIK(avatarIKGoal, stream, human);
                // 解算IK
                human.SolveIK();
            }
        }

        private AnimationHumanStream SetAvatarIK(AvatarIKGoal avatarIKGoal, AnimationStream stream, AnimationHumanStream human)
        {
            // 设置左脚IK位移和权重
            human.SetGoalLocalPosition(avatarIKGoal, avatarIKHandle.GetPosition(stream));
            human.SetGoalWeightPosition(avatarIKGoal, weight);
            // 设置左脚IK旋转和权重
            human.SetGoalLocalRotation(avatarIKGoal, avatarIKHandle.GetRotation(stream));
            human.SetGoalWeightRotation(avatarIKGoal, weight);
            return human;
        }

        // 处理根运动
        public void ProcessRootMotion(AnimationStream stream)
        {

        }
    }
}
