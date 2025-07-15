using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道控制器
    /// </summary>
    public class SkillEditorTrackControl : VisualElement
    {
        // 轨道控制器构造函数
        public SkillEditorTrackControl(VisualElement visual)
        {
            VisualElement trackControlArea = new VisualElement();
            trackControlArea.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackControlStyle"));
            trackControlArea.AddToClassList("TrackControlArea");
            visual.Add(trackControlArea);

            // 创建轨道控制器区域
            trackControlArea.Add(CreateTrackControlAreaContent());
        }

        // 创建轨道控制器区域
        public VisualElement CreateTrackControlAreaContent()
        {
            VisualElement trackControlArea = new VisualElement();
            trackControlArea.AddToClassList("TrackControlAreaContent");
            // 添加Icon
            trackControlArea.Add(CreateTrackControlIcon("d_AnimationClip Icon"));
            // 添加标题
            trackControlArea.Add(CreateTrackControlTitle("技能轨道"));
            // 添加控制器按钮
            trackControlArea.Add(CreateTrackControlButton("MoreOptions"));

            return trackControlArea;
        }

        // 创建控制器图标
        public VisualElement CreateTrackControlIcon(string iconPath)
        {
            VisualElement trackControlIcon = new VisualElement();
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

        // 创建控制器按钮
        public Button CreateTrackControlButton(string buttonName)
        {
            Button trackControlButton = new Button();
            trackControlButton.AddToClassList("TrackControlAreaButton");
            trackControlButton.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>($"Icon/{buttonName}"));
            return trackControlButton;
        }
    }
}
