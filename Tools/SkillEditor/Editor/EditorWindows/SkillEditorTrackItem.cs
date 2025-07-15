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
        public SkillEditorTrackItem(VisualElement visual)
        {
            VisualElement trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }
    }
}
