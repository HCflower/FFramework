using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{

    [CustomEditor(typeof(AttackTrackItemData))]
    public class AttackTrackItemDataInspector : Editor
    {
        private VisualElement root;
        private AttackTrackItemData targetData;

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as AttackTrackItemData;
            root = new VisualElement();
            root.styleSheets.Add(Resources.Load<StyleSheet>("USS/ItemDataInspectorStyle"));

            // 标题样式
            Label title = new Label("攻击轨道项信息");
            title.AddToClassList("ItemDataViewTitle");
            title.text = "攻击轨道项信息";
            root.Add(title);

            return root;
        }

        private VisualElement ItemDataViewContent(string titleName)
        {
            VisualElement content = new VisualElement();
            content.AddToClassList("ItemDataViewContent");
            content.AddToClassList("ItemDataViewContent-Attack"); // 添加攻击轨道项特有的样式类
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