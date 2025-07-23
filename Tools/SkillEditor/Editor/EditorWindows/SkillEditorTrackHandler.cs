using System.Collections.Generic;
using UnityEngine.UIElements;
using FFramework.Kit;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道管理器
    /// 专门负责轨道的创建、管理、删除和配置数据同步
    /// 将轨道相关的复杂逻辑从UI构建器中分离出来
    /// </summary>
    public class SkillEditorTrackHandler
    {
        #region 私有字段

        /// <summary>技能编辑器事件管理器</summary>
        private readonly SkillEditorEvent skillEditorEvent;

        /// <summary>轨道控制区域内容容器</summary>
        private VisualElement trackControlContent;

        /// <summary>所有轨道内容容器</summary>
        private VisualElement allTrackContent;

        #endregion

        #region 构造函数

        /// <summary>
        /// 轨道管理器构造函数
        /// </summary>
        /// <param name="skillEditorEvent">技能编辑器事件管理器实例</param>
        public SkillEditorTrackHandler(SkillEditorEvent skillEditorEvent)
        {
            this.skillEditorEvent = skillEditorEvent;
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化轨道管理器
        /// 设置轨道控制和内容容器的引用
        /// </summary>
        /// <param name="trackControlContent">轨道控制内容容器</param>
        /// <param name="allTrackContent">所有轨道内容容器</param>
        public void Initialize(VisualElement trackControlContent, VisualElement allTrackContent)
        {
            this.trackControlContent = trackControlContent;
            this.allTrackContent = allTrackContent;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 根据技能配置创建轨道
        /// 公共接口，供外部调用
        /// </summary>
        public void CreateTracksFromConfigPublic()
        {
            CreateTracksFromConfig();
        }

        #endregion

        #region 轨道创建和管理

        /// <summary>
        /// 显示轨道创建菜单
        /// 根据轨道类型限制显示可创建的轨道类型选项
        /// </summary>
        /// <param name="button">触发菜单的按钮元素</param>
        public void ShowTrackCreationMenu(VisualElement button)
        {
            var menu = new GenericMenu();

            // 检查是否已存在动画轨道（限制只能有一个）
            bool hasAnimationTrack = SkillEditorData.tracks.Exists(t => t.TrackType == TrackType.AnimationTrack);

            // 创建动画轨道菜单项
            menu.AddItem(new GUIContent("创建 Animation Track"), hasAnimationTrack, () =>
            {
                if (!hasAnimationTrack)
                    CreateTrack(TrackType.AnimationTrack);
                else
                    Debug.LogWarning("(动画轨道只可存在一条)已存在动画轨道，无法创建新的动画轨道。");
            });

            // 创建其他类型轨道菜单项
            menu.AddItem(new GUIContent("创建 Audio Track"), false, () => CreateTrack(TrackType.AudioTrack));
            menu.AddItem(new GUIContent("创建 Effect Track"), false, () => CreateTrack(TrackType.EffectTrack));
            menu.AddItem(new GUIContent("创建 Attack Track"), false, () => CreateTrack(TrackType.AttackTrack));
            menu.AddItem(new GUIContent("创建 Event Track"), false, () => CreateTrack(TrackType.EventTrack));

            // 在按钮下方显示菜单
            var rect = button.worldBound;
            menu.DropDown(new Rect(rect.x, rect.yMax, 0, 0));
        }

        /// <summary>
        /// 创建指定类型的轨道
        /// 创建轨道控制器和轨道内容，并订阅相关事件
        /// </summary>
        /// <param name="trackType">要创建的轨道类型</param>
        public void CreateTrack(TrackType trackType)
        {
            // 计算当前轨道类型的索引
            int trackIndex = SkillEditorData.tracks.Count(t => t.TrackType == trackType);
            string trackName = trackIndex == 0 ? $"{trackType}" : $"{trackType} {trackIndex + 1}";

            // 在配置文件中创建对应的轨道数据
            CreateTrackDataInConfig(trackType, trackIndex);

            // 创建轨道控制器和轨道内容
            var trackControl = new SkillEditorTrackControl(trackControlContent, trackType, trackName);
            var track = new SkillEditorTrack(allTrackContent, trackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig, trackIndex);

            // 创建轨道信息并添加到数据中
            var trackInfo = new SkillEditorTrackInfo(trackControl, track, trackType, trackName);
            trackInfo.TrackIndex = trackIndex; // 设置轨道索引
            SkillEditorData.tracks.Add(trackInfo);

            // 订阅轨道事件
            SubscribeTrackEvents(trackControl);

            // 如果配置文件中有对应轨道的数据，使用索引加载对应的数据
            CreateTrackItemsFromConfigByIndex(track, trackType, trackIndex);
        }

        /// <summary>
        /// 根据技能配置自动创建所有包含数据的轨道
        /// </summary>
        public void CreateTracksFromConfig()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null)
            {
                return;
            }

            // 检查动画轨道
            if (HasAnimationTrackData(skillConfig) && !HasTrackType(TrackType.AnimationTrack))
            {
                CreateTrack(TrackType.AnimationTrack);
            }

            // 为每个音频轨道数据创建对应的UI轨道
            CreateAudioTracksFromConfig(skillConfig);

            // 为每个特效轨道数据创建对应的UI轨道  
            CreateEffectTracksFromConfig(skillConfig);

            // 为每个伤害检测轨道数据创建对应的UI轨道
            CreateInjuryDetectionTracksFromConfig(skillConfig);

            // 为每个事件轨道数据创建对应的UI轨道
            CreateEventTracksFromConfig(skillConfig);
        }

        /// <summary>
        /// 检查是否已存在指定类型的轨道
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        private bool HasTrackType(TrackType trackType)
        {
            return SkillEditorData.tracks.Any(t => t.TrackType == trackType);
        }

        /// <summary>
        /// 为每个音频轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateAudioTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.audioTracks == null) return;

            int existingAudioTrackCount = SkillEditorData.tracks.Count(t => t.TrackType == TrackType.AudioTrack);

            for (int i = 0; i < skillConfig.trackContainer.audioTracks.Count; i++)
            {
                var audioTrackData = skillConfig.trackContainer.audioTracks[i];
                if (audioTrackData?.audioClips != null && audioTrackData.audioClips.Count > 0)
                {
                    // 检查是否已存在对应索引的音频轨道
                    if (i >= existingAudioTrackCount)
                    {
                        CreateTrackWithIndex(TrackType.AudioTrack, i);
                    }
                }
            }
        }

        /// <summary>
        /// 为每个特效轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateEffectTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.effectTracks == null) return;

            int existingEffectTrackCount = SkillEditorData.tracks.Count(t => t.TrackType == TrackType.EffectTrack);

            for (int i = 0; i < skillConfig.trackContainer.effectTracks.Count; i++)
            {
                var effectTrackData = skillConfig.trackContainer.effectTracks[i];
                if (effectTrackData?.effectClips != null && effectTrackData.effectClips.Count > 0)
                {
                    if (i >= existingEffectTrackCount)
                    {
                        CreateTrackWithIndex(TrackType.EffectTrack, i);
                    }
                }
            }
        }

        /// <summary>
        /// 为每个伤害检测轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateInjuryDetectionTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.injuryDetectionTracks == null) return;

            int existingAttackTrackCount = SkillEditorData.tracks.Count(t => t.TrackType == TrackType.AttackTrack);

            for (int i = 0; i < skillConfig.trackContainer.injuryDetectionTracks.Count; i++)
            {
                var attackTrackData = skillConfig.trackContainer.injuryDetectionTracks[i];
                if (attackTrackData?.injuryDetectionClips != null && attackTrackData.injuryDetectionClips.Count > 0)
                {
                    if (i >= existingAttackTrackCount)
                    {
                        CreateTrackWithIndex(TrackType.AttackTrack, i);
                    }
                }
            }
        }

        /// <summary>
        /// 为每个事件轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateEventTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.eventTracks == null) return;

            int existingEventTrackCount = SkillEditorData.tracks.Count(t => t.TrackType == TrackType.EventTrack);

            for (int i = 0; i < skillConfig.trackContainer.eventTracks.Count; i++)
            {
                var eventTrackData = skillConfig.trackContainer.eventTracks[i];
                if (eventTrackData?.eventClips != null && eventTrackData.eventClips.Count > 0)
                {
                    if (i >= existingEventTrackCount)
                    {
                        CreateTrackWithIndex(TrackType.EventTrack, i);
                    }
                }
            }
        }

        /// <summary>
        /// 创建带有索引的轨道
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        private void CreateTrackWithIndex(TrackType trackType, int trackIndex)
        {
            string trackName = $"{trackType} {trackIndex + 1}";

            // 创建轨道控制器和轨道内容
            var trackControl = new SkillEditorTrackControl(trackControlContent, trackType, trackName);
            var track = new SkillEditorTrack(allTrackContent, trackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig, trackIndex);

            // 创建轨道信息并添加到数据中
            var trackInfo = new SkillEditorTrackInfo(trackControl, track, trackType, trackName);
            trackInfo.TrackIndex = trackIndex; // 设置轨道索引
            SkillEditorData.tracks.Add(trackInfo);

            // 订阅轨道事件
            SubscribeTrackEvents(trackControl);

            // 根据轨道索引创建对应的轨道项
            CreateTrackItemsFromConfigByIndex(track, trackType, trackIndex);
        }

        /// <summary>
        /// 在配置文件中创建对应的轨道数据结构
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        private void CreateTrackDataInConfig(TrackType trackType, int trackIndex)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null) return;

            switch (trackType)
            {
                case TrackType.AudioTrack:
                    // 确保音频轨道列表有足够的元素
                    while (skillConfig.trackContainer.audioTracks.Count <= trackIndex)
                    {
                        var newAudioTrack = new FFramework.Kit.AudioTrack();
                        newAudioTrack.trackName = $"Audio Track {skillConfig.trackContainer.audioTracks.Count + 1}";
                        skillConfig.trackContainer.audioTracks.Add(newAudioTrack);
                        Debug.Log($"创建音频轨道数据: {newAudioTrack.trackName} (索引: {skillConfig.trackContainer.audioTracks.Count - 1})");
                    }
                    break;

                case TrackType.EffectTrack:
                    // 确保特效轨道列表有足够的元素
                    while (skillConfig.trackContainer.effectTracks.Count <= trackIndex)
                    {
                        var newEffectTrack = new FFramework.Kit.EffectTrack();
                        newEffectTrack.trackName = $"Effect Track {skillConfig.trackContainer.effectTracks.Count + 1}";
                        skillConfig.trackContainer.effectTracks.Add(newEffectTrack);
                        Debug.Log($"创建特效轨道数据: {newEffectTrack.trackName} (索引: {skillConfig.trackContainer.effectTracks.Count - 1})");
                    }
                    break;

                case TrackType.AttackTrack:
                    // 确保攻击轨道列表有足够的元素
                    while (skillConfig.trackContainer.injuryDetectionTracks.Count <= trackIndex)
                    {
                        var newAttackTrack = new FFramework.Kit.InjuryDetectionTrack();
                        newAttackTrack.trackName = $"Attack Track {skillConfig.trackContainer.injuryDetectionTracks.Count + 1}";
                        skillConfig.trackContainer.injuryDetectionTracks.Add(newAttackTrack);
                        Debug.Log($"创建攻击轨道数据: {newAttackTrack.trackName} (索引: {skillConfig.trackContainer.injuryDetectionTracks.Count - 1})");
                    }
                    break;

                case TrackType.EventTrack:
                    // 确保事件轨道列表有足够的元素
                    while (skillConfig.trackContainer.eventTracks.Count <= trackIndex)
                    {
                        var newEventTrack = new FFramework.Kit.EventTrack();
                        newEventTrack.trackName = $"Event Track {skillConfig.trackContainer.eventTracks.Count + 1}";
                        skillConfig.trackContainer.eventTracks.Add(newEventTrack);
                        Debug.Log($"创建事件轨道数据: {newEventTrack.trackName} (索引: {skillConfig.trackContainer.eventTracks.Count - 1})");
                    }
                    break;

                case TrackType.AnimationTrack:
                    // 动画轨道是单轨道，不需要创建新的数据结构
                    if (skillConfig.trackContainer.animationTrack == null)
                    {
                        skillConfig.trackContainer.animationTrack = new FFramework.Kit.AnimationTrack();
                        Debug.Log("创建动画轨道数据");
                    }
                    break;
            }

            // 标记配置文件为已修改
            UnityEditor.EditorUtility.SetDirty(skillConfig);
        }

        #endregion

        #region 轨道刷新和重建

        /// <summary>
        /// 处理刷新请求事件
        /// 清空现有轨道UI并重新创建所有轨道
        /// </summary>
        public void OnRefreshRequested()
        {
            // 清空现有轨道UI
            trackControlContent?.Clear();
            allTrackContent?.Clear();

            // 重新创建所有轨道UI（包含轨道项）
            RefreshAllTracks();

            // 根据配置创建配置中存在但UI中还没有的轨道
            CreateTracksFromConfig();
        }

        /// <summary>
        /// 刷新所有轨道UI
        /// 重新创建所有轨道的UI元素，优先创建动画轨道
        /// </summary>
        private void RefreshAllTracks()
        {
            if (SkillEditorData.tracks == null) return;

            // 保存现有轨道信息并清空列表
            var tracksToRecreate = new List<SkillEditorTrackInfo>(SkillEditorData.tracks);
            SkillEditorData.tracks.Clear();

            // 优先创建动画轨道
            RecreateTracksByType(tracksToRecreate, TrackType.AnimationTrack);

            // 创建其他类型轨道
            RecreateTracksByType(tracksToRecreate, t => t != TrackType.AnimationTrack);
        }

        /// <summary>
        /// 按类型重新创建轨道
        /// </summary>
        /// <param name="tracksToRecreate">需要重新创建的轨道列表</param>
        /// <param name="targetType">目标轨道类型</param>
        private void RecreateTracksByType(List<SkillEditorTrackInfo> tracksToRecreate, TrackType targetType)
        {
            foreach (var oldTrackInfo in tracksToRecreate.Where(t => t.TrackType == targetType))
            {
                var newTrackControl = new SkillEditorTrackControl(trackControlContent, oldTrackInfo.TrackType, oldTrackInfo.TrackName);
                var newTrack = new SkillEditorTrack(allTrackContent, oldTrackInfo.TrackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig, oldTrackInfo.TrackIndex);
                var newTrackInfo = new SkillEditorTrackInfo(newTrackControl, newTrack, oldTrackInfo.TrackType, oldTrackInfo.TrackName);

                // 保持原有的轨道索引
                newTrackInfo.TrackIndex = oldTrackInfo.TrackIndex;

                SkillEditorData.tracks.Add(newTrackInfo);
                SubscribeTrackEvents(newTrackControl);

                // 使用轨道索引重新创建轨道项 - 确保每个轨道使用对应索引的配置数据
                CreateTrackItemsFromConfigByIndex(newTrack, oldTrackInfo.TrackType, oldTrackInfo.TrackIndex);
            }
        }

        /// <summary>
        /// 按类型重新创建轨道（使用谓词筛选）
        /// </summary>
        /// <param name="tracksToRecreate">需要重新创建的轨道列表</param>
        /// <param name="predicate">轨道类型筛选谓词</param>
        private void RecreateTracksByType(List<SkillEditorTrackInfo> tracksToRecreate, Func<TrackType, bool> predicate)
        {
            foreach (var oldTrackInfo in tracksToRecreate.Where(t => predicate(t.TrackType)))
            {
                var newTrackControl = new SkillEditorTrackControl(trackControlContent, oldTrackInfo.TrackType, oldTrackInfo.TrackName);
                var newTrack = new SkillEditorTrack(allTrackContent, oldTrackInfo.TrackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig);
                var newTrackInfo = new SkillEditorTrackInfo(newTrackControl, newTrack, oldTrackInfo.TrackType, oldTrackInfo.TrackName);

                // 保持原有的轨道索引
                newTrackInfo.TrackIndex = oldTrackInfo.TrackIndex;

                SkillEditorData.tracks.Add(newTrackInfo);
                SubscribeTrackEvents(newTrackControl);

                // 使用轨道索引重新创建轨道项 - 确保每个轨道使用对应索引的配置数据
                CreateTrackItemsFromConfigByIndex(newTrack, oldTrackInfo.TrackType, oldTrackInfo.TrackIndex);
            }
        }

        /// <summary>
        /// 清理所有轨道数据和UI
        /// 当配置文件设置为null时调用，清空所有轨道UI和数据
        /// </summary>
        public void ClearAllTracks()
        {
            // 清空轨道UI容器
            trackControlContent?.Clear();
            allTrackContent?.Clear();

            // 清空轨道数据
            if (SkillEditorData.tracks != null)
            {
                SkillEditorData.tracks.Clear();
            }
            Debug.Log("ClearAllTracks: 轨道清理完成");
        }

        #endregion

        #region 轨道事件处理

        /// <summary>
        /// 订阅轨道控制器事件
        /// 包含删除轨道、激活状态变化、添加轨道项等事件处理
        /// </summary>
        /// <param name="trackControl">轨道控制器实例</param>
        private void SubscribeTrackEvents(SkillEditorTrackControl trackControl)
        {
            // 订阅轨道删除事件
            trackControl.OnDeleteTrack += HandleTrackDelete;

            // 订阅激活状态变化事件
            trackControl.OnActiveStateChanged += HandleTrackActiveStateChanged;

            // 订阅添加轨道项事件
            trackControl.OnAddTrackItem += HandleAddTrackItem;
        }

        /// <summary>
        /// 处理轨道删除事件
        /// 从数据中移除轨道并触发刷新
        /// </summary>
        /// <param name="ctrl">要删除的轨道控制器</param>
        private void HandleTrackDelete(SkillEditorTrackControl ctrl)
        {
            var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
            if (info != null)
            {
                SkillEditorData.tracks.Remove(info);
                Debug.Log($"删除轨道: {info.TrackName}，剩余轨道数量: {SkillEditorData.tracks.Count}");
                skillEditorEvent?.TriggerRefreshRequested();
            }
        }

        /// <summary>
        /// 处理轨道激活状态变化事件
        /// 更新轨道激活状态并刷新显示
        /// </summary>
        /// <param name="ctrl">轨道控制器</param>
        /// <param name="isActive">新的激活状态</param>
        private void HandleTrackActiveStateChanged(SkillEditorTrackControl ctrl, bool isActive)
        {
            var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
            if (info != null)
            {
                info.IsActive = isActive;
                info.Control.RefreshState(isActive);
                Debug.Log($"轨道[{info.TrackName}]激活状态: {(isActive ? "激活" : "失活")}");
            }
        }

        /// <summary>
        /// 处理添加轨道项事件
        /// 根据轨道类型添加相应的轨道项
        /// </summary>
        /// <param name="ctrl">轨道控制器</param>
        private void HandleAddTrackItem(SkillEditorTrackControl ctrl)
        {
            var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
            if (info != null)
            {
                if (info.TrackType == TrackType.EventTrack)
                {
                    info.Track.AddTrackItem("Event");
                }
                else if (info.TrackType == TrackType.AttackTrack)
                {
                    info.Track.AddTrackItem("Attack");
                }
            }
        }

        #endregion

        #region 轨道数据检查方法

        /// <summary>
        /// 检查动画轨道是否有数据
        /// </summary>
        private bool HasAnimationTrackData(SkillConfig skillConfig)
        {
            bool hasData = skillConfig.trackContainer.animationTrack != null &&
                   skillConfig.trackContainer.animationTrack.animationClips != null &&
                   skillConfig.trackContainer.animationTrack.animationClips.Count > 0;

            return hasData;
        }

        /// <summary>
        /// 检查音频轨道是否有数据
        /// </summary>
        private bool HasAudioTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.audioTracks != null &&
                   skillConfig.trackContainer.audioTracks.Count > 0 &&
                   skillConfig.trackContainer.audioTracks.Any(track => track.audioClips != null && track.audioClips.Count > 0);
        }

        /// <summary>
        /// 检查特效轨道是否有数据
        /// </summary>
        private bool HasEffectTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.effectTracks != null &&
                   skillConfig.trackContainer.effectTracks.Count > 0 &&
                   skillConfig.trackContainer.effectTracks.Any(track => track.effectClips != null && track.effectClips.Count > 0);
        }

        /// <summary>
        /// 检查伤害检测轨道是否有数据
        /// </summary>
        private bool HasInjuryDetectionTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.injuryDetectionTracks != null &&
                   skillConfig.trackContainer.injuryDetectionTracks.Count > 0 &&
                   skillConfig.trackContainer.injuryDetectionTracks.Any(track => track.injuryDetectionClips != null && track.injuryDetectionClips.Count > 0);
        }

        /// <summary>
        /// 检查事件轨道是否有数据
        /// </summary>
        private bool HasEventTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.eventTracks != null &&
                   skillConfig.trackContainer.eventTracks.Count > 0 &&
                   skillConfig.trackContainer.eventTracks.Any(track => track.eventClips != null && track.eventClips.Count > 0);
        }

        #endregion

        #region 轨道项创建方法

        /// <summary>
        /// 根据配置数据为指定轨道创建轨道项
        /// </summary>
        /// <param name="track">轨道实例</param>
        /// <param name="trackType">轨道类型</param>
        private void CreateTrackItemsFromConfig(SkillEditorTrack track, TrackType trackType)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null) return;

            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    CreateAnimationTrackItemsFromConfig(track, skillConfig);
                    break;
                case TrackType.AudioTrack:
                    CreateAudioTrackItemsFromConfig(track, skillConfig);
                    break;
                case TrackType.EffectTrack:
                    CreateEffectTrackItemsFromConfig(track, skillConfig);
                    break;
                case TrackType.AttackTrack:
                    CreateInjuryDetectionTrackItemsFromConfig(track, skillConfig);
                    break;
                case TrackType.EventTrack:
                    CreateEventTrackItemsFromConfig(track, skillConfig);
                    break;
            }
        }

        /// <summary>
        /// 从配置创建动画轨道项
        /// </summary>
        private void CreateAnimationTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var animationTrack = skillConfig.trackContainer.animationTrack;
            if (animationTrack?.animationClips == null)
            {
                Debug.Log("CreateAnimationTrackItemsFromConfig: 没有动画片段数据");
                return;
            }

            foreach (var clip in animationTrack.animationClips.ToList())
            {
                if (clip.clip != null)
                {
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    track.AddTrackItem(clip.clip, clip.startFrame, false);
                }
            }
        }

        /// <summary>
        /// 从配置创建音频轨道项
        /// </summary>
        private void CreateAudioTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var audioTracks = skillConfig.trackContainer.audioTracks;
            if (audioTracks == null) return;

            foreach (var audioTrack in audioTracks)
            {
                if (audioTrack.audioClips != null)
                {
                    foreach (var clip in audioTrack.audioClips)
                    {
                        if (clip.clip != null)
                        {
                            // 从配置加载时，使用配置中的名称，并设置addToConfig为false，避免重复添加到配置文件
                            var trackItem = track.AddTrackItem(clip.clip, clip.clipName, clip.startFrame, false);

                            // 从配置中恢复完整的音频属性
                            if (trackItem?.ItemData is AudioTrackItemData audioData)
                            {
                                audioData.volume = clip.volume;
                                audioData.pitch = clip.pitch;
                                audioData.isLoop = clip.isLoop;

                                // 标记数据已修改
                                UnityEditor.EditorUtility.SetDirty(audioData);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从配置创建特效轨道项
        /// </summary>
        private void CreateEffectTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var effectTracks = skillConfig.trackContainer.effectTracks;
            if (effectTracks == null) return;

            foreach (var effectTrack in effectTracks)
            {
                if (effectTrack.effectClips != null)
                {
                    foreach (var clip in effectTrack.effectClips)
                    {
                        if (clip.effectPrefab != null)
                        {
                            // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                            track.AddTrackItem(clip.effectPrefab, clip.startFrame, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从配置创建伤害检测轨道项
        /// </summary>
        private void CreateInjuryDetectionTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var injuryTracks = skillConfig.trackContainer.injuryDetectionTracks;
            if (injuryTracks == null) return;

            foreach (var injuryTrack in injuryTracks)
            {
                if (injuryTrack.injuryDetectionClips != null)
                {
                    foreach (var clip in injuryTrack.injuryDetectionClips)
                    {
                        // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                        track.AddTrackItem(clip.clipName, clip.startFrame, false);
                    }
                }
            }
        }

        /// <summary>
        /// 从配置创建事件轨道项
        /// </summary>
        private void CreateEventTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var eventTracks = skillConfig.trackContainer.eventTracks;
            if (eventTracks == null) return;

            foreach (var eventTrack in eventTracks)
            {
                if (eventTrack.eventClips != null)
                {
                    foreach (var clip in eventTrack.eventClips)
                    {
                        // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                        track.AddTrackItem(clip.clipName, clip.startFrame, false);
                    }
                }
            }
        }

        /// <summary>
        /// 根据指定索引从配置数据为轨道创建轨道项
        /// 用于多轨道支持，每个轨道使用特定索引的配置数据
        /// </summary>
        /// <param name="track">轨道实例</param>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        private void CreateTrackItemsFromConfigByIndex(SkillEditorTrack track, TrackType trackType, int trackIndex)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null) return;

            Debug.Log($"CreateTrackItemsFromConfigByIndex: 轨道类型={trackType}, 轨道索引={trackIndex}");

            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    // 动画轨道只有一条，使用索引0
                    if (trackIndex == 0)
                    {
                        CreateAnimationTrackItemsFromConfig(track, skillConfig);
                    }
                    break;
                case TrackType.AudioTrack:
                    CreateAudioTrackItemsFromConfigByIndex(track, skillConfig, trackIndex);
                    break;
                case TrackType.EffectTrack:
                    CreateEffectTrackItemsFromConfigByIndex(track, skillConfig, trackIndex);
                    break;
                case TrackType.AttackTrack:
                    CreateInjuryDetectionTrackItemsFromConfigByIndex(track, skillConfig, trackIndex);
                    break;
                case TrackType.EventTrack:
                    CreateEventTrackItemsFromConfigByIndex(track, skillConfig, trackIndex);
                    break;
            }
        }

        /// <summary>
        /// 根据索引从配置创建音频轨道项
        /// </summary>
        private void CreateAudioTrackItemsFromConfigByIndex(SkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var audioTracks = skillConfig.trackContainer.audioTracks;
            if (audioTracks == null || trackIndex >= audioTracks.Count)
            {
                Debug.Log($"CreateAudioTrackItemsFromConfigByIndex: 没有找到索引{trackIndex}的音频轨道数据");
                return;
            }

            var audioTrack = audioTracks[trackIndex];
            Debug.Log($"CreateAudioTrackItemsFromConfigByIndex: 为音频轨道索引{trackIndex}加载{audioTrack.audioClips?.Count ?? 0}个音频片段");

            if (audioTrack.audioClips != null)
            {
                foreach (var clip in audioTrack.audioClips)
                {
                    if (clip.clip != null)
                    {
                        Debug.Log($"  - 加载音频片段: {clip.clipName} (起始帧: {clip.startFrame})");
                        // 从配置加载时，使用配置中的名称，并设置addToConfig为false，避免重复添加到配置文件
                        var trackItem = track.AddTrackItem(clip.clip, clip.clipName, clip.startFrame, false);

                        // 从配置中恢复完整的音频属性
                        if (trackItem?.ItemData is AudioTrackItemData audioData)
                        {
                            audioData.volume = clip.volume;
                            audioData.pitch = clip.pitch;
                            audioData.isLoop = clip.isLoop;

                            // 标记数据已修改
                            UnityEditor.EditorUtility.SetDirty(audioData);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根据索引从配置创建特效轨道项
        /// </summary>
        private void CreateEffectTrackItemsFromConfigByIndex(SkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var effectTracks = skillConfig.trackContainer.effectTracks;
            if (effectTracks == null || trackIndex >= effectTracks.Count) return;

            var effectTrack = effectTracks[trackIndex];
            if (effectTrack.effectClips != null)
            {
                foreach (var clip in effectTrack.effectClips)
                {
                    if (clip.effectPrefab != null)
                    {
                        // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                        track.AddTrackItem(clip.effectPrefab, clip.startFrame, false);
                    }
                }
            }
        }

        /// <summary>
        /// 根据索引从配置创建伤害检测轨道项
        /// </summary>
        private void CreateInjuryDetectionTrackItemsFromConfigByIndex(SkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var injuryTracks = skillConfig.trackContainer.injuryDetectionTracks;
            if (injuryTracks == null || trackIndex >= injuryTracks.Count) return;

            var injuryTrack = injuryTracks[trackIndex];
            if (injuryTrack.injuryDetectionClips != null)
            {
                foreach (var clip in injuryTrack.injuryDetectionClips)
                {
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    track.AddTrackItem(clip.clipName, clip.startFrame, false);
                }
            }
        }

        /// <summary>
        /// 根据索引从配置创建事件轨道项
        /// </summary>
        private void CreateEventTrackItemsFromConfigByIndex(SkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var eventTracks = skillConfig.trackContainer.eventTracks;
            if (eventTracks == null || trackIndex >= eventTracks.Count) return;

            var eventTrack = eventTracks[trackIndex];
            if (eventTrack.eventClips != null)
            {
                foreach (var clip in eventTrack.eventClips)
                {
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    track.AddTrackItem(clip.clipName, clip.startFrame, false);
                }
            }
        }

        #endregion
    }
}
