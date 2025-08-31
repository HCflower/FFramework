using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能运行时控制器 - 负责执行技能配置中定义的各种轨道和片段
    /// Transform变换特性（累加模式）：
    /// </summary>
    [DisallowMultipleComponent]
    public class SkillRuntimeController : MonoBehaviour
    {
        [Header("设置")]
        [Tooltip("技能配置文件")] public SkillConfig skillConfig;
        [Tooltip("技能动画状态机")] public Animator skillAnimator;
        [Tooltip("动画播放器")] public Anima Anima;
        [Tooltip("技能控制的摄像机")] public Camera skillCamera;

        [Header("运行时状态")]
        [SerializeField, Tooltip("是否正在播放技能")][ShowOnly] private bool isPlaying = false;
        [SerializeField, Tooltip("是否循环播放")][ShowOnly] private bool isLoop = false;
        [SerializeField, Tooltip("当前播放帧")][ShowOnly] private int currentFrame = 0;
        [SerializeField, Tooltip("播放开始时间")][ShowOnly] private float playStartTime = 0f;
        [SerializeField, Tooltip("播放速度倍数")][ShowOnly] private float playSpeed = 1.0f;
        [SerializeField, Tooltip("当前动画片段名称")][ShowOnly] private string currentAnimaClipName = "";

        [Header("伤害检测")]
        [Tooltip("伤害检测碰撞器")] public List<CollisionGroup> collisionGroup = new List<CollisionGroup>();
        #region 运行时数据

        /// <summary>技能事件接口 - 自动获取</summary>
        public ISkillEvent skillEvent => GetComponent<ISkillEvent>();

        /// <summary>特效实例缓存</summary>
        private Dictionary<string, GameObject> effectInstances = new Dictionary<string, GameObject>();

        /// <summary>音频源组件</summary>
        private AudioSource audioSource;

        /// <summary>动画器原始根运动状态</summary>
        private bool originalApplyRootMotion;

        /// <summary>Transform轨道原始状态</summary>
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;

        /// <summary>摄像机轨道原始状态</summary>
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private float originalCameraFieldOfView;

        #endregion

        #region 公共属性

        /// <summary>是否正在播放技能</summary>
        public bool IsPlaying => isPlaying;
        /// <summary>技能是否播放完毕/// </summary>
        public bool IsSkillFinished = false;

        /// <summary>当前播放帧</summary>
        public int CurrentFrame => currentFrame;

        /// <summary>技能最大帧数</summary>
        public int MaxFrame => skillConfig?.maxFrames ?? 0;

        /// <summary>技能帧率</summary>
        public float FrameRate => skillConfig?.frameRate ?? 30f;

        /// <summary>技能总时长（秒）</summary>
        public float Duration => MaxFrame / FrameRate;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            // 存储初始状态
            StoreOriginalStates();
        }

        private void Update()
        {
            if (isPlaying && skillConfig != null)
            {
                UpdateSkillPlayback();
            }
        }

        private void OnDestroy()
        {
            StopSkill();
            CleanupEffects();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 自动获取或创建AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // 确保Animator存在
            if (skillAnimator == null)
            {
                skillAnimator = GetComponent<Animator>();
            }

            // 如果没有指定摄像机，自动查找场景中的摄像机
            if (skillCamera == null)
            {
                // 首先尝试获取主摄像机
                skillCamera = Camera.main;

                // 如果主摄像机不存在，查找场景中第一个激活的摄像机
                if (skillCamera == null)
                {
                    Camera[] cameras = FindObjectsOfType<Camera>();
                    foreach (var cam in cameras)
                    {
                        if (cam.enabled && cam.gameObject.activeInHierarchy)
                        {
                            skillCamera = cam;
                            Debug.Log($"SkillRuntimeController: 自动找到摄像机 {cam.name}");
                            break;
                        }
                    }
                }

                // 如果仍然没有找到摄像机，给出警告
                if (skillCamera == null)
                {
                    Debug.LogWarning("SkillRuntimeController: 场景中没有找到可用的摄像机，摄像机震动效果将无法工作");
                }
            }
        }

        /// <summary>
        /// 存储原始状态
        /// </summary>
        private void StoreOriginalStates()
        {
            // 存储Transform原始状态
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;

            // 存储摄像机原始状态
            if (skillCamera != null)
            {
                originalCameraPosition = skillCamera.transform.position;
                originalCameraRotation = skillCamera.transform.rotation;
                originalCameraFieldOfView = skillCamera.fieldOfView;
            }

            // 存储动画器原始根运动状态
            if (skillAnimator != null)
            {
                originalApplyRootMotion = skillAnimator.applyRootMotion;
            }
        }

        #endregion

        #region 技能控制

        /// <summary>
        /// 播放技能
        /// </summary>
        /// <param name="loop">是否循环播放</param>
        /// <param name="speed">播放速度倍数</param>
        public void PlaySkill(bool loop = false, float speed = 1.0f)
        {
            if (skillConfig == null || isPlaying)
            {
                Debug.Log($"skillConfig: {skillConfig}, isPlaying: {isPlaying}");
                return;
            }

            IsSkillFinished = false;
            isLoop = loop;
            playSpeed = Mathf.Max(0.1f, speed);
            isPlaying = true;
            currentFrame = 0;
            playStartTime = Time.time;

            // 重置动画状态
            currentAnimaClipName = "";

            Debug.Log($"SkillRuntimeController: 开始播放技能 {skillConfig.skillName}");

            // 使用当前Transform状态作为新的初始状态（实现累加模式）
            UpdateTransformInitialState();

            // 初始化轨道数据
            InitializeTrackData();
        }

        /// <summary>
        /// 停止技能
        /// </summary>
        public void StopSkill()
        {
            if (!isPlaying) return;

            isPlaying = false;
            currentFrame = 0;

            //TODO:停止动画播放

            // 重置动画状态
            currentAnimaClipName = "";

            // 停止所有轨道
            StopAllTracks();

            // 恢复原始状态
            RestoreOriginalStates();
            IsSkillFinished = true;
            Debug.Log($"SkillRuntimeController: 技能播放已停止");
        }

        /// <summary>
        /// 暂停技能
        /// </summary>
        public void PauseSkill()
        {
            isPlaying = false;

            //TODO:停止动画播放

            Debug.Log($"SkillRuntimeController: 技能播放已暂停");
        }

        /// <summary>
        /// 恢复技能播放
        /// </summary>
        public void ResumeSkill()
        {
            if (skillConfig == null) return;

            isPlaying = true;
            playStartTime = Time.time - (currentFrame / FrameRate / playSpeed);

            //TODO 恢复动画

            Debug.Log($"SkillRuntimeController: 技能播放已恢复");
        }

        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        /// <param name="frame">目标帧</param>
        public void SeekToFrame(int frame)
        {
            currentFrame = Mathf.Clamp(frame, 0, MaxFrame);

            if (isPlaying)
            {
                playStartTime = Time.time - (currentFrame / FrameRate / playSpeed);
            }

            // 执行当前帧的所有轨道
            ExecuteFrameData(currentFrame);
        }

        public bool IsPlayingSkill()
        {
            return isPlaying;
        }

        #endregion

        #region 播放更新

        /// <summary>
        /// 更新技能播放
        /// </summary>
        private void UpdateSkillPlayback()
        {
            // 计算当前应该播放的帧
            float elapsedTime = (Time.time - playStartTime) * playSpeed;
            int targetFrame = Mathf.FloorToInt(elapsedTime * FrameRate);

            // 处理循环或结束
            if (targetFrame >= MaxFrame)
            {
                if (isLoop)
                {
                    // 循环播放 - 使用累加模式，保持当前Transform状态作为新的起始点
                    playStartTime = Time.time;
                    targetFrame = 0;
                    currentFrame = 0;

                    // 重置动画状态以确保循环播放时动画能正确重新开始
                    currentAnimaClipName = "";

                    // 使用当前Transform状态作为新的初始状态（累加模式）
                    UpdateTransformInitialState();

                    // 重新初始化轨道数据（使用更新后的Transform状态）
                    InitializeTrackData();
                }
                else
                {
                    // 结束播放
                    targetFrame = MaxFrame - 1;
                    StopSkill();
                    return;
                }
            }

            // 更新到目标帧
            if (currentFrame != targetFrame)
            {
                currentFrame = targetFrame;
                ExecuteFrameData(currentFrame);
            }
        }

        /// <summary>
        /// 执行指定帧的数据
        /// </summary>
        /// <param name="frame">目标帧</param>
        private void ExecuteFrameData(int frame)
        {
            if (skillConfig?.trackContainer == null) return;

            // 执行各种轨道
            ExecuteAnimationTrack(frame);
            ExecuteTransformTrack(frame);
            ExecuteEffectTrack(frame);
            ExecuteAudioTrack(frame);
            ExecuteCameraTrack(frame);
            ExecuteInjuryDetectionTrack(frame);
            ExecuteEventTrack(frame);
        }

        #endregion

        #region 轨道执行

        /// <summary>
        /// 初始化轨道数据
        /// </summary>
        private void InitializeTrackData()
        {
            if (skillConfig?.trackContainer == null) return;

            // 初始化Transform轨道
            var transformTrack = skillConfig.trackContainer.transformTrack;
            if (transformTrack != null && transformTrack.isEnabled)
            {
                foreach (var clip in transformTrack.transformClips)
                {
                    clip.SetInitialTransform(originalPosition, originalRotation.eulerAngles, originalScale);
                }
            }

            // 初始化摄像机轨道
            var cameraTrack = skillConfig.trackContainer.cameraTrack;
            if (cameraTrack != null && cameraTrack.isEnabled && skillCamera != null)
            {
                foreach (var clip in cameraTrack.cameraClips)
                {
                    clip.SetInitialCamera(originalCameraPosition, originalCameraRotation.eulerAngles, originalCameraFieldOfView);
                }
            }
        }

        /// <summary>
        /// 更新Transform初始状态为当前状态（用于累加模式循环播放）
        /// </summary>
        private void UpdateTransformInitialState()
        {
            if (transform != null)
            {
                // 将当前Transform状态作为新的原始状态，实现累加效果
                originalPosition = transform.position;
                originalRotation = transform.rotation;
                originalScale = transform.localScale;
            }

            //TODO： 更新摄像机状态（摄像机通常不需要累加，但为了一致性也可以更新）
            if (skillCamera != null)
            {
                originalCameraPosition = skillCamera.transform.position;
                originalCameraRotation = skillCamera.transform.rotation;
                originalCameraFieldOfView = skillCamera.fieldOfView;
            }
        }

        /// <summary>
        /// 执行动画轨道
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void ExecuteAnimationTrack(int frame)
        {
            var animationTrack = skillConfig.trackContainer?.animationTrack;
            if (animationTrack == null || !animationTrack.isEnabled || skillAnimator == null)
                return;

            foreach (var animClip in animationTrack.animationClips)
            {
                if (animClip.clip == null || !animClip.IsFrameInRange(frame)) continue;

                currentAnimaClipName = animClip.clip.name;
                skillAnimator.applyRootMotion = animClip.applyRootMotion;

                PlaySmartAnima anima = Anima as PlaySmartAnima;
                anima.playSpeed = animClip.animationPlaySpeed;
                anima.isLoop = animClip.clip.isLooping;
                // 计算过渡时间,保留两位小数
                float transitionTime = (float)Mathf.Round(animClip.transitionDurationFrame / skillConfig.frameRate * 100f) / 100f;
                //使用 .Forget() 表示"不用等待这个异步操作完成
                anima.ChangeAnima(animClip.clip, transitionTime).Forget();

                break;
            }
        }


        /// <summary>
        /// 执行Transform轨道
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void ExecuteTransformTrack(int frame)
        {
            var transformTrack = skillConfig.trackContainer?.transformTrack;
            if (transformTrack != null && transformTrack.isEnabled)
            {
                transformTrack.ExecuteAtFrame(transform, frame);
            }
        }

        /// <summary>
        /// 执行特效轨道
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void ExecuteEffectTrack(int frame)
        {
            var effectTrack = skillConfig.trackContainer?.effectTrack;
            if (effectTrack?.effectTracks == null) return;

            foreach (var track in effectTrack.effectTracks)
            {
                if (!track.isEnabled || track.effectClips == null) continue;

                foreach (var effectClip in track.effectClips)
                {
                    if (effectClip.effectPrefab == null) continue;

                    string effectKey = GenerateEffectKey(effectClip);

                    // 检查特效是否应该在当前帧激活
                    if (effectClip.IsFrameInRange(frame))
                    {
                        // 检查是否需要中断特效播放
                        if (effectClip.isCutEffect)
                        {
                            int cutFrame = effectClip.startFrame + effectClip.cutEffectFrameOffset;
                            if (frame >= cutFrame)
                            {
                                // 如果已经到达中断帧，立即停止特效
                                if (effectInstances.TryGetValue(effectKey, out var cutEffectObj) &&
                                    cutEffectObj != null && cutEffectObj.activeInHierarchy)
                                {
                                    // 立即停止特效播放
                                    StopEffectImmediately(cutEffectObj);
                                }
                                continue;
                            }
                        }

                        // 创建特效实例（如果不存在）
                        if (!effectInstances.ContainsKey(effectKey))
                        {
                            CreateEffectInstance(effectClip, effectKey);
                        }

                        // 激活特效（只在需要时激活）
                        if (effectInstances.TryGetValue(effectKey, out var effectObj) &&
                            effectObj != null && !effectObj.activeInHierarchy)
                        {
                            effectObj.SetActive(true);
                            PlayEffect(effectObj, frame - effectClip.startFrame, effectClip);
                        }
                    }
                    else
                    {
                        // 停用特效（只在激活时停用）
                        if (effectInstances.TryGetValue(effectKey, out var effectObj) &&
                            effectObj != null && effectObj.activeInHierarchy)
                        {
                            effectObj.SetActive(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 执行音频轨道
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void ExecuteAudioTrack(int frame)
        {
            var audioTrack = skillConfig.trackContainer?.audioTrack;
            if (audioTrack?.audioTracks == null || audioSource == null) return;

            foreach (var track in audioTrack.audioTracks)
            {
                if (!track.isEnabled || track.audioClips == null) continue;

                foreach (var audioClip in track.audioClips)
                {
                    if (audioClip.clip == null) continue;

                    // 检查是否应该在当前帧开始播放音频
                    if (frame == audioClip.startFrame)
                    {
                        audioSource.clip = audioClip.clip;
                        audioSource.volume = audioClip.volume;
                        audioSource.pitch = audioClip.pitch * playSpeed;
                        audioSource.PlayOneShot(audioClip.clip);
                    }
                }
            }
        }

        /// <summary>
        /// 执行摄像机轨道
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void ExecuteCameraTrack(int frame)
        {
            var cameraTrack = skillConfig.trackContainer?.cameraTrack;
            if (cameraTrack != null && cameraTrack.isEnabled && skillCamera != null)
            {
                cameraTrack.ExecuteAtFrame(skillCamera, frame);

                // 处理摄像机震动 - 转换为运行时轨道
                var runtimeTrack = cameraTrack.ToRuntimeTrack();
                ExecuteCameraShake(runtimeTrack, frame);
            }
            else if (cameraTrack != null && cameraTrack.isEnabled && skillCamera == null)
            {
                Debug.LogWarning($"SkillRuntimeController: 摄像机轨道已启用但未找到摄像机，无法执行震动效果");
            }
        }

        /// <summary>
        /// 执行摄像机震动
        /// </summary>
        /// <param name="cameraTrack">摄像机轨道</param>
        /// <param name="frame">当前帧</param>
        private void ExecuteCameraShake(CameraTrack cameraTrack, int frame)
        {
            if (cameraTrack?.cameraClips == null || skillCamera == null) return;

            // 获取或添加震动组件
            SmoothShake shakeComponent = skillCamera.GetComponent<SmoothShake>();
            if (shakeComponent == null)
            {
                shakeComponent = skillCamera.gameObject.AddComponent<SmoothShake>();
            }
            foreach (var clip in cameraTrack.cameraClips)
            {
                if (!clip.enableShake || clip.shakePreset == null) continue;
                // 设置震动的预设
                shakeComponent.shakePreset = clip.shakePreset;
                // 计算震动的起始帧和结束帧
                int shakeStartFrame = clip.startFrame + clip.animationStartFrameOffset;
                int shakeEndFrame = clip.startFrame + clip.animationDurationFrame;

                // 检查当前帧是否在震动范围内
                if (frame >= shakeStartFrame && frame <= shakeEndFrame)
                {
                    // 如果震动还没有开始，启动震动
                    if (!shakeComponent.IsShaking)
                    {
                        shakeComponent.StartShake(clip.shakePreset);
                    }
                }
                else if (frame > shakeEndFrame && shakeComponent.IsShaking)
                {
                    // 如果超出震动范围且正在震动，停止震动
                    shakeComponent.StopShake();
                }
            }
        }

        /// <summary>
        /// 停止摄像机震动
        /// </summary>
        private void StopCameraShake()
        {
            if (skillCamera != null)
            {
                SmoothShake shakeComponent = skillCamera.GetComponent<SmoothShake>();
                if (shakeComponent != null && shakeComponent.IsShaking)
                {
                    shakeComponent.StopShake();
                }
            }
        }

        /// <summary>
        /// 执行伤害检测轨道
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void ExecuteInjuryDetectionTrack(int frame)
        {
            var injuryTrack = skillConfig.trackContainer?.injuryDetectionTrack;
            if (injuryTrack?.injuryDetectionTracks == null) return;

            // 统计本帧需要激活的碰撞体
            HashSet<Collider> collidersToActivate = new HashSet<Collider>();

            foreach (var track in injuryTrack.injuryDetectionTracks)
            {
                if (!track.isEnabled || track.injuryDetectionClips == null) continue;

                foreach (var injuryClip in track.injuryDetectionClips)
                {
                    if (injuryClip.IsFrameInRange(frame))
                    {
                        // 收集本帧需要激活的所有碰撞体
                        foreach (var group in collisionGroup)
                        {
                            if (group.injuryDetectionGroupUID == injuryClip.injuryDetectionGroupUID)
                            {
                                foreach (var collider in group.colliders)
                                {
                                    if (collider != null)
                                        collidersToActivate.Add(collider);
                                }
                            }
                        }
                    }
                }
            }

            // 激活本帧需要的所有碰撞体
            foreach (var collider in collidersToActivate)
            {
                collider.enabled = true;
            }

            // 禁用其它未激活的碰撞体
            foreach (var group in collisionGroup)
            {
                foreach (var collider in group.colliders)
                {
                    if (collider != null && !collidersToActivate.Contains(collider))
                        collider.enabled = false;
                }
            }
        }

        /// <summary>
        /// 执行事件轨道
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void ExecuteEventTrack(int frame)
        {
            var eventTrack = skillConfig.trackContainer?.eventTrack;
            if (eventTrack?.eventTracks == null) return;

            foreach (var track in eventTrack.eventTracks)
            {
                if (!track.isEnabled || track.eventClips == null) continue;

                foreach (var eventClip in track.eventClips)
                {
                    if (frame == eventClip.startFrame)
                    {
                        // 触发事件
                        skillEvent?.TriggerSkillEvent(eventClip.eventName);
                    }
                }
            }
        }

        #endregion

        #region 特效管理

        /// <summary>
        /// 生成特效唯一键
        /// </summary>
        /// <param name="effectClip">特效片段</param>
        /// <returns>唯一键</returns>
        private string GenerateEffectKey(EffectTrack.EffectClip effectClip)
        {
            return $"{effectClip.effectPrefab.name}_{effectClip.startFrame}_{effectClip.durationFrame}";
        }

        /// <summary>
        /// 创建特效实例
        /// </summary>
        /// <param name="effectClip">特效片段</param>
        /// <param name="effectKey">特效键</param>
        private void CreateEffectInstance(EffectTrack.EffectClip effectClip, string effectKey)
        {
            var effectObj = Instantiate(effectClip.effectPrefab, transform);

            // 应用Transform设置
            effectObj.transform.localPosition = effectClip.position;
            effectObj.transform.localRotation = Quaternion.Euler(effectClip.rotation);
            effectObj.transform.localScale = effectClip.scale;

            effectObj.SetActive(false);
            effectInstances[effectKey] = effectObj;

            // Debug.Log($"SkillRuntimeController: 创建特效实例 {effectClip.effectPrefab.name}");
        }

        /// <summary>
        /// 播放特效
        /// </summary>
        /// <param name="effectObj">特效对象</param>
        /// <param name="frameOffset">帧偏移</param>
        /// <param name="effectClip">特效片段</param>
        private void PlayEffect(GameObject effectObj, int frameOffset, EffectTrack.EffectClip effectClip)
        {
            var particleSystems = effectObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                if (ps.isPlaying) ps.Stop(true);

                // 应用特效播放速度
                var main = ps.main;
                main.simulationSpeed = effectClip.effectPlaySpeed * playSpeed;

                ps.Play();

                // 模拟到正确的时间点
                if (frameOffset > 0)
                {
                    float simulateTime = frameOffset / FrameRate;
                    ps.Simulate(simulateTime, true, false, true);
                }
            }
        }

        /// <summary>
        /// 立即停止特效播放
        /// </summary>
        /// <param name="effectObj">特效对象</param>
        private void StopEffectImmediately(GameObject effectObj)
        {
            if (effectObj == null) return;

            // 停止所有粒子系统
            var particleSystems = effectObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                if (ps.isPlaying)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            // 停止所有Animator
            var animators = effectObj.GetComponentsInChildren<Animator>();
            foreach (var animator in animators)
            {
                if (animator != null && animator.enabled)
                {
                    animator.enabled = false;
                    animator.enabled = true; // 重置状态
                }
            }

            // 停止所有Animation
            var animations = effectObj.GetComponentsInChildren<Animation>();
            foreach (var animation in animations)
            {
                if (animation != null && animation.isPlaying)
                {
                    animation.Stop();
                }
            }

            // 最后设置为非激活状态
            effectObj.SetActive(false);
        }

        /// <summary>
        /// 清理所有特效
        /// </summary>
        private void CleanupEffects()
        {
            foreach (var effectObj in effectInstances.Values)
            {
                if (effectObj != null)
                {
                    DestroyImmediate(effectObj);
                }
            }
            effectInstances.Clear();
        }

        #endregion

        #region 碰撞检测

        /// <summary>
        /// 停用所有碰撞组
        /// </summary>
        private void DeactivateAllCollisionGroups()
        {
            foreach (var group in collisionGroup)
            {
                if (group?.colliders == null) continue;

                foreach (var collider in group.colliders)
                {
                    if (collider != null)
                        collider.enabled = false;
                }
            }
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 停止所有轨道
        /// </summary>
        private void StopAllTracks()
        {
            //TODO 停止动画

            // 停止音频
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // 停止摄像机震动
            StopCameraShake();

            // 停用所有特效
            foreach (var effectObj in effectInstances.Values)
            {
                if (effectObj != null)
                {
                    effectObj.SetActive(false);
                }
            }

            // 停用所有碰撞组
            DeactivateAllCollisionGroups();
        }

        /// <summary>
        /// TODO：恢复原始状态
        /// </summary>
        private void RestoreOriginalStates()
        {
            // Transform状态不恢复，保持技能执行后的变换效果
            // transform.position = originalPosition;
            // transform.rotation = originalRotation;
            // transform.localScale = originalScale;

            // 恢复摄像机状态（只恢复FOV，不恢复位置和旋转）
            if (skillCamera != null)
            {
                // skillCamera.transform.position = originalCameraPosition;  
                // skillCamera.transform.rotation = originalCameraRotation;  
                skillCamera.fieldOfView = originalCameraFieldOfView;
            }

            // 恢复动画器根运动状态
            if (skillAnimator != null)
            {
                skillAnimator.applyRootMotion = originalApplyRootMotion;
            }
        }

        #endregion

        #region 调试信息

        [Button("播放技能")]
        private void DebugPlaySkill()
        {
            PlaySkill(false, 1.0f);
        }

        [Button("停止技能")]
        private void DebugStopSkill()
        {
            StopSkill();
        }

        [Button("循环播放技能")]
        private void DebugPlaySkillLoop()
        {
            PlaySkill(true, 1.0f);
        }

        #endregion
    }

    [Serializable]
    public class CollisionGroup
    {
        [Min(0)][Tooltip("伤害检测组UID")] public string injuryDetectionGroupUID;
        public List<Collider> colliders = new List<Collider>();
    }
}
