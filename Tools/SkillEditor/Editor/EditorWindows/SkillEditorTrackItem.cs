using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道项
    /// 负责管理单个轨道项的显示、交互和拖拽功能
    /// 支持多种轨道类型的可视化表示和Inspector选择功能
    /// </summary>
    public class SkillEditorTrackItem : VisualElement
    {
        #region 私有字段

        /// <summary>轨道项UI容器</summary>
        private VisualElement trackItem;

        /// <summary>轨道项内容容器</summary>
        private VisualElement itemContent;

        /// <summary>轨道项持续帧数</summary>
        private int frameCount;

        /// <summary>所属轨道类型</summary>
        private TrackType trackType;

        /// <summary>是否正在拖拽中</summary>
        private bool isDragging = false;

        /// <summary>拖拽开始位置</summary>
        private Vector2 dragStartPos;

        /// <summary>拖拽前的原始左边距</summary>
        private float originalLeft;

        /// <summary>轨道项的起始帧位置</summary>
        private int startFrame;

        /// <summary>当前轨道项数据对象</summary>
        private BaseTrackItemData currentTrackItemData;

        #endregion

        #region 构造函数

        /// <summary>
        /// 轨道项构造函数
        /// 创建并初始化轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="trackType">轨道类型，决定样式和行为</param>
        /// <param name="frameCount">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        public SkillEditorTrackItem(VisualElement visual, string title, TrackType trackType, int frameCount, int startFrame = 0)
        {
            this.frameCount = frameCount;
            this.trackType = trackType;
            this.startFrame = startFrame;

            // 创建并配置轨道项容器
            InitializeTrackItem();

            // 创建轨道项内容
            itemContent = CreateTrackItemContent(title);
            trackItem.Add(itemContent);

            // 设置宽度和位置
            SetWidth();
            UpdatePosition();
            visual.Add(trackItem);

            // 注册拖拽事件
            RegisterDragEvents();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取当前轨道项的数据对象
        /// </summary>
        public BaseTrackItemData ItemData
        {
            get
            {
                if (currentTrackItemData == null)
                {
                    currentTrackItemData = CreateTrackItemData();
                }
                return currentTrackItemData;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置轨道项的起始帧位置
        /// 根据帧位置和当前帧单位宽度计算实际的像素位置
        /// </summary>
        /// <param name="frame">起始帧位置</param>
        public void SetStartFrame(int frame)
        {
            startFrame = frame;
            UpdatePosition();
        }

        /// <summary>
        /// 设置轨道项的左边距位置（保持向后兼容）
        /// 根据像素位置反推帧位置
        /// </summary>
        /// <param name="left">左边距值（像素）</param>
        public void SetLeft(float left)
        {
            // 根据像素位置计算对应的帧位置
            startFrame = (int)(left / SkillEditorData.FrameUnitWidth);
            trackItem.style.left = left;
        }

        /// <summary>
        /// 更新轨道项的位置
        /// 根据起始帧位置和当前帧单位宽度重新计算像素位置
        /// </summary>
        public void UpdatePosition()
        {
            float pixelPosition = startFrame * SkillEditorData.FrameUnitWidth;
            trackItem.style.left = pixelPosition;
        }

        /// <summary>
        /// 根据帧数和单位宽度设置轨道项宽度
        /// 宽度会根据SkillEditorData中的帧单位宽度动态计算
        /// </summary>
        public void SetWidth()
        {
            itemContent.style.width = frameCount * SkillEditorData.FrameUnitWidth;
        }

        /// <summary>
        /// 更新轨道项的帧数，并重新计算宽度
        /// </summary>
        /// <param name="newFrameCount">新的帧数</param>
        public void UpdateFrameCount(int newFrameCount)
        {
            frameCount = newFrameCount;
            SetWidth();
        }

        /// <summary>
        /// 获取轨道项的起始帧位置
        /// </summary>
        /// <returns>起始帧位置</returns>
        public float GetStartFrame()
        {
            return startFrame;
        }

        /// <summary>
        /// 获取轨道项的结束帧位置
        /// </summary>
        /// <returns>结束帧位置</returns>
        public float GetEndFrame()
        {
            return startFrame + frameCount;
        }

        /// <summary>
        /// 刷新轨道项的显示
        /// 更新宽度和位置以适应缩放变化
        /// </summary>
        public void RefreshDisplay()
        {
            SetWidth();
            UpdatePosition();
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化轨道项容器和基础样式
        /// </summary>
        private void InitializeTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        /// <summary>
        /// 注册拖拽相关的鼠标事件
        /// </summary>
        private void RegisterDragEvents()
        {
            trackItem.RegisterCallback<PointerDownEvent>(OnPointerDown);
            trackItem.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            trackItem.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建轨道项的内容容器
        /// 根据轨道类型应用相应的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的轨道项内容容器</returns>
        public VisualElement CreateTrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");
            itemContent.tooltip = title;
            // 应用轨道类型特定样式
            AddTrackTypeStyleClass(itemContent);

            // 添加标题标签
            AddTitleLabel(itemContent, title);

            return itemContent;
        }

        /// <summary>
        /// 为轨道项内容添加轨道类型特定的样式类
        /// </summary>
        /// <param name="itemContent">要添加样式的内容容器</param>
        private void AddTrackTypeStyleClass(VisualElement itemContent)
        {
            string styleClass = GetTrackTypeStyleClass();
            if (!string.IsNullOrEmpty(styleClass))
                itemContent.AddToClassList(styleClass);
        }

        /// <summary>
        /// 根据轨道类型获取对应的CSS样式类名
        /// </summary>
        /// <returns>轨道类型对应的样式类名</returns>
        private string GetTrackTypeStyleClass()
        {
            return trackType switch
            {
                TrackType.AnimationTrack => "TrackItem-Animation",
                TrackType.AudioTrack => "TrackItem-Audio",
                TrackType.EffectTrack => "TrackItem-Effect",
                TrackType.EventTrack => "TrackItem-Event",
                TrackType.AttackTrack => "TrackItem-Attack",
                TrackType.TransformTrack => "TrackItem-Transform",
                TrackType.CameraTrack => "TrackItem-Camera",
                TrackType.GameObjectTrack => "TrackItem-GameObject",
                _ => ""
            };
        }

        /// <summary>
        /// 为轨道项内容添加标题标签
        /// </summary>
        /// <param name="itemContent">内容容器</param>
        /// <param name="title">标题文本</param>
        private void AddTitleLabel(VisualElement itemContent, string title)
        {
            Label titleLabel = new Label();
            titleLabel.AddToClassList("TrackItemTitle");
            titleLabel.text = title;
            itemContent.Add(titleLabel);
        }

        #endregion

        #region 拖拽事件处理

        /// <summary>
        /// 鼠标按下事件处理
        /// 启动拖拽操作并选中轨道项到Inspector面板
        /// </summary>
        /// <param name="evt">鼠标按下事件参数</param>
        private void OnPointerDown(PointerDownEvent evt)
        {
            StartDrag(evt);
            SelectTrackItemInInspector();
        }

        /// <summary>
        /// 开始拖拽操作
        /// 记录拖拽开始位置和原始帧位置，并捕获指针
        /// </summary>
        /// <param name="evt">鼠标按下事件参数</param>
        private void StartDrag(PointerDownEvent evt)
        {
            isDragging = true;
            dragStartPos = evt.position;
            originalLeft = startFrame * SkillEditorData.FrameUnitWidth; // 基于帧位置计算像素位置
            trackItem.CapturePointer(evt.pointerId);
        }

        /// <summary>
        /// 在Inspector面板中选中当前轨道项
        /// 创建对应类型的ScriptableObject数据并设置为选中对象
        /// </summary>
        private void SelectTrackItemInInspector()
        {
            if (currentTrackItemData == null)
            {
                currentTrackItemData = CreateTrackItemData();
            }
            UnityEditor.Selection.activeObject = currentTrackItemData;
        }

        /// <summary>
        /// 鼠标移动事件处理
        /// 当正在拖拽时更新轨道项位置并对齐到刻度
        /// </summary>
        /// <param name="evt">鼠标移动事件参数</param>
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isDragging) return;

            float newLeft = CalculateNewPosition(evt);
            newLeft = ClampToTrackBounds(newLeft);

            // 更新像素位置和对应的帧位置
            trackItem.style.left = newLeft;
            int newStartFrame = (int)(newLeft / SkillEditorData.FrameUnitWidth);

            // 如果起始帧发生变化，更新数据但不立即刷新Inspector
            if (newStartFrame != startFrame)
            {
                SetStartFrame(newStartFrame);

                // 只更新数据对象，不调用刷新（避免拖拽时频繁刷新）
                if (currentTrackItemData != null)
                {
                    currentTrackItemData.startFrame = startFrame;
                }
            }
        }

        /// <summary>
        /// 鼠标释放事件处理
        /// 结束拖拽操作并释放指针捕获
        /// </summary>
        /// <param name="evt">鼠标释放事件参数</param>
        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!isDragging) return;
            isDragging = false;
            trackItem.ReleasePointer(evt.pointerId);

            int finalStartFrame = (int)(trackItem.style.left.value.value / SkillEditorData.FrameUnitWidth);
            SetStartFrame(finalStartFrame); // 更新检查器面板的起始帧

            // 更新Inspector显示
            if (currentTrackItemData != null)
            {
                UpdateInspectorPanel();
            }
        }

        #endregion

        #region 拖拽辅助方法

        /// <summary>
        /// 计算拖拽时的新位置
        /// 根据鼠标移动距离计算新位置并对齐到帧刻度
        /// </summary>
        /// <param name="evt">鼠标移动事件参数</param>
        /// <returns>对齐到刻度的新左边距位置</returns>
        private float CalculateNewPosition(PointerMoveEvent evt)
        {
            float deltaX = evt.position.x - dragStartPos.x;
            float newLeft = originalLeft + deltaX;

            // 对齐到帧刻度
            float unit = SkillEditorData.FrameUnitWidth;
            return Mathf.Round(newLeft / unit) * unit;
        }

        /// <summary>
        /// 将位置限制在轨道边界内
        /// 确保轨道项不会拖拽到轨道区域之外
        /// </summary>
        /// <param name="newLeft">计算得到的新左边距位置</param>
        /// <returns>限制在边界内的左边距位置</returns>
        private float ClampToTrackBounds(float newLeft)
        {
            if (trackItem.parent == null) return newLeft;

            float trackWidth = trackItem.parent.resolvedStyle.width;
            float itemWidth = trackItem.resolvedStyle.width;
            return Mathf.Clamp(newLeft, 0, trackWidth - itemWidth);
        }

        #endregion

        #region Inspector数据创建方法

        /// <summary>
        /// 创建轨道项对应的数据对象用于Inspector显示
        /// 根据轨道类型创建相应的ScriptableObject数据
        /// </summary>
        /// <returns>创建的轨道项数据对象</returns>
        private BaseTrackItemData CreateTrackItemData()
        {
            string itemName = GetTrackItemName();

            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    return CreateAnimationTrackItemData(itemName);
                case TrackType.AudioTrack:
                    return CreateAudioTrackItemData(itemName);
                case TrackType.EffectTrack:
                    return CreateEffectTrackItemData(itemName);
                case TrackType.EventTrack:
                    return CreateEventTrackItemData(itemName);
                case TrackType.AttackTrack:
                    return CreateAttackTrackItemData(itemName);
                case TrackType.TransformTrack:
                    return CreateTransformTrackItemData(itemName);
                case TrackType.CameraTrack:
                    // TODO: 需要创建CreateCameraTrackItemData方法
                    // return CreateCameraTrackItemData(itemName);
                    return null;
                case TrackType.GameObjectTrack:
                    return CreateGameObjectTrackItemData(itemName);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获取轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 强制刷新Inspector面板，确保数据变更后立即显示
        /// </summary>
        private void UpdateInspectorPanel()
        {
            if (currentTrackItemData != null)
            {
                // 同步轨道项的起始帧位置到数据对象
                currentTrackItemData.startFrame = startFrame;

                // 标记对象为脏状态，这会触发属性绑定的更新
                UnityEditor.EditorUtility.SetDirty(currentTrackItemData);
            }
        }

        #region 动画轨道项数据创建方法
        /// <summary>
        /// 创建动画轨道项的数据对象
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <returns>动画轨道项数据对象</returns>
        private AnimationTrackItemData CreateAnimationTrackItemData(string itemName)
        {
            // 如果已存在数据对象，更新基础信息
            if (currentTrackItemData != null && currentTrackItemData is AnimationTrackItemData animData)
            {
                UpdateBasicTrackItemData(animData, itemName);
                SyncWithConfigData(animData, itemName);
                return animData;
            }

            // 创建新的动画轨道项数据对象
            var newAnimData = ScriptableObject.CreateInstance<AnimationTrackItemData>();
            UpdateBasicTrackItemData(newAnimData, itemName);
            SetDefaultAnimationData(newAnimData);
            SyncWithConfigData(newAnimData, itemName);

            currentTrackItemData = newAnimData;
            return newAnimData;
        }

        /// <summary>
        /// 更新轨道项的基础数据
        /// </summary>
        /// <param name="data">要更新的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void UpdateBasicTrackItemData(AnimationTrackItemData data, string itemName)
        {
            data.trackItemName = itemName;
            data.frameCount = frameCount;
            data.startFrame = startFrame;
        }

        /// <summary>
        /// 设置动画数据的默认值
        /// </summary>
        /// <param name="data">要设置的数据对象</param>
        private void SetDefaultAnimationData(AnimationTrackItemData data)
        {
            data.durationFrame = frameCount; // 默认持续帧数为轨道项帧数
            data.playSpeed = 1f; // 默认播放速度为1
            data.isLoop = false; // 默认不循环播放
            data.applyRootMotion = false; // 默认不应用根运动
            data.animationClip = null; // 默认动画片段为空
        }

        /// <summary>
        /// 从技能配置同步动画数据
        /// </summary>
        /// <param name="data">要同步的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void SyncWithConfigData(AnimationTrackItemData data, string itemName)
        {
            var configClip = GetCorrespondingConfigClip(itemName, startFrame);
            if (configClip?.clip != null)
            {
                data.animationClip = configClip.clip;
                data.durationFrame = configClip.durationFrame;
                data.playSpeed = configClip.playSpeed;
                data.isLoop = configClip.isLoop;
                data.applyRootMotion = configClip.applyRootMotion;
            }
        }

        /// <summary>
        /// 从技能配置中获取对应的动画片段配置数据
        /// </summary>
        /// <param name="clipName">片段名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <returns>对应的动画片段配置，找不到时返回null</returns>
        private FFramework.Kit.AnimationTrack.AnimationClip GetCorrespondingConfigClip(string clipName, int startFrame)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.animationTrack == null)
                return null;

            // 优先通过名称查找
            var candidateClips = skillConfig.trackContainer.animationTrack.animationClips
                .Where(clip => clip.clipName == clipName).ToList();

            if (candidateClips.Count == 0)
                return null;

            if (candidateClips.Count == 1)
                return candidateClips[0];

            // 如果有多个同名片段，通过起始帧匹配
            var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == startFrame);
            return exactMatch ?? candidateClips[0];
        }

        #endregion

        #region 音频轨道项数据创建方法

        /// <summary>
        /// 创建音频轨道项的数据对象
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <returns>音频轨道项数据对象</returns>
        private AudioTrackItemData CreateAudioTrackItemData(string itemName)
        {
            return CreateAudioTrackItemData(itemName, null, 1.0f, 1.0f, false);
        }

        /// <summary>
        /// 创建音频轨道项数据（完整版本）
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="volume">音量</param>
        /// <param name="pitch">音调</param>
        /// <param name="isLoop">是否循环</param>
        /// <returns>音频轨道项数据对象</returns>
        private AudioTrackItemData CreateAudioTrackItemData(string itemName, AudioClip audioClip, float volume, float pitch, bool isLoop)
        {
            var audioData = ScriptableObject.CreateInstance<AudioTrackItemData>();
            audioData.trackItemName = itemName;
            audioData.frameCount = frameCount;
            audioData.startFrame = startFrame;
            audioData.durationFrame = frameCount;
            audioData.audioClip = audioClip;
            audioData.volume = volume;
            audioData.pitch = pitch;
            audioData.isLoop = isLoop;
            return audioData;
        }

        /// <summary>
        /// 创建特效轨道项的数据对象
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <returns>特效轨道项数据对象</returns>
        private BaseTrackItemData CreateEffectTrackItemData(string itemName)
        {
            var effectData = ScriptableObject.CreateInstance<EffectTrackItemData>();
            effectData.trackItemName = itemName;
            effectData.frameCount = frameCount;
            effectData.startFrame = startFrame;
            effectData.durationFrame = frameCount;
            return effectData;
        }

        /// <summary>
        /// 创建攻击轨道项的数据对象
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <returns>攻击轨道项数据对象</returns>
        private BaseTrackItemData CreateAttackTrackItemData(string itemName)
        {
            var attackData = ScriptableObject.CreateInstance<AttackTrackItemData>();
            attackData.trackItemName = itemName;
            attackData.frameCount = frameCount;
            attackData.startFrame = startFrame;
            attackData.durationFrame = frameCount;
            return attackData;
        }

        /// <summary>
        /// 创建事件轨道项的数据对象
        /// </summary>
        /// param name="itemName">轨道项名称</param>
        /// <returns>事件轨道项数据对象</returns>
        private BaseTrackItemData CreateEventTrackItemData(string itemName)
        {
            var eventData = ScriptableObject.CreateInstance<EventTrackItemData>();
            eventData.trackItemName = itemName;
            eventData.frameCount = frameCount;
            eventData.startFrame = startFrame;
            eventData.durationFrame = frameCount;
            return eventData;
        }

        /// <summary>
        /// 创建变换轨道项的数据对象
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <returns>变换轨道项数据对象</returns>
        private BaseTrackItemData CreateTransformTrackItemData(string itemName)
        {
            var transformData = ScriptableObject.CreateInstance<TransformTrackItemData>();
            transformData.trackItemName = itemName;
            transformData.frameCount = frameCount;
            transformData.startFrame = startFrame;
            transformData.durationFrame = frameCount;
            return transformData;
        }

        /// <summary>
        /// 创建游戏物体轨道项的数据对象
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <returns>游戏物体轨道项数据对象</returns>
        private BaseTrackItemData CreateGameObjectTrackItemData(string itemName)
        {
            var gameObjectData = ScriptableObject.CreateInstance<GameObjectTrackItemData>();
            gameObjectData.trackItemName = itemName;
            gameObjectData.frameCount = frameCount;
            gameObjectData.startFrame = startFrame;
            gameObjectData.durationFrame = frameCount;
            return gameObjectData;
        }
        #endregion
        #endregion
    }
}
