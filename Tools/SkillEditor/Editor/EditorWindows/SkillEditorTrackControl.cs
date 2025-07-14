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
        private SkillEditorTrackControl(VisualElement visual)
        {
            VisualElement mainVisualElement = new VisualElement();
            mainVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackControl"));
        }
    }
}
