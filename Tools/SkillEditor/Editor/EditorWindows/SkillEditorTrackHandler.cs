using UnityEngine.UIElements;
using FFramework.Kit;
using UnityEngine;
using UnityEditor;
using System.Linq;

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

        /// <summary>轨道控制区域内容容器</summary>
        private VisualElement trackControlContent;

        /// <summary>所有轨道的容器</summary>
        private VisualElement allTrackContent;

        #endregion

        #region 初始化和公共接口

        /// <summary>
        /// 轨道管理器构造函数
        /// </summary>
        public SkillEditorTrackHandler()
        {
        }

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

        /// <summary>
        /// 根据技能配置创建轨道
        /// 公共接口，供外部调用
        /// </summary>
        public void CreateTracksFromConfigPublic()
        {
            CreateTracksFromConfig();
        }

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

        #region 轨道创建和管理

        /// <summary>
        /// 显示轨道创建菜单
        /// 根据轨道类型限制显示可创建的轨道类型选项
        /// </summary>
        /// <param name="button">触发菜单的按钮元素</param>
        public void ShowTrackCreationMenu(VisualElement button)
        {
            var menu = new GenericMenu();

            // 检查单轨道类型是否已存在
            bool hasAnimationTrack = SkillEditorData.tracks.Exists(t => t.TrackType == TrackType.AnimationTrack);
            bool hasCameraTrack = SkillEditorData.tracks.Exists(t => t.TrackType == TrackType.CameraTrack);
            bool hasTransformTrack = SkillEditorData.tracks.Exists(t => t.TrackType == TrackType.TransformTrack);

            // 添加单轨道类型菜单项
            AddSingleTrackMenuItem(menu, "创建 Animation Track", TrackType.AnimationTrack, hasAnimationTrack);
            AddSingleTrackMenuItem(menu, "创建 Camera Track", TrackType.CameraTrack, hasCameraTrack);
            AddSingleTrackMenuItem(menu, "创建 Transform Track", TrackType.TransformTrack, hasTransformTrack);

            // 添加多轨道类型菜单项
            AddMultiTrackMenuItem(menu, "创建 Audio Track", TrackType.AudioTrack);
            AddMultiTrackMenuItem(menu, "创建 Effect Track", TrackType.EffectTrack);
            AddMultiTrackMenuItem(menu, "创建 Attack Track", TrackType.AttackTrack);
            AddMultiTrackMenuItem(menu, "创建 Event Track", TrackType.EventTrack);
            AddMultiTrackMenuItem(menu, "创建 GameObject Track", TrackType.GameObjectTrack);

            // 在按钮下方显示菜单
            var rect = button.worldBound;
            menu.DropDown(new Rect(rect.x, rect.yMax, 0, 0));
        }

        /// <summary>
        /// 添加单轨道类型菜单项
        /// </summary>
        private void AddSingleTrackMenuItem(GenericMenu menu, string menuText, TrackType trackType, bool exists)
        {
            menu.AddItem(new GUIContent(menuText), exists, () =>
            {
                if (!exists)
                    CreateTrack(trackType);
                else
                    Debug.LogWarning($"({GetTrackTypeName(trackType)}轨道只可存在一条)已存在{GetTrackTypeName(trackType)}轨道，无法创建新的轨道。");
            });
        }

        /// <summary>
        /// 添加多轨道类型菜单项
        /// </summary>
        private void AddMultiTrackMenuItem(GenericMenu menu, string menuText, TrackType trackType)
        {
            menu.AddItem(new GUIContent(menuText), false, () => CreateTrack(trackType));
        }

        /// <summary>
        /// 获取轨道类型的中文名称
        /// </summary>
        private string GetTrackTypeName(TrackType trackType)
        {
            return trackType switch
            {
                TrackType.AnimationTrack => "动画",
                TrackType.CameraTrack => "摄像机",
                TrackType.TransformTrack => "变换",
                TrackType.AudioTrack => "音频",
                TrackType.EffectTrack => "特效",
                TrackType.AttackTrack => "攻击",
                TrackType.EventTrack => "事件",
                TrackType.GameObjectTrack => "游戏物体",
                _ => "未知"
            };
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

            // 创建轨道UI和数据
            CreateTrackUI(trackType, trackName, trackIndex);
        }

        /// <summary>
        /// 创建轨道UI和数据结构
        /// </summary>
        private void CreateTrackUI(TrackType trackType, string trackName, int trackIndex)
        {
            // 使用工厂模式创建轨道控制器和轨道内容
            var trackControl = new SkillEditorTrackController(trackControlContent, trackType, trackName);
            var track = SkillEditorTrackFactory.CreateTrack(trackType, allTrackContent, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig, trackIndex);

            // 创建轨道信息并添加到数据中
            var trackInfo = new SkillEditorTrackInfo(trackControl, track, trackType, trackName)
            {
                TrackIndex = trackIndex,
                IsActive = GetTrackActiveStateFromConfig(trackType, trackIndex)
            };

            SkillEditorData.tracks.Add(trackInfo);

            // 订阅轨道事件
            SubscribeTrackEvents(trackControl);

            // 应用激活状态到UI显示
            trackControl.RefreshState(trackInfo.IsActive);

            // 创建轨道项
            CreateTrackItemsFromConfigByIndex(track, trackType, trackIndex);
        }

        /// <summary>
        /// 根据技能配置自动创建所有包含数据的轨道
        /// </summary>
        public void CreateTracksFromConfig()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null) return;

            // 创建单轨道类型
            CreateSingleTracksFromConfig(skillConfig);

            // 创建多轨道类型
            CreateMultiTracksFromConfig(skillConfig);
        }

        /// <summary>
        /// 创建单轨道类型
        /// </summary>
        private void CreateSingleTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig.trackContainer.animationTrack != null && !HasTrackType(TrackType.AnimationTrack))
            {
                CreateTrack(TrackType.AnimationTrack);
            }

            if (skillConfig.trackContainer.cameraTrack != null && !HasTrackType(TrackType.CameraTrack))
            {
                CreateTrack(TrackType.CameraTrack);
            }

            if (skillConfig.trackContainer.transformTrack != null && !HasTrackType(TrackType.TransformTrack))
            {
                CreateTrack(TrackType.TransformTrack);
            }
        }

        /// <summary>
        /// 创建多轨道类型
        /// </summary>
        private void CreateMultiTracksFromConfig(SkillConfig skillConfig)
        {
            CreateAudioTracksFromConfig(skillConfig);
            CreateEffectTracksFromConfig(skillConfig);
            CreateInjuryDetectionTracksFromConfig(skillConfig);
            CreateEventTracksFromConfig(skillConfig);
            CreateGameObjectTracksFromConfig(skillConfig);
        }

        /// <summary>
        /// 检查是否已存在指定类型的轨道
        /// </summary>
        private bool HasTrackType(TrackType trackType)
        {
            return SkillEditorData.tracks.Any(t => t.TrackType == trackType);
        }

        /// <summary>
        /// 创建带有索引的轨道
        /// </summary>
        private void CreateTrackWithIndex(TrackType trackType, int trackIndex)
        {
            string trackName = SkillEditorTrackFactory.GetDefaultTrackName(trackType, trackIndex);
            CreateTrackUI(trackType, trackName, trackIndex);
        }

        #endregion

        #region 多轨道类型创建方法

        /// <summary>
        /// 为每个音频轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateAudioTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.audioTrack == null) return;

            var audioTracks = skillConfig.trackContainer.audioTrack.audioTracks;
            if (audioTracks != null && audioTracks.Count > 0)
            {
                for (int i = 0; i < audioTracks.Count; i++)
                {
                    CreateTrackWithIndex(TrackType.AudioTrack, i);
                }
            }
            else
            {
                CreateTrackWithIndex(TrackType.AudioTrack, 0);
            }
        }

        /// <summary>
        /// 为每个特效轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateEffectTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.effectTrack == null) return;

            var effectTracks = skillConfig.trackContainer.effectTrack.effectTracks;
            if (effectTracks != null && effectTracks.Count > 0)
            {
                for (int i = 0; i < effectTracks.Count; i++)
                {
                    CreateTrackWithIndex(TrackType.EffectTrack, i);
                }
            }
            else
            {
                CreateTrackWithIndex(TrackType.EffectTrack, 0);
            }
        }

        /// <summary>
        /// 为每个伤害检测轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateInjuryDetectionTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.injuryDetectionTrack == null) return;

            var injuryTracks = skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks;
            if (injuryTracks != null && injuryTracks.Count > 0)
            {
                for (int i = 0; i < injuryTracks.Count; i++)
                {
                    CreateTrackWithIndex(TrackType.AttackTrack, injuryTracks[i].trackIndex);
                }
            }
            else
            {
                CreateTrackWithIndex(TrackType.AttackTrack, 0);
            }
        }

        /// <summary>
        /// 为每个事件轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateEventTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.eventTrack == null) return;

            var eventTracks = skillConfig.trackContainer.eventTrack.eventTracks;
            if (eventTracks != null && eventTracks.Count > 0)
            {
                for (int i = 0; i < eventTracks.Count; i++)
                {
                    CreateTrackWithIndex(TrackType.EventTrack, eventTracks[i].trackIndex);
                }
            }
            else
            {
                CreateTrackWithIndex(TrackType.EventTrack, 0);
            }
        }

        /// <summary>
        /// 为每个游戏物体轨道数据创建对应的UI轨道
        /// </summary>
        private void CreateGameObjectTracksFromConfig(SkillConfig skillConfig)
        {
            if (skillConfig?.trackContainer?.gameObjectTrack == null) return;

            var gameObjectTracks = skillConfig.trackContainer.gameObjectTrack.gameObjectTracks;
            if (gameObjectTracks != null && gameObjectTracks.Count > 0)
            {
                for (int i = 0; i < gameObjectTracks.Count; i++)
                {
                    CreateTrackWithIndex(TrackType.GameObjectTrack, gameObjectTracks[i].trackIndex);
                }
            }
            else
            {
                CreateTrackWithIndex(TrackType.GameObjectTrack, 0);
            }
        }

        #endregion

        #region 轨道配置数据管理

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
                case TrackType.AnimationTrack:
                    CreateSingleTrackData<FFramework.Kit.AnimationTrackSO>(
                        () => skillConfig.trackContainer.animationTrack,
                        (so) => skillConfig.trackContainer.animationTrack = so,
                        "AnimationTrack",
                        "动画",
                        (track) =>
                        {
                            track.animationClips = new System.Collections.Generic.List<FFramework.Kit.AnimationTrack.AnimationClip>();
                        }
                    );
                    break;

                case TrackType.TransformTrack:
                    CreateSingleTrackData<FFramework.Kit.TransformTrackSO>(
                        () => skillConfig.trackContainer.transformTrack,
                        (so) => skillConfig.trackContainer.transformTrack = so,
                        "TransformTrack",
                        "变换",
                        (track) =>
                        {
                            track.transformClips = new System.Collections.Generic.List<FFramework.Kit.TransformTrack.TransformClip>();
                        }
                    );
                    break;

                case TrackType.CameraTrack:
                    CreateSingleTrackData<FFramework.Kit.CameraTrackSO>(
                        () => skillConfig.trackContainer.cameraTrack,
                        (so) => skillConfig.trackContainer.cameraTrack = so,
                        "CameraTrack",
                        "摄像机",
                        (track) =>
                        {
                            track.cameraClips = new System.Collections.Generic.List<FFramework.Kit.CameraTrack.CameraClip>();
                        }
                    );
                    break;

                case TrackType.AudioTrack:
                    CreateMultiTrackData<FFramework.Kit.AudioTrackSO, FFramework.Kit.AudioTrack>(
                        () => skillConfig.trackContainer.audioTrack,
                        (so) => skillConfig.trackContainer.audioTrack = so,
                        "AudioTracks",
                        "音频轨道集合",
                        trackIndex,
                        (trackSO) =>
                        {
                            trackSO.audioTracks = new System.Collections.Generic.List<FFramework.Kit.AudioTrack>();
                        },
                        (trackData, name, index) =>
                        {
                            trackData.trackName = name;
                            trackData.isEnabled = true;
                            trackData.trackIndex = index;
                            trackData.audioClips = new System.Collections.Generic.List<FFramework.Kit.AudioTrack.AudioClip>();
                        },
                        (trackSO, trackData) => trackSO.audioTracks.Add(trackData)
                    );
                    break;

                case TrackType.EffectTrack:
                    CreateMultiTrackData<FFramework.Kit.EffectTrackSO, FFramework.Kit.EffectTrack>(
                        () => skillConfig.trackContainer.effectTrack,
                        (so) => skillConfig.trackContainer.effectTrack = so,
                        "EffectTracks",
                        "特效轨道集合",
                        trackIndex,
                        (trackSO) =>
                        {
                            trackSO.effectTracks = new System.Collections.Generic.List<FFramework.Kit.EffectTrack>();
                        },
                        (trackData, name, index) =>
                        {
                            trackData.trackName = name;
                            trackData.isEnabled = true;
                            trackData.trackIndex = index;
                            trackData.effectClips = new System.Collections.Generic.List<FFramework.Kit.EffectTrack.EffectClip>();
                        },
                        (trackSO, trackData) => trackSO.effectTracks.Add(trackData)
                    );
                    break;

                case TrackType.AttackTrack:
                    CreateMultiTrackData<FFramework.Kit.InjuryDetectionTrackSO, FFramework.Kit.InjuryDetectionTrack>(
                        () => skillConfig.trackContainer.injuryDetectionTrack,
                        (so) => skillConfig.trackContainer.injuryDetectionTrack = so,
                        "InjuryDetectionTracks",
                        "伤害检测轨道集合",
                        trackIndex,
                        (trackSO) =>
                        {
                            trackSO.injuryDetectionTracks = new System.Collections.Generic.List<FFramework.Kit.InjuryDetectionTrack>();
                        },
                        (trackData, name, index) =>
                        {
                            trackData.trackName = name;
                            trackData.isEnabled = true;
                            trackData.trackIndex = index;
                            trackData.injuryDetectionClips = new System.Collections.Generic.List<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip>();
                        },
                        (trackSO, trackData) => trackSO.injuryDetectionTracks.Add(trackData)
                    );
                    break;

                case TrackType.EventTrack:
                    CreateMultiTrackData<FFramework.Kit.EventTrackSO, FFramework.Kit.EventTrack>(
                        () => skillConfig.trackContainer.eventTrack,
                        (so) => skillConfig.trackContainer.eventTrack = so,
                        "EventTracks",
                        "事件轨道集合",
                        trackIndex,
                        (trackSO) =>
                        {
                            trackSO.eventTracks = new System.Collections.Generic.List<FFramework.Kit.EventTrack>();
                        },
                        (trackData, name, index) =>
                        {
                            trackData.trackName = name;
                            trackData.isEnabled = true;
                            trackData.trackIndex = index;
                            trackData.eventClips = new System.Collections.Generic.List<FFramework.Kit.EventTrack.EventClip>();
                        },
                        (trackSO, trackData) => trackSO.eventTracks.Add(trackData)
                    );
                    break;

                case TrackType.GameObjectTrack:
                    CreateMultiTrackData<FFramework.Kit.GameObjectTrackSO, FFramework.Kit.GameObjectTrack>(
                        () => skillConfig.trackContainer.gameObjectTrack,
                        (so) => skillConfig.trackContainer.gameObjectTrack = so,
                        "GameObjectTracks",
                        "游戏物体轨道集合",
                        trackIndex,
                        (trackSO) =>
                        {
                            trackSO.gameObjectTracks = new System.Collections.Generic.List<FFramework.Kit.GameObjectTrack>();
                        },
                        (trackData, name, index) =>
                        {
                            trackData.trackName = name;
                            trackData.isEnabled = true;
                            trackData.trackIndex = index;
                            trackData.gameObjectClips = new System.Collections.Generic.List<FFramework.Kit.GameObjectTrack.GameObjectClip>();
                        },
                        (trackSO, trackData) => trackSO.gameObjectTracks.Add(trackData)
                    );
                    break;
            }
        }

        /// <summary>
        /// 创建单轨道类型的轨道数据
        /// </summary>
        /// <typeparam name="T">轨道SO类型</typeparam>
        /// <param name="getTrackSO">获取轨道SO的函数</param>
        /// <param name="setTrackSO">设置轨道SO的函数</param>
        /// <param name="soName">SO资产名称</param>
        /// <param name="trackTypeName">轨道类型名称（用于日志）</param>
        /// <param name="initializeTrack">初始化轨道特定属性的函数</param>
        private void CreateSingleTrackData<T>(
            System.Func<T> getTrackSO,
            System.Action<T> setTrackSO,
            string soName,
            string trackTypeName,
            System.Action<T> initializeTrack
        ) where T : UnityEngine.ScriptableObject
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (getTrackSO() == null)
            {
                var newTrack = UnityEngine.ScriptableObject.CreateInstance<T>();
                string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(
                    System.Enum.Parse<TrackType>(typeof(T).Name.Replace("TrackSO", "Track").Replace("InjuryDetection", "Attack").Replace("Animation", "Animation")), 0);

                // 设置通用属性
                var trackNameProperty = typeof(T).GetField("trackName");
                if (trackNameProperty != null)
                {
                    trackNameProperty.SetValue(newTrack, factoryTrackName);
                }

                newTrack.name = soName;

                // 初始化轨道特定属性
                initializeTrack(newTrack);

                // 将轨道SO作为子资产添加到技能配置文件中
                UnityEditor.AssetDatabase.AddObjectToAsset(newTrack, skillConfig);

                setTrackSO(newTrack);
                UnityEditor.EditorUtility.SetDirty(skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"创建{trackTypeName}轨道数据: {factoryTrackName} 作为子资产嵌套到 {skillConfig.name}");
            }
        }

        /// <summary>
        /// 创建多轨道类型的轨道数据
        /// </summary>
        /// <typeparam name="TSO">轨道SO类型</typeparam>
        /// <typeparam name="TTrack">单个轨道数据类型</typeparam>
        /// <param name="getTrackSO">获取轨道SO的函数</param>
        /// <param name="setTrackSO">设置轨道SO的函数</param>
        /// <param name="soName">SO资产名称</param>
        /// <param name="trackTypeName">轨道类型名称（用于日志）</param>
        /// <param name="trackIndex">轨道索引</param>
        /// <param name="initializeTrackSO">初始化轨道SO的函数</param>
        /// <param name="initializeTrackData">初始化单个轨道数据的函数</param>
        /// <param name="addTrackToSO">将轨道数据添加到SO的函数</param>
        private void CreateMultiTrackData<TSO, TTrack>(
            System.Func<TSO> getTrackSO,
            System.Action<TSO> setTrackSO,
            string soName,
            string trackTypeName,
            int trackIndex,
            System.Action<TSO> initializeTrackSO,
            System.Action<TTrack, string, int> initializeTrackData,
            System.Action<TSO, TTrack> addTrackToSO
        ) where TSO : UnityEngine.ScriptableObject, new() where TTrack : new()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;

            // 确保轨道SO存在
            if (getTrackSO() == null)
            {
                var newTrackSO = UnityEngine.ScriptableObject.CreateInstance<TSO>();
                newTrackSO.name = soName;

                // 初始化轨道SO
                initializeTrackSO(newTrackSO);

                // 将轨道SO作为子资产添加到技能配置文件中
                UnityEditor.AssetDatabase.AddObjectToAsset(newTrackSO, skillConfig);

                setTrackSO(newTrackSO);
                UnityEditor.EditorUtility.SetDirty(skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"创建{trackTypeName}数据作为子资产嵌套到 {skillConfig.name}");
            }

            // 为该轨道索引添加新的轨道数据
            var trackSO = getTrackSO();
            if (trackSO != null)
            {
                string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(
                    GetTrackTypeFromSOType<TSO>(), trackIndex);
                var newTrack = new TTrack();

                // 初始化轨道数据
                initializeTrackData(newTrack, factoryTrackName, trackIndex);

                // 添加到SO中
                addTrackToSO(trackSO, newTrack);
                UnityEditor.EditorUtility.SetDirty(skillConfig);
            }
        }

        /// <summary>
        /// 根据SO类型获取对应的轨道类型
        /// </summary>
        private TrackType GetTrackTypeFromSOType<T>()
        {
            var typeName = typeof(T).Name;
            return typeName switch
            {
                "AudioTrackSO" => TrackType.AudioTrack,
                "EffectTrackSO" => TrackType.EffectTrack,
                "InjuryDetectionTrackSO" => TrackType.AttackTrack,
                "EventTrackSO" => TrackType.EventTrack,
                "GameObjectTrackSO" => TrackType.GameObjectTrack,
                _ => TrackType.AudioTrack // 默认值
            };
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
                    RemoveMultiTrackData(
                        skillConfig.trackContainer.audioTrack?.audioTracks,
                        trackIndex,
                        "音频",
                        () => skillConfig.trackContainer.audioTrack,
                        (so) => skillConfig.trackContainer.audioTrack = null
                    );
                    break;

                case TrackType.EffectTrack:
                    RemoveMultiTrackData(
                        skillConfig.trackContainer.effectTrack?.effectTracks,
                        trackIndex,
                        "特效",
                        () => skillConfig.trackContainer.effectTrack,
                        (so) => skillConfig.trackContainer.effectTrack = null
                    );
                    break;

                case TrackType.AttackTrack:
                    RemoveMultiTrackData(
                        skillConfig.trackContainer.injuryDetectionTrack?.injuryDetectionTracks,
                        trackIndex,
                        "攻击",
                        () => skillConfig.trackContainer.injuryDetectionTrack,
                        (so) => skillConfig.trackContainer.injuryDetectionTrack = null
                    );
                    break;

                case TrackType.EventTrack:
                    RemoveMultiTrackData(
                        skillConfig.trackContainer.eventTrack?.eventTracks,
                        trackIndex,
                        "事件",
                        () => skillConfig.trackContainer.eventTrack,
                        (so) => skillConfig.trackContainer.eventTrack = null
                    );
                    break;

                case TrackType.GameObjectTrack:
                    RemoveMultiTrackData(
                        skillConfig.trackContainer.gameObjectTrack?.gameObjectTracks,
                        trackIndex,
                        "游戏物体",
                        () => skillConfig.trackContainer.gameObjectTrack,
                        (so) => skillConfig.trackContainer.gameObjectTrack = null
                    );
                    break;

                case TrackType.TransformTrack:
                    RemoveSingleTrackData(
                        skillConfig.trackContainer.transformTrack,
                        "变换",
                        (so) => skillConfig.trackContainer.transformTrack = null
                    );
                    break;

                case TrackType.CameraTrack:
                    RemoveSingleTrackData(
                        skillConfig.trackContainer.cameraTrack,
                        "摄像机",
                        (so) => skillConfig.trackContainer.cameraTrack = null
                    );
                    break;

                case TrackType.AnimationTrack:
                    RemoveSingleTrackData(
                        skillConfig.trackContainer.animationTrack,
                        "动画",
                        (so) => skillConfig.trackContainer.animationTrack = null
                    );
                    break;
            }

            // 标记配置文件为已修改并保存资产
            UnityEditor.EditorUtility.SetDirty(skillConfig);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 删除多轨道类型的轨道数据
        /// </summary>
        /// <typeparam name="T">轨道数据类型，必须有trackIndex属性</typeparam>
        /// <param name="trackList">轨道列表</param>
        /// <param name="trackIndex">要删除的轨道索引</param>
        /// <param name="trackTypeName">轨道类型名称（用于日志）</param>
        /// <param name="getTrackSO">获取轨道SO的函数</param>
        /// <param name="clearTrackSO">清空轨道SO引用的函数</param>
        private void RemoveMultiTrackData<T>(
            System.Collections.Generic.List<T> trackList,
            int trackIndex,
            string trackTypeName,
            System.Func<UnityEngine.ScriptableObject> getTrackSO,
            System.Action<UnityEngine.ScriptableObject> clearTrackSO
        ) where T : class
        {
            if (trackList == null) return;

            Debug.Log($"{trackTypeName}轨道删除: 查找索引 {trackIndex}, 现有轨道数量: {trackList.Count}");

            // 打印所有现有轨道的索引（仅对音频和特效轨道）
            if (trackTypeName == "音频" || trackTypeName == "特效")
            {
                for (int i = 0; i < trackList.Count; i++)
                {
                    var track = trackList[i];
                    var trackIndexProperty = track.GetType().GetField("trackIndex");
                    var trackNameProperty = track.GetType().GetField("trackName");
                    if (trackIndexProperty != null && trackNameProperty != null)
                    {
                        var index = trackIndexProperty.GetValue(track);
                        var name = trackNameProperty.GetValue(track);
                        Debug.Log($"  {trackTypeName}轨道[{i}]: trackIndex={index}, trackName={name}");
                    }
                }
            }

            // 使用反射查找要删除的轨道
            T trackToRemove = null;
            foreach (var track in trackList)
            {
                var trackIndexProperty = track.GetType().GetField("trackIndex");
                if (trackIndexProperty != null)
                {
                    var index = (int)trackIndexProperty.GetValue(track);
                    if (index == trackIndex)
                    {
                        trackToRemove = track;
                        break;
                    }
                }
            }

            if (trackToRemove != null)
            {
                trackList.Remove(trackToRemove);
                Debug.Log($"从配置中删除{trackTypeName}轨道数据，索引: {trackIndex}");

                // 如果没有更多轨道了，删除整个SO
                if (trackList.Count == 0)
                {
                    var trackSO = getTrackSO();
                    clearTrackSO(trackSO);

                    // 从资产中移除ScriptableObject子资产
                    if (trackSO != null)
                    {
                        UnityEditor.AssetDatabase.RemoveObjectFromAsset(trackSO);
                        Debug.Log($"删除{trackTypeName}轨道SO子资产");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未找到要删除的{trackTypeName}轨道，索引: {trackIndex}");
            }
        }

        /// <summary>
        /// 删除单轨道类型的轨道数据
        /// </summary>
        /// <param name="trackSO">轨道ScriptableObject</param>
        /// <param name="trackTypeName">轨道类型名称（用于日志）</param>
        /// <param name="clearTrackSO">清空轨道SO引用的函数</param>
        private void RemoveSingleTrackData(
            UnityEngine.ScriptableObject trackSO,
            string trackTypeName,
            System.Action<UnityEngine.ScriptableObject> clearTrackSO
        )
        {
            if (trackSO != null)
            {
                clearTrackSO(trackSO);

                // 从资产中移除ScriptableObject子资产
                UnityEditor.AssetDatabase.RemoveObjectFromAsset(trackSO);
                Debug.Log($"删除{trackTypeName}轨道SO子资产");
            }
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
                Debug.Log($"准备删除轨道: {info.TrackName}, 类型: {info.TrackType}, 索引: {info.TrackIndex}");

                // 从配置数据中删除对应的轨道数据
                RemoveTrackDataFromConfig(info.TrackType, info.TrackIndex);

                // 从UI数据中移除轨道
                SkillEditorData.tracks.Remove(info);
                Debug.Log($"删除轨道: {info.TrackName}，剩余轨道数量: {SkillEditorData.tracks.Count}");

                // 触发刷新以重建UI和重新索引
                SkillEditorEvent.TriggerRefreshRequested();
            }
            else
            {
                Debug.LogWarning("HandleTrackDelete: 未找到要删除的轨道信息");
            }
        }

        /// <summary>
        /// 处理轨道激活状态变化事件
        /// 更新轨道激活状态并刷新显示，同时同步到配置文件
        /// </summary>
        /// <param name="ctrl">轨道控制器</param>
        /// <param name="isActive">新的激活状态</param>
        private void HandleTrackActiveStateChanged(SkillEditorTrackController ctrl, bool isActive)
        {
            var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
            if (info != null)
            {
                // 更新UI数据中的激活状态
                info.IsActive = isActive;
                info.Control.RefreshState(isActive);

                // 同步激活状态到配置文件
                UpdateTrackActiveStateInConfig(info.TrackType, info.TrackIndex, isActive);
            }
        }

        /// <summary>
        /// 更新轨道在配置文件中的激活状态
        /// 根据轨道类型和索引找到对应的配置数据并更新isEnabled状态
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        /// <param name="isActive">新的激活状态</param>
        private void UpdateTrackActiveStateInConfig(TrackType trackType, int trackIndex, bool isActive)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null)
            {
                Debug.LogWarning("无法更新轨道激活状态：技能配置为空");
                return;
            }

            try
            {
                switch (trackType)
                {
                    case TrackType.AnimationTrack:
                        if (skillConfig.trackContainer.animationTrack != null)
                        {
                            skillConfig.trackContainer.animationTrack.isEnabled = isActive;
                            UnityEditor.EditorUtility.SetDirty(skillConfig.trackContainer.animationTrack);
                        }
                        break;

                    case TrackType.CameraTrack:
                        if (skillConfig.trackContainer.cameraTrack != null)
                        {
                            skillConfig.trackContainer.cameraTrack.isEnabled = isActive;
                            UnityEditor.EditorUtility.SetDirty(skillConfig.trackContainer.cameraTrack);
                        }
                        break;

                    case TrackType.TransformTrack:
                        if (skillConfig.trackContainer.transformTrack != null)
                        {
                            skillConfig.trackContainer.transformTrack.isEnabled = isActive;
                            UnityEditor.EditorUtility.SetDirty(skillConfig.trackContainer.transformTrack);
                        }
                        break;

                    case TrackType.AudioTrack:
                        if (skillConfig.trackContainer.audioTrack?.audioTracks != null)
                        {
                            var audioTrack = skillConfig.trackContainer.audioTrack.audioTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (audioTrack != null)
                            {
                                audioTrack.isEnabled = isActive;
                                UnityEditor.EditorUtility.SetDirty(skillConfig.trackContainer.audioTrack);
                            }
                        }
                        break;

                    case TrackType.EffectTrack:
                        if (skillConfig.trackContainer.effectTrack?.effectTracks != null)
                        {
                            var effectTrack = skillConfig.trackContainer.effectTrack.effectTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (effectTrack != null)
                            {
                                effectTrack.isEnabled = isActive;
                                UnityEditor.EditorUtility.SetDirty(skillConfig.trackContainer.effectTrack);
                            }
                        }
                        break;

                    case TrackType.AttackTrack:
                        if (skillConfig.trackContainer.injuryDetectionTrack?.injuryDetectionTracks != null)
                        {
                            var attackTrack = skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (attackTrack != null)
                            {
                                attackTrack.isEnabled = isActive;
                                UnityEditor.EditorUtility.SetDirty(skillConfig.trackContainer.injuryDetectionTrack);
                            }
                        }
                        break;

                    case TrackType.EventTrack:
                        if (skillConfig.trackContainer.eventTrack?.eventTracks != null)
                        {
                            var eventTrack = skillConfig.trackContainer.eventTrack.eventTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (eventTrack != null)
                            {
                                eventTrack.isEnabled = isActive;
                                UnityEditor.EditorUtility.SetDirty(skillConfig.trackContainer.eventTrack);
                            }
                        }
                        break;

                    case TrackType.GameObjectTrack:
                        if (skillConfig.trackContainer.gameObjectTrack?.gameObjectTracks != null)
                        {
                            var gameObjectTrack = skillConfig.trackContainer.gameObjectTrack.gameObjectTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (gameObjectTrack != null)
                            {
                                gameObjectTrack.isEnabled = isActive;
                                UnityEditor.EditorUtility.SetDirty(skillConfig.trackContainer.gameObjectTrack);
                            }
                        }
                        break;

                    default:
                        Debug.LogWarning($"未支持的轨道类型: {trackType}");
                        break;
                }

                // 标记主配置文件为已修改并保存
                UnityEditor.EditorUtility.SetDirty(skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"更新轨道激活状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从配置文件中获取轨道的激活状态
        /// 根据轨道类型和索引从配置文件中读取isEnabled状态
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        /// <returns>轨道的激活状态，默认为true</returns>
        private bool GetTrackActiveStateFromConfig(TrackType trackType, int trackIndex)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null)
            {
                return true; // 默认激活状态
            }

            try
            {
                switch (trackType)
                {
                    case TrackType.AnimationTrack:
                        return skillConfig.trackContainer.animationTrack?.isEnabled ?? true;

                    case TrackType.CameraTrack:
                        return skillConfig.trackContainer.cameraTrack?.isEnabled ?? true;

                    case TrackType.TransformTrack:
                        return skillConfig.trackContainer.transformTrack?.isEnabled ?? true;

                    case TrackType.AudioTrack:
                        if (skillConfig.trackContainer.audioTrack?.audioTracks != null)
                        {
                            var audioTrack = skillConfig.trackContainer.audioTrack.audioTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            return audioTrack?.isEnabled ?? true;
                        }
                        break;

                    case TrackType.EffectTrack:
                        if (skillConfig.trackContainer.effectTrack?.effectTracks != null)
                        {
                            var effectTrack = skillConfig.trackContainer.effectTrack.effectTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            return effectTrack?.isEnabled ?? true;
                        }
                        break;

                    case TrackType.AttackTrack:
                        if (skillConfig.trackContainer.injuryDetectionTrack?.injuryDetectionTracks != null)
                        {
                            var attackTrack = skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            return attackTrack?.isEnabled ?? true;
                        }
                        break;

                    case TrackType.EventTrack:
                        if (skillConfig.trackContainer.eventTrack?.eventTracks != null)
                        {
                            var eventTrack = skillConfig.trackContainer.eventTrack.eventTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            return eventTrack?.isEnabled ?? true;
                        }
                        break;

                    case TrackType.GameObjectTrack:
                        if (skillConfig.trackContainer.gameObjectTrack?.gameObjectTracks != null)
                        {
                            var gameObjectTrack = skillConfig.trackContainer.gameObjectTrack.gameObjectTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            return gameObjectTrack?.isEnabled ?? true;
                        }
                        break;

                    default:
                        Debug.LogWarning($"未支持的轨道类型: {trackType}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取轨道激活状态失败: {ex.Message}");
            }

            return true; // 默认返回激活状态
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
                    info.Track.AddTrackItem("InjuryDetection");
                }
                else if (info.TrackType == TrackType.TransformTrack)
                {
                    info.Track.AddTrackItem("Transform");
                }
            }
        }

        #endregion

        #region 轨道项创建方法

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

            switch (trackType)
            {
                case TrackType.AnimationTrack when trackIndex == 0 && track is AnimationSkillEditorTrack animationTrack:
                    AnimationSkillEditorTrack.CreateTrackItemsFromConfig(animationTrack, skillConfig);
                    break;

                case TrackType.TransformTrack when trackIndex == 0 && track is TransformSkillEditorTrack transformTrack:
                    TransformSkillEditorTrack.CreateTrackItemsFromConfig(transformTrack, skillConfig, trackIndex);
                    break;

                case TrackType.CameraTrack when trackIndex == 0 && track is CameraSkillEditorTrack cameraTrack:
                    CameraSkillEditorTrack.CreateTrackItemsFromConfig(cameraTrack, skillConfig, trackIndex);
                    break;

                case TrackType.AudioTrack when track is AudioSkillEditorTrack audioTrack:
                    AudioSkillEditorTrack.CreateTrackItemsFromConfig(audioTrack, skillConfig, trackIndex);
                    break;

                case TrackType.EffectTrack when track is EffectSkillEditorTrack effectTrack:
                    EffectSkillEditorTrack.CreateTrackItemsFromConfig(effectTrack, skillConfig, trackIndex);
                    break;

                case TrackType.AttackTrack when track is InjuryDetectionSkillEditorTrack attackTrack:
                    InjuryDetectionSkillEditorTrack.CreateTrackItemsFromConfig(attackTrack, skillConfig, trackIndex);
                    break;

                case TrackType.EventTrack when track is EventSkillEditorTrack eventTrack:
                    EventSkillEditorTrack.CreateTrackItemsFromConfig(eventTrack, skillConfig, trackIndex);
                    break;

                case TrackType.GameObjectTrack when track is GameObjectSkillEditorTrack gameObjectTrack:
                    GameObjectSkillEditorTrack.CreateTrackItemsFromConfig(gameObjectTrack, skillConfig, trackIndex);
                    break;
            }
        }

        #endregion
    }
}
