using FFramework.Kit;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器预览器处理器
    /// 负责管理所有类型的预览器（动画、特效、Transform等）
    /// </summary>
    public class SkillEditorPreviewerHandler : System.IDisposable
    {
        #region 私有字段

        /// <summary>动画预览管理器</summary>
        private SkillAnimationPreviewer animationPreviewer;

        /// <summary>特效预览管理器</summary>
        private SkillEffectPreviewer effectPreviewer;

        /// <summary>Transform预览管理器</summary>
        private SkillTransformPreviewer transformPreviewer;

        /// <summary>音频预览管理器</summary>
        private SkillAudioPreviewer audioPreviewer;

        /// <summary>伤害检测预览管理器</summary>
        private SkillInjuryDetectionPreviewer injuryDetectionPreviewer;

        /// <summary>当前技能所有者</summary>
        private SkillRuntimeController currentSkillOwner;

        #endregion

        #region 构造函数和析构

        /// <summary>
        /// 构造函数，初始化预览器处理器
        /// </summary>
        public SkillEditorPreviewerHandler()
        {
            // 注意：所有预览器都需要技能所有者参数，它们会在 UpdateSkillOwner 方法中进行初始化

            // 订阅事件
            SkillEditorEvent.OnCurrentFrameChanged += OnCurrentFrameChanged;
            SkillEditorEvent.OnPlayStateChanged += OnPlayStateChanged;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            animationPreviewer?.Dispose();
            effectPreviewer?.Dispose();
            transformPreviewer?.Dispose();
            audioPreviewer?.Dispose();
            injuryDetectionPreviewer?.Dispose();

            // 取消事件订阅
            SkillEditorEvent.OnCurrentFrameChanged -= OnCurrentFrameChanged;
            SkillEditorEvent.OnPlayStateChanged -= OnPlayStateChanged;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取动画预览管理器
        /// </summary>
        public SkillAnimationPreviewer AnimationPreviewer => animationPreviewer;

        /// <summary>
        /// 获取特效预览管理器
        /// </summary>
        public SkillEffectPreviewer EffectPreviewer => effectPreviewer;

        /// <summary>
        /// 获取Transform预览管理器
        /// </summary>
        public SkillTransformPreviewer TransformPreviewer => transformPreviewer;

        /// <summary>
        /// 获取音频预览管理器
        /// </summary>
        public SkillAudioPreviewer AudioPreviewer => audioPreviewer;

        /// <summary>
        /// 获取伤害检测预览管理器
        /// </summary>
        public SkillInjuryDetectionPreviewer InjuryDetectionPreviewer => injuryDetectionPreviewer;

        /// <summary>
        /// 当前技能所有者
        /// </summary>
        public SkillRuntimeController CurrentSkillOwner => currentSkillOwner;

        #endregion

        #region 公共方法

        /// <summary>
        /// 更新技能所属对象
        /// </summary>
        /// <param name="selectedGameObject">选择的游戏对象</param>
        public void UpdateSkillOwner(SkillRuntimeController selectedGameObject)
        {
            currentSkillOwner = selectedGameObject;

            // 更新技能配置中的owner字段
            if (SkillEditorData.CurrentSkillConfig != null)
            {
                SkillEditorData.CurrentSkillConfig.owner = selectedGameObject;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(SkillEditorData.CurrentSkillConfig);
#endif
            }

            // 重新初始化动画预览器
            InitializeAnimationPreviewer(selectedGameObject);

            // 重新初始化音频预览器
            InitializeAudioPreviewer(selectedGameObject);

            // 重新初始化特效预览器
            InitializeEffectPreviewer(selectedGameObject);

            // 重新初始化Transform预览器
            InitializeTransformPreviewer(selectedGameObject);

            // 重新初始化伤害检测预览器
            InitializeInjuryDetectionPreviewer(selectedGameObject);
        }

        /// <summary>
        /// 更新技能配置
        /// </summary>
        /// <param name="newConfig">新的技能配置</param>
        public void UpdateSkillConfig(FFramework.Kit.SkillConfig newConfig)
        {
            // 更新特效预览器配置
            UpdateEffectPreviewerConfig(newConfig);

            // 更新Transform预览器配置
            UpdateTransformPreviewerConfig(newConfig);

            // 更新伤害检测预览器配置
            UpdateInjuryDetectionPreviewerConfig(newConfig);
        }

        /// <summary>
        /// 刷新特效预览器数据 - 当轨道项发生变化时调用
        /// </summary>
        public void RefreshEffectPreviewerData()
        {
            if (effectPreviewer != null && effectPreviewer.IsPreviewActive)
            {
                effectPreviewer.RefreshEffectData();
            }
        }

        /// <summary>
        /// 刷新Transform预览器数据 - 当轨道项发生变化时调用
        /// </summary>
        public void RefreshTransformPreviewerData()
        {
            if (transformPreviewer != null && transformPreviewer.IsPreviewActive)
            {
                transformPreviewer.RefreshTransformData();
            }
        }

        /// <summary>
        /// 预览指定的音频剪辑
        /// </summary>
        /// <param name="audioClip">要预览的音频剪辑</param>
        /// <param name="currentFrame">当前帧数</param>
        public void PreviewAudioClip(AudioClip audioClip, int currentFrame)
        {
            if (audioPreviewer != null)
            {
                // 如果音频预览器没有启动，先启动它
                if (!audioPreviewer.IsPreviewActive)
                {
                    audioPreviewer.StartPreview();
                }

                audioPreviewer.PreviewAudio(audioClip, currentFrame);
            }
        }

        /// <summary>
        /// 停止音频预览
        /// </summary>
        public void StopAudioPreview()
        {
            audioPreviewer?.StopAudio();
        }

        /// <summary>
        /// 启动所有预览器
        /// </summary>
        public void StartAllPreviewers()
        {
            if (SkillEditorData.CurrentSkillConfig == null) return;

            // 启动动画预览
            if (animationPreviewer != null && !animationPreviewer.IsPreviewing)
            {
                animationPreviewer.StartPreview(SkillEditorData.CurrentSkillConfig);
            }

            // 启动特效预览
            if (effectPreviewer != null && !effectPreviewer.IsPreviewActive)
            {
                effectPreviewer.StartPreview();
            }

            // 启动Transform预览
            if (transformPreviewer != null && !transformPreviewer.IsPreviewActive)
            {
                transformPreviewer.StartPreview();
            }

            // 启动音频预览
            if (audioPreviewer != null && !audioPreviewer.IsPreviewActive)
            {
                audioPreviewer.StartPreview();
            }

            // 启动伤害检测预览
            if (injuryDetectionPreviewer != null && !injuryDetectionPreviewer.IsPreviewActive)
            {
                injuryDetectionPreviewer.StartPreview();
            }
        }

        /// <summary>
        /// 停止所有预览器
        /// </summary>
        public void StopAllPreviewers()
        {
            animationPreviewer?.StopPreview();
            effectPreviewer?.StopPreview();
            transformPreviewer?.StopPreview();
            audioPreviewer?.StopPreview();
            injuryDetectionPreviewer?.StopPreview();
        }

        /// <summary>
        /// 预览指定帧
        /// </summary>
        /// <param name="frame">要预览的帧数</param>
        public void PreviewFrame(int frame)
        {
            // 驱动动画到指定帧
            if (animationPreviewer != null && animationPreviewer.IsPreviewing)
            {
                animationPreviewer.PreviewFrame(frame);
            }

            // 驱动特效到指定帧
            if (effectPreviewer != null && effectPreviewer.IsPreviewActive)
            {
                effectPreviewer.PreviewFrame(frame);
            }

            // 驱动Transform到指定帧
            if (transformPreviewer != null && transformPreviewer.IsPreviewActive)
            {
                transformPreviewer.PreviewFrame(frame);
            }

            // 驱动音频到指定帧
            if (audioPreviewer != null && audioPreviewer.IsPreviewActive)
            {
                // 注意：这里需要从技能配置中获取当前帧对应的音频剪辑
                // 这个逻辑可能需要根据具体的音频轨道数据结构来实现
                audioPreviewer.PreviewFrame(frame);
            }

            // 驱动伤害检测到指定帧
            if (injuryDetectionPreviewer != null && injuryDetectionPreviewer.IsPreviewActive)
            {
                injuryDetectionPreviewer.PreviewFrame(frame);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 当前帧变更事件处理
        /// 当当前帧变化时，根据播放状态智能驱动所有预览器
        /// </summary>
        /// <param name="frame">新的当前帧</param>
        private void OnCurrentFrameChanged(int frame)
        {
            if (SkillEditorData.CurrentSkillConfig == null) return;

            // 处理动画预览
            HandleAnimationPreview(frame);

            // 处理特效预览
            HandleEffectPreview(frame);

            // 处理Transform预览
            HandleTransformPreview(frame);

            // 处理音频预览
            HandleAudioPreview(frame);

            // 处理伤害检测预览
            HandleInjuryDetectionPreview(frame);
        }

        /// <summary>
        /// 播放状态变更事件处理
        /// 处理所有预览器的播放状态变化
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        private void OnPlayStateChanged(bool isPlaying)
        {
            HandleAnimationPlayState(isPlaying);
            HandleEffectPlayState(isPlaying);
            HandleTransformPlayState(isPlaying);
            HandleAudioPlayState(isPlaying);
            HandleInjuryDetectionPlayState(isPlaying);
        }

        #endregion

        #region 动画预览处理

        /// <summary>
        /// 处理动画预览
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void HandleAnimationPreview(int frame)
        {
            if (animationPreviewer == null) return;

            // 如果没有在预览状态，则启动预览
            if (!animationPreviewer.IsPreviewing)
            {
                animationPreviewer.StartPreview(SkillEditorData.CurrentSkillConfig);
            }

            // 驱动动画到指定帧（UpdateEditModePreview会检查播放状态）
            animationPreviewer.PreviewFrame(frame);
        }

        /// <summary>
        /// 处理动画播放状态
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        private void HandleAnimationPlayState(bool isPlaying)
        {
            if (animationPreviewer == null) return;

            if (isPlaying)
            {
                // 开始播放或恢复播放
                if (SkillEditorData.CurrentSkillConfig != null && animationPreviewer.PreviewTarget != null)
                {
                    // 如果动画预览器正在预览状态，只需要恢复播放速度
                    if (animationPreviewer.IsPreviewing)
                    {
                        animationPreviewer.SetPreviewSpeed(1.0f);
                    }
                    else
                    {
                        // 如果没有在预览状态，启动预览并跳转到当前帧
                        animationPreviewer.StartPreview(SkillEditorData.CurrentSkillConfig);
                        animationPreviewer.PreviewFrame(SkillEditorData.CurrentFrame);
                    }
                }
            }
            else
            {
                // 暂停时只设置播放速度为0，保持当前帧位置
                animationPreviewer.SetPreviewSpeed(0.0f);
            }
        }

        #endregion

        #region 特效预览处理

        /// <summary>
        /// 处理特效预览
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void HandleEffectPreview(int frame)
        {
            // 如果特效预览器不存在，尝试初始化
            if (effectPreviewer == null && SkillEditorData.CurrentSkillConfig.owner != null)
            {
                InitializeEffectPreviewer(SkillEditorData.CurrentSkillConfig.owner);
                Debug.Log($"自动初始化特效预览器 - 技能所有者: {SkillEditorData.CurrentSkillConfig.owner.name}");
            }

            if (effectPreviewer != null)
            {
                // 如果没有在预览状态，则启动预览
                if (!effectPreviewer.IsPreviewActive)
                {
                    effectPreviewer.StartPreview();
                }

                // 根据播放状态使用不同的预览方法
                if (SkillEditorData.IsPlaying)
                {
                    effectPreviewer.TickView(frame);
                }
                else
                {
                    effectPreviewer.OnPlay(frame);
                }

                // 驱动特效到指定帧
                effectPreviewer.PreviewFrame(frame);
            }
            else
            {
                Debug.LogWarning($"特效预览器为空 - 帧: {frame}, 技能配置: {SkillEditorData.CurrentSkillConfig?.skillName}, 技能所有者: {SkillEditorData.CurrentSkillConfig?.owner?.name}");
            }
        }

        /// <summary>
        /// 处理特效播放状态
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        private void HandleEffectPlayState(bool isPlaying)
        {
            if (effectPreviewer == null) return;

            if (isPlaying)
            {
                // 开始播放或恢复播放特效
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    if (!effectPreviewer.IsPreviewActive)
                    {
                        effectPreviewer.StartPreview();
                    }
                    // 使用TickView方法处理播放状态下的特效更新
                    effectPreviewer.TickView(SkillEditorData.CurrentFrame);
                    effectPreviewer.PreviewFrame(SkillEditorData.CurrentFrame);
                }
            }
            else
            {
                // 暂停时使用OnPlay方法显示当前帧状态
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    effectPreviewer.OnPlay(SkillEditorData.CurrentFrame);
                }
            }
        }

        /// <summary>
        /// 初始化特效预览器
        /// </summary>
        /// <param name="selectedGameObject">选择的游戏对象作为技能所有者</param>
        private void InitializeEffectPreviewer(SkillRuntimeController selectedGameObject)
        {
            // 清理现有的特效预览器
            if (effectPreviewer != null)
            {
                effectPreviewer.Dispose();
                effectPreviewer = null;
            }

            // 如果有技能所有者和技能配置，创建新的特效预览器
            if (selectedGameObject != null && SkillEditorData.CurrentSkillConfig != null)
            {
                effectPreviewer = new SkillEffectPreviewer(selectedGameObject, SkillEditorData.CurrentSkillConfig);
                Debug.Log($"初始化特效预览器 - 技能所有者: {selectedGameObject.name}");
            }
        }

        /// <summary>
        /// 更新特效预览器的配置
        /// </summary>
        /// <param name="newConfig">新的技能配置</param>
        private void UpdateEffectPreviewerConfig(SkillConfig newConfig)
        {
            if (newConfig == null) return;

            // 如果特效预览器不存在，尝试使用技能配置中的owner初始化
            if (effectPreviewer == null)
            {
                if (newConfig.owner != null)
                {
                    InitializeEffectPreviewer(newConfig.owner);
                }
                return;
            }

            // 重新初始化特效预览器以使用新配置
            SkillRuntimeController currentOwner = effectPreviewer.SkillOwner;
            if (currentOwner != null)
            {
                effectPreviewer.Dispose();
                effectPreviewer = new SkillEffectPreviewer(currentOwner, newConfig);
            }
        }

        /// <summary>
        /// 初始化动画预览器
        /// </summary>
        /// <param name="selectedGameObject">选择的游戏对象作为技能所有者</param>
        private void InitializeAnimationPreviewer(SkillRuntimeController selectedGameObject)
        {
            // 清理现有的动画预览器
            if (animationPreviewer != null)
            {
                animationPreviewer.Dispose();
                animationPreviewer = null;
            }

            // 创建新的动画预览器
            animationPreviewer = new SkillAnimationPreviewer();

            // 如果有技能所有者，设置预览目标
            if (selectedGameObject != null)
            {
                if (animationPreviewer.SetPreviewTarget(selectedGameObject))
                {
                    Debug.Log($"初始化动画预览器 - 技能所有者: {selectedGameObject.name}");
                }
            }
        }

        /// <summary>
        /// 初始化音频预览器
        /// </summary>
        /// <param name="selectedGameObject">选择的游戏对象作为技能所有者</param>
        private void InitializeAudioPreviewer(SkillRuntimeController selectedGameObject)
        {
            // 清理现有的音频预览器
            if (audioPreviewer != null)
            {
                audioPreviewer.Dispose();
                audioPreviewer = null;
            }

            // 创建新的音频预览器
            audioPreviewer = new SkillAudioPreviewer();
            Debug.Log($"初始化音频预览器 - 技能所有者: {selectedGameObject?.name}");
        }

        #endregion

        #region Transform预览处理

        /// <summary>
        /// 处理Transform预览
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void HandleTransformPreview(int frame)
        {
            // 如果Transform预览器不存在，尝试初始化
            if (transformPreviewer == null && SkillEditorData.CurrentSkillConfig.owner != null)
            {
                InitializeTransformPreviewer(SkillEditorData.CurrentSkillConfig.owner);
                Debug.Log($"自动初始化Transform预览器 - 技能所有者: {SkillEditorData.CurrentSkillConfig.owner.name}");
            }

            if (transformPreviewer != null)
            {
                // 如果没有在预览状态，则启动预览
                if (!transformPreviewer.IsPreviewActive)
                {
                    transformPreviewer.StartPreview();
                }

                // 驱动Transform到指定帧
                transformPreviewer.PreviewFrame(frame);
            }
            else
            {
                Debug.LogWarning($"Transform预览器为空 - 帧: {frame}, 技能配置: {SkillEditorData.CurrentSkillConfig?.skillName}, 技能所有者: {SkillEditorData.CurrentSkillConfig?.owner?.name}");
            }
        }

        /// <summary>
        /// 处理Transform播放状态
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        private void HandleTransformPlayState(bool isPlaying)
        {
            if (transformPreviewer == null) return;

            if (isPlaying)
            {
                // 开始播放或恢复播放Transform
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    if (!transformPreviewer.IsPreviewActive)
                    {
                        transformPreviewer.StartPreview();
                    }
                    transformPreviewer.PreviewFrame(SkillEditorData.CurrentFrame);
                }
            }
            else
            {
                // 暂停时保持当前Transform状态
                // Transform预览器没有播放速度概念，只是显示当前帧的状态
            }
        }

        /// <summary>
        /// 初始化Transform预览器
        /// </summary>
        /// <param name="selectedGameObject">选择的游戏对象作为技能所有者</param>
        private void InitializeTransformPreviewer(SkillRuntimeController selectedGameObject)
        {
            // 清理现有的Transform预览器
            if (transformPreviewer != null)
            {
                transformPreviewer.Dispose();
                transformPreviewer = null;
            }

            // 如果有技能所有者和技能配置，创建新的Transform预览器
            if (selectedGameObject != null && SkillEditorData.CurrentSkillConfig != null)
            {
                transformPreviewer = new SkillTransformPreviewer(selectedGameObject, SkillEditorData.CurrentSkillConfig);
                Debug.Log($"初始化Transform预览器 - 技能所有者: {selectedGameObject.name}");
            }
        }

        /// <summary>
        /// 更新Transform预览器的配置
        /// </summary>
        /// <param name="newConfig">新的技能配置</param>
        private void UpdateTransformPreviewerConfig(SkillConfig newConfig)
        {
            if (newConfig == null) return;

            // 如果Transform预览器不存在，尝试使用技能配置中的owner初始化
            if (transformPreviewer == null)
            {
                if (newConfig.owner != null)
                {
                    InitializeTransformPreviewer(newConfig.owner);
                }
                return;
            }

            // 重新初始化Transform预览器以使用新配置
            SkillRuntimeController currentOwner = transformPreviewer.SkillOwner;
            if (currentOwner != null)
            {
                transformPreviewer.Dispose();
                transformPreviewer = new SkillTransformPreviewer(currentOwner, newConfig);
            }
        }

        #endregion

        #region 音频预览处理

        /// <summary>
        /// 处理音频预览
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void HandleAudioPreview(int frame)
        {
            if (audioPreviewer == null) return;

            // 如果没有在预览状态，则启动预览
            if (!audioPreviewer.IsPreviewActive)
            {
                audioPreviewer.StartPreview();
            }

            // 根据播放状态选择合适的音频播放方法
            if (SkillEditorData.IsPlaying)
            {
                // 播放模式：使用 TickView 在经过起始帧时播放音频
                audioPreviewer.TickView(frame);
            }
            else
            {
                // 非播放模式：使用 OnPlay 进行交互式音频预览
                audioPreviewer.OnPlay(frame);
            }
        }

        /// <summary>
        /// 处理音频播放状态
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        private void HandleAudioPlayState(bool isPlaying)
        {
            if (audioPreviewer == null) return;

            if (isPlaying)
            {
                // 开始播放或恢复播放音频
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    if (!audioPreviewer.IsPreviewActive)
                    {
                        audioPreviewer.StartPreview();
                    }

                    // 获取当前帧对应的音频并播放
                    AudioClip currentAudioClip = GetAudioClipForFrame(SkillEditorData.CurrentFrame);
                    if (currentAudioClip != null)
                    {
                        audioPreviewer.PreviewFrame(SkillEditorData.CurrentFrame, currentAudioClip);
                    }
                }
            }
            else
            {
                // 暂停时停止音频播放
                audioPreviewer.StopAudio();
            }
        }

        /// <summary>
        /// 获取指定帧对应的音频剪辑
        /// </summary>
        /// <param name="frame">帧数</param>
        /// <returns>音频剪辑，如果没有则返回null</returns>
        private AudioClip GetAudioClipForFrame(int frame)
        {
            // 根据实际的音频轨道数据结构来实现
            if (SkillEditorData.CurrentSkillConfig?.trackContainer?.audioTrack?.audioTracks != null)
            {
                // 遍历所有音频轨道
                foreach (var audioTrack in SkillEditorData.CurrentSkillConfig.trackContainer.audioTrack.audioTracks)
                {
                    if (audioTrack?.audioClips != null)
                    {
                        // 遍历轨道中的音频片段
                        foreach (var audioClip in audioTrack.audioClips)
                        {
                            // 检查当前帧是否在这个音频项的时间范围内
                            if (frame >= audioClip.startFrame && frame <= audioClip.startFrame + audioClip.durationFrame)
                            {
                                return audioClip.clip;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取音频剪辑的起始帧
        /// </summary>
        /// <param name="audioClip">音频剪辑</param>
        /// <returns>音频剪辑的起始帧</returns>
        private int GetAudioStartFrameForClip(AudioClip audioClip)
        {
            if (SkillEditorData.CurrentSkillConfig?.trackContainer?.audioTrack?.audioTracks != null)
            {
                foreach (var audioTrack in SkillEditorData.CurrentSkillConfig.trackContainer.audioTrack.audioTracks)
                {
                    if (audioTrack?.audioClips != null)
                    {
                        foreach (var audioClipItem in audioTrack.audioClips)
                        {
                            if (audioClipItem.clip == audioClip)
                            {
                                return audioClipItem.startFrame;
                            }
                        }
                    }
                }
            }
            return -1; // 默认返回-1表示未找到
        }

        /// <summary>
        /// 获取音频剪辑的结束帧
        /// </summary>
        /// <param name="audioClip">音频剪辑</param>
        /// <returns>音频剪辑的结束帧</returns>
        private int GetAudioEndFrameForClip(AudioClip audioClip)
        {
            if (SkillEditorData.CurrentSkillConfig?.trackContainer?.audioTrack?.audioTracks != null)
            {
                foreach (var audioTrack in SkillEditorData.CurrentSkillConfig.trackContainer.audioTrack.audioTracks)
                {
                    if (audioTrack?.audioClips != null)
                    {
                        foreach (var audioClipItem in audioTrack.audioClips)
                        {
                            if (audioClipItem.clip == audioClip)
                            {
                                return audioClipItem.startFrame + audioClipItem.durationFrame;
                            }
                        }
                    }
                }
            }
            return -1; // 默认返回-1表示未找到
        }

        #endregion

        #region 伤害检测预览处理

        /// <summary>
        /// 处理伤害检测预览
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void HandleInjuryDetectionPreview(int frame)
        {
            // 如果伤害检测预览器不存在，尝试初始化
            if (injuryDetectionPreviewer == null && SkillEditorData.CurrentSkillConfig.owner != null)
            {
                InitializeInjuryDetectionPreviewer(SkillEditorData.CurrentSkillConfig.owner);
                Debug.Log($"自动初始化伤害检测预览器 - 技能所有者: {SkillEditorData.CurrentSkillConfig.owner.name}");
            }

            if (injuryDetectionPreviewer != null)
            {
                // 如果没有在预览状态，则启动预览
                if (!injuryDetectionPreviewer.IsPreviewActive)
                {
                    injuryDetectionPreviewer.StartPreview();
                }

                // 驱动伤害检测到指定帧
                injuryDetectionPreviewer.PreviewFrame(frame);
            }
            else
            {
                Debug.LogWarning($"伤害检测预览器为空 - 帧: {frame}, 技能配置: {SkillEditorData.CurrentSkillConfig?.skillName}, 技能所有者: {SkillEditorData.CurrentSkillConfig?.owner?.name}");
            }
        }

        /// <summary>
        /// 处理伤害检测播放状态
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        private void HandleInjuryDetectionPlayState(bool isPlaying)
        {
            if (injuryDetectionPreviewer == null) return;

            if (isPlaying)
            {
                // 开始播放或恢复播放伤害检测
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    if (!injuryDetectionPreviewer.IsPreviewActive)
                    {
                        injuryDetectionPreviewer.StartPreview();
                    }
                    injuryDetectionPreviewer.PreviewFrame(SkillEditorData.CurrentFrame);
                }
            }
            else
            {
                // 暂停时保持当前伤害检测状态
                // 伤害检测预览器没有播放速度概念，只是显示当前帧的状态
            }
        }

        /// <summary>
        /// 初始化伤害检测预览器
        /// </summary>
        /// <param name="selectedGameObject">选择的游戏对象作为技能所有者</param>
        private void InitializeInjuryDetectionPreviewer(SkillRuntimeController selectedGameObject)
        {
            // 清理现有的伤害检测预览器
            if (injuryDetectionPreviewer != null)
            {
                injuryDetectionPreviewer.Dispose();
                injuryDetectionPreviewer = null;
            }

            // 如果有技能所有者和技能配置，创建新的伤害检测预览器
            if (selectedGameObject != null && SkillEditorData.CurrentSkillConfig != null)
            {
                injuryDetectionPreviewer = new SkillInjuryDetectionPreviewer(selectedGameObject, SkillEditorData.CurrentSkillConfig);
                Debug.Log($"初始化伤害检测预览器 - 技能所有者: {selectedGameObject.name}");
            }
        }

        /// <summary>
        /// 更新伤害检测预览器的配置
        /// </summary>
        /// <param name="newConfig">新的技能配置</param>
        private void UpdateInjuryDetectionPreviewerConfig(SkillConfig newConfig)
        {
            if (newConfig == null) return;

            // 如果伤害检测预览器不存在，尝试使用技能配置中的owner初始化
            if (injuryDetectionPreviewer == null)
            {
                if (newConfig.owner != null)
                {
                    InitializeInjuryDetectionPreviewer(newConfig.owner);
                }
                return;
            }

            // 重新初始化伤害检测预览器以使用新配置
            SkillRuntimeController currentOwner = injuryDetectionPreviewer.SkillOwner;
            if (currentOwner != null)
            {
                injuryDetectionPreviewer.Dispose();
                injuryDetectionPreviewer = new SkillInjuryDetectionPreviewer(currentOwner, newConfig);
            }
        }

        #endregion
    }
}
