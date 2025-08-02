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

        private SkillRuntimeController skillOwner;
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
        public SkillRuntimeController SkillOwner => skillOwner;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造特效预览器
        /// </summary>
        /// <param name="owner">技能所有者</param>
        /// <param name="config">技能配置</param>
        public SkillEffectPreviewer(SkillRuntimeController owner, SkillConfig config)
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
            // 先隐藏所有特效，然后更新配置数据
            HideAllEffects();
            UpdateEffectInstancesData();

            foreach (var instance in activeEffectInstances.Values)
            {
                var effectClip = instance.clipData;

                // 检查当前帧是否在特效播放范围内
                if (IsEffectActiveAtFrame(effectClip, currentFrame))
                {
                    instance.isActive = true;

                    // 计算播放时间并激活特效
                    float playTime = CalculateEffectPlayTime(effectClip, currentFrame);
                    PlayEffectAtTime(instance.effectObject, playTime);
                }
            }
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

            // 使用本地坐标系，使特效能跟随父物体移动
            effectTransform.localPosition = effectClip.position;
            effectTransform.localRotation = Quaternion.Euler(effectClip.rotation);
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

            // 简单控制粒子系统播放状态
            var particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                if (active && !ps.isPlaying)
                    ps.Play();
                else if (!active && ps.isPlaying)
                    ps.Stop();
            }
        }

        /// <summary>
        /// 检查特效在指定帧是否应该激活
        /// </summary>
        /// <param name="effectClip">特效片段</param>
        /// <param name="frame">当前帧</param>
        /// <returns>是否应该激活</returns>
        private bool IsEffectActiveAtFrame(EffectTrack.EffectClip effectClip, int frame)
        {
            return frame >= effectClip.startFrame && frame < effectClip.startFrame + effectClip.durationFrame;
        }

        /// <summary>
        /// 计算特效播放时间
        /// </summary>
        /// <param name="effectClip">特效片段</param>
        /// <param name="currentFrame">当前帧</param>
        /// <returns>播放时间（秒）</returns>
        private float CalculateEffectPlayTime(EffectTrack.EffectClip effectClip, int currentFrame)
        {
            int relativeFrame = currentFrame - effectClip.startFrame;
            return Mathf.Max(0, relativeFrame) / GetFrameRate();
        }

        /// <summary>
        /// 在指定时间播放特效
        /// </summary>
        /// <param name="effectObject">特效对象</param>
        /// <param name="timeInSeconds">播放时间</param>
        private void PlayEffectAtTime(GameObject effectObject, float timeInSeconds)
        {
            if (effectObject == null) return;

            effectObject.SetActive(true);

            var particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                if (ps.isPlaying) ps.Stop(true);

                ps.Play();
                if (timeInSeconds > 0)
                    ps.Simulate(timeInSeconds, true, false, true);
            }
        }

        /// <summary>
        /// 隐藏所有特效
        /// </summary>
        private void HideAllEffects()
        {
            foreach (var instance in activeEffectInstances.Values)
            {
                instance.isActive = false;
                SetEffectActive(instance.effectObject, false);
            }
        }

        /// <summary>
        /// 获取帧率（从技能配置或使用默认值）
        /// </summary>
        /// <returns>帧率</returns>
        private float GetFrameRate()
        {
            if (skillConfig != null) return skillConfig.frameRate;
            if (SkillEditorData.CurrentSkillConfig != null) return SkillEditorData.CurrentSkillConfig.frameRate;
            return 30f; // 默认30fps
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
