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

        /// <summary>
        /// 检查技能配置是否包含变换轨道数据
        /// </summary>
        private bool HasTransformTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.transformTrack != null &&
                   skillConfig.trackContainer.transformTrack.transformClips != null &&
                   skillConfig.trackContainer.transformTrack.transformClips.Count > 0;
        }

        /// <summary>
        /// 所有轨道的容器
        /// </summary>
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

            // 检查是否已存在摄像机轨道（限制只能有一个）
            bool hasCameraTrack = SkillEditorData.tracks.Exists(t => t.TrackType == TrackType.CameraTrack);

            // 创建动画轨道菜单项
            menu.AddItem(new GUIContent("创建 Animation Track"), hasAnimationTrack, () =>
            {
                if (!hasAnimationTrack)
                    CreateTrack(TrackType.AnimationTrack);
                else
                    Debug.LogWarning("(动画轨道只可存在一条)已存在动画轨道，无法创建新的动画轨道。");
            });

            // 创建摄像机轨道菜单项
            menu.AddItem(new GUIContent("创建 Camera Track"), hasCameraTrack, () =>
            {
                if (!hasCameraTrack)
                    CreateTrack(TrackType.CameraTrack);
                else
                    Debug.LogWarning("(摄像机轨道只可存在一条)已存在摄像机轨道，无法创建新的摄像机轨道。");
            });

            // 创建其他类型轨道菜单项
            menu.AddItem(new GUIContent("创建 Audio Track"), false, () => CreateTrack(TrackType.AudioTrack));
            menu.AddItem(new GUIContent("创建 Effect Track"), false, () => CreateTrack(TrackType.EffectTrack));
            menu.AddItem(new GUIContent("创建 Attack Track"), false, () => CreateTrack(TrackType.AttackTrack));
            menu.AddItem(new GUIContent("创建 Event Track"), false, () => CreateTrack(TrackType.EventTrack));
            menu.AddItem(new GUIContent("创建 Transform Track"), false, () => CreateTrack(TrackType.TransformTrack));
            menu.AddItem(new GUIContent("创建 GameObject Track"), false, () => CreateTrack(TrackType.GameObjectTrack));

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
            string trackName = SkillEditorTrackFactory.GetDefaultTrackName(trackType, trackIndex);

            // 在配置文件中创建对应的轨道数据
            CreateTrackDataInConfig(trackType, trackIndex);

            // 使用工厂模式创建轨道控制器和轨道内容
            var trackControl = new SkillEditorTrackController(trackControlContent, trackType, trackName);
            var track = SkillEditorTrackFactory.CreateTrack(trackType, allTrackContent, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig, trackIndex);

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

            // 检查动画轨道 - 只要轨道SO存在就创建UI
            if (skillConfig.trackContainer.animationTrack != null && !HasTrackType(TrackType.AnimationTrack))
            {
                CreateTrack(TrackType.AnimationTrack);
            }

            // 检查摄像机轨道 - 只要轨道SO存在就创建UI
            if (skillConfig.trackContainer.cameraTrack != null && !HasTrackType(TrackType.CameraTrack))
            {
                CreateTrack(TrackType.CameraTrack);
            }

            // 为每个音频轨道数据创建对应的UI轨道
            CreateAudioTracksFromConfig(skillConfig);

            // 为每个特效轨道数据创建对应的UI轨道  
            CreateEffectTracksFromConfig(skillConfig);

            // 为每个伤害检测轨道数据创建对应的UI轨道
            CreateInjuryDetectionTracksFromConfig(skillConfig);

            // 为每个事件轨道数据创建对应的UI轨道
            CreateEventTracksFromConfig(skillConfig);

            // 为每个变换轨道数据创建对应的UI轨道
            CreateTransformTracksFromConfig(skillConfig);

            // 为每个游戏物体轨道数据创建对应的UI轨道
            CreateGameObjectTracksFromConfig(skillConfig);


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
            if (skillConfig?.trackContainer?.audioTrack == null) return;

            // 只要轨道SO存在就创建UI，不需要检查clips数据
            CreateTrackWithIndex(TrackType.AudioTrack, 0);
        }

        /// <summary>
        /// 为每个特效轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateEffectTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.effectTrack == null) return;

            // 只要轨道SO存在就创建UI，不需要检查clips数据
            CreateTrackWithIndex(TrackType.EffectTrack, 0);
        }

        /// <summary>
        /// 为每个伤害检测轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateInjuryDetectionTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.injuryDetectionTrack == null) return;

            // 只要轨道SO存在就创建UI，不需要检查clips数据
            CreateTrackWithIndex(TrackType.AttackTrack, 0);
        }

        /// <summary>
        /// 为每个事件轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateEventTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.eventTrack == null) return;

            // 只要轨道SO存在就创建UI，不需要检查clips数据
            CreateTrackWithIndex(TrackType.EventTrack, 0);
        }

        /// <summary>
        /// 为每个变换轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateTransformTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.transformTrack == null) return;

            // 只要轨道SO存在就创建UI，不需要检查clips数据
            CreateTrackWithIndex(TrackType.TransformTrack, 0);
        }

        /// <summary>
        /// 为每个游戏物体轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateGameObjectTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.gameObjectTrack == null) return;

            // 只要轨道SO存在就创建UI，不需要检查clips数据
            CreateTrackWithIndex(TrackType.GameObjectTrack, 0);
        }

        /// <summary>
        /// 为每个摄像机轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateCameraTracksFromConfig(SkillConfig skillConfig)
        {
            // 摄像机轨道改为单轨道模式，此方法不再需要
            // 摄像机轨道的创建逻辑已移至CreateTracksFromConfig方法中的单轨道检查
        }

        /// <summary>
        /// 创建带有索引的轨道
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        private void CreateTrackWithIndex(TrackType trackType, int trackIndex)
        {
            string trackName = SkillEditorTrackFactory.GetDefaultTrackName(trackType, trackIndex);

            // 使用工厂模式创建轨道控制器和轨道内容
            var trackControl = new SkillEditorTrackController(trackControlContent, trackType, trackName);
            var track = SkillEditorTrackFactory.CreateTrack(trackType, allTrackContent, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig, trackIndex);

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

            // 获取技能配置文件的路径，用于将轨道SO作为子资产添加
            var skillConfigPath = UnityEditor.AssetDatabase.GetAssetPath(skillConfig);
            if (string.IsNullOrEmpty(skillConfigPath))
            {
                Debug.LogError("无法获取技能配置文件路径，无法创建轨道SO文件");
                return;
            }

            switch (trackType)
            {
                case TrackType.AudioTrack:
                    // 确保音频轨道存在
                    if (skillConfig.trackContainer.audioTrack == null)
                    {
                        var newAudioTrack = ScriptableObject.CreateInstance<FFramework.Kit.AudioTrackSO>();
                        string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.AudioTrack, 0);
                        newAudioTrack.trackName = factoryTrackName;
                        newAudioTrack.audioClips = new System.Collections.Generic.List<FFramework.Kit.AudioTrack.AudioClip>();
                        newAudioTrack.name = "AudioTrack";

                        // 将轨道SO作为子资产添加到技能配置文件中
                        UnityEditor.AssetDatabase.AddObjectToAsset(newAudioTrack, skillConfig);

                        skillConfig.trackContainer.audioTrack = newAudioTrack;
                        UnityEditor.EditorUtility.SetDirty(skillConfig);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"创建音频轨道数据: {newAudioTrack.trackName} 作为子资产嵌套到 {skillConfig.name}");
                    }
                    break;

                case TrackType.EffectTrack:
                    // 确保特效轨道存在
                    if (skillConfig.trackContainer.effectTrack == null)
                    {
                        var newEffectTrack = ScriptableObject.CreateInstance<FFramework.Kit.EffectTrackSO>();
                        string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.EffectTrack, 0);
                        newEffectTrack.trackName = factoryTrackName;
                        newEffectTrack.effectClips = new System.Collections.Generic.List<FFramework.Kit.EffectTrack.EffectClip>();
                        newEffectTrack.name = "EffectTrack";

                        // 将轨道SO作为子资产添加到技能配置文件中
                        UnityEditor.AssetDatabase.AddObjectToAsset(newEffectTrack, skillConfig);

                        skillConfig.trackContainer.effectTrack = newEffectTrack;
                        UnityEditor.EditorUtility.SetDirty(skillConfig);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"创建特效轨道数据: {newEffectTrack.trackName} 作为子资产嵌套到 {skillConfig.name}");
                    }
                    break;

                case TrackType.AttackTrack:
                    // 确保伤害检测轨道存在
                    if (skillConfig.trackContainer.injuryDetectionTrack == null)
                    {
                        var newAttackTrack = ScriptableObject.CreateInstance<FFramework.Kit.InjuryDetectionTrackSO>();
                        string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.AttackTrack, 0);
                        newAttackTrack.trackName = factoryTrackName;
                        newAttackTrack.injuryDetectionClips = new System.Collections.Generic.List<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip>();
                        newAttackTrack.name = "InjuryDetectionTrack";

                        // 将轨道SO作为子资产添加到技能配置文件中
                        UnityEditor.AssetDatabase.AddObjectToAsset(newAttackTrack, skillConfig);

                        skillConfig.trackContainer.injuryDetectionTrack = newAttackTrack;
                        UnityEditor.EditorUtility.SetDirty(skillConfig);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"创建攻击轨道数据: {newAttackTrack.trackName} 作为子资产嵌套到 {skillConfig.name}");
                    }
                    break;

                case TrackType.EventTrack:
                    // 确保事件轨道存在
                    if (skillConfig.trackContainer.eventTrack == null)
                    {
                        var newEventTrack = ScriptableObject.CreateInstance<FFramework.Kit.EventTrackSO>();
                        string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.EventTrack, 0);
                        newEventTrack.trackName = factoryTrackName;
                        newEventTrack.eventClips = new System.Collections.Generic.List<FFramework.Kit.EventTrack.EventClip>();
                        newEventTrack.name = "EventTrack";

                        // 将轨道SO作为子资产添加到技能配置文件中
                        UnityEditor.AssetDatabase.AddObjectToAsset(newEventTrack, skillConfig);

                        skillConfig.trackContainer.eventTrack = newEventTrack;
                        UnityEditor.EditorUtility.SetDirty(skillConfig);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"创建事件轨道数据: {newEventTrack.trackName} 作为子资产嵌套到 {skillConfig.name}");
                    }
                    break;

                case TrackType.TransformTrack:
                    // 确保变换轨道存在
                    if (skillConfig.trackContainer.transformTrack == null)
                    {
                        var newTransformTrack = ScriptableObject.CreateInstance<FFramework.Kit.TransformTrackSO>();
                        string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.TransformTrack, 0);
                        newTransformTrack.trackName = factoryTrackName;
                        newTransformTrack.transformClips = new System.Collections.Generic.List<FFramework.Kit.TransformTrack.TransformClip>();
                        newTransformTrack.name = "TransformTrack";

                        // 将轨道SO作为子资产添加到技能配置文件中
                        UnityEditor.AssetDatabase.AddObjectToAsset(newTransformTrack, skillConfig);

                        skillConfig.trackContainer.transformTrack = newTransformTrack;
                        UnityEditor.EditorUtility.SetDirty(skillConfig);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"创建变换轨道数据: {newTransformTrack.trackName} 作为子资产嵌套到 {skillConfig.name}");
                    }
                    break;

                case TrackType.AnimationTrack:
                    // 动画轨道是单轨道
                    if (skillConfig.trackContainer.animationTrack == null)
                    {
                        var newAnimationTrack = ScriptableObject.CreateInstance<FFramework.Kit.AnimationTrackSO>();
                        string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.AnimationTrack, 0);
                        newAnimationTrack.trackName = factoryTrackName;
                        newAnimationTrack.animationClips = new System.Collections.Generic.List<FFramework.Kit.AnimationTrack.AnimationClip>();
                        newAnimationTrack.name = "AnimationTrack";

                        // 将轨道SO作为子资产添加到技能配置文件中
                        UnityEditor.AssetDatabase.AddObjectToAsset(newAnimationTrack, skillConfig);

                        skillConfig.trackContainer.animationTrack = newAnimationTrack;
                        UnityEditor.EditorUtility.SetDirty(skillConfig);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"创建动画轨道数据: {newAnimationTrack.trackName} 作为子资产嵌套到 {skillConfig.name}");
                    }
                    break;

                case TrackType.CameraTrack:
                    // 摄像机轨道是单轨道
                    if (skillConfig.trackContainer.cameraTrack == null)
                    {
                        var newCameraTrack = ScriptableObject.CreateInstance<FFramework.Kit.CameraTrackSO>();
                        string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.CameraTrack, 0);
                        newCameraTrack.trackName = factoryTrackName;
                        newCameraTrack.cameraClips = new System.Collections.Generic.List<FFramework.Kit.CameraTrack.CameraClip>();
                        newCameraTrack.name = "CameraTrack";

                        // 将轨道SO作为子资产添加到技能配置文件中
                        UnityEditor.AssetDatabase.AddObjectToAsset(newCameraTrack, skillConfig);

                        skillConfig.trackContainer.cameraTrack = newCameraTrack;
                        UnityEditor.EditorUtility.SetDirty(skillConfig);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"创建摄像机轨道数据: {newCameraTrack.trackName} 作为子资产嵌套到 {skillConfig.name}");
                    }
                    break;

                case TrackType.GameObjectTrack:
                    // 确保游戏物体轨道存在
                    if (skillConfig.trackContainer.gameObjectTrack == null)
                    {
                        var newGameObjectTrack = ScriptableObject.CreateInstance<FFramework.Kit.GameObjectTrackSO>();
                        string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.GameObjectTrack, 0);
                        newGameObjectTrack.trackName = factoryTrackName;
                        newGameObjectTrack.gameObjectClips = new System.Collections.Generic.List<FFramework.Kit.GameObjectTrack.GameObjectClip>();
                        newGameObjectTrack.name = "GameObjectTrack";

                        // 将轨道SO作为子资产添加到技能配置文件中
                        UnityEditor.AssetDatabase.AddObjectToAsset(newGameObjectTrack, skillConfig);

                        skillConfig.trackContainer.gameObjectTrack = newGameObjectTrack;
                        UnityEditor.EditorUtility.SetDirty(skillConfig);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"创建游戏物体轨道数据: {newGameObjectTrack.trackName} 作为子资产嵌套到 {skillConfig.name}");
                    }
                    break;
            }
        }

        /// <summary>
        /// 从配置文件中删除对应的轨道数据结构
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        private void RemoveTrackDataFromConfig(TrackType trackType, int trackIndex)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null) return;

            switch (trackType)
            {
                case TrackType.AudioTrack:
                    // 音频轨道是单轨道，清空数据即可
                    if (skillConfig.trackContainer.audioTrack != null)
                    {
                        skillConfig.trackContainer.audioTrack = null;
                        Debug.Log("从配置中删除音频轨道数据");
                    }
                    break;

                case TrackType.EffectTrack:
                    // 特效轨道是单轨道，清空数据即可
                    if (skillConfig.trackContainer.effectTrack != null)
                    {
                        skillConfig.trackContainer.effectTrack = null;
                        Debug.Log("从配置中删除特效轨道数据");
                    }
                    break;

                case TrackType.AttackTrack:
                    // 伤害检测轨道是单轨道，清空数据即可
                    if (skillConfig.trackContainer.injuryDetectionTrack != null)
                    {
                        skillConfig.trackContainer.injuryDetectionTrack = null;
                        Debug.Log("从配置中删除攻击轨道数据");
                    }
                    break;

                case TrackType.EventTrack:
                    // 事件轨道是单轨道，清空数据即可
                    if (skillConfig.trackContainer.eventTrack != null)
                    {
                        skillConfig.trackContainer.eventTrack = null;
                        Debug.Log("从配置中删除事件轨道数据");
                    }
                    break;

                case TrackType.TransformTrack:
                    // 变换轨道是单轨道，清空数据即可
                    if (skillConfig.trackContainer.transformTrack != null)
                    {
                        skillConfig.trackContainer.transformTrack = null;
                        Debug.Log("从配置中删除变换轨道数据");
                    }
                    break;

                case TrackType.CameraTrack:
                    // 摄像机轨道是单轨道，清空数据即可
                    if (skillConfig.trackContainer.cameraTrack != null)
                    {
                        skillConfig.trackContainer.cameraTrack = null;
                        Debug.Log("从配置中删除摄像机轨道数据");
                    }
                    break;

                case TrackType.AnimationTrack:
                    // 动画轨道是单轨道，清空数据即可
                    if (skillConfig.trackContainer.animationTrack != null)
                    {
                        skillConfig.trackContainer.animationTrack = null;
                        Debug.Log("从配置中删除动画轨道数据");
                    }
                    break;

                case TrackType.GameObjectTrack:
                    // 游戏物体轨道是单轨道，清空数据即可
                    if (skillConfig.trackContainer.gameObjectTrack != null)
                    {
                        skillConfig.trackContainer.gameObjectTrack = null;
                        Debug.Log("从配置中删除游戏物体轨道数据");
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

            // 清空轨道数据
            if (SkillEditorData.tracks != null)
            {
                SkillEditorData.tracks.Clear();
            }

            // 根据配置重新创建所有轨道
            CreateTracksFromConfig();
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
        private void SubscribeTrackEvents(SkillEditorTrackController trackControl)
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
        private void HandleTrackDelete(SkillEditorTrackController ctrl)
        {
            var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
            if (info != null)
            {
                // 从配置数据中删除对应的轨道数据
                RemoveTrackDataFromConfig(info.TrackType, info.TrackIndex);

                // 从UI数据中移除轨道
                SkillEditorData.tracks.Remove(info);
                Debug.Log($"删除轨道: {info.TrackName}，剩余轨道数量: {SkillEditorData.tracks.Count}");

                // 触发刷新以重建UI和重新索引
                skillEditorEvent?.TriggerRefreshRequested();
            }
        }

        /// <summary>
        /// 处理轨道激活状态变化事件
        /// 更新轨道激活状态并刷新显示
        /// </summary>
        /// <param name="ctrl">轨道控制器</param>
        /// <param name="isActive">新的激活状态</param>
        private void HandleTrackActiveStateChanged(SkillEditorTrackController ctrl, bool isActive)
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
        private void HandleAddTrackItem(SkillEditorTrackController ctrl)
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
                else if (info.TrackType == TrackType.TransformTrack)
                {
                    info.Track.AddTrackItem("Transform");
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
        /// 检查摄像机轨道是否有数据
        /// </summary>
        private bool HasCameraTrackData(SkillConfig skillConfig)
        {
            bool hasData = skillConfig.trackContainer.cameraTrack != null &&
                   skillConfig.trackContainer.cameraTrack.cameraClips != null &&
                   skillConfig.trackContainer.cameraTrack.cameraClips.Count > 0;

            return hasData;
        }

        /// <summary>
        /// 检查音频轨道是否有数据
        /// </summary>
        private bool HasAudioTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.audioTrack != null &&
                   skillConfig.trackContainer.audioTrack.audioClips != null &&
                   skillConfig.trackContainer.audioTrack.audioClips.Count > 0;
        }

        /// <summary>
        /// 检查特效轨道是否有数据
        /// </summary>
        private bool HasEffectTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.effectTrack != null &&
                   skillConfig.trackContainer.effectTrack.effectClips != null &&
                   skillConfig.trackContainer.effectTrack.effectClips.Count > 0;
        }

        /// <summary>
        /// 检查伤害检测轨道是否有数据
        /// </summary>
        private bool HasInjuryDetectionTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.injuryDetectionTrack != null &&
                   skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionClips != null &&
                   skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionClips.Count > 0;
        }

        /// <summary>
        /// 检查事件轨道是否有数据
        /// </summary>
        private bool HasEventTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.eventTrack != null &&
                   skillConfig.trackContainer.eventTrack.eventClips != null &&
                   skillConfig.trackContainer.eventTrack.eventClips.Count > 0;
        }

        /// <summary>
        /// 检查游戏物体轨道是否有数据
        /// </summary>
        private bool HasGameObjectTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.gameObjectTrack != null &&
                   skillConfig.trackContainer.gameObjectTrack.gameObjectClips != null &&
                   skillConfig.trackContainer.gameObjectTrack.gameObjectClips.Count > 0;
        }


        #endregion

        #region 轨道项创建方法

        /// <summary>
        /// 从配置创建动画轨道项
        /// </summary>
        private void CreateAnimationTrackItemsFromConfig(BaseSkillEditorTrack track, SkillConfig skillConfig)
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
        private void CreateAudioTrackItemsFromConfig(BaseSkillEditorTrack track, SkillConfig skillConfig)
        {
            var audioTrack = skillConfig.trackContainer.audioTrack;
            if (audioTrack?.audioClips == null)
            {
                Debug.Log("CreateAudioTrackItemsFromConfig: 没有音频片段数据");
                return;
            }

            foreach (var clip in audioTrack.audioClips.ToList())
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

        /// <summary>
        /// 从配置创建特效轨道项
        /// </summary>
        private void CreateEffectTrackItemsFromConfig(BaseSkillEditorTrack track, SkillConfig skillConfig)
        {
            var effectTrack = skillConfig.trackContainer.effectTrack;
            if (effectTrack?.effectClips == null) return;

            foreach (var clip in effectTrack.effectClips)
            {
                if (clip.effectPrefab != null)
                {
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    track.AddTrackItem(clip.effectPrefab, clip.startFrame, false);
                }
            }
        }

        /// <summary>
        /// 从配置创建伤害检测轨道项
        /// </summary>
        private void CreateInjuryDetectionTrackItemsFromConfig(BaseSkillEditorTrack track, SkillConfig skillConfig)
        {
            var injuryTrack = skillConfig.trackContainer.injuryDetectionTrack;
            if (injuryTrack?.injuryDetectionClips == null) return;

            foreach (var clip in injuryTrack.injuryDetectionClips)
            {
                // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                track.AddTrackItem(clip.clipName, clip.startFrame, false);
            }
        }

        /// <summary>
        /// 从配置创建事件轨道项
        /// </summary>
        private void CreateEventTrackItemsFromConfig(BaseSkillEditorTrack track, SkillConfig skillConfig)
        {
            var eventTrack = skillConfig.trackContainer.eventTrack;
            if (eventTrack?.eventClips == null) return;

            foreach (var clip in eventTrack.eventClips)
            {
                // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                track.AddTrackItem(clip.clipName, clip.startFrame, false);
            }
        }

        /// <summary>
        /// 根据指定索引从配置数据为轨道创建轨道项
        /// 用于多轨道支持，每个轨道使用特定索引的配置数据
        /// </summary>
        /// <param name="track">轨道实例</param>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        private void CreateTrackItemsFromConfigByIndex(BaseSkillEditorTrack track, TrackType trackType, int trackIndex)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null) return;

            // Debug.Log($"CreateTrackItemsFromConfigByIndex: 轨道类型={trackType}, 轨道索引={trackIndex}");

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
                case TrackType.TransformTrack:
                    CreateTransformTrackItemsFromConfigByIndex(track, skillConfig, trackIndex);
                    break;
                case TrackType.CameraTrack:
                    CreateCameraTrackItemsFromConfigByIndex(track, skillConfig, trackIndex);
                    break;
                case TrackType.GameObjectTrack:
                    CreateGameObjectTrackItemsFromConfigByIndex(track, skillConfig, trackIndex);
                    break;
            }
        }

        /// <summary>
        /// 根据索引从配置创建音频轨道项
        /// </summary>
        private void CreateAudioTrackItemsFromConfigByIndex(BaseSkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var audioTrack = skillConfig.trackContainer.audioTrack;
            if (audioTrack?.audioClips == null)
            {
                Debug.Log($"CreateAudioTrackItemsFromConfigByIndex: 没有找到音频轨道数据");
                return;
            }

            // Debug.Log($"CreateAudioTrackItemsFromConfigByIndex: 为音频轨道加载{audioTrack.audioClips?.Count ?? 0}个音频片段");

            foreach (var clip in audioTrack.audioClips)
            {
                if (clip.clip != null)
                {
                    // Debug.Log($"  - 加载音频片段: {clip.clipName} (起始帧: {clip.startFrame})");
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

        /// <summary>
        /// 根据索引从配置创建特效轨道项
        /// </summary>
        private void CreateEffectTrackItemsFromConfigByIndex(BaseSkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var effectTrack = skillConfig.trackContainer.effectTrack;
            if (effectTrack?.effectClips == null) return;

            foreach (var clip in effectTrack.effectClips)
            {
                if (clip.effectPrefab != null)
                {
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    track.AddTrackItem(clip.effectPrefab, clip.startFrame, false);
                }
            }
        }

        /// <summary>
        /// 根据索引从配置创建伤害检测轨道项
        /// </summary>
        private void CreateInjuryDetectionTrackItemsFromConfigByIndex(BaseSkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var injuryTrack = skillConfig.trackContainer.injuryDetectionTrack;
            if (injuryTrack?.injuryDetectionClips == null) return;

            foreach (var clip in injuryTrack.injuryDetectionClips)
            {
                // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                // 同时传递持续帧数以正确设置轨道项宽度
                var trackItem = track.AddTrackItem(clip.clipName, clip.startFrame, false);

                // 更新轨道项的持续帧数和相关数据
                if (trackItem?.ItemData is AttackTrackItemData attackData)
                {
                    attackData.durationFrame = clip.durationFrame;
                    // 从配置中恢复完整的攻击属性
                    attackData.targetLayers = clip.targetLayers;
                    attackData.isMultiInjuryDetection = clip.isMultiInjuryDetection;
                    attackData.multiInjuryDetectionInterval = clip.multiInjuryDetectionInterval;
                    attackData.colliderType = clip.colliderType;
                    attackData.innerCircleRadius = clip.innerCircleRadius;
                    attackData.outerCircleRadius = clip.outerCircleRadius;
                    attackData.sectorAngle = clip.sectorAngle;
                    attackData.sectorThickness = clip.sectorThickness;
                    attackData.position = clip.position;
                    attackData.rotation = clip.rotation;
                    attackData.scale = clip.scale;

                    // 标记数据已修改
                    UnityEditor.EditorUtility.SetDirty(attackData);
                }

                // 更新轨道项的帧数和宽度显示
                trackItem?.UpdateFrameCount(clip.durationFrame);
            }
        }

        /// <summary>
        /// 根据索引从配置创建事件轨道项
        /// </summary>
        private void CreateEventTrackItemsFromConfigByIndex(BaseSkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var eventTrack = skillConfig.trackContainer.eventTrack;
            if (eventTrack?.eventClips == null) return;

            foreach (var clip in eventTrack.eventClips)
            {
                // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                var trackItem = track.AddTrackItem(clip.clipName, clip.startFrame, false);

                // 更新轨道项的持续帧数和相关数据
                if (trackItem?.ItemData is EventTrackItemData eventData)
                {
                    eventData.durationFrame = clip.durationFrame;
                    // 从配置中恢复完整的事件属性
                    eventData.eventType = clip.eventType;
                    eventData.eventParameters = clip.eventParameters;

                    // 标记数据已修改
                    UnityEditor.EditorUtility.SetDirty(eventData);
                }

                // 更新轨道项的帧数和宽度显示
                trackItem?.UpdateFrameCount(clip.durationFrame);
            }
        }

        /// <summary>
        /// 根据索引从配置创建变换轨道项
        /// </summary>
        private void CreateTransformTrackItemsFromConfigByIndex(BaseSkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var transformTrack = skillConfig.trackContainer.transformTrack;
            if (transformTrack == null)
            {
                Debug.Log($"CreateTransformTrackItemsFromConfigByIndex: 没有找到变换轨道数据");
                return;
            }

            // Debug.Log($"CreateTransformTrackItemsFromConfigByIndex: 为变换轨道加载{transformTrack.transformClips?.Count ?? 0}个变换片段");

            if (transformTrack.transformClips != null)
            {
                foreach (var clip in transformTrack.transformClips)
                {
                    // Debug.Log($"  - 加载变换片段: {clip.clipName} (起始帧: {clip.startFrame})");
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    var trackItem = track.AddTrackItem(clip.clipName, clip.startFrame, false);

                    // 更新轨道项的持续帧数和相关数据
                    if (trackItem?.ItemData is TransformTrackItemData transformData)
                    {
                        transformData.durationFrame = clip.durationFrame;
                        // 从配置中恢复完整的变换属性
                        transformData.enablePosition = clip.enablePosition;
                        transformData.enableRotation = clip.enableRotation;
                        transformData.enableScale = clip.enableScale;
                        transformData.startPosition = clip.startPosition;
                        transformData.startRotation = clip.startRotation;
                        transformData.startScale = clip.startScale;
                        transformData.endPosition = clip.endPosition;
                        transformData.endRotation = clip.endRotation;
                        transformData.endScale = clip.endScale;
                        transformData.curveType = clip.curveType;
                        transformData.customCurve = clip.customCurve;
                        transformData.isRelative = clip.isRelative;

                        // 标记数据已修改
                        UnityEditor.EditorUtility.SetDirty(transformData);
                    }

                    // 更新轨道项的帧数和宽度显示
                    trackItem?.UpdateFrameCount(clip.durationFrame);
                }
            }
        }

        /// <summary>
        /// 根据索引从配置创建摄像机轨道项
        /// </summary>
        private void CreateCameraTrackItemsFromConfigByIndex(BaseSkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var cameraTrack = skillConfig.trackContainer.cameraTrack;
            if (cameraTrack == null)
            {
                Debug.Log($"CreateCameraTrackItemsFromConfigByIndex: 没有找到摄像机轨道数据");
                return;
            }

            // 摄像机轨道是单轨道，只有trackIndex为0时才处理
            if (trackIndex != 0)
            {
                Debug.Log($"CreateCameraTrackItemsFromConfigByIndex: 摄像机轨道是单轨道，只处理索引0，当前索引为{trackIndex}");
                return;
            }

            if (cameraTrack.cameraClips != null)
            {
                foreach (var clip in cameraTrack.cameraClips)
                {
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    var trackItem = track.AddTrackItem(clip.clipName, clip.startFrame, false);

                    // 更新轨道项的持续帧数和相关数据
                    if (trackItem?.ItemData is CameraTrackItemData cameraData)
                    {
                        cameraData.durationFrame = clip.durationFrame;
                        // 从配置中恢复完整的摄像机属性
                        cameraData.enablePosition = clip.enablePosition;
                        cameraData.enableRotation = clip.enableRotation;
                        cameraData.enableFieldOfView = clip.enableFieldOfView;
                        cameraData.startPosition = clip.startPosition;
                        cameraData.startRotation = clip.startRotation;
                        cameraData.startFieldOfView = clip.startFieldOfView;
                        cameraData.endPosition = clip.endPosition;
                        cameraData.endRotation = clip.endRotation;
                        cameraData.endFieldOfView = clip.endFieldOfView;
                        cameraData.curveType = clip.curveType;
                        cameraData.customCurve = clip.customCurve;
                        cameraData.isRelative = clip.isRelative;

                        // 标记数据已修改
                        UnityEditor.EditorUtility.SetDirty(cameraData);
                    }

                    // 更新轨道项的帧数和宽度显示
                    trackItem?.UpdateFrameCount(clip.durationFrame);
                }
            }
        }

        /// <summary>
        /// 根据索引从配置创建游戏物体轨道项
        /// </summary>
        private void CreateGameObjectTrackItemsFromConfigByIndex(BaseSkillEditorTrack track, SkillConfig skillConfig, int trackIndex)
        {
            var gameObjectTrack = skillConfig.trackContainer.gameObjectTrack;
            if (gameObjectTrack == null)
            {
                Debug.Log($"CreateGameObjectTrackItemsFromConfigByIndex: 没有找到游戏物体轨道数据");
                return;
            }

            // Debug.Log($"CreateGameObjectTrackItemsFromConfigByIndex: 为游戏物体轨道加载{gameObjectTrack.gameObjectClips?.Count ?? 0}个游戏物体片段");

            if (gameObjectTrack.gameObjectClips != null)
            {
                foreach (var clip in gameObjectTrack.gameObjectClips)
                {
                    if (clip.prefab != null)
                    {
                        // Debug.Log($"  - 加载游戏物体片段: {clip.clipName} (起始帧: {clip.startFrame})");
                        // 从配置加载时，使用配置中的名称，并设置addToConfig为false，避免重复添加到配置文件
                        var trackItem = track.AddTrackItem(clip.prefab, clip.clipName, clip.startFrame, false);

                        // 从配置中恢复完整的游戏物体属性
                        if (trackItem?.ItemData is GameObjectTrackItemData gameObjectData)
                        {
                            gameObjectData.autoDestroy = clip.autoDestroy;
                            gameObjectData.positionOffset = clip.positionOffset;
                            gameObjectData.rotationOffset = clip.rotationOffset;
                            gameObjectData.scale = clip.scale;
                            gameObjectData.useParent = clip.useParent;
                            gameObjectData.parentName = clip.parentName;
                            gameObjectData.destroyDelay = clip.destroyDelay;

                            // 标记数据已修改
                            UnityEditor.EditorUtility.SetDirty(gameObjectData);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
