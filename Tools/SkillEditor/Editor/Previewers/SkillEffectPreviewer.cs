using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FFramework.Kit;

namespace SkillEditor
{
    /// <summary>
    /// 技能特效预览器
    /// 参考动画预览系统实现的特效预览功能
    /// 支持在编辑器模式下预览特效，检查技能所有者子对象是否存在特效资源，如果不存在就创建，如果存在则驱动预览
    /// </summary>
    public class SkillEffectPreviewer
    {
        #region 私有字段

        private GameObject skillOwner;
        private SkillConfig skillConfig;
        private Transform effectContainer;
        private bool isPreviewActive = false;
        private int currentFrame = 0;

        // 存储已创建的特效实例及其对应的配置数据
        private Dictionary<string, EffectInstance> activeEffectInstances = new Dictionary<string, EffectInstance>();

        #endregion

        #region 内部数据结构

        /// <summary>
        /// 特效实例数据
        /// </summary>
        private class EffectInstance
        {
            public GameObject effectObject;          // 特效实例对象
            public EffectTrack.EffectClip clipData; // 特效片段配置数据
            public string effectKey;                 // 特效唯一标识（用于查找和管理）
            public bool isActive;                    // 当前帧是否活跃

            public EffectInstance(GameObject obj, EffectTrack.EffectClip clip, string key)
            {
                effectObject = obj;
                clipData = clip;
                effectKey = key;
                isActive = false;
            }
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 是否正在预览中
        /// </summary>
        public bool IsPreviewActive => isPreviewActive;

        /// <summary>
        /// 当前预览帧
        /// </summary>
        public int CurrentFrame => currentFrame;

        /// <summary>
        /// 技能所有者对象
        /// </summary>
        public GameObject SkillOwner => skillOwner;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造特效预览器
        /// </summary>
        /// <param name="owner">技能所有者</param>
        /// <param name="config">技能配置</param>
        public SkillEffectPreviewer(GameObject owner, SkillConfig config)
        {
            skillOwner = owner;
            skillConfig = config;

            // 创建或获取特效容器
            CreateOrGetEffectContainer();
        }

        #endregion

        #region 预览控制方法

        /// <summary>
        /// 开始特效预览
        /// </summary>
        public void StartPreview()
        {
            if (skillOwner == null || skillConfig == null)
            {
                Debug.LogWarning("SkillEffectPreviewer: 无法开始预览，技能所有者或配置为空");
                return;
            }

            if (isPreviewActive)
            {
                Debug.Log("SkillEffectPreviewer: 预览已在进行中");
                return;
            }

            isPreviewActive = true;
            currentFrame = 0;

            Debug.Log($"SkillEffectPreviewer: 开始特效预览 - 技能所有者: {skillOwner.name}");

            // 预加载所有特效资源
            PreloadAllEffects();

            // 预览第一帧
            PreviewFrame(0);
        }

        /// <summary>
        /// 停止特效预览
        /// </summary>
        public void StopPreview()
        {
            if (!isPreviewActive) return;

            isPreviewActive = false;
            currentFrame = 0;

            // 隐藏所有特效
            HideAllEffects();

            Debug.Log("SkillEffectPreviewer: 停止特效预览");
        }

        /// <summary>
        /// 预览指定帧的特效
        /// </summary>
        /// <param name="frame">目标帧</param>
        public void PreviewFrame(int frame)
        {
            if (!isPreviewActive || skillConfig == null)
            {
                return;
            }

            currentFrame = frame;

            // 获取当前帧的轨道数据
            var frameData = skillConfig.GetTrackDataAtFrame(frame);

            // 更新特效显示状态
            UpdateEffectVisibility(frameData);

            // 应用特效变换
            ApplyEffectTransforms(frameData);
        }

        #endregion

        #region 特效管理方法

