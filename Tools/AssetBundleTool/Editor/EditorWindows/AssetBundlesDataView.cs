using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
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
        #region 编辑器视觉元素
        private VisualElement mainContent;
        private Label abListContent;
        private Label abItemContent;

        private ScrollView assetBundlesScrollView;
        private ScrollView assetBundleItemScrollView;
        private EnumField typeEnumField;

        //当前AssetBundle中所有资源大小
        private Label assetBundleSizeField;

        #endregion

        /// <summary>
        /// AB包控制区域初始化
        /// </summary>
        /// <param name="visual">父级元素</param>    
        public void Init(VisualElement visual)
        {
            mainContent = new VisualElement();
            mainContent.styleSheets.Add(Resources.Load<StyleSheet>("USS/AssetBundlesDataView"));
            mainContent.AddToClassList("MainContent");
            ABListContent(mainContent);
            ABSearchContent(abListContent);
            ABScrollViewContent(abListContent);
            ABItemContent(mainContent);
            ABItemSearchContent(abItemContent);
            ABItemScrollViewContent(abItemContent);
            visual.Add(mainContent);

            UpdateAssetBundlesDataItem();
        }

        private void OnDisable()
        {
            assetBundlesScrollView.Clear();
            assetBundleItemScrollView.Clear();
        }

        //AB列表区域
        private void ABListContent(VisualElement visual)
        {
            abListContent = new Label();
            abListContent.AddToClassList("ABListContent");
            visual.Add(abListContent);
        }

        //AB列表搜索区域
        private void ABSearchContent(VisualElement visual)
        {
            Label abSearchContent = new Label();
            abSearchContent.AddToClassList("ABCreateSearchLabel");

            TextField abSearchInput = new TextField();
            abSearchInput.AddToClassList("ABSearchInput");
            abSearchInput.RegisterValueChangedCallback(evt =>
            {
                string searchText = evt.newValue.ToLower();
                var groups = AssetBundleEditorData.currentABConfig?.AssetBundleList;

                if (string.IsNullOrEmpty(searchText))
                {
                    AssetBundleEditorData.currentFilteredGroups = null;
                    UpdateAssetBundlesItem();
                    return;
                }

                var filteredGroups = groups.Where(g =>
                    g.assetBundleName.ToLower().Contains(searchText)
                ).ToList();

                AssetBundleEditorData.currentFilteredGroups = filteredGroups;
                UpdateAssetBundlesItem();
                assetBundlesScrollView.MarkDirtyRepaint();
            });

            // 获取TextElement
            var textElement = abSearchInput.Q<TextElement>(className: "unity-text-element");
            if (textElement != null) textElement.style.marginLeft = 18;

            Label icon = new Label();
            icon.AddToClassList("SearchIcon");
            abSearchInput.Add(icon);
            abSearchContent.Add(abSearchInput);
            visual.Add(abSearchContent);
        }

        //AB包滚动区域
        private void ABScrollViewContent(VisualElement visual)
        {
            assetBundlesScrollView = new ScrollView();
            assetBundlesScrollView.AddToClassList("ABListScrollView");
            assetBundlesScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            UpdateAssetBundlesItem();
            visual.Add(assetBundlesScrollView);
        }

        //AB包项区域
        private void ABItemContent(VisualElement visual)
        {
            abItemContent = new Label();
            abItemContent.AddToClassList("ABItemListContent");
            visual.Add(abItemContent);
        }

        //AB包项搜索区域
        private void ABItemSearchContent(VisualElement visual)
        {
            Label abItemSearchContent = new Label();
            abItemSearchContent.AddToClassList("ABItemSearchContent");

            TextField abItemSearchInput = new TextField();
            abItemSearchInput.AddToClassList("ABSearchInput");
            // 获取TextElement
            var textElement = abItemSearchInput.Q<TextElement>(className: "unity-text-element");
            if (textElement != null) textElement.style.marginLeft = 18;

            Label icon = new Label();
            icon.AddToClassList("SearchIcon");

            SearchType(abItemSearchContent);
            abItemSearchInput.Add(icon);
            abItemSearchContent.Add(abItemSearchInput);
            LimitItemShowType(abItemSearchContent);
            RefreshABItemData(abItemSearchContent);
            visual.Add(abItemSearchContent);

            if (AssetBundleEditorData.currentAssetBundleGroup != null)
            {
                // 添加搜索功能
                abItemSearchInput.RegisterValueChangedCallback(evt =>
                {
                    if (string.IsNullOrEmpty(evt.newValue))
                    {
                        // 搜索框为空时重置搜索类型为Self
                        AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                        typeEnumField.value = ABItemSearchType.Self;
                    }
                    UpdateAssetBundlesDataItem(evt.newValue);
                });
            }
            ABItemDataInfoBar(visual);
        }

        //AB包项搜索类型
        private void SearchType(VisualElement visual)
        {
            this.typeEnumField = new EnumField(ABItemSearchType.Self);
            this.typeEnumField.AddToClassList("ABItemSearchTypeEnumField");

            // 仅更新搜索类型，不自动刷新视图
            this.typeEnumField.RegisterValueChangedCallback(evt =>
            {
                AssetBundleEditorData.currentABItemSearchType = (ABItemSearchType)evt.newValue;
            });

            visual.Add(this.typeEnumField);
        }

        //限制AB包项显示类型
        private void LimitItemShowType(VisualElement visual)
        {
            EnumField limitTypeEnumField = new EnumField(ABItemShowType.All);
            limitTypeEnumField.AddToClassList("LimitItemShowTypeEnumField");

            // 根据当前资源类型过滤显示选项
            if (AssetBundleEditorData.currentAsset != null)
            {
                string assetType = AssetBundleEditorData.currentAsset.AssetsObject.GetType().Name;
                if (System.Enum.TryParse(assetType, out ABItemShowType showType))
                    limitTypeEnumField.value = showType;
                else
                    // 如果资源类型不在枚举中，默认不显示
                    limitTypeEnumField.value = ABItemShowType.All;
            }
            // 添加值变化回调，刷新列表
            limitTypeEnumField.RegisterValueChangedCallback(evt =>
            {
                AssetBundleEditorData.currentABItemShowType = (ABItemShowType)evt.newValue;

                // 获取搜索框引用
                var searchInput = visual.parent.Q<TextField>();
                if (searchInput != null && string.IsNullOrEmpty(searchInput.value))
                {
                    // 搜索框为空时重置搜索类型为Self
                    AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                    typeEnumField.value = ABItemSearchType.Self;
                }

                UpdateAssetBundlesDataItem();
            });

            visual.Add(limitTypeEnumField);
        }

        //刷新AB包项数据
        private void RefreshABItemData(VisualElement visual)
        {
            Button refreshButton = new Button();
            refreshButton.AddToClassList("RefreshABItemDataButton");
            visual.Add(refreshButton);
        }

        //AB包项滚动区域
        private void ABItemScrollViewContent(VisualElement visual)
        {
            assetBundleItemScrollView = new ScrollView();
            assetBundleItemScrollView.AddToClassList("ABItemScrollView");
            // 设置滚动条隐藏
            assetBundleItemScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            assetBundleItemScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

            // 设置拖拽接收
            assetBundleItemScrollView.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            });

            assetBundleItemScrollView.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (AssetBundleEditorData.currentAssetBundleGroup == null) return;

                foreach (var draggedObj in DragAndDrop.objectReferences)
                {
                    var assetPath = AssetDatabase.GetAssetPath(draggedObj);
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    // 处理文件夹
                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        var allAssets = AssetDatabase.FindAssets("", new[] { assetPath })
                            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                            .Where(path => !AssetDatabase.IsValidFolder(path));

                        foreach (var path in allAssets)
                        {
                            ProcessAsset(path);
                        }
                        continue;
                    }

                    ProcessAsset(assetPath);
                }

                UpdateAssetBundlesDataItem();
                assetBundleItemScrollView.MarkDirtyRepaint();
                evt.StopPropagation();
            });
            visual.Add(assetBundleItemScrollView);
        }

        //检查资源添加是否合法
        private void ProcessAsset(string assetPath)
        {
            // 检查文件类型是否支持
            string extension = Path.GetExtension(assetPath).ToLower();
            string[] unsupportedExtensions = {
                ".cs",       // C#脚本
                ".shader",   // 着色器文件
                ".asmdef",   // 程序集定义
                ".unity",    // 场景文件//TODO:需特殊处理
            };
            if (unsupportedExtensions.Contains(extension))
            {
                Debug.LogWarning($"不支持将{extension}文件打包到AssetBundle: {assetPath}");
                return;
            }

            // 检查是否已存在相同GUID的资源
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            // 查找所有包含该资源的AB包组
            var existingGroups = AssetBundleEditorData.currentABConfig.AssetBundleList
                .Where(g => g.assets.Any(a => AssetDatabase.AssetPathToGUID(a.assetPath) == guid))
                .ToList();

            if (existingGroups.Count > 0)
            {
                string groupNames = string.Join(", ",
                    existingGroups.Select(g => $"{g.assetBundleName}"));
                Debug.LogWarning($"资源:{assetPath}<color=yellow>已存在.</color>\n所在AB包组-> {groupNames}");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            var newAsset = new AssetBundleAssetsData
            {
                assetName = asset.name,
                assetPath = assetPath,
                AssetsObject = asset
            };

            AssetBundleEditorData.currentAssetBundleGroup.assets.Add(newAsset);
        }

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
        private void DataInfoItem(VisualElement visual, string dataName, out Label dataInfo, string attach = null)
        {
            dataInfo = new Label();
            //title
            if (attach != null) dataInfo.text = $"{dataName}: {attach}";
            else dataInfo.text = dataName;
            dataInfo.AddToClassList("ABItemDataInfo");
            visual.Add(dataInfo);
        }

        //AB包项数据
        private void AddAssetBundles(VisualElement visual, AssetBundleGroup assetBundleGroup, bool isSelect = false)
        {
            Button AssetBundleItem = new Button();
            AssetBundleItem.AddToClassList("DefaultAssetBundleItem");
            if (isSelect) AssetBundleItem.AddToClassList("SelectAssetBundleItem");
            //标题
            Label title = new Label();
            title.AddToClassList("AssetBundleItemTitle");
            title.text = assetBundleGroup.assetBundleName;
            AssetBundleItem.Add(title);
            //点击事件
            AssetBundleItem.clicked += () =>
            {
                AssetBundleEditorData.currentAssetBundleGroup = assetBundleGroup;
                // 重置搜索类型为Self，确保显示当前AB包内容
                AssetBundleEditorData.currentABItemSearchType = ABItemSearchType.Self;
                UpdateAssetBundlesItem();
                UpdateAssetBundlesDataItem();
            };
            //构建路径枚举
            EnumField buildPathType = new EnumField(BuildPathType.Local)
            {
                value = assetBundleGroup.buildPathType
            };
            buildPathType.AddToClassList("BuildPathTypeEnumField");
            buildPathType.RegisterValueChangedCallback(evt =>
            {
                assetBundleGroup.buildPathType = (BuildPathType)evt.newValue;
            });
            AssetBundleItem.Add(buildPathType);

            //删除ABGroup按钮
            Button delete = new Button();
            delete.AddToClassList("DeleteABGroupButton");
            delete.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除AssetBundle组: '{assetBundleGroup.assetBundleName}' ?", "是", "否"))
                    return;
                AssetBundleEditorData.currentABConfig.AssetBundleList.Remove(assetBundleGroup);
                UpdateAssetBundlesItem();
            };
            AssetBundleItem.Add(delete);

            visual.Add(AssetBundleItem);
        }

        //AB包滚动区域
        private void UpdateAssetBundlesItem()
        {
            assetBundlesScrollView.Clear();
            var groupsToShow = AssetBundleEditorData.currentFilteredGroups ?? AssetBundleEditorData.currentABConfig?.AssetBundleList;
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

                TextField groupName = new TextField();
                groupName.AddToClassList("AddABGroupInput");
                groupName.tooltip = "请输入AssetBundle组名";
                groupName.value = ""; // 确保初始值为空

                Button sureAddABGroup = new Button();
                sureAddABGroup.AddToClassList("AddABGroupSureButton");
                sureAddABGroup.text = "+"; // 设置按钮文本

                Button cancelAddABGroup = new Button();
                cancelAddABGroup.AddToClassList("CancelABGroupButton");
                cancelAddABGroup.text = "Cancel"; // 设置取消按钮文本

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

                addABGroupButton.Add(groupName);
                addABGroupButton.Add(sureAddABGroup);
                addABGroupButton.Add(cancelAddABGroup);

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
                    AssetBundleEditorData.currentABConfig.AssetBundleList = new List<AssetBundleGroup>();
                }

                // 检查是否存在同名组
                if (AssetBundleEditorData.currentABConfig.AssetBundleList.Any(x =>
                    string.Equals(x.assetBundleName, groupNameText, StringComparison.OrdinalIgnoreCase)))
                {
                    Debug.LogWarning($"<color=red>已经存在同名AssetBundleGroup: {groupNameText}</color>");
                    return;
                }

                // 创建新的AssetBundle组
                var newGroup = new AssetBundleGroup
                {
                    assetBundleName = groupNameText,
                    assets = new List<AssetBundleAssetsData>() // 初始化assets列表
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

        //AB包数据
        private void AddAssetBundlesDataItem(VisualElement visual, AssetBundleAssetsData asset, bool isSelect = false)
        {
            //主体区域
            Button assetBundlesDataItemContent = new Button();
            assetBundlesDataItemContent.AddToClassList("AssetBundlesDataItemContent");
            // 红蓝交错显示，选中状态优先
            string styleClass = isSelect ? "SelectAssetBundlesDataItem" :
                (visual.childCount % 2 == 0 ? "DefaultAssetBundlesDataItem" : "DefaultAssetBundlesDataItem-Gray");
            assetBundlesDataItemContent.AddToClassList(styleClass);
            assetBundlesDataItemContent.clicked += () =>
            {
                AssetBundleEditorData.currentAsset = asset;
                EditorGUIUtility.PingObject(asset.AssetsObject);
                UpdateAssetBundlesDataItem();
            };
            //资源名称
            AssetDataInfo(assetBundlesDataItemContent, asset.assetName);
            //资源大小
            FileInfo fileInfo = new FileInfo(asset.assetPath);
            AssetDataInfo(assetBundlesDataItemContent, FormatFileSize(fileInfo.Length), true);
            //资源路径
            AssetDataInfo(assetBundlesDataItemContent, asset.assetPath);
            //资源类型
            AssetDataInfo(assetBundlesDataItemContent, asset.AssetsObject.GetType().Name, true);
            //资源控制器
            AddAssetController(assetBundlesDataItemContent, asset);
            visual.Add(assetBundlesDataItemContent);
        }

        //更新AB包数据项
        private void UpdateAssetBundlesDataItem(string searchText = null)
        {
            if (assetBundleItemScrollView == null || AssetBundleEditorData.currentABConfig == null) return;
            assetBundleItemScrollView.Clear();
            // 获取要显示的资源集合
            var assetsToShow = GetAssetsToShow();
            if (assetsToShow == null) return;
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
        private IEnumerable<AssetBundleAssetsData> ApplyFilters(IEnumerable<AssetBundleAssetsData> assets, string searchText)
        {
            if (assets == null) return Enumerable.Empty<AssetBundleAssetsData>();

            var result = assets;

            // 应用类型过滤
            if (AssetBundleEditorData.currentABItemShowType != ABItemShowType.All)
            {
                result = result.Where(asset => IsAssetTypeMatch(asset));
            }

            // 应用搜索过滤
            if (!string.IsNullOrEmpty(searchText))
            {
                result = result.Where(asset =>
                    asset.assetName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return result;
        }

        /// <summary>
        /// 检查资源类型是否匹配
        /// </summary>
        private bool IsAssetTypeMatch(AssetBundleAssetsData asset)
        {
            if (asset?.AssetsObject == null) return false;

            string assetType = asset.AssetsObject.GetType().Name;
            return Enum.TryParse(assetType, out ABItemShowType showType) &&
                   showType == AssetBundleEditorData.currentABItemShowType;
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
            dataInfo.style.unityTextAlign = isCenter ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            visual.Add(dataInfo);
        }

        //创建资源控制器
        private void AddAssetController(VisualElement visual, AssetBundleAssetsData asset)
        {
            Label dataInfo = new Label();
            dataInfo.AddToClassList("AssetBundlesDataItemInfo");
            //移除资源
            Button removeAsset = new Button();
            removeAsset.AddToClassList("CancelAsset");
            dataInfo.Add(removeAsset);
            removeAsset.clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("确认删除", $"确定删除资源: '{asset.assetName}' ?", "是", "否"))
                    return;
                AssetBundleEditorData.currentAssetBundleGroup.assets.Remove(asset);
                UpdateAssetBundlesDataItem();
            };
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
                assetsToCalculate = AssetBundleEditorData.currentABConfig.AssetBundleList
                    .SelectMany(group => group.assets);
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
                if (asset?.AssetsObject == null) continue;

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
            if (bytes == 0) return "0 B";

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

