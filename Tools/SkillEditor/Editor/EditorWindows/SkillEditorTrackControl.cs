using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道控制器
    /// </summary>
    public class SkillEditorTrackControl : VisualElement
    {
        // 激活/失活事件
        public event System.Action<SkillEditorTrackControl, bool> OnActiveStateChanged;
        // 轨道名称更改事件
        public event System.Action<SkillEditorTrackControl, string> OnTrackNameChanged;
        // 轨道删除事件
        public System.Action<SkillEditorTrackControl> OnDeleteTrack;
        // 子轨道添加事件
        public event System.Action<SkillEditorTrackControl> OnAddTrackItem;
        private VisualElement trackControlArea;
        private VisualElement trackControlAreaContent;
        private Image trackControlIcon;

        public TrackType TrackType { get; private set; }
        public string TrackName { get; private set; }

        // 轨道控制器构造函数
        public SkillEditorTrackControl(VisualElement visual, TrackType trackType, string trackName = null)
        {
            TrackType = trackType;
            TrackName = trackName;
            trackControlArea = new VisualElement();
            trackControlArea.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackControlStyle"));
            trackControlArea.AddToClassList("TrackControlArea");
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
                case TrackType.AttackTrack:
                    trackControlArea.AddToClassList("TrackControlArea-Attack");
                    break;
                default:
                    break;
            }
            visual.Add(trackControlArea);

            // 创建轨道控制器区域
            trackControlAreaContent = CreateTrackControlAreaContent(trackType, trackName);
            trackControlArea.Add(trackControlAreaContent);
        }

        // 创建轨道控制器区域
        public VisualElement CreateTrackControlAreaContent(TrackType trackType, string trackName)
        {
            trackControlAreaContent = new VisualElement();
            trackControlAreaContent.AddToClassList("TrackControlAreaContent");

            // 图标和标题映射
            string icon = null;
            string defaultTitle = null;
            bool hasButton = false;
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    icon = "d_AnimationClip Icon";
                    defaultTitle = "动画轨道";
                    break;
                case TrackType.AudioTrack:
                    icon = "d_AudioImporter Icon";
                    defaultTitle = "音频轨道";
                    hasButton = true;
                    break;
                case TrackType.EffectTrack:
                    icon = "d_VisualEffectAsset Icon";
                    defaultTitle = "特效轨道";
                    hasButton = true;
                    break;
                case TrackType.EventTrack:
                    icon = "SignalAsset Icon";
                    defaultTitle = "事件轨道";
                    hasButton = true;
                    break;
                case TrackType.AttackTrack:
                    icon = "d_BoxCollider Icon";
                    defaultTitle = "攻击轨道";
                    hasButton = true;
                    break;
                default:
                    break;
            }
            if (icon != null)
                trackControlAreaContent.Add(CreateTrackControlIcon(icon));
            trackControlAreaContent.Add(CreateTrackControlTitle(!string.IsNullOrEmpty(trackName) ? trackName : defaultTitle));
            if (hasButton)
                trackControlAreaContent.Add(CreateTrackControlButton("MoreOptions"));
            return trackControlAreaContent;
        }

        // 创建控制器图标
        public Image CreateTrackControlIcon(string iconPath)
        {
            trackControlIcon = new Image();
            trackControlIcon.AddToClassList("TrackControlAreaIcon");
            trackControlIcon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>($"Icon/{iconPath}"));
            return trackControlIcon;
        }

        // 创建控制器标题
        public Label CreateTrackControlTitle(string title)
        {
            Label trackControlTitle = new Label();
            trackControlTitle.AddToClassList("TrackControlAreaTitle");
            trackControlTitle.text = title;
            return trackControlTitle;
        }

        // 创建控制器下拉菜单
        public Button CreateTrackControlButton(string buttonName)
        {
            Button trackControlButton = new Button();
            trackControlButton.AddToClassList("TrackControlAreaButton");
            trackControlButton.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>($"Icon/{buttonName}"));

            // 添加点击事件来显示下拉菜单
            trackControlButton.clicked += () => { ShowDropdownMenu(trackControlButton); };

            return trackControlButton;
        }

        // 显示下拉菜单的方法
        private void ShowDropdownMenu(VisualElement button)
        {
            GenericMenu menu = new GenericMenu();

            // 添加菜单项
            menu.AddItem(new GUIContent("设置轨道为激活状态"), false, () =>
            {
                OnActiveStateChanged?.Invoke(this, true);
            });
            menu.AddItem(new GUIContent("设置轨道为失活状态"), false, () =>
            {
                OnActiveStateChanged?.Invoke(this, false);
            });
            menu.AddItem(new GUIContent("更改当前轨道名称"), false, () =>
            {
                // 示例：弹窗输入新名称（实际可用EditorUtility.DisplayDialog或自定义UI）
                string newName = TrackName;
                OnTrackNameChanged?.Invoke(this, newName);
            });
            if (TrackType == TrackType.AttackTrack || TrackType == TrackType.EventTrack)
            {
                menu.AddItem(new GUIContent("添加子轨道"), false, () =>
                {
                    // 触发添加子轨道事件
                    OnAddTrackItem?.Invoke(this);
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("删除当前轨道"), false, () =>
            {
                // 触发删除事件
                OnDeleteTrack?.Invoke(this);
            });

            // 使用按钮的世界边界，菜单显示在按钮正下方
            var rect = button.worldBound;
            menu.DropDown(new Rect(rect.x, rect.yMax, 0, 0));
        }

        // 刷新状态
        public void RefreshState(bool isActive)
        {
            if (!isActive)
                trackControlIcon.AddToClassList("TrackInactiveState");
            else
                trackControlIcon.RemoveFromClassList("TrackInactiveState");
        }
    }
}
