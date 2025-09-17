using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBundleToolEditor
{
    /// <summary>
    /// 创建配置面板
    /// </summary>
    public class AssetBundleConfigureView : VisualElement
    {
        #region  VisualElement

        private VisualElement mainContent;
        private VisualElement createConfigure;
        private ScrollView configureScrollView;
        private VisualElement createConfigureParent;
        private TextField savePathTextField;

        #endregion

        #region  数据

        private string configureName = "AssetBundleConfigure"; // 配置名称
        private string savePath; // 保存路径
        private bool isOpencreateConfigure = false;
        private List<AssetBundleConfig> assetBundleConfigList = new List<AssetBundleConfig>();

        #endregion
        public void Init(VisualElement visual)
        {
            mainContent = new VisualElement();
            mainContent.styleSheets.Add(
                Resources.Load<StyleSheet>("USS/AssetBundlesConfigureView")
            );
            mainContent.AddToClassList("MainContent");
            // 注册鼠标点击事件
            mainContent.RegisterCallback<MouseDownEvent>(OnMouseDown);
            CreateScrollView(mainContent);
            isOpencreateConfigure = false;
            visual.Add(mainContent);
        }

        void OnDisable()
        {
            mainContent.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            savePath = string.Empty;
            isOpencreateConfigure = false;
            createConfigure = null;
            configureScrollView = null;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            // 检查是否是右键点击,1表示右键
            if (evt.button == 1 && !isOpencreateConfigure)
            {
                isOpencreateConfigure = true;
                CreateAssetBundleConfigureBar(mainContent);
            }
        }

        //创建配置
        private void CreateAssetBundleConfigureBar(VisualElement visual)
        {
            createConfigure = new VisualElement();
            createConfigure.AddToClassList("CreateConfigureContent");
            Label title = new Label();
            title.text = "Create Configure";
            title.AddToClassList("CreateConfigureTitle");
            createConfigure.Add(title);

            CreateConfigureContent(createConfigure);
            CreateConfigureSavePathContent(createConfigure);
            createConfigureParent = visual;
            visual.Add(createConfigure);
        }

        //创建配置名称输入
        private void CreateConfigureContent(VisualElement visual)
        {
            Label controlContent = new Label();
            controlContent.AddToClassList("CreateConfigureItem");

            TextField textField = new TextField();
            textField.AddToClassList("CreateConfigureInput");
            textField.value = configureName;
            textField.RegisterValueChangedCallback(
                (evt) =>
                {
                    configureName = evt.newValue;
                }
            );

            controlContent.Add(textField);
            CreateConfigureButton(controlContent);
            visual.Add(controlContent);
        }

        //创建配置保存路径输入
        private void CreateConfigureSavePathContent(VisualElement visual)
        {
            Label controlContent = new Label();
            controlContent.AddToClassList("CreateConfigureItem");

            savePathTextField = new TextField();
            savePathTextField.AddToClassList("CreateConfigureInput");
            savePathTextField.value = savePath;
            savePathTextField.RegisterValueChangedCallback(
                (evt) =>
                {
                    savePath = evt.newValue;
                }
            );
            controlContent.Add(savePathTextField);
            SaveConfigureButton(controlContent);
            visual.Add(controlContent);
        }

        //创建配置按钮
        private void CreateConfigureButton(VisualElement visual)
        {
            Button createConfigureButton = new Button();
            createConfigureButton.text = "+";
            createConfigureButton.AddToClassList("CreateConfigureButton");
            createConfigureButton.clicked += () =>
            {
                if (!string.IsNullOrEmpty(savePath))
                {
                    string fullPath = Path.Combine(savePath, configureName);
                    if (File.Exists(fullPath))
                    {
                        Debug.LogError(
                            $"<color=red>错误：路径 {fullPath} 已存在同名配置文件！</color>"
                        );
                        return;
                    }
                    Debug.Log(
                        $"AssetBundleConfigure已保存到<color=yellow>{savePath}路径下.</color>"
                    );
                    isOpencreateConfigure = false;
                    CreateAssetsBundlesHandles.CreateConfigureData(savePath, configureName);
                    savePath = string.Empty;
                    createConfigureParent.Remove(createConfigure);
                    createConfigure = null;
                }
                else
                {
                    Debug.Log("<color=yellow>请先选择AssetBundleConfigure保存路径</color>");
                }
            };

            visual.Add(createConfigureButton);
            CreateCancelButton(visual);
        }

        //创建文件保存位置按钮
        private void SaveConfigureButton(VisualElement visual)
        {
            Button selectSavePathButton = new Button();
            selectSavePathButton.AddToClassList("SaveConfigureButton");
            selectSavePathButton.clicked += () =>
            {
                savePathTextField.value = GetFolderPath();
            };
            visual.Add(selectSavePathButton);
        }

        //创建取消按钮
        private void CreateCancelButton(VisualElement visual)
        {
            Button CancelButton = new Button();
            CancelButton.AddToClassList("CancelButton");
            CancelButton.text = "X";
            CancelButton.clicked += () =>
            {
                isOpencreateConfigure = false;
                createConfigureParent.Remove(createConfigure);
                createConfigure = null;
            };
            visual.Add(CancelButton);
        }

        //获取文件夹路径
        private string GetFolderPath()
        {
            // 打开文件夹选择面板
            string exportPath = EditorUtility.OpenFolderPanel("选择导出路径", "Assets", "");
            // 确保选择的是相对 Assets 目录的路径
            if (!string.IsNullOrEmpty(exportPath) && exportPath.StartsWith(Application.dataPath))
            {
                // 取得相对路径
                exportPath = "Assets" + exportPath.Substring(Application.dataPath.Length);
                savePath = exportPath;
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未在 Assets 文件夹中选择任何目录路径。", "OK");
            }
            return exportPath;
        }

        //创建滚动视图
        private void CreateScrollView(VisualElement visual)
        {
            configureScrollView = new ScrollView();
            configureScrollView.AddToClassList("ConfigureScrollView");
            configureScrollView.tooltip = "右键创建配置文件";
            if (configureScrollView != null)
            {
                var scrollViewContent = configureScrollView.contentContainer;
                scrollViewContent.style.flexDirection = FlexDirection.Row;
                scrollViewContent.style.flexWrap = Wrap.Wrap;
            }
            configureScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            //设置默认选中第一个配置
            if (assetBundleConfigList != null && assetBundleConfigList.Count > 0)
                AssetBundleEditorData.currentABConfig = assetBundleConfigList[0];
            UpdateConfigureItemView();
            visual.Add(configureScrollView);
        }

        //创建配置项
        private void CreateConfigureItemView(
            AssetBundleConfig data,
            VisualElement visual,
            bool isSelected = false
        )
        {
            Button assetBundleConfigItem = new Button();
            assetBundleConfigItem.AddToClassList("DefaultConfigureItem");
            //文本
            Label assetBundleConfigName = new Label();
            assetBundleConfigName.AddToClassList("ConfigureItemTitle");
            assetBundleConfigName.text = data.name;
            assetBundleConfigItem.Add(assetBundleConfigName);
            //图标
            Image icon = new Image();
            icon.AddToClassList(
                isSelected == true ? "SelectConfigureItemImage" : "DefaultConfigureItemImage"
            );
            assetBundleConfigItem.Add(icon);
            //选中
            assetBundleConfigItem.clicked += () =>
            {
                EditorGUIUtility.PingObject(data);
                {
                    AssetBundleEditorData.ClearData();
                }
                AssetBundleEditorData.currentABConfig = data;
                Debug.Log("当前选中配置项:" + data.name);
                UpdateConfigureItemView();
            };
            visual.Add(assetBundleConfigItem);
        }

        //更新配置项视图
        private void UpdateConfigureItemView()
        {
            configureScrollView.Clear();
            assetBundleConfigList.Clear();
            assetBundleConfigList = CreateAssetsBundlesHandles.GetAllConfigureDataSOList();
            foreach (var item in assetBundleConfigList)
            {
                if (item == AssetBundleEditorData.currentABConfig)
                    CreateConfigureItemView(item, configureScrollView, true);
                else
                    CreateConfigureItemView(item, configureScrollView);
            }
        }
    }
}
