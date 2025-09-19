using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System;

namespace AssetBundleToolEditor
{
    /// <summary>
    /// AB包控制面板视图
    /// </summary>
    public class AssetBundlesDataView : VisualElement
    {
        #region UI元素声明
        private VisualElement mainContent;
        private Label abListContent;
        private Label abItemContent;
        private ScrollView assetBundlesScrollView;
        private ScrollView assetBundleItemScrollView;
        private EnumField typeEnumField;
        private Label assetBundleSizeField;
        #endregion

        #region 常量定义
        private static readonly Color HighlightColor = new Color(0.984f, 0.753f, 0.431f);
        private static readonly Color DisabledColor = Color.gray;
        private static readonly Color DefaultColor = Color.white;

        private static readonly string[] UnsupportedExtensions =
        {
            ".cs", ".shader", ".asmdef", ".unity"
        };
        #endregion

        #region 初始化
        /// <summary>
        /// AB包控制区域初始化
        /// </summary>
        public void Init(VisualElement visual)
        {
            CreateMainContent();
            SetupABListSection();
            SetupABItemSection();
            UpdateAssetBundlesDataItem();
            visual.Add(mainContent);
        }

        private void CreateMainContent()
        {
            mainContent = new VisualElement();
            mainContent.styleSheets.Add(Resources.Load<StyleSheet>("USS/AssetBundlesDataView"));
            mainContent.AddToClassList("MainContent");
        }

        private void SetupABListSection()
        {
            ABListContent(mainContent);
            ABSearchContent(abListContent);
            ABScrollViewContent(abListContent);
        }

        private void SetupABItemSection()
        {
            ABItemContent(mainContent);
            ABItemSearchContent(abItemContent);
            ABItemScrollViewContent(abItemContent);
        }
        #endregion

        #region AB包列表相关
        private void ABListContent(VisualElement visual)
        {
            abListContent = new Label();
            abListContent.AddToClassList("ABListContent");
            visual.Add(abListContent);
        }

        private void ABSearchContent(VisualElement visual)
        {
            var searchContainer = CreateSearchContainer();
            var searchInput = CreateSearchInput();
            SetupABSearchLogic(searchInput);

            searchContainer.Add(searchInput);
            visual.Add(searchContainer);
        }

        private Label CreateSearchContainer()
        {
            var container = new Label();
            container.AddToClassList("ABCreateSearchLabel");
            return container;
        }

        private TextField CreateSearchInput()
        {
            var searchInput = new TextField();
            searchInput.AddToClassList("ABSearchInput");

            var textElement = searchInput.Q<TextElement>(className: "unity-text-element");
            if (textElement != null)
                textElement.style.marginLeft = 18;

            var icon = new Label();
            icon.AddToClassList("SearchIcon");
            searchInput.Add(icon);

            return searchInput;
        }

        private void SetupABSearchLogic(TextField searchInput)
        {
            searchInput.RegisterValueChangedCallback(evt =>
            {
                FilterAssetBundleGroups(evt.newValue);
            });
        }

        private void FilterAssetBundleGroups(string searchText)
        {
            var groups = AssetBundleEditorData.currentABConfig?.AssetBundleList;

            if (string.IsNullOrEmpty(searchText))
            {
                AssetBundleEditorData.currentFilteredGroups = null;
            }
            else
            {
                var filteredGroups = groups?
                    .Where(g => g.assetBundleName.ToLower().Contains(searchText.ToLower()))
                    .ToList();
                AssetBundleEditorData.currentFilteredGroups = filteredGroups;
            }

            UpdateAssetBundlesItem();
        }

        private void ABScrollViewContent(VisualElement visual)
        {
            assetBundlesScrollView = new ScrollView();
            assetBundlesScrollView.AddToClassList("ABListScrollView");
            assetBundlesScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            UpdateAssetBundlesItem();
            visual.Add(assetBundlesScrollView);
        }
        #endregion

        #region AB包项相关
        private void ABItemContent(VisualElement visual)
        {
            abItemContent = new Label();
            abItemContent.AddToClassList("ABItemListContent");
            visual.Add(abItemContent);
        }

        private void ABItemSearchContent(VisualElement visual)
        {
            var searchContainer = CreateItemSearchContainer();
            var searchInput = CreateItemSearchInput();

            // 先添加搜索类型控件
            CreateSearchTypeControl(searchContainer);

            // 然后立即添加搜索框
            searchContainer.Add(searchInput);

            // 再添加其他控件
            CreateItemShowTypeControl(searchContainer);
            CreateQuantityStatistics(searchContainer);
            CreateRefreshButton(searchContainer);
            CreateDebugDependencyCollectionButton(searchContainer);

            // 最后设置搜索逻辑
            SetupItemSearchLogic(searchInput, searchContainer);

            visual.Add(searchContainer);
            ABItemDataInfoBar(visual);
        }

        private Label CreateItemSearchContainer()
        {
            var container = new Label();
            container.AddToClassList("ABItemSearchContent");
            return container;
        }

