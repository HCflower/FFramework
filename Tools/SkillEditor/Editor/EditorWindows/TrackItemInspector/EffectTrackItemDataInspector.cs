using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(EffectTrackItemData))]
    public class EffectTrackItemDataInspector : Editor
    {
        private VisualElement root;
        private EffectTrackItemData targetData;

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as EffectTrackItemData;
            root = new VisualElement();
            root.styleSheets.Add(Resources.Load<StyleSheet>("USS/ItemDataInspectorStyle"));

            // 标题样式
            Label title = new Label("特效轨道项信息");
            title.AddToClassList("ItemDataViewTitle");
            title.text = "特效轨道项信息";
            root.Add(title);

            return root;
        }

        private VisualElement ItemDataViewContent(string titleName)
        {
            VisualElement content = new VisualElement();
            content.AddToClassList("ItemDataViewContent");
            content.AddToClassList("ItemDataViewContent-Effect"); // 添加音频特定样式
            if (!string.IsNullOrEmpty(titleName))
            {
                //标题文本
                Label title = new Label(titleName);
                title.style.width = 100;
                content.Add(title);
            }
            return content;
        }
    }
}
