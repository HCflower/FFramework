using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能运行时控制器 - 负责执行技能配置中定义的各种轨道和片段
    /// </summary>
    public class SkillRuntimeController : MonoBehaviour
    {
        [Header("技能配置")]
        [Tooltip("当前执行的技能配置")]
        public SkillConfig skillConfig;

        [Header("运行时状态")]
        [Tooltip("是否正在播放技能")]
        public bool isPlaying = false;

        [Tooltip("当前播放进度(秒)")]
        public float currentTime = 0f;

        [Tooltip("当前帧数")]
        public int currentFrame = 0;

        [Header("组件引用")]
        [Tooltip("动画控制器")]
        public Animator animator;

        [Tooltip("音频播放器")]
        public AudioSource audioSource;

        // 运行时数据
        private Dictionary<AudioTrack.AudioClip, AudioSource> activeAudioClips = new Dictionary<AudioTrack.AudioClip, AudioSource>();
        private Dictionary<EffectTrack.EffectClip, GameObject> activeEffects = new Dictionary<EffectTrack.EffectClip, GameObject>();
        private List<InjuryDetectionTrack.InjuryDetectionClip> activeDamageClips = new List<InjuryDetectionTrack.InjuryDetectionClip>();

        // 事件系统
        public event Action<string, string> OnEventTriggered;
        public event Action<InjuryDetectionTrack.InjuryDetectionClip, Collider> OnDamageDetected;
        public event Action OnSkillComplete;
        public event Action OnSkillStart;

        #region 公共方法

        /// <summary>
        /// 播放技能
        /// </summary>
        public void PlaySkill()
        {
            if (skillConfig == null)
            {
                Debug.LogWarning("SkillConfig is null, cannot play skill.");
                return;
            }

            if (isPlaying)
            {
                StopSkill();
            }

            StartCoroutine(ExecuteSkill());
        }

        /// <summary>
        /// 停止技能播放
        /// </summary>
        public void StopSkill()
        {
            if (!isPlaying) return;

            isPlaying = false;
            currentTime = 0f;
            currentFrame = 0;

            // 清理活动的音效
            foreach (var audioSource in activeAudioClips.Values)
            {
                if (audioSource != null)
                    audioSource.Stop();
            }
            activeAudioClips.Clear();

            // 清理活动的特效
            foreach (var effect in activeEffects.Values)
            {
                if (effect != null)
                    Destroy(effect);
            }
            activeEffects.Clear();

            // 清理伤害检测
            activeDamageClips.Clear();

            StopAllCoroutines();
        }

        /// <summary>
        /// 暂停技能播放
        /// </summary>
        public void PauseSkill()
        {
            isPlaying = false;
        }

        /// <summary>
        /// 恢复技能播放
        /// </summary>
        public void ResumeSkill()
        {
            if (skillConfig != null && currentTime < GetSkillDuration())
            {
                isPlaying = true;
            }
        }

        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        public void SeekToTime(float time)
        {
            currentTime = Mathf.Clamp(time, 0, GetSkillDuration());
            currentFrame = skillConfig.TimeToFrames(currentTime);
        }

        /// <summary>
        /// 获取技能总时长
        /// </summary>
        public float GetSkillDuration()
        {
            return skillConfig != null ? skillConfig.trackContainer.GetTotalDuration(skillConfig.frameRate) : 0f;
        }

        #endregion

        #region 技能执行核心

        /// <summary>
        /// 执行技能的主协程
        /// </summary>
        private IEnumerator ExecuteSkill()
        {
            isPlaying = true;
            currentTime = 0f;
            currentFrame = 0;

            OnSkillStart?.Invoke();

            float skillDuration = GetSkillDuration();
            float frameTime = 1f / skillConfig.frameRate;

            while (currentTime < skillDuration && isPlaying)
            {
                // 更新当前帧
                currentFrame = skillConfig.TimeToFrames(currentTime);

                // 执行当前帧的所有轨道
                ExecuteFrame(currentFrame);

                // 等待下一帧
                yield return new WaitForSeconds(frameTime);
                currentTime += frameTime;
            }

            // 技能播放完成
            isPlaying = false;
            OnSkillComplete?.Invoke();
        }

        /// <summary>
        /// 执行指定帧的所有轨道内容
        /// </summary>
        private void ExecuteFrame(int frame)
        {
            if (skillConfig?.trackContainer == null) return;

            // 执行动画轨道
            ExecuteAnimationTrack(frame);

            // 执行音效轨道
            foreach (var audioTrack in skillConfig.trackContainer.audioTracks)
            {
                if (audioTrack.isEnabled)
                    ExecuteAudioTrack(audioTrack, frame);
            }

            // 执行特效轨道
            foreach (var effectTrack in skillConfig.trackContainer.effectTracks)
            {
                if (effectTrack.isEnabled)
                    ExecuteEffectTrack(effectTrack, frame);
            }

            // 执行伤害检测轨道
            foreach (var damageTrack in skillConfig.trackContainer.injuryDetectionTracks)
            {
                if (damageTrack.isEnabled)
                    ExecuteDamageTrack(damageTrack, frame);
            }

            // 执行事件轨道
            foreach (var eventTrack in skillConfig.trackContainer.eventTracks)
            {
                if (eventTrack.isEnabled)
                    ExecuteEventTrack(eventTrack, frame);
            }
        }

        #endregion

        #region 轨道执行方法

        /// <summary>
        /// 执行动画轨道
        /// </summary>
        private void ExecuteAnimationTrack(int frame)
        {
            if (animator == null) return;

            var animTrack = skillConfig.trackContainer.animationTrack;
            if (!animTrack.isEnabled) return;

            foreach (var clip in animTrack.animationClips)
            {
                if (frame == clip.startFrame && clip.clip != null)
                {
                    // 播放动画
                    animator.speed = clip.playSpeed;

                    if (clip.applyRootMotion)
                        animator.applyRootMotion = true;

                    // 这里需要根据实际的动画系统来调用播放方法
                    // 例如使用 AnimatorController 或直接播放 AnimationClip
                    PlayAnimationClip(clip);
                }
            }
        }

        /// <summary>
        /// 执行音效轨道
        /// </summary>
        private void ExecuteAudioTrack(AudioTrack audioTrack, int frame)
        {
            foreach (var clip in audioTrack.audioClips)
            {
                if (frame == clip.startFrame && clip.clip != null)
                {
                    // 创建新的 AudioSource 来播放音效
                    var audioSource = CreateAudioSource();
                    audioSource.clip = clip.clip;
                    audioSource.volume = clip.volume;
                    audioSource.pitch = clip.pitch;
                    audioSource.loop = clip.loop;
                    audioSource.Play();

                    activeAudioClips[clip] = audioSource;

                    // 如果不是循环播放，在播放完成后清理
                    if (!clip.loop)
                    {
                        StartCoroutine(CleanupAudioAfterPlay(clip, audioSource));
                    }
                }
            }
        }

        /// <summary>
        /// 执行特效轨道
        /// </summary>
        private void ExecuteEffectTrack(EffectTrack effectTrack, int frame)
        {
            foreach (var clip in effectTrack.effectClips)
            {
                // 特效开始播放
                if (frame == clip.startFrame && clip.effectPrefab != null)
                {
                    var effectObject = Instantiate(clip.effectPrefab, transform);
                    effectObject.transform.localPosition = clip.position;
                    effectObject.transform.localEulerAngles = clip.rotation;
                    effectObject.transform.localScale = clip.scale;

                    activeEffects[clip] = effectObject;

                    // 如果有持续时间限制，在结束时销毁特效
                    if (clip.durationFrame > 0)
                    {
                        float duration = skillConfig.FramesToTime(clip.durationFrame);
                        StartCoroutine(DestroyEffectAfterDuration(clip, effectObject, duration));
                    }
                }
            }
        }

        /// <summary>
        /// 执行伤害检测轨道
        /// </summary>
        private void ExecuteDamageTrack(InjuryDetectionTrack damageTrack, int frame)
        {
            foreach (var clip in damageTrack.injuryDetectionClips)
            {
                // 伤害检测开始
                if (frame == clip.startFrame)
                {
                    activeDamageClips.Add(clip);
                    StartCoroutine(ExecuteDamageDetection(clip));
                }
            }
        }

        /// <summary>
        /// 执行事件轨道
        /// </summary>
        private void ExecuteEventTrack(EventTrack eventTrack, int frame)
        {
            foreach (var clip in eventTrack.eventClips)
            {
                if (frame == clip.startFrame)
                {
                    // 触发事件
                    OnEventTriggered?.Invoke(clip.eventType, clip.eventParameters);
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 播放动画片段
        /// </summary>
        private void PlayAnimationClip(AnimationTrack.AnimationClip clip)
        {
            // 这里需要根据具体的动画系统实现
            // 可能需要使用 Animator.Play() 或其他方法

            if (animator.runtimeAnimatorController != null)
            {
                // 如果使用 AnimatorController
                // animator.Play(clipName);
            }
            else
            {
                // 如果直接播放 AnimationClip
                // 可能需要使用 Animation 组件或其他方式
            }
        }

        /// <summary>
        /// 创建临时音频源
        /// </summary>
        private AudioSource CreateAudioSource()
        {
            var tempObject = new GameObject("TempAudioSource");
            tempObject.transform.SetParent(transform);
            return tempObject.AddComponent<AudioSource>();
        }

        /// <summary>
        /// 音效播放完成后清理
        /// </summary>
        private IEnumerator CleanupAudioAfterPlay(AudioTrack.AudioClip clip, AudioSource audioSource)
        {
            yield return new WaitForSeconds(audioSource.clip.length / audioSource.pitch);

            if (activeAudioClips.ContainsKey(clip))
                activeAudioClips.Remove(clip);

            if (audioSource != null)
                Destroy(audioSource.gameObject);
        }

        /// <summary>
        /// 在指定时间后销毁特效
        /// </summary>
        private IEnumerator DestroyEffectAfterDuration(EffectTrack.EffectClip clip, GameObject effectObject, float duration)
        {
            yield return new WaitForSeconds(duration);

            if (activeEffects.ContainsKey(clip))
                activeEffects.Remove(clip);

            if (effectObject != null)
                Destroy(effectObject);
        }

        /// <summary>
        /// 执行伤害检测
        /// </summary>
        private IEnumerator ExecuteDamageDetection(InjuryDetectionTrack.InjuryDetectionClip clip)
        {
            float duration = skillConfig.FramesToTime(clip.durationFrame);
            float elapsed = 0f;

            while (elapsed < duration && activeDamageClips.Contains(clip))
            {
                // 执行伤害检测逻辑
                DetectDamage(clip);

                // 如果是多段伤害，等待间隔时间
                if (clip.isMultiInjuryDetection)
                {
                    yield return new WaitForSeconds(clip.multiInjuryDetectionInterval);
                    elapsed += clip.multiInjuryDetectionInterval;
                }
                else
                {
                    // 单次伤害检测，执行一次后退出
                    break;
                }
            }

            activeDamageClips.Remove(clip);
        }

        /// <summary>
        /// 执行具体的伤害检测
        /// </summary>
        private void DetectDamage(InjuryDetectionTrack.InjuryDetectionClip clip)
        {
            Vector3 worldPosition = transform.TransformPoint(clip.position);
            Quaternion worldRotation = transform.rotation * Quaternion.Euler(clip.rotation);
            Vector3 worldScale = Vector3.Scale(transform.lossyScale, clip.scale);

            Collider[] hitColliders = null;

            // 根据碰撞体类型执行不同的检测
            switch (clip.colliderType)
            {
                case ColliderType.Box:
                    hitColliders = Physics.OverlapBox(worldPosition, worldScale * 0.5f, worldRotation, clip.targetLayers);
                    break;
                case ColliderType.Sphere:
                    hitColliders = Physics.OverlapSphere(worldPosition, worldScale.x, clip.targetLayers);
                    break;
                case ColliderType.Capsule:
                    // 胶囊体检测需要更复杂的实现
                    hitColliders = Physics.OverlapSphere(worldPosition, worldScale.x, clip.targetLayers);
                    break;
                case ColliderType.sector:
                    // 扇形检测需要自定义实现
                    hitColliders = DetectSectorCollision(clip, worldPosition, worldRotation);
                    break;
            }

            // 处理检测到的碰撞体
            if (hitColliders != null)
            {
                foreach (var hitCollider in hitColliders)
                {
                    OnDamageDetected?.Invoke(clip, hitCollider);
                }
            }
        }

        /// <summary>
        /// 扇形碰撞检测
        /// </summary>
        private Collider[] DetectSectorCollision(InjuryDetectionTrack.InjuryDetectionClip clip, Vector3 worldPosition, Quaternion worldRotation)
        {
            // 先使用球形检测获取可能的目标
            var candidates = Physics.OverlapSphere(worldPosition, clip.outerCircleRadius, clip.targetLayers);
            var validTargets = new List<Collider>();

            Vector3 forward = worldRotation * Vector3.forward;

            foreach (var candidate in candidates)
            {
                Vector3 toTarget = candidate.transform.position - worldPosition;
                float distance = toTarget.magnitude;

                // 检查距离范围
                if (distance < clip.innerCircleRadius || distance > clip.outerCircleRadius)
                    continue;

                // 检查角度范围
                float angle = Vector3.Angle(forward, toTarget);
                if (angle <= clip.sectorAngle * 0.5f)
                {
                    validTargets.Add(candidate);
                }
            }

            return validTargets.ToArray();
        }

        #endregion

        #region Unity生命周期

        private void OnValidate()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !isPlaying || skillConfig == null) return;

            // 绘制当前激活的伤害检测区域
            Gizmos.color = Color.red;
            foreach (var clip in activeDamageClips)
            {
                Vector3 worldPosition = transform.TransformPoint(clip.position);
                Quaternion worldRotation = transform.rotation * Quaternion.Euler(clip.rotation);
                Vector3 worldScale = Vector3.Scale(transform.lossyScale, clip.scale);

                switch (clip.colliderType)
                {
                    case ColliderType.Box:
                        Gizmos.matrix = Matrix4x4.TRS(worldPosition, worldRotation, worldScale);
                        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                        break;
                    case ColliderType.Sphere:
                        Gizmos.DrawWireSphere(worldPosition, worldScale.x);
                        break;
                    case ColliderType.sector:
                        // 绘制扇形检测范围
                        DrawSectorGizmo(worldPosition, worldRotation, clip);
                        break;
                }
            }
        }

        private void DrawSectorGizmo(Vector3 position, Quaternion rotation, InjuryDetectionTrack.InjuryDetectionClip clip)
        {
            Vector3 forward = rotation * Vector3.forward;
            float halfAngle = clip.sectorAngle * 0.5f;

            // 绘制扇形边界
            for (int i = 0; i <= 20; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / 20f);
                Vector3 direction = Quaternion.AngleAxis(angle, rotation * Vector3.up) * forward;

                Gizmos.DrawLine(position + direction * clip.innerCircleRadius,
                               position + direction * clip.outerCircleRadius);
            }

            // 绘制内外圆弧
            for (int i = 0; i < 20; i++)
            {
                float angle1 = Mathf.Lerp(-halfAngle, halfAngle, i / 20f);
                float angle2 = Mathf.Lerp(-halfAngle, halfAngle, (i + 1) / 20f);

                Vector3 dir1 = Quaternion.AngleAxis(angle1, rotation * Vector3.up) * forward;
                Vector3 dir2 = Quaternion.AngleAxis(angle2, rotation * Vector3.up) * forward;

                // 外圆弧
                Gizmos.DrawLine(position + dir1 * clip.outerCircleRadius,
                               position + dir2 * clip.outerCircleRadius);

                // 内圆弧
                if (clip.innerCircleRadius > 0)
                {
                    Gizmos.DrawLine(position + dir1 * clip.innerCircleRadius,
                                   position + dir2 * clip.innerCircleRadius);
                }
            }
        }

        #endregion
    }
}
