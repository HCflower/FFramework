using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道控制器
    /// 负责管理轨道的可视化控制面板，包括图标显示、标题编辑、状态控制和右键菜单功能
    /// 提供轨道激活/失活、重命名、删除和子轨道管理等功能
    /// </summary>
    public class SkillEditorTrackController : VisualElement
    {
        #region 事件定义

        /// <summary>轨道激活状态变化事件</summary>
        public event System.Action<SkillEditorTrackController, bool> OnActiveStateChanged;

        /// <summary>轨道名称更改事件</summary>
        public event System.Action<SkillEditorTrackController, string> OnTrackNameChanged;

        /// <summary>轨道删除事件</summary>
        public System.Action<SkillEditorTrackController> OnDeleteTrack;

        /// <summary>子轨道添加事件</summary>
        public event System.Action<SkillEditorTrackController> OnAddTrackItem;

        #endregion

        #region 私有字段

        /// <summary>轨道控制区域容器</summary>
        private VisualElement trackControlArea;
        public VisualElement TrackControlArea => trackControlArea;

        /// <summary>轨道控制内容容器</summary>
        private VisualElement trackControlAreaContent;
        public VisualElement TrackControlAreaContent => trackControlAreaContent;

        /// <summary>轨道图标元素</summary>
        private Image trackControlIcon;

        #endregion

        #region 公共属性

        /// <summary>轨道类型</summary>
        public TrackType TrackType { get; private set; }

        /// <summary>轨道名称</summary>
        public string TrackName { get; private set; }

        #endregion

        #region 构造函数



        /// <summary>
        /// 轨道控制器构造函数
        /// 根据轨道类型初始化控制器界面和样式
        /// </summary>
        /// <param name="visual">父容器，控制器将添加到此容器中</param>
        /// <param name="trackType">轨道类型，决定显示的图标和样式</param>
        /// <param name="trackName">自定义轨道名称，为空则使用默认名称</param>
        public SkillEditorTrackController(VisualElement visual, TrackType trackType, string trackName = null)
        {
            TrackType = trackType;
            TrackName = trackName;

            // 创建并配置轨道控制区域
            InitializeTrackControlArea(visual, trackType);

            // 创建轨道控制器内容
            trackControlAreaContent = CreateTrackControlAreaContent(trackType, trackName);
            trackControlArea.Add(trackControlAreaContent);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化轨道控制区域容器和样式
        /// 根据轨道类型应用对应的CSS样式类
        /// </summary>
        /// <param name="visual">父容器</param>
        /// <param name="trackType">轨道类型</param>
        private void InitializeTrackControlArea(VisualElement visual, TrackType trackType)
        {
            trackControlArea = new VisualElement();
            trackControlArea.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackControlStyle"));
            trackControlArea.AddToClassList("TrackControlArea");

            // 根据轨道类型添加特定样式类
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    trackControlArea.AddToClassList("TrackControlArea-Animation");
                    break;
                case TrackType.AudioTrack:
                    trackControlArea.AddToClassList("TrackControlArea-Audio");
                    break;
                case TrackType.EffectTrack:
                    trackControlArea.AddToClassList("TrackControlArea-Effect");
                    break;
                case TrackType.EventTrack:
                    trackControlArea.AddToClassList("TrackControlArea-Event");
                    break;
                case TrackType.InjuryDetectionTrack:
                    trackControlArea.AddToClassList("TrackControlArea-Attack");
                    break;
                case TrackType.TransformTrack:
                    trackControlArea.AddToClassList("TrackControlArea-Transform");
                    break;
                case TrackType.CameraTrack:
                    trackControlArea.AddToClassList("TrackControlArea-Camera");
                    break;
                case TrackType.GameObjectTrack:
                    trackControlArea.AddToClassList("TrackControlArea-GameObject");
                    break;
            }

            visual.Add(trackControlArea);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 创建轨道控制器区域内容
        /// 根据轨道类型生成相应的图标、标题和功能按钮
        /// </summary>
        /// <param name="trackType">轨道类型，决定显示的图标和是否显示功能按钮</param>
        /// <param name="trackName">轨道显示名称，为空则使用默认名称</param>
        /// <returns>配置完成的轨道控制内容容器</returns>
        public VisualElement CreateTrackControlAreaContent(TrackType trackType, string trackName)
        {
            trackControlAreaContent = new VisualElement();
            trackControlAreaContent.AddToClassList("TrackControlAreaContent");

            // 获取轨道类型对应的配置信息
            var trackConfig = GetTrackConfiguration(trackType);

            // 添加轨道图标
            if (!string.IsNullOrEmpty(trackConfig.icon))
                trackControlAreaContent.Add(CreateTrackControlIcon(trackConfig.icon));

            // 添加轨道标题
            string displayName = !string.IsNullOrEmpty(trackName) ? trackName : trackConfig.defaultTitle;
            trackControlAreaContent.Add(CreateTrackControlTitle(displayName));

            // 添加功能按钮（部分轨道类型支持）
            if (trackConfig.hasButton)
                trackControlAreaContent.Add(CreateTrackControlButton("MoreOptions"));

            return trackControlAreaContent;
        }

        /// <summary>
        /// 创建控制器图标元素
        /// </summary>
        /// <param name="iconPath">图标资源路径</param>
        /// <returns>配置完成的图标元素</returns>
        public Image CreateTrackControlIcon(string iconPath)
        {
            trackControlIcon = new Image();
            trackControlIcon.AddToClassList("TrackControlAreaIcon");
            trackControlIcon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>($"Icon/{iconPath}"));
            return trackControlIcon;
        }

        /// <summary>
        /// 创建控制器标题标签
        /// </summary>
        /// <param name="title">标题文本内容</param>
        /// <returns>配置完成的标题标签元素</returns>
        public Label CreateTrackControlTitle(string title)
        {
            Label trackControlTitle = new Label();
            trackControlTitle.AddToClassList("TrackControlAreaTitle");
            trackControlTitle.text = title;
            return trackControlTitle;
        }

        /// <summary>
        /// 创建控制器功能按钮
        /// 点击按钮会显示包含轨道操作选项的下拉菜单
        /// </summary>
        /// <param name="buttonName">按钮图标名称</param>
        /// <returns>配置完成的功能按钮</returns>
        public Button CreateTrackControlButton(string buttonName)
        {
            Button trackControlButton = new Button();
            trackControlButton.AddToClassList("TrackControlAreaButton");
            trackControlButton.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>($"Icon/{buttonName}"));

            // 绑定点击事件显示下拉菜单
            trackControlButton.clicked += () => { ShowDropdownMenu(trackControlButton); };

            return trackControlButton;
        }

        /// <summary>
        /// 刷新轨道控制器的激活状态显示
        /// 根据激活状态调整图标的视觉效果
        /// </summary>
        /// <param name="isActive">轨道是否处于激活状态</param>
        public void RefreshState(bool isActive)
        {
            if (!isActive)
                trackControlIcon.AddToClassList("TrackInactiveState");
            else
                trackControlIcon.RemoveFromClassList("TrackInactiveState");
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 获取轨道类型配置信息
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <returns>轨道配置信息</returns>
        private (string icon, string defaultTitle, bool hasButton) GetTrackConfiguration(TrackType trackType)
        {
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    return ("d_AnimationClip Icon", "动画轨道", false);
                case TrackType.AudioTrack:
                    return ("d_AudioImporter Icon", "音频轨道", true);
                case TrackType.EffectTrack:
                    return ("d_VisualEffectAsset Icon", "特效轨道", true);
                case TrackType.EventTrack:
                    return ("SignalAsset Icon", "事件轨道", true);
                case TrackType.InjuryDetectionTrack:
                    return ("d_BoxCollider Icon", "攻击轨道", true);
                case TrackType.TransformTrack:
                    return ("d_Transform Icon", "变化轨道", true);
                case TrackType.CameraTrack:
                    return ("d_Camera Icon", "相机轨道", true);
                case TrackType.GameObjectTrack:
                    return ("d_Prefab Icon", "游戏物体轨道", true);
                default:
                    return (null, "未知轨道", false);
            }
        }

        /// <summary>
        /// 显示轨道操作下拉菜单
        /// 包含激活/失活、重命名、添加子轨道、删除等功能选项
        /// </summary>
        /// <param name="button">触发菜单显示的按钮元素</param>
        private void ShowDropdownMenu(VisualElement button)
        {
            GenericMenu menu = new GenericMenu();

            // 获取当前轨道的激活状态
            var info = SkillEditorData.tracks?.Find(t => t.Control == this);
            bool currentActiveState = info?.IsActive ?? true;

            // 轨道状态控制选项 - 根据当前状态显示相反操作
            if (currentActiveState)
            {
                menu.AddItem(new GUIContent("设置轨道为失活状态"), false, () =>
                {
                    OnActiveStateChanged?.Invoke(this, false);
                });
                menu.AddDisabledItem(new GUIContent("设置轨道为激活状态"));
            }
            else
            {
                menu.AddItem(new GUIContent("设置轨道为激活状态"), false, () =>
                {
                    OnActiveStateChanged?.Invoke(this, true);
                });
                menu.AddDisabledItem(new GUIContent("设置轨道为失活状态"));
            }

            // 轨道重命名选项
            menu.AddItem(new GUIContent("更改当前轨道名称"), false, () =>
            {
                // 示例：弹窗输入新名称（实际可用EditorUtility.DisplayDialog或自定义UI）
                string newName = TrackName;
                OnTrackNameChanged?.Invoke(this, newName);
            });

            //TODO: 子轨道管理选项（仅特定轨道类型支持）
            if (TrackType == TrackType.InjuryDetectionTrack || TrackType == TrackType.EventTrack || TrackType == TrackType.TransformTrack)
            {
                menu.AddItem(new GUIContent("添加轨道项"), false, () =>
                {
                    OnAddTrackItem?.Invoke(this);
                });
            }

            // 分割线和删除选项
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("删除当前轨道"), false, () =>
            {
                OnDeleteTrack?.Invoke(this);
            });

            // 在按钮正下方显示菜单
            var rect = button.worldBound;
            menu.DropDown(new Rect(rect.x, rect.yMax, 0, 0));
        }

        #endregion
    }
}