        private TextField CreateItemSearchInput()
        {
            var searchInput = new TextField();
            searchInput.AddToClassList("ABSearchInput");

            var textElement = searchInput.Q<TextElement>(className: "unity-text-element");
            if (textElement != null)
                textElement.style.marginLeft = 18;

            var icon = new Label();
            icon.AddToClassList("SearchIcon");
            searchInput.Add(icon);

            return searchInput;
        }

        // 搜索逻辑
        private void SetupItemSearchLogic(TextField searchInput, VisualElement container)
        {
            if (AssetBundleEditorData.currentAssetBundleGroup != null)
            {
                searchInput.RegisterValueChangedCallback(evt =>
                {
                    if (string.IsNullOrEmpty(evt.newValue))
                    {
                        AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                        if (typeEnumField != null)
                            typeEnumField.value = ABItemSearchType.Self;
                    }
                    UpdateAssetBundlesDataItem(evt.newValue);
                });
            }
        }
        #endregion

        #region 控件创建方法
        private void CreateSearchTypeControl(VisualElement visual)
        {
            typeEnumField = new EnumField(ABItemSearchType.Self);
            typeEnumField.AddToClassList("ABItemSearchTypeEnumField");
            typeEnumField.tooltip = "搜索类型(自己/全部)";

            typeEnumField.RegisterValueChangedCallback(evt =>
            {
                AssetBundleEditorData.currentABItemSearchType = (ABItemSearchType)evt.newValue;
            });

            visual.Add(typeEnumField);
        }

        private void CreateItemShowTypeControl(VisualElement visual)
        {
            var limitTypeEnumField = new EnumField(ABItemShowType.All);
            limitTypeEnumField.AddToClassList("LimitItemShowTypeEnumField");
            limitTypeEnumField.tooltip = "显示类型";

            SetInitialShowType(limitTypeEnumField);
            SetupShowTypeChangeHandler(limitTypeEnumField, visual);

            visual.Add(limitTypeEnumField);
        }

        private void SetInitialShowType(EnumField limitTypeEnumField)
        {
            if (AssetBundleEditorData.currentAsset?.AssetsObject != null)
            {
                string assetType = AssetBundleEditorData.currentAsset.AssetsObject.GetType().Name;
                if (Enum.TryParse(assetType, out ABItemShowType showType))
                    limitTypeEnumField.value = showType;
            }
        }

        private void SetupShowTypeChangeHandler(EnumField limitTypeEnumField, VisualElement visual)
        {
            limitTypeEnumField.RegisterValueChangedCallback(evt =>
            {
                AssetBundleEditorData.currentABItemShowType = (ABItemShowType)evt.newValue;

                var searchInput = visual.parent.Q<TextField>();
                if (searchInput != null && string.IsNullOrEmpty(searchInput.value))
                {
                    AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                    if (typeEnumField != null)
                        typeEnumField.value = ABItemSearchType.Self;
                }

                UpdateAssetBundlesDataItem();
            });
        }

        private void CreateQuantityStatistics(VisualElement visual)
        {
            var count = new Label();
            count.AddToClassList("ABItemQuantityStatistics");

            if (AssetBundleEditorData.currentAssetBundleGroup?.assets != null)
            {
                count.text = "Count:" + AssetBundleEditorData.currentAssetBundleGroup.assets.Count;
            }

            visual.Add(count);
        }

        private void CreateRefreshButton(VisualElement visual)
        {
            var refreshButton = new Button();
            refreshButton.AddToClassList("RefreshABItemDataButton");
            refreshButton.tooltip = "刷新面板数据";
            refreshButton.clicked += RefreshAllPanelData;
            visual.Add(refreshButton);
        }

        private void CreateDebugDependencyCollectionButton(VisualElement visual)
        {
            var debugButton = new Button();
            debugButton.AddToClassList("DebugDependencyCollectionButton");
            debugButton.tooltip = "测试AssetBundle组依赖项收集";
            debugButton.clicked += () =>
            {
                AssetBundleEditorData.currentABConfig.DebugDependencyCollection(AssetBundleEditorData.currentAssetBundleGroup);
            };
            visual.Add(debugButton);
        }

        #endregion

        #region 数据刷新
        /// <summary>
        /// 刷新所有面板数据
        /// </summary>
        private void RefreshAllPanelData()
        {
            try
            {
                Debug.Log("<color=cyan>开始刷新面板数据...</color>");
                ValidateAndCleanAssets();
                ResetSearchTypeToSelf();
                UpdateAllDisplays();

                Debug.Log("<color=green>面板数据刷新完成!</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>刷新面板数据时出错: {ex.Message}</color>");
                Debug.LogException(ex);
            }
        }

        private void ResetSearchTypeToSelf()
        {
            if (AssetBundleEditorData.currentAssetBundleGroup != null)
            {
                AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                if (typeEnumField != null)
                    typeEnumField.value = ABItemSearchType.Self;
            }
        }

        private void UpdateAllDisplays()
        {
            UpdateAssetSizeDisplay();
            UpdateAssetBundlesItem();
            UpdateAssetBundlesDataItem();
            UpdateItemQuantityStatistics();
        }
        #endregion

