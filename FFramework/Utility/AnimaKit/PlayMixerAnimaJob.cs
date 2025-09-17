using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Collections;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 混合动画播放工作Job优化版
    /// </summary>
    [AddComponentMenu("Anima/PlayMixerAnimaJob")]
    public class PlayMixerAnimaJob : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip animationClip1;
        public AnimationClip animationClip2;
        public Transform rootBone;
        [Range(0, 1)] public float weight;
        private PlayableGraph playableGraph;
        private AnimationScriptPlayable jobPlayable;
        private NativeArray<TransformStreamHandle> transformHandles;

        private void Start()
        {
            playableGraph = PlayableGraph.Create();
            // 获取所有骨骼
            var bones = rootBone.GetComponentsInChildren<Transform>();
            transformHandles = new NativeArray<TransformStreamHandle>(bones.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            // 绑定骨骼
            for (int i = 0; i < bones.Length; i++)
            {
                transformHandles[i] = animator.BindStreamTransform(bones[i]);
            }
            // 创建动画工作
            MixerJob job = new MixerJob();
            job.transformHandles = transformHandles;
            job.weight = weight;
            jobPlayable = AnimationScriptPlayable.Create<MixerJob>(playableGraph, job);
            // 设置不处理输入-手动处理
            jobPlayable.SetProcessInputs(false);
            jobPlayable.AddInput(AnimationClipPlayable.Create(playableGraph, animationClip1), 0, 1.0f);
            jobPlayable.AddInput(AnimationClipPlayable.Create(playableGraph, animationClip2), 0, 0.0f);
            // 创建动画输出
            var output = AnimationPlayableOutput.Create(playableGraph, "Anima", animator);
            output.SetSourcePlayable(jobPlayable);
        }

        private void OnDisable()
        {
            playableGraph.Destroy();
            transformHandles.Dispose();
        }

        [Button("Play Animation")]
        private void PlayAnimation()
        {
            jobPlayable.SetSpeed(1);
            playableGraph.Play();
        }

        [Button("Pause Animation")]
        private void PauseAnimation()
        {
            jobPlayable.SetSpeed(0);
        }
    }

    public struct MixerJob : IAnimationJob
    {
        public NativeArray<TransformStreamHandle> transformHandles;
        public float weight;

        // 处理动画
        public void ProcessAnimation(AnimationStream stream)
        {
            var stream0 = stream.GetInputStream(0);
            var stream1 = stream.GetInputStream(1);
            // 遍历所有骨骼
            foreach (var bone in transformHandles)
            {
                var position = Vector3.Lerp(bone.GetLocalPosition(stream0), bone.GetLocalPosition(stream1), weight);
                bone.SetLocalPosition(stream, position);
                var rotation = Quaternion.Slerp(bone.GetLocalRotation(stream0), bone.GetLocalRotation(stream1), weight);
                bone.SetLocalRotation(stream, rotation);
            }
        }

        // 处理根运动
        public void ProcessRootMotion(AnimationStream stream)
        {
            // 获取动画流
            var stream0 = stream.GetInputStream(0);
            var stream1 = stream.GetInputStream(1);
            // 计算差值
            stream.velocity = Vector3.Lerp(stream0.velocity, stream1.velocity, weight);
            stream.angularVelocity = Vector3.Lerp(stream0.angularVelocity, stream1.angularVelocity, weight);
        }
    }
}
