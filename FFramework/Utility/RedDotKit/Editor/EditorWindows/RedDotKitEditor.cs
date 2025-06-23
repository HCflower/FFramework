using UnityEngine.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using UnityEditor.Search;

namespace RedDotKitEditor
{
    /// <summary>
    /// 红点系统编辑器
    /// </summary>
    public class RedDotKitEditor : EditorWindow
    {

        #region 数据
        private RedDotKitConfig redDotKitConfig;
        #endregion

        #region UI元素
        private ObjectField redDotKitConfigField;
        #endregion

        [MenuItem("FFramework/🔴RedDotKitEditor &R", priority = 5)]
        public static void SkillEditorCreateWindow()
        {
            RedDotKitEditor window = GetWindow<RedDotKitEditor>();
            window.minSize = new Vector2(800, 450);
            window.titleContent = new GUIContent("RedDotKitEditor");
            window.Show();
        }

        private void OnEnable()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/RedDotKitEditor"));
            VisualElement rootView = new VisualElement();
            rootView.AddToClassList("RootView");
            rootVisualElement.Add(rootView);
            //创建控制区域
            CraeteControlAreaView(rootView, out VisualElement treeControlArea);
            CreateControlAreaViewTitle(treeControlArea, "RedDotTree");
            CraeteControlAreaView(rootView, out VisualElement nodesControlArea);
            CreateControlAreaViewTitle(nodesControlArea, "Nodes");
            CraeteControlAreaView(rootView, out VisualElement nodeParentsControlArea);
            CreateControlAreaViewTitle(nodeParentsControlArea, "Set Parents");
            CraeteControlAreaView(rootView, out VisualElement settingControlArea);
            CreateControlAreaViewTitle(settingControlArea, "Settings");
        }

        //创建控制区域
        private void CraeteControlAreaView(VisualElement visual, out VisualElement controlAreaView)
        {
            controlAreaView = new VisualElement();
            controlAreaView.AddToClassList("ControlAreaView");
            visual.Add(controlAreaView);
        }

        //创建控制区域标题
        private void CreateControlAreaViewTitle(VisualElement visual, string titleName)
        {
            Label title = new Label(titleName);
            title.AddToClassList("ControlAreaViewTitle");
            visual.Add(title);
        }

        //创建数据滚动视图
        private void CreateDataScrollView(VisualElement visual, out ScrollView scrollView)
        {
            scrollView = new ScrollView();
            scrollView.AddToClassList("DataScrollView");
            visual.Add(scrollView);
        }
    }
}