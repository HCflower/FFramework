using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

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
            assetBundleItemScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

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
                Debug.LogWarning($"资源已存在于以下AB包中: {assetPath}\n所在AB包: {groupNames}");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
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
            DataInfoItem(dataInfo, "Asset");
            DataInfoItem(dataInfo, "Size");
            DataInfoItem(dataInfo, "Path");
            DataInfoItem(dataInfo, "Type");
            DataInfoItem(dataInfo, "Controller");
            visual.Add(dataInfo);
        }

        //数据信息项
        private void DataInfoItem(VisualElement visual, string dataName)
        {
            Label dataInfo = new Label();
            dataInfo.text = dataName;
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
            addABGroupButton.AddToClassList("AddABGroupButton");
            addABGroupButton.clicked += () =>
            {
                TextField groupName = new TextField();
                groupName.AddToClassList("AddABGroupInput");
                addABGroupButton.Add(groupName);

                Button sureAddABGroup = new Button();
                sureAddABGroup.AddToClassList("AddABGroupSureButton");
                sureAddABGroup.clicked += () =>
                {
                    //确定后移出,文本输入框和确认按钮
                    addABGroupButton.Remove(groupName);
                    addABGroupButton.Remove(sureAddABGroup);
                    //检查是否存在同名ABGroup或空名
                    if (string.IsNullOrWhiteSpace(groupName.text))
                    {
                        Debug.Log("<color=yellow>AB包组名称不能为空!</color>");
                    }
                    else if (AssetBundleEditorData.currentABConfig != null && AssetBundleEditorData.currentABConfig.AssetBundleList.Any(x => x.assetBundleName == groupName.text))
                    {
                        Debug.Log("<color=red>已经存在同名AssetBundleGroup!</color>");
                    }
                    else
                    {
                        AssetBundleEditorData.currentABConfig.AssetBundleList.Add(new AssetBundleGroup
                        {
                            assetBundleName = groupName.text
                        });
                        UpdateAssetBundlesItem();
                        AssetDatabase.Refresh();
                    }
                    AssetDatabase.Refresh();
                };
                addABGroupButton.Add(sureAddABGroup);
            };
            visual.Add(addABGroupButton);
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
            AssetDataInfo(assetBundlesDataItemContent, FormatFileSize(fileInfo.Length));
            //资源路径
            AssetDataInfo(assetBundlesDataItemContent, asset.assetPath);
            //资源类型
            AssetDataInfo(assetBundlesDataItemContent, asset.AssetsObject.GetType().Name);
            //资源控制器
            AddAssetController(assetBundlesDataItemContent, asset);
            visual.Add(assetBundlesDataItemContent);
        }

        //更新AB包数据项
        private void UpdateAssetBundlesDataItem(string searchText = null)
        {
            if (assetBundleItemScrollView == null) return;
            assetBundleItemScrollView.Clear();

            if (AssetBundleEditorData.currentABConfig != null)
            {
                IEnumerable<AssetBundleAssetsData> assetsToShow;

                // 根据搜索范围类型决定显示哪些资源
                if (AssetBundleEditorData.currentABItemSearchType == ABItemSearchType.All)
                {
                    // 显示所有AB包组的资源
                    assetsToShow = AssetBundleEditorData.currentABConfig.AssetBundleList
                        .SelectMany(group => group.assets);
                }
                else
                {
                    // 只显示当前AB包组的资源
                    if (AssetBundleEditorData.currentAssetBundleGroup == null) return;
                    assetsToShow = AssetBundleEditorData.currentAssetBundleGroup.assets;
                }

                // 根据搜索范围类型决定显示哪些资源
                if (AssetBundleEditorData.currentABItemSearchType == ABItemSearchType.All)
                {
                    // 显示所有AB包组的资源
                    assetsToShow = AssetBundleEditorData.currentABConfig.AssetBundleList
                        .SelectMany(group => group.assets);
                }
                else
                {
                    // 只显示当前AB包组的资源
                    if (AssetBundleEditorData.currentAssetBundleGroup == null) return;
                    assetsToShow = AssetBundleEditorData.currentAssetBundleGroup.assets;
                }

                // 应用类型过滤（limitTypeEnumField）
                if (AssetBundleEditorData.currentABItemShowType != ABItemShowType.All)
                {
                    assetsToShow = assetsToShow.Where(asset =>
                    {
                        string assetType = asset.AssetsObject.GetType().Name;
                        return System.Enum.TryParse(assetType, out ABItemShowType showType) &&
                               showType == AssetBundleEditorData.currentABItemShowType;
                    });
                }

                // 应用搜索过滤（完全独立于typeEnumField）
                if (!string.IsNullOrEmpty(searchText))
                {
                    assetsToShow = assetsToShow.Where(asset =>
                        asset.assetName.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // 添加元素
                foreach (AssetBundleAssetsData asset in assetsToShow)
                {
                    if (asset == AssetBundleEditorData.currentAsset)
                        AddAssetBundlesDataItem(assetBundleItemScrollView, asset, true);
                    else
                        AddAssetBundlesDataItem(assetBundleItemScrollView, asset);
                }
            }
        }

        //资源数据信息
        private void AssetDataInfo(VisualElement visual, string dataName)
        {
            Label dataInfo = new Label();
            dataInfo.text = dataName;
            dataInfo.AddToClassList("AssetBundlesDataItemInfo");
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
                AssetBundleEditorData.currentAssetBundleGroup.assets.Remove(asset);
                UpdateAssetBundlesDataItem();
            };
            visual.Add(dataInfo);
        }

        // 格式化文件大小
        private string FormatFileSize(long bytes)
        {
            const int KB = 1024;
            const int MB = KB * 1024;
            const int GB = MB * 1024;

            if (bytes >= GB)
                return $"{(bytes / (float)GB):0.00}GB";
            if (bytes >= MB)
                return $"{(bytes / (float)MB):0.00}MB";
            if (bytes >= KB)
                return $"{(bytes / (float)KB):0.00}KB";

            return $"{bytes}B";
        }
    }
}

