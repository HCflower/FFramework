using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 轨道项
    /// </summary>
    public class SkillEditorTrackItem : VisualElement
    {
        // 轨道项构造函数
        private SkillEditorTrackItem(VisualElement visual)
        {
            VisualElement mainVisualElement = new VisualElement();
            mainVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }
    }
}
