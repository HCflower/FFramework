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
            CreateClearABGroupData(searchContainer);

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

        // 创建计数区域
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

        // 创建刷新按钮
        private void CreateRefreshButton(VisualElement visual)
        {
            var refreshButton = new Button();
            refreshButton.AddToClassList("RefreshABItemDataButton");
            refreshButton.tooltip = "刷新面板数据";
            refreshButton.clicked += RefreshAllPanelData;
            visual.Add(refreshButton);
        }

        // 创建AB包资源导出调试按钮
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

        // 创建AB包资源清理按钮
        private void CreateClearABGroupData(VisualElement visual)
        {
            Button clearABGroupData = new Button();
            clearABGroupData.AddToClassList("ClearABGroupDataButton");
            clearABGroupData.tooltip = "AB包资源清理按钮";
            clearABGroupData.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认清理", $"确定要清理AB包组 '{AssetBundleEditorData.currentAssetBundleGroup.assetBundleName}' 的所有资源吗？", "是", "否"))
                    return;
                AssetBundleEditorData.currentABConfig.ClearABGroupData(AssetBundleEditorData.currentAssetBundleGroup);
                // 添加界面刷新
                UpdateAssetBundlesDataItem();
                UpdateItemQuantityStatistics();
            };
            visual.Add(clearABGroupData);
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

        // 刷新搜索范围类型
        private void ResetSearchTypeToSelf()
        {
            if (AssetBundleEditorData.currentAssetBundleGroup != null)
            {
                AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                if (typeEnumField != null)
                    typeEnumField.value = ABItemSearchType.Self;
            }
        }

        // 刷新所有显示
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
            DataInfoItem(dataInfo, "Asset Name", out Label assetName);
            assetName.style.paddingLeft = 10;
            DataInfoItem(dataInfo, "Size", out assetBundleSizeField, GetTotalAssetsSize());
            DataInfoItem(dataInfo, "Path", out Label _);
            DataInfoItem(dataInfo, "Type", out Label _);
            DataInfoItem(dataInfo, "Controller", out Label _);
            visual.Add(dataInfo);
        }

        //数据信息项
        private void DataInfoItem(VisualElement visual, string dataName, out Label dataInfo, string attach = null)
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
            Button ABGroupControl = new Button();
            ABGroupControl.AddToClassList("DefaultAssetBundleItem");
            if (isSelect)
                ABGroupControl.AddToClassList("SelectAssetBundleItem");

            // 当前AssetBundle是否构建
            Label icon = new Label();
            icon.AddToClassList("AssetBundleGroupIcon");
            icon.style.unityBackgroundImageTintColor = assetBundleGroup.isEnableBuild ? DefaultColor : DisabledColor;
            ABGroupControl.Add(icon);

            // 标题
            Label title = new Label();
            title.AddToClassList("AssetBundleItemTitle");
            title.text = assetBundleGroup.assetBundleName;
            ABGroupControl.Add(title);

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

            ABGroupControl.Add(tipsIconArea);

            // 点击事件
            ABGroupControl.clicked += () =>
            {
                AssetBundleEditorData.currentAssetBundleGroup = assetBundleGroup;
                AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                UpdateAssetBundlesItem();
                UpdateAssetBundlesDataItem();
            };

            // 下拉选项
            Button moreOptions = new Button();
            moreOptions.AddToClassList("ABGroupMoreOptionsButton");
            moreOptions.clicked += () =>
            {
                ShowDropdownMenu(ABGroupControl, moreOptions, assetBundleGroup);
            };
            ABGroupControl.Add(moreOptions);

            visual.Add(ABGroupControl);
        }

        #region 下拉菜单
        // 显示AB包组的配置设置下拉菜单
        private void ShowDropdownMenu(VisualElement visual, VisualElement button, AssetBundleGroup assetBundleGroup)
        {
            GenericMenu menu = new GenericMenu();

            // 构建状态选项
            AddBuildStateOptions(menu, assetBundleGroup);
            menu.AddSeparator("");

            // 构建路径类型选项
            AddBuildPathTypeOptions(menu, assetBundleGroup);
            menu.AddSeparator("");

            // 分割文件选项
            AddPackSeparatelyOptions(menu, assetBundleGroup);
            menu.AddSeparator("");

            // 可寻址资源选项
            AddAddressableOptions(menu, assetBundleGroup);
            menu.AddSeparator("");

            // 前缀选项
            AddPrefixOptions(menu, assetBundleGroup);
            menu.AddSeparator("");

            // 操作选项
            AddOperationOptions(visual, menu, assetBundleGroup);

            // 显示菜单
            ShowMenuAtPosition(menu, button);
        }

        // 菜单项配置结构
        private struct MenuItemConfig
        {
            public bool currentState;
            public string enabledText;
            public string disabledText;
            public string toggleToEnabledText;
            public string toggleToDisabledText;
            public System.Action<bool> onToggle;
        }

        // 添加构建状态选项
        private void AddBuildStateOptions(GenericMenu menu, AssetBundleGroup group)
        {
            var config = new MenuItemConfig
            {
                currentState = group.isEnableBuild,
                enabledText = "当前为可构建状态",
                disabledText = "当前为不可构建状态",
                toggleToEnabledText = "设置为可构建",
                toggleToDisabledText = "设置为不可构建",
                onToggle = (enabled) =>
                {
                    group.isEnableBuild = enabled;
                    UpdateAssetBundlesItem();
                }
            };

            AddToggleMenuItems(menu, config);
        }

        // 添加构建路径类型选项
        private void AddBuildPathTypeOptions(GenericMenu menu, AssetBundleGroup group)
        {
            var isLocal = group.buildPathType == BuildPathType.Local;

            var config = new MenuItemConfig
            {
                currentState = isLocal,
                enabledText = "当前构建路径类型为Local",
                disabledText = "当前构建路径类型为Remote",
                toggleToEnabledText = "设置构建路径类型为Local",
                toggleToDisabledText = "设置构建路径类型为Remote",
                onToggle = (toLocal) =>
                {
                    group.buildPathType = toLocal ? BuildPathType.Local : BuildPathType.Remote;
                    UpdateAssetBundlesItem();
                }
            };

            AddToggleMenuItems(menu, config);
        }

        // 添加分割文件选项
        private void AddPackSeparatelyOptions(GenericMenu menu, AssetBundleGroup group)
        {
            var config = new MenuItemConfig
            {
                currentState = group.isEnablePackSeparately,
                enabledText = "使用单文件AB包构建",
                disabledText = "不使用单文件AB包构建",
                toggleToEnabledText = "设置为使用单文件AB包构建",
                toggleToDisabledText = "设置为不使用单文件AB包构建",
                onToggle = (enabled) =>
                {
                    group.isEnablePackSeparately = enabled;
                    UpdateAssetBundlesItem();
                }
            };

            AddToggleMenuItems(menu, config);
        }

        // 添加可寻址资源选项
        private void AddAddressableOptions(GenericMenu menu, AssetBundleGroup group)
        {
            var config = new MenuItemConfig
            {
                currentState = group.isEnableAddressable,
                enabledText = "使用可寻址资源定位",
                disabledText = "不使用可寻址资源定位",
                toggleToEnabledText = "设置为使用可寻址资源定位",
                toggleToDisabledText = "设置为不使用可寻址资源定位",
                onToggle = (enabled) =>
                {
                    group.isEnableAddressable = enabled;
                    UpdateAssetBundlesItem();
                }
            };

            AddToggleMenuItems(menu, config);
        }

        // 添加前缀选项
        private void AddPrefixOptions(GenericMenu menu, AssetBundleGroup group)
        {
            var config = new MenuItemConfig
            {
                currentState = group.prefixIsAssetBundleName,
                enabledText = "使用AB包名为前缀",
                disabledText = "不使用AB包名为前缀",
                toggleToEnabledText = "设置为使用AB包名为前缀",
                toggleToDisabledText = "设置为不使用AB包名为前缀",
                onToggle = (enabled) =>
                {
                    group.prefixIsAssetBundleName = enabled;
                    UpdateAssetBundlesItem();
                }
            };

            AddToggleMenuItems(menu, config);
        }

        // 添加操作选项
        private void AddOperationOptions(VisualElement visual, GenericMenu menu, AssetBundleGroup group)
        {
            menu.AddItem(new GUIContent("更改当前AB包组的名称"), false, () =>
            {
                TextField changeABGroupName = new TextField();
                changeABGroupName.value = group.assetBundleName;
                changeABGroupName.AddToClassList("ChangeABGroupName");
                changeABGroupName.tooltip = "请输入新的AssetBundle组名称(Esc:取消修改,Enter:确认修改)";
                visual.Add(changeABGroupName);
                changeABGroupName.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        group.assetBundleName = changeABGroupName.value;
                        // 保存数据
                        AssetBundleEditorData.currentABConfig.SaveData();
                        // 刷新界面
                        UpdateAssetBundlesItem();
                        evt.StopPropagation();
                    }
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        visual.Remove(changeABGroupName);
                        // 刷新界面
                        UpdateAssetBundlesItem();
                        evt.StopPropagation();
                    }
                });
            });

            menu.AddItem(new GUIContent("删除当前轨道"), false, () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除AssetBundle组: '{group.assetBundleName}' ?", "是", "否"))
                    return;

                AssetBundleEditorData.currentABConfig.AssetBundleList.Remove(group);
                UpdateAssetBundlesItem();
            });
        }

        // 统一的切换菜单项添加方法
        private void AddToggleMenuItems(GenericMenu menu, MenuItemConfig config)
        {
            if (config.currentState)
            {
                menu.AddItem(new GUIContent(config.enabledText), true, null);
                menu.AddItem(new GUIContent(config.toggleToDisabledText), false, () => config.onToggle(false));
            }
            else
            {
                menu.AddItem(new GUIContent(config.toggleToEnabledText), false, () => config.onToggle(true));
                menu.AddItem(new GUIContent(config.disabledText), true, null);
            }
        }

        // 在指定位置显示菜单
        private void ShowMenuAtPosition(GenericMenu menu, VisualElement button)
        {
            var rect = button.worldBound;
            menu.DropDown(new Rect(rect.x, rect.yMax, 0, 0));
        }

        #endregion

        // AB包滚动区域
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

        // 添加AB包组
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
                groupName.value = "DefaultGroup";
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
                    HandleAddAssetBundleGroup(addABGroupButton, groupName, sureAddABGroup, cancelAddABGroup);
                };

                // 取消按钮事件
                cancelAddABGroup.clicked += () =>
                {
                    RemoveInputElements(addABGroupButton, groupName, sureAddABGroup, cancelAddABGroup);
                };

                // 回车键确认
                groupName.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        HandleAddAssetBundleGroup(addABGroupButton, groupName, sureAddABGroup, cancelAddABGroup);
                        evt.StopPropagation();
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        RemoveInputElements(addABGroupButton, groupName, sureAddABGroup, cancelAddABGroup);
                        evt.StopPropagation();
                    }
                });

                // 聚焦到输入框
                groupName.Focus();
            };

            visual.Add(addABGroupButton);
        }

        // 处理添加AssetBundle组的逻辑
        private void HandleAddAssetBundleGroup(Button addButton, TextField groupName, Button sureButton, Button cancelButton)
        {
            string groupNameText = groupName.value?.Trim();
            // 移除UI元素
            RemoveInputElements(addButton, groupName, sureButton, cancelButton);
            // 校验：只允许字母和空格
            if (string.IsNullOrWhiteSpace(groupNameText))
            {
                Debug.LogWarning("<color=yellow>AB包组名称不能为空或只有空格!</color>");
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

        // 移除输入相关的UI元素
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

        #region 设置Icon
        // 图标显示映射字典
        private static readonly Dictionary<string, string> AssetIconMappings = new Dictionary<string, string>
        {
            { "Texture2D", "Icon/TextureIcon" },
            { "Sprite", "Icon/SpriteIcon" },
            { "Material", "Icon/MaterialIcon" },
            { "Mesh", "Icon/MeshIcon" },
            { "AudioClip", "Icon/AudioClipIcon" },
            { "AnimationClip", "Icon/AnimationClipIcon" },
            { "AnimatorController", "Icon/AnimatorControllerIcon" },
            { "Font", "Icon/FontIcon" },
            { "Shader", "Icon/ShaderIcon" },
            { "TextAsset", "Icon/TextAssetIcon" },
            { "ParticleSystem", "Icon/ParticleSystemIcon" },
            { "TrailRenderer", "Icon/TrailRendererIcon" },        // 拖尾效果图标
            { "LineRenderer", "Icon/LineRendererIcon" },          // 线渲染器图标
            { "VisualEffect", "Icon/VisualEffectAssetIcon" },                   // VFX Graph图标
            { "LightProbeGroup", "Icon/LightProbeIcon" },         // 光照探针组图标
            { "ReflectionProbe", "Icon/ReflectionProbeIcon" }     // 反射探针图标
        };

        private Dictionary<string, string> GetAssetIconMappings()
        {
            return AssetIconMappings;
        }

        // 检查是否为模型文件
        private static readonly HashSet<string> ModelFileExtensions = new HashSet<string>
        {
            ".fbx", ".obj", ".dae", ".3ds", ".blend", ".max", ".ma", ".mb"
        };

        // 根据资源类型获取对应的Icon路径
        private string GetIconPathByAssetType(UnityEngine.Object asset, string assetPath = null)
        {
            if (asset == null)
                return GetDefaultIconPath();

            var assetType = asset.GetType();
            string typeName = assetType.Name;

            // 特殊处理：GameObject类型需要进一步判断
            if (typeName == "GameObject")
            {
                return GetGameObjectIconPath(assetPath);
            }

            // 处理其他资源类型
            return GetStandardAssetIconPath(typeName, assetType);
        }

        // 获取GameObject类型资源的图标路径
        private string GetGameObjectIconPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return "Icon/PrefabIcon";

            // 加载GameObject检查组件
            var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (gameObject != null)
            {
                // 检查是否包含粒子系统组件
                if (gameObject.GetComponent<ParticleSystem>() != null)
                {
                    return "Icon/ParticleSystemIcon";
                }

                // 检查是否包含拖尾渲染器组件
                if (gameObject.GetComponent<TrailRenderer>() != null)
                {
                    return "Icon/TrailRendererIcon";
                }

                // 检查是否包含线渲染器组件
                if (gameObject.GetComponent<LineRenderer>() != null)
                {
                    return "Icon/LineRendererIcon";
                }

                // 检查是否为Visual Effectt特效
                if (gameObject.GetComponent<UnityEngine.VFX.VisualEffect>() != null)
                {
                    return "Icon/VisualEffectAssetIcon";
                }
            }

            // 检查是否为模型文件
            string extension = Path.GetExtension(assetPath).ToLower();
            if (IsModelFile(extension))
            {
                return "Icon/MeshIcon";
            }

            return "Icon/PrefabIcon";
        }

        // 获取标准资源类型的图标路径
        private string GetStandardAssetIconPath(string typeName, System.Type assetType)
        {
            // 使用字典映射提高性能和可维护性
            var iconMappings = GetAssetIconMappings();

            if (iconMappings.TryGetValue(typeName, out string iconPath))
            {
                return iconPath;
            }

            // 处理ScriptableObject派生类
            if (typeof(ScriptableObject).IsAssignableFrom(assetType))
            {
                return "Icon/ScriptableObjectIcon";
            }

            return GetDefaultIconPath();
        }

        private bool IsModelFile(string extension)
        {
            return ModelFileExtensions.Contains(extension);
        }

        /// <summary>
        /// 获取默认图标路径
        /// </summary>
        private string GetDefaultIconPath()
        {
            return "Icon/DefaultAssetIcon";
        }

        // 为了向后兼容，保留原方法签名
        private string GetIconPathByAssetType(UnityEngine.Object asset)
        {
            return GetIconPathByAssetType(asset, AssetDatabase.GetAssetPath(asset));
        }

        #endregion

        #region 创建AB包资源项
        //AB包数据
        private void AddAssetBundlesDataItem(VisualElement visual, AssetBundleAssetsData asset, bool isSelect = false)
        {
            // 主体区域
            VisualElement mainABGroupItemArea = new VisualElement();
            mainABGroupItemArea.AddToClassList("MainABGroupItemArea");
            visual.Add(mainABGroupItemArea);

            // 主体按钮
            Button assetBundlesDataItemContent = new Button();
            assetBundlesDataItemContent.AddToClassList("AssetBundlesDataItemContent");
            mainABGroupItemArea.Add(assetBundlesDataItemContent);

            // 红蓝交错显示，选中状态优先（叠加样式）
            string styleClass = (visual.childCount % 2 == 0) ? "DefaultAssetBundlesDataItem" : "DefaultAssetBundlesDataItem-Gray";
            assetBundlesDataItemContent.AddToClassList(styleClass);
            if (isSelect) assetBundlesDataItemContent.AddToClassList("SelectAssetBundlesDataItem");

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
            assetIcon.style.backgroundImage = new StyleBackground
            (
                Resources.Load<Texture2D>($"{GetIconPathByAssetType(asset.AssetsObject, asset.assetPath)}")
            );
            assetIcon.style.position = Position.Absolute;
            assetIcon.style.unityBackgroundImageTintColor =
                AssetBundleEditorData.currentAssetBundleGroup.isEnablePackSeparately && !asset.isEnableBuild
                ? Color.gray
                : Color.white;
            assetBundlesDataItemContent.Add(assetIcon);
            // 资源名称
            AssetDataInfo(assetBundlesDataItemContent, asset.assetName, out Label assetName, true);
            assetName.style.paddingLeft = 10;
            // 资源大小
            FileInfo fileInfo = new FileInfo(asset.assetPath);
            AssetDataInfo(assetBundlesDataItemContent, FormatFileSize(fileInfo.Length), out Label _, true);
            // 资源路径
            AssetDataInfo(assetBundlesDataItemContent, asset.assetPath, out Label _);
            // 资源类型
            AssetDataInfo(assetBundlesDataItemContent, GetActualAssetShowType(asset).ToString(), out Label _, true);
            // 资源控制器
            AddAssetController(assetBundlesDataItemContent, mainABGroupItemArea, asset);
            // 引用资源列表 
            AddDependencyResourceList(mainABGroupItemArea);
            // 刷新一下计数
            UpdateItemQuantityStatistics();
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
        private void AssetDataInfo(VisualElement visual, string dataName, out Label dataInfo, bool isCenter = false)
        {
            dataInfo = new Label();
            dataInfo.text = dataName;
            dataInfo.AddToClassList("AssetBundlesDataItemInfo");
            dataInfo.style.unityTextAlign = isCenter ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            visual.Add(dataInfo);
        }

        //创建资源控制器
        private void AddAssetController(VisualElement visual, VisualElement dependencyArea, AssetBundleAssetsData asset)
        {
            Label assetControl = new Label();
            assetControl.AddToClassList("AssetBundlesDataItemInfo");
            assetControl.style.justifyContent = Justify.FlexEnd;

            //移除资源
            Button removeAsset = new Button();
            removeAsset.AddToClassList("ControlButton");
            removeAsset.AddToClassList("DeleteAssetButton");
            removeAsset.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除资源: '{asset.assetName}' ?", "是", "否"))
                    return;
                AssetBundleEditorData.currentAssetBundleGroup.assets.Remove(asset);
                // 刷新一下计数
                UpdateItemQuantityStatistics();
                UpdateAssetBundlesDataItem();
            };
            assetControl.Add(removeAsset);

            // 是否参与构建AB包(只有启用拆分文件时才有效)
            if (AssetBundleEditorData.currentAssetBundleGroup.isEnablePackSeparately)
            {
                Toggle isEnableBuildToggle = new Toggle();
                isEnableBuildToggle.AddToClassList("ControlButton");
                isEnableBuildToggle.AddToClassList("AssetIsEnableBuildToggle");
                isEnableBuildToggle.tooltip = "是否参与构建AB包(只有启用拆分文件时才有效)";
                isEnableBuildToggle.value = asset.isEnableBuild;
                isEnableBuildToggle.RegisterValueChangedCallback((evt) =>
                {
                    asset.isEnableBuild = evt.newValue;
                    UpdateAssetBundlesDataItem(); // 增加刷新
                });
                assetControl.Add(isEnableBuildToggle);
            }

            if (AssetBundleEditorData.currentAssetBundleGroup.isEnableAddressable)
            {
                // 显示引用资源列表
                AddShowDependencyResource(assetControl, dependencyArea, asset);
            }

            visual.Add(assetControl);
        }

        // 显示引用资源列表
        private void AddShowDependencyResource(VisualElement visual, VisualElement mainABGroupItemArea, AssetBundleAssetsData asset)
        {
            Button showDependencyResource = new Button();
            showDependencyResource.AddToClassList("ControlButton");
            showDependencyResource.AddToClassList("ShowDependencyResource");
            showDependencyResource.tooltip = "显示引用资源";
            showDependencyResource.clicked += () =>
            {
                var dependency = mainABGroupItemArea.Q<VisualElement>("DependencyResourceAreaList");
                if (dependency != null)
                {
                    // 获取所有依赖项
                    var deps = AssetDatabase.GetDependencies(asset.assetPath, true);
                    // 过滤自身和无效项
                    var validDeps = deps.Where(path =>
                        path != asset.assetPath &&
                        File.Exists(path) &&
                        !string.IsNullOrEmpty(path) &&
                        !UnsupportedExtensions.Contains(Path.GetExtension(path).ToLower())
                    ).ToList();

                    // 只清理 dependency 区域
                    dependency.Clear();
                    foreach (var dep in validDeps)
                    {
                        Button depButton = new Button();
                        depButton.AddToClassList("DependencyListButton");
                        depButton.text = dep;
                        depButton.clicked += () =>
                        {
                            var depObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dep);
                            if (depObj != null) EditorGUIUtility.PingObject(depObj);
                        };

                        // Icon
                        Label icon = new Label();
                        icon.AddToClassList("TipsIcon");
                        icon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>($"Icon/DefaultAssetIcon"));
                        icon.style.position = Position.Absolute;
                        icon.style.left = 2;
                        depButton.Add(icon);

                        // 只添加到 dependency 区域
                        dependency.Add(depButton);
                    }
                }
            };
            visual.Add(showDependencyResource);
        }

        // 引用资源列表
        private void AddDependencyResourceList(VisualElement visual)
        {
            VisualElement assetShowDependencyResourceArea = new VisualElement();
            assetShowDependencyResourceArea.name = "DependencyResourceAreaList";
            assetShowDependencyResourceArea.AddToClassList("AssetShowDependencyResourceArea");
            visual.Add(assetShowDependencyResourceArea);
        }

        #endregion

        #region 资源过滤
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
        private IEnumerable<AssetBundleAssetsData> ApplyFilters(IEnumerable<AssetBundleAssetsData> assets, string searchText)
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
        /// 检查资源类型是否匹配（修复版本）
        /// </summary>
        private bool IsAssetTypeMatch(AssetBundleAssetsData asset)
        {
            if (asset?.AssetsObject == null)
                return false;

            var targetShowType = AssetBundleEditorData.currentABItemShowType;

            // 获取实际的资源类型
            var actualType = GetActualAssetShowType(asset);

            return actualType == targetShowType;
        }

        // 获取资源的实际显示类型
        private ABItemShowType GetActualAssetShowType(AssetBundleAssetsData asset)
        {
            if (asset?.AssetsObject == null)
                return ABItemShowType.Other;

            var unityType = asset.AssetsObject.GetType();
            string typeName = unityType.Name;
            string assetPath = asset.assetPath;

            // 特殊处理：GameObject 需要进一步判断
            if (typeName == "GameObject")
            {
                // 加载GameObject检查组件
                var gameObject = asset.AssetsObject as GameObject;
                if (gameObject != null)
                {
                    // 检查是否包含粒子系统组件
                    if (gameObject.GetComponent<ParticleSystem>() != null)
                    {
                        return ABItemShowType.ParticleSystem;
                    }

                    // 检查是否包含拖尾渲染器组件
                    if (gameObject.GetComponent<TrailRenderer>() != null)
                    {
                        return ABItemShowType.TrailRenderer;
                    }

                    // 检查是否包含线渲染器组件
                    if (gameObject.GetComponent<LineRenderer>() != null)
                    {
                        return ABItemShowType.TrailRenderer; // 归类为拖尾效果
                    }

                    // 检查是否为Visual Effectt特效
                    if (gameObject.GetComponent<UnityEngine.VFX.VisualEffect>() != null)
                    {
                        return ABItemShowType.VisualEffect;
                    }
                }

                // 检查是否为模型文件
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string extension = Path.GetExtension(assetPath).ToLower();
                    if (IsModelFile(extension))
                    {
                        return ABItemShowType.Mesh;
                    }
                }
                return ABItemShowType.Prefab;
            }

            // 类型名称映射
            var typeMapping = new Dictionary<string, ABItemShowType>
            {
                { "Texture2D", ABItemShowType.Texture2D },
                { "Sprite", ABItemShowType.Sprite },
                { "Material", ABItemShowType.Material },
                { "Mesh", ABItemShowType.Mesh },
                { "AudioClip", ABItemShowType.AudioClip },
                { "AnimationClip", ABItemShowType.AnimationClip },
                { "AnimatorController", ABItemShowType.AnimatorController },
                { "Font", ABItemShowType.Font },
                { "Shader", ABItemShowType.Shader },
                { "TextAsset", ABItemShowType.TextAsset },
                { "ParticleSystem", ABItemShowType.ParticleSystem },        // 纯粒子系统组件
                { "TrailRenderer", ABItemShowType.TrailRenderer },          // 拖尾效果
                { "LineRenderer", ABItemShowType.TrailRenderer },           // 线渲染器归类为拖尾效果
                { "VisualEffect", ABItemShowType.VisualEffect },            // Visual Effectt特效
                { "SceneAsset", ABItemShowType.Scene }
            };

            // 直接映射
            if (typeMapping.TryGetValue(typeName, out ABItemShowType mappedType))
            {
                return mappedType;
            }

            // ScriptableObject 派生类
            if (typeof(ScriptableObject).IsAssignableFrom(unityType))
            {
                return ABItemShowType.ScriptableObject;
            }

            return ABItemShowType.Other;
        }

        #endregion

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