        #region 资源验证
        /// <summary>
        /// 验证并清理无效资源
        /// </summary>
        private void ValidateAndCleanAssets()
        {
            if (AssetBundleEditorData.currentABConfig?.AssetBundleList == null)
                return;

            int removedCount = 0;
            var groupsToRemove = new List<AssetBundleGroup>();

            foreach (var group in AssetBundleEditorData.currentABConfig.AssetBundleList)
            {
                removedCount += ValidateGroup(group, groupsToRemove);
            }

            RemoveInvalidGroups(groupsToRemove);

            if (removedCount > 0)
            {
                Debug.Log($"<color=yellow>已清理 {removedCount} 个无效资源</color>");
                EditorUtility.SetDirty(AssetBundleEditorData.currentABConfig);
            }
        }

        private int ValidateGroup(AssetBundleGroup group, List<AssetBundleGroup> groupsToRemove)
        {
            if (group?.assets == null)
            {
                groupsToRemove.Add(group);
                return 0;
            }

            var assetsToRemove = new List<AssetBundleAssetsData>();

            foreach (var asset in group.assets)
            {
                if (!IsAssetValid(asset))
                {
                    assetsToRemove.Add(asset);
                }
            }

            // 移除无效资源
            foreach (var asset in assetsToRemove)
            {
                group.assets.Remove(asset);
            }

            if (group.assets.Count == 0)
            {
                Debug.LogWarning($"<color=yellow>AB包组 '{group.assetBundleName}' 已无有效资源，建议删除</color>");
            }

            return assetsToRemove.Count;
        }

        private bool IsAssetValid(AssetBundleAssetsData asset)
        {
            if (asset?.AssetsObject == null || string.IsNullOrEmpty(asset.assetPath))
                return false;

            if (!File.Exists(asset.assetPath))
            {
                Debug.LogWarning($"<color=yellow>资源文件不存在，已移除: {asset.assetPath}</color>");
                return false;
            }

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.assetPath);
            if (obj == null)
            {
                Debug.LogWarning($"<color=yellow>无法加载资源，已移除: {asset.assetPath}</color>");
                return false;
            }

            // 更新资源引用
            if (asset.AssetsObject != obj)
            {
                asset.AssetsObject = obj;
                asset.assetName = obj.name;
            }

            return true;
        }

        private void RemoveInvalidGroups(List<AssetBundleGroup> groupsToRemove)
        {
            foreach (var group in groupsToRemove)
            {
                AssetBundleEditorData.currentABConfig.AssetBundleList.Remove(group);
                Debug.LogWarning($"<color=yellow>已移除空的AB包组: {group?.assetBundleName ?? "未命名"}</color>");
            }
        }
        #endregion

        #region 更新方法
        private void UpdateAssetSizeDisplay()
        {
            if (assetBundleSizeField != null)
            {
                assetBundleSizeField.text = "Size: " + GetTotalAssetsSize();
            }
        }

        private void UpdateItemQuantityStatistics()
        {
            var countLabel = mainContent?.Q<Label>(className: "ABItemQuantityStatistics");
            if (countLabel != null && AssetBundleEditorData.currentAssetBundleGroup != null)
            {
                int count = AssetBundleEditorData.currentABItemSearchType == ABItemSearchType.All
                    ? AssetBundleEditorData.currentABConfig?.AssetBundleList?.SelectMany(g => g.assets).Count() ?? 0
                    : AssetBundleEditorData.currentAssetBundleGroup.assets?.Count ?? 0;

                countLabel.text = $"Count:{count}";
            }
        }
        #endregion

        #region 拖拽处理
        private void ABItemScrollViewContent(VisualElement visual)
        {
            assetBundleItemScrollView = new ScrollView();
            assetBundleItemScrollView.AddToClassList("ABItemScrollView");
            assetBundleItemScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            assetBundleItemScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

            SetupDragAndDrop();
            visual.Add(assetBundleItemScrollView);
        }