        /// <summary>
        /// 预加载所有特效资源
        /// 检查技能所有者子对象是否存在特效资源，如果不存在就创建
        /// </summary>
        private void PreloadAllEffects()
        {
            if (skillConfig?.trackContainer?.effectTrack?.effectTracks == null) return;

            // 清理旧的特效实例数据，重新构建
            activeEffectInstances.Clear();

            foreach (var effectTrack in skillConfig.trackContainer.effectTrack.effectTracks)
            {
                if (!effectTrack.isEnabled) continue;

                foreach (var effectClip in effectTrack.effectClips)
                {
                    if (effectClip.effectPrefab == null) continue;

                    string effectKey = GenerateEffectKey(effectClip);

                    // 检查是否已经存在该特效实例
                    if (activeEffectInstances.ContainsKey(effectKey)) continue;

                    // 先检查技能所有者子对象是否存在该特效资源
                    GameObject existingEffect = FindExistingEffectInChildren(effectClip.effectPrefab);

                    GameObject effectInstance;
                    if (existingEffect != null)
                    {
                        // 存在则直接使用
                        effectInstance = existingEffect;
                        Debug.Log($"SkillEffectPreviewer: 找到现有特效实例: {effectClip.clipName}");
                    }
                    else
                    {
                        // 不存在则创建新实例
                        effectInstance = CreateEffectInstance(effectClip);
                        Debug.Log($"SkillEffectPreviewer: 创建新特效实例: {effectClip.clipName}");
                    }

                    if (effectInstance != null)
                    {
                        // 创建特效实例数据并添加到管理字典
                        var instance = new EffectInstance(effectInstance, effectClip, effectKey);
                        activeEffectInstances[effectKey] = instance;

                        // 初始设置为不可见
                        SetEffectActive(effectInstance, false);
                    }
                }
            }

            Debug.Log($"SkillEffectPreviewer: 预加载完成，共管理 {activeEffectInstances.Count} 个特效实例");
        }

        /// <summary>
        /// 刷新特效数据 - 当轨道项发生变化时调用
        /// </summary>
        public void RefreshEffectData()
        {
            if (!isPreviewActive) return;

            Debug.Log("SkillEffectPreviewer: 刷新特效数据");

            // 重新预加载所有特效（这会清理旧数据并重新构建）
            PreloadAllEffects();

            // 重新预览当前帧
            PreviewFrame(currentFrame);
        }

