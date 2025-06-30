using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System;

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
        private TextField treeNameTextField;
        private ScrollView treeSelectScrollView;
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
            CreateTreeAreaView(rootView);

            CraeteControlAreaView(rootView, out VisualElement nodesControlArea);
            CreateControlAreaViewTitle(nodesControlArea, "Nodes");

            CraeteControlAreaView(rootView, out VisualElement nodeParentsControlArea);
            CreateControlAreaViewTitle(nodeParentsControlArea, "Set Parents");

            CraeteControlAreaView(rootView, out VisualElement settingControlArea);
            CreateControlAreaViewTitle(settingControlArea, "Settings");
        }

        // 创建树控制区域
        private void CreateTreeAreaView(VisualElement visual)
        {
            CraeteControlAreaView(visual, out VisualElement treeControlArea);
            //创建树视图标题
            CreateControlAreaViewTitle(treeControlArea, "RedDotTree");
            //创建配置文件选择区域
            CreateConfigDataSelectContent(treeControlArea);
            //创建树区域
            CreateRedDotTreeContent(treeControlArea);
            //树选择滚动视图
            CreateDataScrollView(treeControlArea, out treeSelectScrollView);
        }

        // 刷新视图区域
        private void RefreshView()
        {
            treeSelectScrollView.Clear();
            if (redDotKitConfig != null)
            {
                foreach (var tree in redDotKitConfig.RedDotTrees)
                {
                    AddScrollViewItemButton(treeSelectScrollView, tree.treeName);
                }
            }
        }

        // 创建树内容
        private void CreateRedDotTreeContent(VisualElement treeControlArea)
        {
            //创建树区域
            VisualElement createTreeContent = new VisualElement();
            createTreeContent.AddToClassList("ControlContent");
            treeControlArea.Add(createTreeContent);

            //创建配置文件标题
            Label configDataTitle = new Label("Create Tree");
            configDataTitle.AddToClassList("ControlContentTitle");
            createTreeContent.Add(configDataTitle);

            //文本输入框
            treeNameTextField = new TextField();
            treeNameTextField.AddToClassList("TreeNameTextField");
            createTreeContent.Add(treeNameTextField);

            //创建按钮
            Button createTreeButton = new Button() { text = "+" };
            createTreeButton.AddToClassList("Button");
            createTreeButton.clicked += () =>
            {
                if (redDotKitConfig == null)
                {
                    Debug.LogError("Please select a profile first.");
                    return;
                }
                else if (!string.IsNullOrWhiteSpace(treeNameTextField.value))
                {
                    // 检查树名是否已存在
                    if (redDotKitConfig.RedDotTrees.Exists(t => t.treeName == treeNameTextField.value))
                    {
                        Debug.LogError($"树 {treeNameTextField.value} 已存在!");
                        return;
                    }

                    // 创建新树定义
                    var newTree = new RedDotKitConfig.TreeDefinition
                    {
                        treeName = treeNameTextField.value,
                        //TODO: rootKey = (RedDotKey)Enum.Parse(typeof(RedDotKey), treeNameTextField.value + "_Root"),
                        nodeRelations = new System.Collections.Generic.List<RedDotKitConfig.NodeRelation>()
                    };

                    // 添加到配置并保存
                    redDotKitConfig.RedDotTrees.Add(newTree);
                    EditorUtility.SetDirty(redDotKitConfig);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"成功创建树: {treeNameTextField.value}");
                    AddScrollViewItemButton(treeSelectScrollView, treeNameTextField.value);
                }
            };
            createTreeContent.Add(createTreeButton);
        }

        // 创建配置文件选择区域
        private void CreateConfigDataSelectContent(VisualElement treeControlArea)
        {
            VisualElement configDataContent = new VisualElement();
            configDataContent.AddToClassList("ControlContent");
            treeControlArea.Add(configDataContent);

            //创建配置文件标题
            Label configDataTitle = new Label("Config Data");
            configDataTitle.AddToClassList("ControlContentTitle");
            configDataContent.Add(configDataTitle);

            //创建配置文件选择
            redDotKitConfigField = new ObjectField();
            redDotKitConfigField.AddToClassList("ConfigDataField");
            redDotKitConfigField.objectType = typeof(RedDotKitConfig);
            redDotKitConfigField.RegisterValueChangedCallback(evt =>
            {
                redDotKitConfig = (RedDotKitConfig)evt.newValue;
                RefreshView();
            });
            configDataContent.Add(redDotKitConfigField);

            //创建按钮
            Button createConfigDataButton = new Button() { text = "O" };
            createConfigDataButton.AddToClassList("Button");
            createConfigDataButton.clicked += () => RefreshView();
            configDataContent.Add(createConfigDataButton);
        }

        // 创建控制区域
        private void CraeteControlAreaView(VisualElement visual, out VisualElement controlAreaView)
        {
            controlAreaView = new VisualElement();
            controlAreaView.AddToClassList("ControlAreaView");
            visual.Add(controlAreaView);
        }

        // 创建控制区域标题
        private void CreateControlAreaViewTitle(VisualElement visual, string titleName)
        {
            Label title = new Label(titleName);
            title.AddToClassList("ControlAreaViewTitle");
            visual.Add(title);
        }

        // 创建数据滚动视图
        private void CreateDataScrollView(VisualElement visual, out ScrollView scrollView)
        {
            scrollView = new ScrollView();
            scrollView.AddToClassList("ScrollView");
            visual.Add(scrollView);
        }

        // 添加滚动视图项
        private void AddScrollViewItemButton(ScrollView scrollView, string title, Action buttonAction = null, Action deleteAction = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Debug.Log("<color=red>The name of the tree cannot be null or full of spaces.</color>");
                return;
            }

            //创建滚动视图项
            VisualElement item = new VisualElement();
            item.AddToClassList("ControlContent");
            scrollView.Add(item);

            //创建按钮
            Button button = new Button(buttonAction);
            button.AddToClassList("ScrollViewItemButton");
            button.text = title;
            item.Add(button);

            //创建删除按钮
            Button deleteButton = new Button(deleteAction);
            deleteButton.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("Confirm the deletion", $"OK to delete the tree '{title}' ?", "OK", "NO"))
                    return;

                // 从配置中移除
                var treeToRemove = redDotKitConfig?.RedDotTrees?.Find(t => t.treeName == title);
                if (treeToRemove != null)
                {
                    redDotKitConfig.RedDotTrees.Remove(treeToRemove);
                    EditorUtility.SetDirty(redDotKitConfig);
                    AssetDatabase.SaveAssets();
                }
                // 从UI移除
                scrollView.Remove(item);
                Debug.Log($"<color=red>删除Tree:</color>{title}");

                // 刷新视图
                RefreshView();
            };
            deleteButton.AddToClassList("ScrollViewItemDeleteButton");
            deleteButton.text = "X";
            item.Add(deleteButton);
        }
    }
}