        private void SetupDragAndDrop()
        {
            assetBundleItemScrollView.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            });

            assetBundleItemScrollView.RegisterCallback<DragPerformEvent>(HandleDragPerform);
        }

        private void HandleDragPerform(DragPerformEvent evt)
        {
            if (AssetBundleEditorData.currentAssetBundleGroup == null)
                return;

            foreach (var draggedObj in DragAndDrop.objectReferences)
            {
                ProcessDraggedObject(draggedObj);
            }

            UpdateAssetBundlesDataItem();
            assetBundleItemScrollView.MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void ProcessDraggedObject(UnityEngine.Object draggedObj)
        {
            var assetPath = AssetDatabase.GetAssetPath(draggedObj);
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                ProcessFolder(assetPath);
            }
            else
            {
                ProcessAsset(assetPath);
            }
        }

        private void ProcessFolder(string folderPath)
        {
            var allAssets = AssetDatabase
                .FindAssets("", new[] { folderPath })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => !AssetDatabase.IsValidFolder(path));

            foreach (var path in allAssets)
            {
                ProcessAsset(path);
            }
        }
        #endregion

        #region 资源处理
        private void ProcessAsset(string assetPath)
        {
            if (!IsAssetTypeSupported(assetPath) || IsAssetAlreadyExists(assetPath))
                return;

            AddAssetToCurrentGroup(assetPath);
        }

        private bool IsAssetTypeSupported(string assetPath)
        {
            string extension = Path.GetExtension(assetPath).ToLower();

            if (UnsupportedExtensions.Contains(extension))
            {
                Debug.LogWarning($"不支持将{extension}文件打包到AssetBundle: {assetPath}");
                return false;
            }

            return true;
        }

        private bool IsAssetAlreadyExists(string assetPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            var existingGroups = AssetBundleEditorData.currentABConfig.AssetBundleList
                .Where(g => g.assets.Any(a => AssetDatabase.AssetPathToGUID(a.assetPath) == guid))
                .ToList();

            if (existingGroups.Count > 0)
            {
                string groupNames = string.Join(", ", existingGroups.Select(g => g.assetBundleName));
                Debug.LogWarning($"资源:{assetPath}<color=yellow>已存在.</color>\n所在AB包组-> {groupNames}");
                return true;
            }

            return false;
        }

        private void AddAssetToCurrentGroup(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            var newAsset = new AssetBundleAssetsData
            {
                assetName = asset.name,
                assetPath = assetPath,
                AssetsObject = asset,
            };

            AssetBundleEditorData.currentAssetBundleGroup.assets.Add(newAsset);
        }
        #endregion

        //AB包项数据信息
        private void ABItemDataInfoBar(VisualElement visual)
        {
            Label dataInfo = new Label();
            dataInfo.AddToClassList("ABItemDataInfoBar");
            DataInfoItem(dataInfo, "Asset Name", out Label _);
            DataInfoItem(dataInfo, "Size", out assetBundleSizeField, GetTotalAssetsSize());
            DataInfoItem(dataInfo, "Path", out Label _);
            DataInfoItem(dataInfo, "Type", out Label _);
            DataInfoItem(dataInfo, "Controller", out Label _);
            visual.Add(dataInfo);
        }

        //数据信息项
        private void DataInfoItem(
            VisualElement visual,
            string dataName,
            out Label dataInfo,
            string attach = null
        )
        {
            dataInfo = new Label();
            //title
            if (attach != null)
                dataInfo.text = $"{dataName}: {attach}";
            else
                dataInfo.text = dataName;
            dataInfo.AddToClassList("ABItemDataInfo");
            visual.Add(dataInfo);
        }

        // AddAssetBundles 方法
        private void AddAssetBundles(VisualElement visual, AssetBundleGroup assetBundleGroup, bool isSelect = false)
        {
            Button assetBundleItem = new Button();
            assetBundleItem.AddToClassList("DefaultAssetBundleItem");
            if (isSelect)
                assetBundleItem.AddToClassList("SelectAssetBundleItem");

            // 当前AssetBundle是否构建
            Label icon = new Label();
            icon.AddToClassList("AssetBundleGroupIcon");
            icon.style.unityBackgroundImageTintColor = assetBundleGroup.isEnableBuild ? DefaultColor : DisabledColor;
            assetBundleItem.Add(icon);

            // 标题
            Label title = new Label();
            title.AddToClassList("AssetBundleItemTitle");
            title.text = assetBundleGroup.assetBundleName;
            assetBundleItem.Add(title);

            // 提示图标区域
            VisualElement tipsIconArea = new VisualElement();
            tipsIconArea.AddToClassList("TipsIconArea");

            // 图标生成方法，减少重复
            void AddTipIcon(string iconName, bool highlight, Color? elseColor = null)
            {
                Label tipIcon = new Label();
                tipIcon.AddToClassList("TipsIcon");
                tipIcon.style.backgroundImage = new StyleBackground(
                    Resources.Load<Texture2D>($"Icon/{iconName}")
                );
                tipIcon.style.unityBackgroundImageTintColor = highlight
                    ? HighlightColor
                    : (elseColor ?? DisabledColor);
                tipsIconArea.Add(tipIcon);
            }

            // 保存到本地
            AddTipIcon("LocalData", assetBundleGroup.buildPathType == BuildPathType.Local);
            // 保存到远端
            AddTipIcon("RemoteData", assetBundleGroup.buildPathType == BuildPathType.Remote);
            // 是否启用拆分AB包
            AddTipIcon("AssetBundlePackSeparately", assetBundleGroup.isEnablePackSeparately);
            // 是否启用可寻址资源定位系统
            AddTipIcon("ResourceAddressing", assetBundleGroup.isEnableAddressable);

            assetBundleItem.Add(tipsIconArea);

            // 点击事件
            assetBundleItem.clicked += () =>
            {
                AssetBundleEditorData.currentAssetBundleGroup = assetBundleGroup;
                AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                UpdateAssetBundlesItem();
                UpdateAssetBundlesDataItem();
            };

            // TODO：设置ABGroup按钮
            Button moreOptions = new Button();
            moreOptions.AddToClassList("ABGroupMoreOptionsButton");
            moreOptions.clicked += () =>
            {
                // 下拉列表
                ShowDropdownMenu(moreOptions);
            };
            assetBundleItem.Add(moreOptions);

            visual.Add(assetBundleItem);
        }

        private void ShowDropdownMenu(VisualElement button)
        {
            GenericMenu menu = new GenericMenu();

            var assetBundleGroup = AssetBundleEditorData.currentAssetBundleGroup;
            // 获取当前AB包组是否可以构建
            bool isEnableBuild = assetBundleGroup.isEnableBuild;
            // 根据当前是否可以构建状态显示相反操作
            if (isEnableBuild)
            {
                menu.AddDisabledItem(new GUIContent("√当前为可构建状态"));
                menu.AddItem(new GUIContent("设置为不可构建"), false, () =>
                {
                    assetBundleGroup.isEnableBuild = false;
                    UpdateAssetBundlesItem();
                });
            }
            else
            {
                menu.AddItem(new GUIContent("设置为可构建"), false, () =>
                {
                    assetBundleGroup.isEnableBuild = true;
                    UpdateAssetBundlesItem();
                });
                menu.AddDisabledItem(new GUIContent("√当前为不可构建状态"));
            }
            // 分割线和删除选项
            menu.AddSeparator("");

            // 当前AB包组是本地构建还是远端构建
            var buildPathType = assetBundleGroup.buildPathType;
            if (buildPathType == BuildPathType.Local)
            {
                menu.AddDisabledItem(new GUIContent("√当前构建路径类型为Local"));
                menu.AddItem(new GUIContent("设置构建路径类型为Remote"), false, () =>
                {
                    assetBundleGroup.buildPathType = BuildPathType.Remote;
                    UpdateAssetBundlesItem();
                });
            }
            else if (buildPathType == BuildPathType.Remote)
            {
                menu.AddItem(new GUIContent("设置构建路径类型为Local"), false, () =>
                {
                    assetBundleGroup.buildPathType = BuildPathType.Local;
                    UpdateAssetBundlesItem();
                });
                menu.AddDisabledItem(new GUIContent("√当前构建路径类型为Remote"));
            }
            menu.AddSeparator("");

            // 是否分割AB包文件 - 构建时一个文件一个AB包
            bool isEnablePackSeparately = assetBundleGroup.isEnablePackSeparately;
            if (isEnablePackSeparately)
            {
                menu.AddDisabledItem(new GUIContent("√使用单文件AB包构建"));
                menu.AddItem(new GUIContent("设置为不使用单文件AB包构建"), false, () =>
                {
                    assetBundleGroup.isEnablePackSeparately = false;
                    UpdateAssetBundlesItem();
                });
            }
            else
            {
                menu.AddItem(new GUIContent("设置为使用单文件AB包构建"), false, () =>
                {
                    assetBundleGroup.isEnablePackSeparately = true;
                    UpdateAssetBundlesItem();
                });
                menu.AddDisabledItem(new GUIContent("√不使用单文件AB包构建"));
            }
            menu.AddSeparator("");

            // 是否启用可寻址资源定位系统
            bool isEnableAddressable = assetBundleGroup.isEnableAddressable;
            if (isEnableAddressable)
            {
                menu.AddDisabledItem(new GUIContent("√使用可寻址资源定位系统"));
                menu.AddItem(new GUIContent("设置为不使用可寻址资源定位系统"), false, () =>
                {
                    assetBundleGroup.isEnableAddressable = false;
                    UpdateAssetBundlesItem();
                });
            }
            else
            {
                menu.AddItem(new GUIContent("设置为使用可寻址资源定位系统"), false, () =>
                {
                    assetBundleGroup.isEnableAddressable = true;
                    UpdateAssetBundlesItem();
                });
                menu.AddDisabledItem(new GUIContent("√不使用可寻址资源定位系统"));
            }
            menu.AddSeparator("");

            // 是否使用AB包名作为前缀
            bool prefixIsAssetBundleName = assetBundleGroup.prefixIsAssetBundleName;
            if (prefixIsAssetBundleName)
            {
                menu.AddDisabledItem(new GUIContent("√使用AB包名为前缀"));
                menu.AddItem(new GUIContent("设置为不使用AB包名为前缀"), false, () =>
                {
                    assetBundleGroup.prefixIsAssetBundleName = false;
                    UpdateAssetBundlesItem();
                });
            }
            else
            {
                menu.AddItem(new GUIContent("设置为使用AB包名为前缀"), false, () =>
                {
                    assetBundleGroup.prefixIsAssetBundleName = true;
                    UpdateAssetBundlesItem();
                });
                menu.AddDisabledItem(new GUIContent("√不使用AB包名为前缀"));
            }

            menu.AddSeparator("");
            // 轨道重命名选项
            menu.AddItem(new GUIContent("更改当前轨道名称"), false, () =>
            {
                UpdateAssetBundlesItem();
            });

            menu.AddItem(new GUIContent("删除当前轨道"), false, () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除AssetBundle组: '{assetBundleGroup.assetBundleName}' ?", "是", "否"))
                    return;
                AssetBundleEditorData.currentABConfig.AssetBundleList.Remove(assetBundleGroup);
                UpdateAssetBundlesItem();
            });

            // 在按钮正下方显示菜单
            var rect = button.worldBound;
            menu.DropDown(new Rect(rect.x, rect.yMax, 0, 0));
        }

        //AB包滚动区域
        private void UpdateAssetBundlesItem()
        {
            assetBundlesScrollView.Clear();
            var groupsToShow =
                AssetBundleEditorData.currentFilteredGroups
                ?? AssetBundleEditorData.currentABConfig?.AssetBundleList;
            if (groupsToShow == null || groupsToShow.Count == 0)
            {
                //添加增减AB包组按钮
                AddAssetBundleGroup(assetBundlesScrollView);
                return;
            }
            if (AssetBundleEditorData.currentAssetBundleGroup == null)
            {
                AssetBundleEditorData.currentAssetBundleGroup = groupsToShow[0];
            }
            //添加元素
            foreach (var assetBundleGroup in groupsToShow)
            {
                bool isSelected = assetBundleGroup == AssetBundleEditorData.currentAssetBundleGroup;
                AddAssetBundles(assetBundlesScrollView, assetBundleGroup, isSelected);

                if (isSelected)
                {
                    // 重置搜索类型为Self，确保显示当前AB包内容
                    AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                    UpdateAssetBundlesDataItem();
                }
            }
            //添加增减AB包组按钮
            AddAssetBundleGroup(assetBundlesScrollView);
        }

        //添加AB包组
        private void AddAssetBundleGroup(VisualElement visual)
        {
            Button addABGroupButton = new Button();
            addABGroupButton.text = "+";
            addABGroupButton.AddToClassList("AddABGroupButton");
            addABGroupButton.clicked += () =>
            {
                // 避免重复添加输入框
                if (addABGroupButton.Q<TextField>() != null)
                    return;
                // 创建输入框
                TextField groupName = new TextField();
                groupName.AddToClassList("AddABGroupInput");
                groupName.tooltip = "请输入AssetBundle组名";
                groupName.value = ""; // 确保初始值为空
                addABGroupButton.Add(groupName);
                // 创建确认按钮
                Button sureAddABGroup = new Button();
                sureAddABGroup.AddToClassList("AddABGroupSureButton");
                sureAddABGroup.text = "+"; // 设置按钮文本
                addABGroupButton.Add(sureAddABGroup);
                // 创建取消按钮
                Button cancelAddABGroup = new Button();
                cancelAddABGroup.AddToClassList("CancelABGroupButton");
                cancelAddABGroup.text = "Cancel"; // 设置取消按钮文本
                addABGroupButton.Add(cancelAddABGroup);

                // 确认按钮事件
                sureAddABGroup.clicked += () =>
                {
                    HandleAddAssetBundleGroup(
                        addABGroupButton,
                        groupName,
                        sureAddABGroup,
                        cancelAddABGroup
                    );
                };

                // 取消按钮事件
                cancelAddABGroup.clicked += () =>
                {
                    RemoveInputElements(
                        addABGroupButton,
                        groupName,
                        sureAddABGroup,
                        cancelAddABGroup
                    );
                };

                // 回车键确认
                groupName.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        HandleAddAssetBundleGroup(
                            addABGroupButton,
                            groupName,
                            sureAddABGroup,
                            cancelAddABGroup
                        );
                        evt.StopPropagation();
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        RemoveInputElements(
                            addABGroupButton,
                            groupName,
                            sureAddABGroup,
                            cancelAddABGroup
                        );
                        evt.StopPropagation();
                    }
                });

                // 聚焦到输入框
                groupName.Focus();
            };

            visual.Add(addABGroupButton);
        }

        /// <summary>
        /// 处理添加AssetBundle组的逻辑
        /// </summary>
        private void HandleAddAssetBundleGroup(Button addButton, TextField groupName, Button sureButton, Button cancelButton)
        {
            try
            {
                string groupNameText = groupName.value?.Trim();
                // 移除UI元素
                RemoveInputElements(addButton, groupName, sureButton, cancelButton);
                // 验证输入
                if (string.IsNullOrWhiteSpace(groupNameText))
                {
                    Debug.LogWarning("<color=yellow>AB包组名称不能为空!</color>");
                    return;
                }

                // 检查配置是否存在
                if (AssetBundleEditorData.currentABConfig == null)
                {
                    Debug.LogError("<color=red>当前AB配置为空!</color>");
                    return;
                }

                // 初始化AssetBundleList（如果为null）
                if (AssetBundleEditorData.currentABConfig.AssetBundleList == null)
                {
                    AssetBundleEditorData.currentABConfig.AssetBundleList =
                        new List<AssetBundleGroup>();
                }

                // 检查是否存在同名组
                if (
                    AssetBundleEditorData.currentABConfig.AssetBundleList.Any(x =>
                        string.Equals(
                            x.assetBundleName,
                            groupNameText,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                {
                    Debug.LogWarning($"<color=red>已经存在同名AssetBundleGroup: {groupNameText}</color>");
                    return;
                }

                // 创建新的AssetBundle组
                var newGroup = new AssetBundleGroup
                {
                    assetBundleName = groupNameText,
                    assets = new List<AssetBundleAssetsData>(), // 初始化assets列表
                };

                AssetBundleEditorData.currentABConfig.AssetBundleList.Add(newGroup);

                // 标记配置为已修改（如果有这个功能）
                EditorUtility.SetDirty(AssetBundleEditorData.currentABConfig);

                // 更新UI
                UpdateAssetBundlesItem();

                Debug.Log($"<color=green>成功添加AssetBundle组: {groupNameText}</color>");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>添加AssetBundle组时发生错误: {ex.Message}</color>");
                Debug.LogException(ex);
            }
            finally
            {
                // 确保刷新资源数据库
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 移除输入相关的UI元素
        /// </summary>
        private void RemoveInputElements(Button addButton, TextField groupName, Button sureButton, Button cancelButton)
        {
            try
            {
                if (addButton.Contains(groupName))
                    addButton.Remove(groupName);
                if (addButton.Contains(sureButton))
                    addButton.Remove(sureButton);
                if (addButton.Contains(cancelButton))
                    addButton.Remove(cancelButton);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"移除UI元素时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据资源类型获取对应的Icon路径
        /// </summary>
        private string GetIconPathByAssetType(UnityEngine.Object asset)
        {
            if (asset == null) return "Icon/DefaultAssetIcon";

            System.Type assetType = asset.GetType();
            string typeName = assetType.Name;

            // 根据资源类型映射Icon路径
            switch (typeName)
            {
                case "Texture2D":
                    return "Icon/TextureIcon";
                case "Sprite":
                    return "Icon/SpriteIcon";
                case "Material":
                    return "Icon/MaterialIcon";
                case "Mesh":
                    return "Icon/MeshIcon";
                case "GameObject":
                    return "Icon/PrefabIcon";
                case "AudioClip":
                    return "Icon/AudioClipIcon";
                case "AnimationClip":
                    return "Icon/AnimationClipIcon";
                case "AnimatorController":
                    return "Icon/AnimatorControllerIcon";
                case "Font":
                    return "Icon/FontIcon";
                case "Shader":
                    return "Icon/ShaderIcon";
                case "TextAsset":
                    return "Icon/TextAssetIcon";
                default:
                    // 检查是否继承自ScriptableObject
                    if (typeof(ScriptableObject).IsAssignableFrom(assetType))
                    {
                        return "Icon/ScriptableObjectIcon";
                    }
                    return "Icon/DefaultAssetIcon";
            }
        }

        //AB包数据
        private void AddAssetBundlesDataItem(VisualElement visual, AssetBundleAssetsData asset, bool isSelect = false)
        {
            //主体区域
            Button assetBundlesDataItemContent = new Button();
            assetBundlesDataItemContent.AddToClassList("AssetBundlesDataItemContent");
            // 红蓝交错显示，选中状态优先
            string styleClass = isSelect
                ? "SelectAssetBundlesDataItem"
                : (visual.childCount % 2 == 0
                ? "DefaultAssetBundlesDataItem"
                : "DefaultAssetBundlesDataItem-Gray");

            assetBundlesDataItemContent.AddToClassList(styleClass);
            assetBundlesDataItemContent.clicked += () =>
            {
                AssetBundleEditorData.currentAsset = asset;
                EditorGUIUtility.PingObject(asset.AssetsObject);
                UpdateAssetBundlesDataItem();
            };
            // 资源Icon
            Label assetIcon = new Label();
            assetIcon.AddToClassList("TipsIcon");
            assetIcon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>($"{GetIconPathByAssetType(asset.AssetsObject)}"));
            assetIcon.style.position = Position.Absolute;
            assetBundlesDataItemContent.Add(assetIcon);
            //资源名称
            AssetDataInfo(assetBundlesDataItemContent, asset.assetName, true);
            //资源大小
            FileInfo fileInfo = new FileInfo(asset.assetPath);
            AssetDataInfo(assetBundlesDataItemContent, FormatFileSize(fileInfo.Length), true);
            //资源路径
            AssetDataInfo(assetBundlesDataItemContent, asset.assetPath);
            //资源类型
            AssetDataInfo(assetBundlesDataItemContent, asset.AssetsObject.GetType().Name, true);
            //资源控制器
            AddAssetController(assetBundlesDataItemContent, asset);
            // 刷新一下计数
            UpdateItemQuantityStatistics();
            visual.Add(assetBundlesDataItemContent);
        }

        //更新AB包数据项
        private void UpdateAssetBundlesDataItem(string searchText = null)
        {
            if (assetBundleItemScrollView == null || AssetBundleEditorData.currentABConfig == null)
                return;
            assetBundleItemScrollView.Clear();
            // 获取要显示的资源集合
            var assetsToShow = GetAssetsToShow();
            if (assetsToShow == null)
                return;
            // 应用过滤条件
            var filteredAssets = ApplyFilters(assetsToShow, searchText);
            // 添加元素到UI
            AddAssetsToScrollView(filteredAssets);

            //更新AB包资源大小
            assetBundleSizeField.text = "Size: " + GetTotalAssetsSize();
        }

        /// <summary>
        /// 根据搜索范围类型获取要显示的资源
        /// </summary>
        private IEnumerable<AssetBundleAssetsData> GetAssetsToShow()
        {
            return AssetBundleEditorData.currentABItemSearchType == ABItemSearchType.All
                ? AssetBundleEditorData.currentABConfig.AssetBundleList?.SelectMany(group => group.assets)
                : AssetBundleEditorData.currentAssetBundleGroup?.assets;
        }

        /// <summary>
        /// 应用类型和搜索过滤条件
        /// </summary>
        private IEnumerable<AssetBundleAssetsData> ApplyFilters(
            IEnumerable<AssetBundleAssetsData> assets,
            string searchText
        )
        {
            if (assets == null)
                return Enumerable.Empty<AssetBundleAssetsData>();

            var result = assets;

            // 应用类型过滤
            if (AssetBundleEditorData.currentABItemShowType != ABItemShowType.All)
            {
                result = result.Where(asset => IsAssetTypeMatch(asset));
            }

            // 应用搜索过滤
            if (!string.IsNullOrEmpty(searchText))
            {
                result = result.Where(asset => asset.assetName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return result;
        }

        /// <summary>
        /// 检查资源类型是否匹配
        /// </summary>
        private bool IsAssetTypeMatch(AssetBundleAssetsData asset)
        {
            if (asset?.AssetsObject == null)
                return false;

            string assetType = asset.AssetsObject.GetType().Name;
            return Enum.TryParse(assetType, out ABItemShowType showType)
                   && showType == AssetBundleEditorData.currentABItemShowType;
        }

        /// <summary>
        /// 将资源添加到滚动视图
        /// </summary>
        private void AddAssetsToScrollView(IEnumerable<AssetBundleAssetsData> assets)
        {
            foreach (var asset in assets)
            {
                bool isSelected = asset == AssetBundleEditorData.currentAsset;
                AddAssetBundlesDataItem(assetBundleItemScrollView, asset, isSelected);
            }
        }

        //资源数据信息
        private void AssetDataInfo(VisualElement visual, string dataName, bool isCenter = false)
        {
            Label dataInfo = new Label();
            dataInfo.text = dataName;
            dataInfo.AddToClassList("AssetBundlesDataItemInfo");
            dataInfo.style.unityTextAlign = isCenter
                ? TextAnchor.MiddleCenter
                : TextAnchor.MiddleLeft;
            visual.Add(dataInfo);
        }

        //创建资源控制器
        private void AddAssetController(VisualElement visual, AssetBundleAssetsData asset)
        {
            Label dataInfo = new Label();
            dataInfo.AddToClassList("AssetBundlesDataItemInfo");
            //移除资源
            Button removeAsset = new Button();
            removeAsset.AddToClassList("DeleteAsset");
            removeAsset.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除资源: '{asset.assetName}' ?", "是", "否"))
                    return;
                AssetBundleEditorData.currentAssetBundleGroup.assets.Remove(asset);
                // 刷新一下计数
                UpdateItemQuantityStatistics();
                UpdateAssetBundlesDataItem();
            };
            dataInfo.Add(removeAsset);
            visual.Add(dataInfo);
        }

        #region 资源大小

        /// <summary>
        /// 获取所有资源的总大小（格式化显示）
        /// </summary>
        private string GetTotalAssetsSize()
        {
            try
            {
                long totalBytes = CalculateTotalAssetsSize();
                return FormatFileSize(totalBytes);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"计算资源总大小时出错: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// 计算资源总大小（字节）
        /// </summary>
        private long CalculateTotalAssetsSize()
        {
            if (AssetBundleEditorData.currentABConfig?.AssetBundleList == null)
                return 0;

            long totalSize = 0;
            var processedAssets = new HashSet<string>(); // 防止重复计算同一资源

            // 根据当前搜索类型决定计算范围
            IEnumerable<AssetBundleAssetsData> assetsToCalculate;

            if (AssetBundleEditorData.currentABItemSearchType == ABItemSearchType.All)
            {
                // 计算所有AB包组的资源
                assetsToCalculate = AssetBundleEditorData.currentABConfig.AssetBundleList.SelectMany(group => group.assets);
            }
            else
            {
                // 只计算当前AB包组的资源
                if (AssetBundleEditorData.currentAssetBundleGroup?.assets == null)
                    return 0;
                assetsToCalculate = AssetBundleEditorData.currentAssetBundleGroup.assets;
            }

            // 直接统计所有资源，不应用过滤条件
            foreach (var asset in assetsToCalculate)
            {
                if (asset?.AssetsObject == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(asset.AssetsObject);
                if (string.IsNullOrEmpty(assetPath) || processedAssets.Contains(assetPath))
                    continue;

                processedAssets.Add(assetPath);

                // 获取资源文件大小
                var fileInfo = new System.IO.FileInfo(assetPath);
                if (fileInfo.Exists)
                {
                    totalSize += fileInfo.Length;
                }
            }

            return totalSize;
        }

        /// <summary>
        /// 格式化文件大小显示
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes == 0)
                return "0 B";

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:F2} {units[unitIndex]}";
        }

        #endregion
    }
}