        /// <summary>
        /// 在技能所有者的子对象中查找已存在的特效
        /// </summary>
        /// <param name="effectPrefab">特效预制体</param>
        /// <returns>找到的特效对象，如果没找到返回null</returns>
        private GameObject FindExistingEffectInChildren(GameObject effectPrefab)
        {
            if (effectPrefab == null || effectContainer == null) return null;

            // 通过名称匹配查找现有特效
            string targetName = effectPrefab.name;

            // 遍历特效容器的所有子对象
            for (int i = 0; i < effectContainer.childCount; i++)
            {
                Transform child = effectContainer.GetChild(i);

                // 检查名称匹配（忽略(Clone)后缀）
                string childName = child.name.Replace("(Clone)", "").Trim();
                if (childName == targetName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// 创建特效实例
        /// </summary>
        /// <param name="effectClip">特效片段数据</param>
        /// <returns>创建的特效实例</returns>
        private GameObject CreateEffectInstance(EffectTrack.EffectClip effectClip)
        {
            if (effectClip.effectPrefab == null || effectContainer == null) return null;

            GameObject effectInstance;

            if (Application.isPlaying)
            {
                // 运行时模式：直接实例化
                effectInstance = UnityEngine.Object.Instantiate(effectClip.effectPrefab, effectContainer);
            }
            else
            {
                // 编辑器模式：使用PrefabUtility创建实例
                effectInstance = (GameObject)PrefabUtility.InstantiatePrefab(effectClip.effectPrefab, effectContainer);
            }

            if (effectInstance != null)
            {
                // 设置名称（移除Clone后缀以便于识别）
                effectInstance.name = effectClip.effectPrefab.name;

                // 应用初始变换
                ApplyTransformToEffect(effectInstance, effectClip);
            }

            return effectInstance;
        }

        /// <summary>
        /// 创建或获取特效容器
        /// </summary>
        private void CreateOrGetEffectContainer()
        {
            if (skillOwner == null) return;

            // 查找现有的特效容器
            Transform existingContainer = skillOwner.transform.Find("EffectPreviewContainer");

            if (existingContainer != null)
            {
                effectContainer = existingContainer;
                Debug.Log("SkillEffectPreviewer: 使用现有特效容器");
            }
            else
            {
                // 创建新的特效容器
                GameObject containerObj = new GameObject("EffectPreviewContainer");
                containerObj.transform.SetParent(skillOwner.transform);
                containerObj.transform.localPosition = Vector3.zero;
                containerObj.transform.localRotation = Quaternion.identity;
                containerObj.transform.localScale = Vector3.one;

                effectContainer = containerObj.transform;
                Debug.Log("SkillEffectPreviewer: 创建新特效容器");
            }
        }

        /// <summary>
        /// 更新特效可见性和播放进度
        /// </summary>
        /// <param name="frameData">帧数据</param>
        private void UpdateEffectVisibility(FrameTrackData frameData)
        {
            Debug.Log($"SkillEffectPreviewer.UpdateEffectVisibility: 当前帧={currentFrame}, frameData中特效片段数={frameData.effectClips.Count}");

            // 首先隐藏所有特效
            foreach (var instance in activeEffectInstances.Values)
            {
                instance.isActive = false;
                SetEffectActive(instance.effectObject, false);
            }
            // 先更新所有实例的片段数据为最新配置
            UpdateEffectInstancesData();

            foreach (var instance in activeEffectInstances.Values)
            {
                var effectClip = instance.clipData;

                // 检查当前帧是否在特效开始播放的范围内
                if (currentFrame >= effectClip.startFrame)
                {
                    // 计算特效从开始播放到现在的时间
                    float timeFromStart = CalculateEffectAbsoluteTime(effectClip, currentFrame);

                    // 获取特效的实际持续时间
                    float effectDuration = GetEffectActualDuration(instance.effectObject);

                    Debug.Log($"  检查特效: {effectClip.clipName}, 起始帧={effectClip.startFrame}, 当前时间={timeFromStart:F3}s, 特效时长={effectDuration:F3}s");

                    // 只要特效还没播放完成，就保持激活
                    if (timeFromStart <= effectDuration)
                    {
                        Debug.Log($"    特效在播放范围内，激活 (播放进度: {timeFromStart / effectDuration * 100:F1}%)");

                        instance.isActive = true;

                        // 使用基于绝对时间的播放方法，让特效按其本身的时间尺度播放
                        SetEffectActiveWithTime(instance.effectObject, true, timeFromStart);
                    }
                    else
                    {
                        Debug.Log($"    特效已播放完成，隐藏");
                    }
                }
                else
                {
                    Debug.Log($"  特效尚未开始: {effectClip.clipName}, 起始帧={effectClip.startFrame}, 当前帧={currentFrame}");
                }
            }

            // 统计最终激活的特效数量
            int activeCount = activeEffectInstances.Values.Count(i => i.isActive);
        }

        /// <summary>
        /// 更新特效实例的片段数据为最新配置
        /// </summary>
        private void UpdateEffectInstancesData()
        {
            if (skillConfig?.trackContainer?.effectTrack?.effectTracks == null) return;

            foreach (var effectTrack in skillConfig.trackContainer.effectTrack.effectTracks)
            {
                if (!effectTrack.isEnabled) continue;

                foreach (var effectClip in effectTrack.effectClips)
                {
                    if (effectClip.effectPrefab == null) continue;

                    string effectKey = GenerateEffectKey(effectClip);

                    if (activeEffectInstances.TryGetValue(effectKey, out var instance))
                    {
                        // 更新为最新的片段数据
                        instance.clipData = effectClip;
                    }
                }
            }
        }

        /// <summary>
        /// 应用特效变换
        /// </summary>
        /// <param name="frameData">帧数据</param>
        private void ApplyEffectTransforms(FrameTrackData frameData)
        {
            foreach (var effectClip in frameData.effectClips)
            {
                string effectKey = GenerateEffectKey(effectClip);

                if (activeEffectInstances.TryGetValue(effectKey, out var instance))
                {
                    ApplyTransformToEffect(instance.effectObject, effectClip);
                }
            }
        }

        /// <summary>
        /// 应用变换到特效对象
        /// </summary>
        /// <param name="effectObject">特效对象</param>
        /// <param name="effectClip">特效片段数据</param>
        private void ApplyTransformToEffect(GameObject effectObject, EffectTrack.EffectClip effectClip)
        {
            if (effectObject == null) return;

            Transform effectTransform = effectObject.transform;

            // 应用位置、旋转、缩放
            effectTransform.position = effectClip.position;
            effectTransform.rotation = Quaternion.Euler(effectClip.rotation);
            effectTransform.localScale = effectClip.scale;
        }

        /// <summary>
        /// 设置特效激活状态
        /// </summary>
        /// <param name="effectObject">特效对象</param>
        /// <param name="active">是否激活</param>
        private void SetEffectActive(GameObject effectObject, bool active)
        {
            if (effectObject == null) return;

            effectObject.SetActive(active);

            // 如果有粒子系统，控制播放状态
            var particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                if (active)
                {
                    if (!ps.isPlaying)
                        ps.Play();
                }
                else
                {
                    if (ps.isPlaying)
                        ps.Stop();
                }
            }
        }

        /// <summary>
        /// 设置特效激活状态并控制播放进度
        /// </summary>
        /// <param name="effectObject">特效对象</param>
        /// <param name="active">是否激活</param>
        /// <param name="progress">播放进度 (0.0 - 1.0)</param>
        private void SetEffectActiveWithProgress(GameObject effectObject, bool active, float progress)
        {
            if (effectObject == null) return;

            effectObject.SetActive(active);

            if (!active) return;

            // 控制粒子系统的播放进度
            var particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();

            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;

                // 停止当前播放
                if (ps.isPlaying)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // 获取特效实例数据
                var effectInstance = activeEffectInstances.Values.FirstOrDefault(inst => inst.effectObject == effectObject);

                // 使用粒子系统自身的持续时间而不是强制压缩到片段时长
                float particleDuration = GetParticleSystemDuration(ps);

                if (effectInstance?.clipData != null)
                {
                    float frameRate = GetFrameRate();
                    float clipDurationInSeconds = effectInstance.clipData.durationFrame / frameRate;

                    // 计算当前帧在片段中的实际时间位置
                    float timeInClip = progress * clipDurationInSeconds;

                    Debug.Log($"特效播放时间计算 - {ps.name}: 片段时长={clipDurationInSeconds:F2}s, 粒子时长={particleDuration:F2}s, 进度={progress:F3}, 时间位置={timeInClip:F3}s");

                    // 播放特效并模拟到指定时间
                    ps.Play();
                    if (timeInClip > 0)
                    {
                        // 考虑粒子系统的模拟速度
                        var main = ps.main;
                        float simulationSpeed = main.simulationSpeed;
                        float adjustedTime = timeInClip / simulationSpeed;

                        // 限制模拟时间不超过粒子系统的总持续时间
                        adjustedTime = Mathf.Min(adjustedTime, particleDuration / simulationSpeed);

                        ps.Simulate(adjustedTime, true, false, true);
                    }
                }
                else
                {
                    // 回退方案：直接使用粒子系统持续时间
                    float targetTime = progress * particleDuration;

                    ps.Play();
                    if (targetTime > 0)
                    {
                        var main = ps.main;
                        float simulationSpeed = main.simulationSpeed;
                        float adjustedTime = targetTime / simulationSpeed;

                        ps.Simulate(adjustedTime, true, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// 设置特效激活状态并控制播放时间（基于绝对时间而不是压缩进度）
        /// </summary>
        /// <param name="effectObject">特效对象</param>
        /// <param name="active">是否激活</param>
        /// <param name="timeInSeconds">播放时间（秒）</param>
        private void SetEffectActiveWithTime(GameObject effectObject, bool active, float timeInSeconds)
        {
            if (effectObject == null) return;

            effectObject.SetActive(active);

            if (!active) return;

            // 控制粒子系统的播放时间
            var particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();

            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;

                // 停止当前播放
                if (ps.isPlaying)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // 播放特效
                ps.Play();

                if (timeInSeconds > 0)
                {
                    // 考虑粒子系统的模拟速度
                    var main = ps.main;
                    float simulationSpeed = main.simulationSpeed;
                    float adjustedTime = timeInSeconds / simulationSpeed;

                    // 模拟到指定时间
                    ps.Simulate(adjustedTime, true, false, true);
                }
            }
        }

        /// <summary>
        /// 获取帧率（从技能配置或使用默认值）
        /// </summary>
        /// <returns>帧率</returns>
        private float GetFrameRate()
        {
            // 尝试从技能配置获取帧率
            if (skillConfig != null)
            {
                return skillConfig.frameRate;
            }

            // 尝试从当前技能编辑器配置获取帧率
            if (SkillEditorData.CurrentSkillConfig != null)
            {
                return SkillEditorData.CurrentSkillConfig.frameRate;
            }

            // 默认30fps
            return 30f;
        }

        /// <summary>
        /// 计算特效播放进度
        /// </summary>
        /// <param name="effectClip">特效片段</param>
        /// <param name="currentFrame">当前帧</param>
        /// <returns>播放进度 (0.0 - 1.0)</returns>
        private float CalculateEffectProgress(EffectTrack.EffectClip effectClip, int currentFrame)
        {
            if (effectClip.durationFrame <= 0)
            {
                Debug.LogWarning($"特效 {effectClip.clipName} 持续帧数无效: {effectClip.durationFrame}");
                return 0f;
            }

            // 计算当前帧相对于特效开始的偏移
            int relativeFrame = currentFrame - effectClip.startFrame;

            // 确保在有效范围内
            relativeFrame = Mathf.Max(0, relativeFrame);

            // 计算进度百分比
            float progress = (float)relativeFrame / effectClip.durationFrame;

            // 限制在 0-1 范围内
            progress = Mathf.Clamp01(progress);

            return progress;
        }

        /// <summary>
        /// 计算特效在片段中的实际播放时间（秒）
        /// </summary>
        /// <param name="effectClip">特效片段</param>
        /// <param name="currentFrame">当前帧</param>
        /// <returns>在片段中的时间位置（秒）</returns>
        private float CalculateEffectTimeInClip(EffectTrack.EffectClip effectClip, int currentFrame)
        {
            if (effectClip.durationFrame <= 0) return 0f;

            // 计算当前帧相对于特效开始的偏移
            int relativeFrame = currentFrame - effectClip.startFrame;
            relativeFrame = Mathf.Max(0, relativeFrame);

            // 转换为时间（秒）
            float frameRate = GetFrameRate();
            float timeInClip = relativeFrame / frameRate;

            return timeInClip;
        }

        /// <summary>
        /// 计算特效从开始播放到当前的绝对时间（不受轨道片段长度限制）
        /// </summary>
        /// <param name="effectClip">特效片段</param>
        /// <param name="currentFrame">当前帧</param>
        /// <returns>从特效开始播放的绝对时间（秒）</returns>
        private float CalculateEffectAbsoluteTime(EffectTrack.EffectClip effectClip, int currentFrame)
        {
            // 计算当前帧相对于特效开始的偏移
            int relativeFrame = currentFrame - effectClip.startFrame;
            relativeFrame = Mathf.Max(0, relativeFrame);

            // 转换为绝对时间（秒），不受轨道片段长度限制
            float frameRate = GetFrameRate();
            float timeFromStart = relativeFrame / frameRate;

            return timeFromStart;
        }

        /// <summary>
        /// 获取粒子系统的持续时间
        /// </summary>
        /// <param name="ps">粒子系统</param>
        /// <returns>持续时间（秒）</returns>
        private float GetParticleSystemDuration(ParticleSystem ps)
        {
            if (ps == null) return 0f;

            // 考虑粒子的生命周期和发射持续时间
            var main = ps.main;
            var emission = ps.emission;

            float emissionDuration = 0f;
            float particleLifetime = 0f;

            // 获取发射持续时间
            if (emission.enabled)
            {
                if (main.loop)
                {
                    // 如果是循环的，使用一个循环的持续时间
                    emissionDuration = main.duration;
                }
                else
                {
                    // 非循环时，使用完整的持续时间
                    emissionDuration = main.duration;
                }
            }

            // 获取粒子生命时间
            if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            {
                particleLifetime = main.startLifetime.constant;
            }
            else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            {
                // 使用最大生命时间
                particleLifetime = main.startLifetime.constantMax;
            }
            else
            {
                // 对于曲线模式，使用最大值作为估算
                particleLifetime = main.startLifetime.constantMax;
            }

            // 特效的总持续时间 = 发射时间 + 粒子最大生命时间
            // 但如果特效设计为瞬间效果（比如爆炸），主要持续时间应该是粒子生命时间
            float totalDuration;

            if (emissionDuration <= 0.1f) // 瞬间发射的特效
            {
                totalDuration = particleLifetime;
            }
            else // 持续发射的特效
            {
                totalDuration = emissionDuration + particleLifetime;
            }

            return totalDuration;
        }

        /// <summary>
        /// 获取特效的实际持续时间
        /// </summary>
        /// <param name="effectObject">特效对象</param>
        /// <returns>特效的实际持续时间（秒）</returns>
        private float GetEffectActualDuration(GameObject effectObject)
        {
            if (effectObject == null) return 0f;

            // 简单缓存：检查是否已经计算过这个对象的持续时间
            string cacheKey = effectObject.GetInstanceID().ToString();

            float maxDuration = 0f;

            // 获取所有粒子系统并找出最长的持续时间
            var particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();

            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;

                float psDuration = GetParticleSystemDuration(ps);
                maxDuration = Mathf.Max(maxDuration, psDuration);
            }

            // 如果没有粒子系统，返回默认值
            if (maxDuration <= 0f)
            {
                maxDuration = 1f; // 默认1秒
                Debug.Log($"特效 {effectObject.name} 没有粒子系统，使用默认时长: {maxDuration}s");
            }
            else
            {
                Debug.Log($"特效 {effectObject.name} 实际持续时间: {maxDuration:F2}s");
            }

            return maxDuration;
        }

        /// <summary>
        /// 隐藏所有特效
        /// </summary>
        private void HideAllEffects()
        {
            foreach (var instance in activeEffectInstances.Values)
            {
                SetEffectActive(instance.effectObject, false);
                instance.isActive = false;
            }
        }

        /// <summary>
        /// 生成特效唯一标识键
        /// </summary>
        /// <param name="effectClip">特效片段数据</param>
        /// <returns>唯一标识键</returns>
        private string GenerateEffectKey(EffectTrack.EffectClip effectClip)
        {
            // 使用特效预制体名称和片段名称作为唯一标识，而不依赖可变的起始帧
            // 这样当起始帧发生变化时，仍能找到对应的特效实例
            string prefabName = effectClip.effectPrefab != null ? effectClip.effectPrefab.name : "null";
            return $"{prefabName}_{effectClip.clipName}";
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            StopPreview();

            // 清理特效实例
            foreach (var instance in activeEffectInstances.Values)
            {
                if (instance.effectObject != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(instance.effectObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(instance.effectObject);
                    }
                }
            }

            activeEffectInstances.Clear();

            // 清理特效容器（仅在编辑器模式下，且容器为空时）
            if (!Application.isPlaying && effectContainer != null && effectContainer.childCount == 0)
            {
                UnityEngine.Object.DestroyImmediate(effectContainer.gameObject);
            }

            skillOwner = null;
            skillConfig = null;
            effectContainer = null;

            Debug.Log("SkillEffectPreviewer: 资源清理完成");
        }

        #endregion

        #region 调试方法

        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息字符串</returns>
        public string GetDebugInfo()
        {
            if (!isPreviewActive) return "特效预览未激活";

            int activeCount = activeEffectInstances.Values.Count(i => i.isActive);
            int totalCount = activeEffectInstances.Count;

            return $"特效预览 - 帧:{currentFrame}, 激活特效:{activeCount}/{totalCount}";
        }

        #endregion
    }
}
