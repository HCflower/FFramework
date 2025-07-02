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
        private RedDotKitConfig redDotKitConfig;            //红点树配置文件
        private string redDotTreeName;                      //红点树名称
        private RedDotKey currentSelectedRedDotKey;         //当前选中的节点
        #endregion

        #region UI元素
        private VisualElement rootView;                     //根视图
        private ObjectField redDotKitConfigField;           //红点树配置文件选择框    
        private TextField treeNameTextField;                //树名称输入框
        private TextField redDotKeyTextField;               //节点Key名称输入框
        private TextField redDotCountTextField;               //节点Key名称输入框
        private EnumField addNodeEnumField;                 //节点类型选择框
        private ScrollView treeSelectScrollView;            //树选择滚动视图
        private ScrollView treeNodesScrollView;             //树节点滚动视图
        private EnumField addParentNodeEnumField;           //添加父级节点类型选择框
        private ScrollView parentNodesScrollView;           //当前节点的父级节点滚动视图
        private ScrollView settingScrollView;               //设置滚动视图
        private EnumField setRootKeyEnumField;              //设置根节点选择框

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

            //创建根视图
            rootView = new VisualElement();
            rootView.AddToClassList("RootView");
            rootVisualElement.Add(rootView);

            //创建控制区域
            CreateTreeAreaView(rootView);
            //创建节点控制区域
            CreateNodesAreaView(rootView);
            //创建父级节点控制区域
            CreateParentNodesAreaView(rootView);
            //创建设置区域
            CreateSettingAreaView(rootView);

            //数据加载
            if (redDotKitConfig)
            {
                redDotKitConfigField.value = redDotKitConfig;
                RefreshView();
            }
        }

        // 刷新视图区域
        private void RefreshView()
        {
            //清理并刷新树选择滚动视图
            treeSelectScrollView.Clear();
            if (redDotKitConfig != null)
            {
                foreach (var tree in redDotKitConfig.RedDotTrees)
                {
                    AddScrollViewTreeItemButton(treeSelectScrollView, tree.treeName);
                }
            }

            //清理并刷新树节点滚动视图
            treeNodesScrollView.Clear();
            if (redDotKitConfig != null)
            {
                foreach (var tree in redDotKitConfig.RedDotTrees)
                {
                    if (tree.treeName == redDotTreeName)
                    {
                        foreach (var node in tree.nodeRelations)
                        {
                            AddScrollViewNodesItemButton(treeNodesScrollView, node.nodeKey);
                        }
                    }
                }
            }

            //清理并刷新父级节点滚动视图
            parentNodesScrollView.Clear();
            if (redDotKitConfig != null)
            {
                foreach (var tree in redDotKitConfig.RedDotTrees)
                {
                    if (tree.treeName == redDotTreeName)
                    {
                        foreach (var node in tree.nodeRelations)
                        {
                            if (node.nodeKey == currentSelectedRedDotKey)
                            {
                                foreach (var parent in node.parentKeys)
                                {
                                    AddScrollViewParentNodesItemButton(parentNodesScrollView, parent);
                                }
                            }
                        }
                    }
                }
            }

            //清理并刷新设置滚动视图
            settingScrollView.Clear();
            if (redDotKitConfig != null && redDotTreeName != null && currentSelectedRedDotKey != RedDotKey.None)
            {
                //创建设置内容
                CreateSettingControlContent();
            }
        }

        #region  红点树控制区域

        // 创建树控制区域
        private void CreateTreeAreaView(VisualElement visual)
        {
            CraeteControlAreaView(visual, out VisualElement treeControlArea);
            //创建树视图标题
            CreateControlAreaViewTitle(treeControlArea, "RedDotTree");
            //创建配置文件选择区域
            CreateConfigDataSelectContent(treeControlArea);
            //创建树区域
            CreateRedDotTreeControlContent(treeControlArea);
            //树选择滚动视图
            CreateDataScrollView(treeControlArea, out treeSelectScrollView);
        }

        // 创建树内容
        private void CreateRedDotTreeControlContent(VisualElement treeControlArea)
        {
            //创建树区域
            CreateControlContent(treeControlArea, out VisualElement createTreeContent);
            //创建控制功能标题
            CreateFieldTitle(createTreeContent, "Add Tree:");
            //创建树名称输入框
            CreateTextField(createTreeContent, ref treeNameTextField);
            //创建按钮
            CreateControlButton(createTreeContent, "+", out Button createTreeButton);
            createTreeButton.clicked += () =>
            {
                if (redDotKitConfig == null)
                {
                    Debug.LogError("请先选择配置文件.");
                    return;
                }
                else if (!string.IsNullOrWhiteSpace(treeNameTextField.value))
                {
                    // 检查树名是否已存在
                    if (redDotKitConfig.RedDotTrees.Exists(t => t.treeName == treeNameTextField.value))
                    {
                        Debug.LogError($"树 {treeNameTextField.value} 已经存在!");
                        return;
                    }

                    // 创建新树定义
                    var newTree = new RedDotKitConfig.TreeDefinition
                    {
                        treeName = treeNameTextField.value,
                        nodeRelations = new System.Collections.Generic.List<RedDotKitConfig.NodeRelation>()
                    };

                    // 添加到配置并保存
                    redDotKitConfig.RedDotTrees.Add(newTree);
                    EditorUtility.SetDirty(redDotKitConfig);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"已成功创建树: {treeNameTextField.value}");
                    AddScrollViewTreeItemButton(treeSelectScrollView, treeNameTextField.value);
                }
            };
        }

        // 创建配置文件选择区域
        private void CreateConfigDataSelectContent(VisualElement treeControlArea)
        {
            //创建配置文件选择区域
            CreateControlContent(treeControlArea, out VisualElement configDataContent);

            //创建配置文件标题
            Label configDataTitle = new Label("Config Data:");
            configDataTitle.AddToClassList("ControlContentTitle");
            configDataContent.Add(configDataTitle);
            //配置文件选择区域
            CreateObjectField(configDataContent, ref redDotKitConfigField, typeof(RedDotKitConfig));
            redDotKitConfigField.RegisterValueChangedCallback(evt =>
            {
                redDotKitConfig = (RedDotKitConfig)evt.newValue;
                RefreshView();
            });

            //创建按钮
            CreateControlButton(configDataContent, "R", out Button createConfigDataButton);
            createConfigDataButton.clicked += () => RefreshView();
        }

        #endregion

        #region 节点控制区域 

        // 创建节点控制区域
        private void CreateNodesAreaView(VisualElement rootView)
        {
            CraeteControlAreaView(rootView, out VisualElement nodesControlArea);
            CreateControlAreaViewTitle(nodesControlArea, "Nodes");
            //创建RedDotKey枚举值区域
            CreateRedDotKeyControlContent(nodesControlArea);
            //添加节点
            CreateAddNodeControlContent(nodesControlArea);
            //创建节点滚动视图
            CreateDataScrollView(nodesControlArea, out treeNodesScrollView);
        }

        // 创建红点Key控制区域
        private void CreateRedDotKeyControlContent(VisualElement visual)
        {
            //创建添加RedDotKey控制区域
            CreateControlContent(visual, out VisualElement addKeyControlContent);
            //创建控制功能标题
            CreateFieldTitle(addKeyControlContent, "Add Key:");
            //创建文本输入框
            CreateTextField(addKeyControlContent, ref redDotKeyTextField);
            //创建按钮
            CreateControlButton(addKeyControlContent, "+", out Button createControlButton);
            createControlButton.clicked += () =>
            {
                if (string.IsNullOrWhiteSpace(redDotKeyTextField.value))
                {
                    Debug.LogError("节点Key名称不能为空");
                    return;
                }

                // 验证Key名称格式
                if (!System.Text.RegularExpressions.Regex.IsMatch(redDotKeyTextField.value, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                {
                    Debug.LogError("节点Key名称必须以字母或下划线开头,且只能包含字母、数字和下划线" + redDotKeyTextField.value);
                    return;
                }

                // 使用输入的名称创建Key
                RedDotKey newKey = RedDotKitEditorHandler.CreateNodeKey(redDotKeyTextField.value);

                if (newKey == RedDotKey.None)
                {
                    Debug.LogError($"节点Key {redDotKeyTextField.value} 已存在");
                    return;
                }

                // 更新枚举字段显示
                addNodeEnumField.value = newKey;

                // 刷新UI
                addNodeEnumField.MarkDirtyRepaint();

                Debug.Log($"成功添加节点Key: {newKey}");
            };
        }

        // 添加节点控制区域
        private void CreateAddNodeControlContent(VisualElement visual)
        {
            //创建添加RedDotKey控制区域
            CreateControlContent(visual, out VisualElement addAddNodeControlContent);
            //创建控制功能标题
            CreateFieldTitle(addAddNodeControlContent, "Add Node:");
            //创建文本输入框
            CreateEnumField(addAddNodeControlContent, ref addNodeEnumField, typeof(RedDotKey));
            //添加节点按钮
            CreateControlButton(addAddNodeControlContent, "+", out Button addNodeButton);
            addNodeButton.clicked += () =>
            {
                var nodeKey = (RedDotKey)addNodeEnumField.value;
                var tree = redDotKitConfig?.RedDotTrees?.Find(t => t.treeName == redDotTreeName);

                if (tree == null)
                {
                    Debug.LogError($"找不到树: {redDotTreeName}");
                    return;
                }

                if (tree.nodeRelations.Exists(r => r.nodeKey == nodeKey))
                {
                    Debug.LogWarning($"节点 {nodeKey} 已存在于树 {redDotTreeName} 中");
                    return;
                }

                tree.nodeRelations.Add(new RedDotKitConfig.NodeRelation { nodeKey = nodeKey });

                EditorUtility.SetDirty(redDotKitConfig);
                AssetDatabase.SaveAssets();
                RefreshView();
            };
            //删除枚举按钮
            CreateControlButton(addAddNodeControlContent, "-", out Button deleteControlButton);
            deleteControlButton.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
            deleteControlButton.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除节点Key: '{addNodeEnumField.value.ToString()}' ?", "是", "否"))
                    return;
                RedDotKitEditorHandler.DeleteNodeKey(addNodeEnumField.value.ToString());
            };
        }

        // 添加滚动视图项
        private void AddScrollViewTreeItemButton(ScrollView scrollView, string title)
        {
            CreateScrollViewItemButton(scrollView, title, out Button itemButton, out Button deleteButton);
            //添加Item按钮事件监听
            itemButton.clicked += () =>
            {
                redDotTreeName = title;
                RefreshView();
                Debug.Log("当前红点树的名称:" + redDotTreeName);
            };
            //设置选择样式
            if (redDotTreeName == title)
            {
                itemButton.AddToClassList("ScrollViewItemButton-Select");
            }
            //添加删除按钮事件监听
            deleteButton.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除树 '{title}' ?", "是", "否"))
                    return;

                // 从配置中移除
                var treeToRemove = redDotKitConfig?.RedDotTrees?.Find(t => t.treeName == title);
                if (treeToRemove != null)
                {
                    redDotKitConfig.RedDotTrees.Remove(treeToRemove);
                    EditorUtility.SetDirty(redDotKitConfig);
                    AssetDatabase.SaveAssets();
                }
                // 刷新UI视图
                Debug.Log($"<color=red>删除树:</color>{title}");
                RefreshView();
            };
        }

        // 添加节点项按钮
        private void AddScrollViewNodesItemButton(ScrollView scrollView, RedDotKey redDotKey)
        {
            CreateScrollViewItemButton(scrollView, redDotKey.ToString(), out Button itemButton, out Button deleteButton);
            //添加Item按钮事件监听
            itemButton.clicked += () =>
            {
                currentSelectedRedDotKey = redDotKey;
                RefreshView();
            };
            //设置选择样式
            if (currentSelectedRedDotKey == redDotKey)
            {
                itemButton.AddToClassList("ScrollViewItemButton-Select");
            }
            //添加删除按钮事件监听
            deleteButton.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除节点 '{redDotKey.ToString()}' ?", "是", "否"))
                    return;

                //从配置中移除该节点
                var nodeRelationToRemove = redDotKitConfig?.RedDotTrees?.Find(t => t.treeName == redDotTreeName)?.nodeRelations?.Find(r => r.nodeKey == redDotKey);
                if (nodeRelationToRemove != null)
                {
                    redDotKitConfig.RedDotTrees.Find(t => t.treeName == redDotTreeName).nodeRelations.Remove(nodeRelationToRemove);
                    EditorUtility.SetDirty(redDotKitConfig);
                    AssetDatabase.SaveAssets();
                }

                // 刷新UI视图
                Debug.Log($"<color=red>删除节点:</color>{redDotKey.ToString()}");
                RefreshView();
            };
        }

        #endregion


        #region 父级节点控制区域

        //创建父级节点控制区域
        private void CreateParentNodesAreaView(VisualElement rootView)
        {
            CraeteControlAreaView(rootView, out VisualElement nodeParentsControlArea);
            CreateControlAreaViewTitle(nodeParentsControlArea, "Set Parents");
            //创建添加父级节点区域
            CreateParentNodeControlArea(nodeParentsControlArea);
            //创建父级节点滚动视图
            CreateDataScrollView(nodeParentsControlArea, out parentNodesScrollView);
        }

        // 创建父级节点控制区域
        private void CreateParentNodeControlArea(VisualElement visual)
        {
            //创建添加RedDotKey控制区域
            CreateControlContent(visual, out VisualElement parentNodesControlArea);
            //创建控制功能标题
            CreateFieldTitle(parentNodesControlArea, "Add Parent Node:");
            //创建文本输入框
            CreateEnumField(parentNodesControlArea, ref addParentNodeEnumField, typeof(RedDotKey));
            //添加节点按钮
            CreateControlButton(parentNodesControlArea, "+", out Button addParentNodeButton);
            addParentNodeButton.clicked += () =>
            {
                if (redDotTreeName != null && currentSelectedRedDotKey != RedDotKey.None)
                {
                    var parentKey = (RedDotKey)addParentNodeEnumField.value;

                    // 不能设置自己为父节点
                    if (parentKey == currentSelectedRedDotKey)
                    {
                        Debug.LogError("不能设置自己为自己的父节点");
                        return;
                    }

                    var tree = redDotKitConfig.RedDotTrees.Find(t => t.treeName == redDotTreeName);
                    if (tree != null)
                    {
                        // 父节点必须是当前树中存在的节点
                        if (!tree.nodeRelations.Exists(r => r.nodeKey == parentKey))
                        {
                            Debug.LogError($"节点 {parentKey} 不存在于当前树中，不能设置为父节点");
                            return;
                        }

                        var nodeRelation = tree.nodeRelations.Find(r => r.nodeKey == currentSelectedRedDotKey);
                        if (nodeRelation != null)
                        {
                            nodeRelation.parentKeys.Add(parentKey);
                            EditorUtility.SetDirty(redDotKitConfig);
                            AssetDatabase.SaveAssets();
                            RefreshView();
                        }
                    }
                }
            };
        }

        // 添加父级节点项按钮
        private void AddScrollViewParentNodesItemButton(ScrollView scrollView, RedDotKey redDotKey)
        {
            CreateScrollViewItemButton(scrollView, redDotKey.ToString(), out Button _, out Button deleteButton);
            //添加删除按钮事件监听
            deleteButton.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除该父节点 '{redDotKey.ToString()}' ?", "是", "否"))
                    return;

                //从配置中移除该父级节点
                var tree = redDotKitConfig?.RedDotTrees?.Find(t => t.treeName == redDotTreeName);
                if (tree != null)
                {
                    var nodeRelation = tree.nodeRelations.Find(r => r.nodeKey == currentSelectedRedDotKey);
                    if (nodeRelation != null)
                    {
                        nodeRelation.parentKeys.Remove(redDotKey);
                        EditorUtility.SetDirty(redDotKitConfig);
                        AssetDatabase.SaveAssets();
                    }
                }

                // 刷新UI视图
                Debug.Log($"<color=red>删除父节点:</color>{redDotKey.ToString()}");
                RefreshView();
            };
        }

        #endregion

        #region 设置区域

        // 创建设置区域
        private void CreateSettingAreaView(VisualElement rootView)
        {
            CraeteControlAreaView(rootView, out VisualElement settingControlArea);
            CreateControlAreaViewTitle(settingControlArea, "Settings");
            //创建设置滚动视图
            CreateDataScrollView(settingControlArea, out settingScrollView);
            //创建控制类容
            CreateSettingControlContent();
        }

        private void CreateSettingControlContent()
        {
            //创建设置根节点区域
            CreateSetRootKeyControlArea(settingScrollView);
            //设置红点数量
            SetRedDotCount(settingScrollView);
            //设置是否显示红点数量状态
            SetRedDotCountState(settingScrollView);
            //初始化红点树数据
            CreateNormalButton(settingScrollView, "Init Red Dot Tree", () =>
            {
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("请在运行时初始化红点树");
                    return;
                }
                else if (redDotKitConfig != null) RedDotKit.InitRedDotTree(redDotKitConfig);
            });

            //刷新视图
            CreateNormalButton(settingScrollView, "Refresh View", RefreshView);
            //保存数据
            CreateNormalButton(settingScrollView, "Save Data", () =>
            {
                if (redDotKitConfig != null) redDotKitConfig.SaveData();
            });
            //保存数据到JSON文件
            CreateNormalButton(settingScrollView, "Save Data To JSON", () =>
            {
                if (redDotKitConfig != null) redDotKitConfig.SaveToJson();
            });
            //从JSON文件中加载数据
            CreateNormalButton(settingScrollView, "Load Data From JSON", () =>
            {
                if (redDotKitConfig != null) redDotKitConfig.LoadFromJson();
            });
        }

        //设置是否显示红点数量
        private void SetRedDotCountState(VisualElement visual)
        {
            //创建设置区域
            CreateControlContent(visual, out VisualElement setIsShowRedDotCountControlContent);
            //创建控制功能标题
            CreateFieldTitle(setIsShowRedDotCountControlContent, "Set Is Show RedDot Count:");
            //创建Toggle
            Toggle toggle = new Toggle();
            toggle.AddToClassList("Toggle");
            // 从当前选中节点获取显示状态
            if (redDotKitConfig != null && redDotTreeName != null && currentSelectedRedDotKey != RedDotKey.None)
            {
                var tree = redDotKitConfig.RedDotTrees.Find(t => t.treeName == redDotTreeName);
                if (tree != null)
                {
                    var node = tree.nodeRelations.Find(n => n.nodeKey == currentSelectedRedDotKey);
                    if (node != null)
                    {
                        toggle.value = node.isShowRedDotCount;
                    }
                }
            }

            setIsShowRedDotCountControlContent.Add(toggle);
            //添加设置按钮
            CreateControlButton(setIsShowRedDotCountControlContent, ">", out Button SetIsShowRedDotCount);
            SetIsShowRedDotCount.clicked += () =>
            {
                if (redDotKitConfig != null && redDotTreeName != null && currentSelectedRedDotKey != RedDotKey.None)
                {
                    var tree = redDotKitConfig.RedDotTrees.Find(t => t.treeName == redDotTreeName);
                    if (tree != null)
                    {
                        var node = tree.nodeRelations.Find(n => n.nodeKey == currentSelectedRedDotKey);
                        if (node != null)
                        {
                            node.isShowRedDotCount = toggle.value;
                            EditorUtility.SetDirty(redDotKitConfig);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            };
        }

        // 创建设置根节点区域
        private void CreateSetRootKeyControlArea(VisualElement visual)
        {
            //创建设置区域
            CreateControlContent(visual, out VisualElement setRootKeyControlContent);
            //创建控制功能标题
            CreateFieldTitle(setRootKeyControlContent, "Set Tree Root Key:");
            //创建文本输入框
            CreateEnumField(setRootKeyControlContent, ref setRootKeyEnumField, typeof(RedDotKey));
            RedDotKitConfig.TreeDefinition tree = null;
            if (redDotKitConfig != null && redDotTreeName != null)
            {
                tree = redDotKitConfig.RedDotTrees.Find(t => t.treeName == redDotTreeName);
                setRootKeyEnumField.value = tree.rootKey;
            }
            //添加节点按钮
            CreateControlButton(setRootKeyControlContent, ">", out Button setRootKeyButton);
            setRootKeyButton.clicked += () =>
            {
                if (tree != null)
                {
                    tree.rootKey = (RedDotKey)setRootKeyEnumField.value;
                    EditorUtility.SetDirty(redDotKitConfig);
                    AssetDatabase.SaveAssets();
                }
            };
        }

        // 设置红点数量
        private void SetRedDotCount(VisualElement visual)
        {
            //设置当前节点的默认数量
            CreateControlContent(visual, out VisualElement setCountControlContent);
            //创建控制功能标题
            CreateFieldTitle(setCountControlContent, "Set RedDot Count:");
            RedDotKitConfig.NodeRelation nodeRelation = null;
            if (redDotTreeName != null && currentSelectedRedDotKey != RedDotKey.None)
            {
                var tree = redDotKitConfig.RedDotTrees.Find(t => t.treeName == redDotTreeName);
                if (tree != null)
                {
                    nodeRelation = tree.nodeRelations.Find(r => r.nodeKey == currentSelectedRedDotKey);
                }
            }
            //创建文本输入框
            CreateTextField(setCountControlContent, ref redDotCountTextField);
            if (redDotKitConfig != null && redDotTreeName != null)
                redDotCountTextField.value = nodeRelation.redDotCount.ToString();
            //创建按钮
            CreateControlButton(setCountControlContent, ">", out Button setRedDotCountButton);
            setRedDotCountButton.clicked += () =>
            {
                if (nodeRelation != null)
                {
                    nodeRelation.redDotCount = int.Parse(redDotCountTextField.value);
                    EditorUtility.SetDirty(redDotKitConfig);
                    AssetDatabase.SaveAssets();
                }
            };
        }

        #endregion

        #region 公共

        // 创建控制区域
        private void CreateControlContent(VisualElement visual, out VisualElement controlContent)
        {
            controlContent = new VisualElement();
            controlContent.AddToClassList("ControlContent");
            visual.Add(controlContent);
        }

        // 创建标题
        private void CreateFieldTitle(VisualElement visual, string titleName)
        {
            //创建配置文件标题
            Label title = new Label(titleName);
            title.AddToClassList("ControlContentTitle");
            visual.Add(title);
        }

        // 创建文本输入框(带标题)
        private void CreateTextField(VisualElement visual, ref TextField textField)
        {
            //文本输入框
            textField = new TextField();
            textField.AddToClassList("Field");
            visual.Add(textField);
        }

        // 创建文件选择区域
        private void CreateObjectField(VisualElement visual, ref ObjectField objectField, Type type)
        {
            //创建配置文件选择
            objectField = new ObjectField();
            objectField.AddToClassList("Field");
            objectField.objectType = type;
            visual.Add(objectField);
        }

        // 创建枚举选择区域
        private void CreateEnumField(VisualElement visual, ref EnumField enumField, Type enumType, Enum defaultValue = null)
        {
            // 创建枚举字段选择
            enumField = new EnumField();
            enumField.AddToClassList("Field");
            enumField.Init(defaultValue ?? (Enum)Activator.CreateInstance(enumType));
            visual.Add(enumField);
        }

        // 创建控制按钮
        private void CreateControlButton(VisualElement visual, string titleName, out Button button)
        {
            button = new Button() { text = titleName };
            button.AddToClassList("Button");
            visual.Add(button);
        }

        // 创建控制区域视图
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

        //  添加滚动视图项按钮
        private bool CreateScrollViewItemButton(ScrollView scrollView, string title, out Button itemButton, out Button deleteButton)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Debug.Log("<color=red>树的名称不能为 null 或充满空格。.</color>");
                itemButton = null;
                deleteButton = null;
                return false;
            }

            //创建滚动视图项
            VisualElement item = new VisualElement();
            item.AddToClassList("ControlContent");
            scrollView.Add(item);

            //创建按钮
            itemButton = new Button();
            itemButton.AddToClassList("ScrollViewItemButton");
            itemButton.text = title;
            item.Add(itemButton);

            //创建删除按钮
            deleteButton = new Button();
            deleteButton.AddToClassList("ScrollViewItemDeleteButton");
            deleteButton.text = "X";
            //添加删除按钮事件监听
            deleteButton.clicked += () => scrollView.Remove(item);
            item.Add(deleteButton);
            return true;
        }

        // 创建普通按钮
        private void CreateNormalButton(VisualElement visual, string titleName, Action action)
        {
            //创建按钮
            Button normalButton = new Button();
            normalButton.AddToClassList("ScrollViewItemButton");
            normalButton.text = titleName;
            normalButton.clicked += () => action();
            visual.Add(normalButton);
        }

        #endregion
    }
}