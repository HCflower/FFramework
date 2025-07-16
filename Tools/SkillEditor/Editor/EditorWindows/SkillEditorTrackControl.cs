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
        // 轨道删除事件
        public System.Action<SkillEditorTrackControl> OnDeleteTrack;
        private VisualElement trackControlAreaContent;

        // 轨道控制器构造函数
        public SkillEditorTrackControl(VisualElement visual, TrackType trackType)
        {
            VisualElement trackControlArea = new VisualElement();
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
            trackControlArea.Add(CreateTrackControlAreaContent(trackType));
        }

        // 创建轨道控制器区域
        public VisualElement CreateTrackControlAreaContent(TrackType trackType)
        {
            trackControlAreaContent = new VisualElement();
            trackControlAreaContent.AddToClassList("TrackControlAreaContent");
            // 添加Icon            
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    trackControlAreaContent.Add(CreateTrackControlIcon("d_AnimationClip Icon"));
                    // 添加标题
                    trackControlAreaContent.Add(CreateTrackControlTitle("动画轨道"));
                    break;
                case TrackType.AudioTrack:
                    trackControlAreaContent.Add(CreateTrackControlIcon("d_AudioImporter Icon"));
                    // 添加标题
                    trackControlAreaContent.Add(CreateTrackControlTitle("音频轨道"));
                    // 添加控制器按钮
                    trackControlAreaContent.Add(CreateTrackControlButton("MoreOptions"));
                    break;
                case TrackType.EffectTrack:
                    trackControlAreaContent.Add(CreateTrackControlIcon("d_VisualEffectAsset Icon"));
                    // 添加标题
                    trackControlAreaContent.Add(CreateTrackControlTitle("特效轨道"));
                    // 添加控制器按钮
                    trackControlAreaContent.Add(CreateTrackControlButton("MoreOptions"));
                    break;
                case TrackType.EventTrack:
                    trackControlAreaContent.Add(CreateTrackControlIcon("SignalAsset Icon"));
                    // 添加标题
                    trackControlAreaContent.Add(CreateTrackControlTitle("事件轨道"));
                    // 添加控制器按钮
                    trackControlAreaContent.Add(CreateTrackControlButton("MoreOptions"));
                    break;
                case TrackType.AttackTrack:
                    trackControlAreaContent.Add(CreateTrackControlIcon("d_BoxCollider Icon"));
                    // 添加标题
                    trackControlAreaContent.Add(CreateTrackControlTitle("攻击轨道"));
                    // 添加控制器按钮
                    trackControlAreaContent.Add(CreateTrackControlButton("MoreOptions"));
                    break;
                default:
                    break;
            }
            return trackControlAreaContent;
        }

        // 创建控制器图标
        public Image CreateTrackControlIcon(string iconPath)
        {
            Image trackControlIcon = new Image();
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
            menu.AddItem(new GUIContent("设置轨道为激活状态"), false, () => Debug.Log("设置轨道为激活状态"));
            menu.AddItem(new GUIContent("设置轨道为失活状态"), false, () => Debug.Log("设置轨道为失活状态"));
            menu.AddItem(new GUIContent("更改当前轨道名称"), false, () => Debug.Log("更改当前轨道名称"));
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
    }
}
