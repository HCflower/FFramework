using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBundleToolEditor
{
    /// <summary>
    /// AB工具编辑窗口
    /// </summary>
    public class AssetBundleEditor : EditorWindow
    {
        #region 编辑器视觉元素
        private Toolbar abToolbar;
        private VisualElement mainContent;
        private AssetBundleConfigureView configureView;
        private AssetBundlesDataView assetBundlesDataView;
        private AssetBundlesSettingAndBuildingView settingAndBuildingView;
        #endregion

        [MenuItem("FFramework/📦AssetBundleTool #B", priority = 0)]
        public static void SkillEditorCreateWindow()
        {
            AssetBundleEditor window = GetWindow<AssetBundleEditor>();
            window.minSize = new Vector2(900, 450);
            window.titleContent = new GUIContent("AssetBundleToolEditor");
            window.Show();
        }

        private void OnEnable()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/AssetBundlesEditor"));
            CreateToolBar(rootVisualElement);
            MainContent(rootVisualElement);
            //控制区域
            ChangeABConfigureView(mainContent);
        }

        private void OnDisable()
        {
            //保存所有数据
            AssetBundleEditorData.currentABConfig?.SaveData();
            rootVisualElement.Clear();
        }

        //工具栏
        private void CreateToolBar(VisualElement visual)
        {
            abToolbar = new Toolbar();
            abToolbar.AddToClassList("ABToolBar");
            // 创建配置文件
            CreateControllerButton(
                abToolbar,
                "CreateConfigure",
                () =>
                {
                    mainContent.Clear();
                    ChangeABConfigureView(mainContent);
                },
                out Label CreateConfigIcon
            );
            CreateConfigIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/CreateConfigIcon");

            // AB包数据
            CreateControllerButton(
                abToolbar,
                "AssetBundleData",
                () =>
                {
                    mainContent.Clear();
                    ChangeABDataView(mainContent);
                },
                out Label AssetBundlesDataIcon
            );
            AssetBundlesDataIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/AssetBundle");

            // 设置和构建
            CreateControllerButton(
                abToolbar,
                "Setting/Building",
                () =>
                {
                    mainContent.Clear();
                    ChangeSettingAndBuildingView(mainContent);
                },
                out Label BuildAndSettingIcon
            );
            BuildAndSettingIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/Setting");

            // 刷新配置
            CreateControllerButton(
                abToolbar,
                "UpdateConfigure",
                () =>
                {
                    if (AssetBundleEditorData.currentABConfig != null)
                    {
                        AssetBundleEditorData.currentABConfig.GenerateJSON();
                        AssetBundleEditorData.currentABConfig.LoadFromJSON();
                    }
                    else
                        Debug.Log("未选择没有配置文件!");
                },
                out Label UpdateConfigureIcon
            );
            UpdateConfigureIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/AB-Refresh");

            // 保存配置
            CreateControllerButton(
                abToolbar,
                "SaveConfigure",
                () =>
                {
                    if (AssetBundleEditorData.currentABConfig != null)
                        AssetBundleEditorData.currentABConfig.SaveData();
                    else
                        Debug.Log("未选择没有配置文件!");
                },
                out Label SaveIcon
            );
            SaveIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/Save");

            // 上传资源到服务器
            CreateControllerButton(
                abToolbar,
                "UploadToServer",
                () =>
                {
                    string assetspath = AssetBundleEditorData.currentABConfig.RemoteSavePath;
                    //资源地址
                    string resServer = AssetBundleEditorData.currentABConfig.ResServerPath;
                    string mainFolderName = AssetBundleEditorData.currentABConfig.MainFolderName;
                    string buildTarget =
                        AssetBundleEditorData.currentABConfig.BuildTarget.ToString();
                    string resServerPath = resServer + "/" + mainFolderName + "/" + buildTarget;

                    string ftpUser = AssetBundleEditorData.currentABConfig.ID;
                    string ftpPwd = AssetBundleEditorData.currentABConfig.Password;
                    NetworkProtocolsType networkProtocolsType = AssetBundleEditorData
                        .currentABConfig
                        .NetworkProtocolsType;
                    CreateAssetsBundlesHandles.UploadAllAssetBundlesFile(
                        assetspath,
                        resServerPath,
                        networkProtocolsType,
                        ftpUser,
                        ftpPwd
                    );
                },
                out Label RefreshIcon
            );
            RefreshIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/Upload");
            visual.Add(abToolbar);
        }

        //主区域
        private void MainContent(VisualElement visual)
        {
            mainContent = new VisualElement();
            mainContent.AddToClassList("MainContent");
            visual.Add(mainContent);
        }

        //切换AB配置控制视图按钮
        private void ChangeABConfigureView(VisualElement visual)
        {
            visual.Clear();
            if (configureView == null)
            {
                configureView = new AssetBundleConfigureView();
            }
            configureView.Init(visual);
        }

        //切换AB数据控制视图按钮
        private void ChangeABDataView(VisualElement visual)
        {
            visual.Clear();
            if (assetBundlesDataView == null)
            {
                assetBundlesDataView = new AssetBundlesDataView();
            }
            assetBundlesDataView.Init(visual);
        }

        //切换控制视图按钮
        public void ChangeSettingAndBuildingView(VisualElement visual)
        {
            visual.Clear();
            if (settingAndBuildingView == null)
            {
                settingAndBuildingView = new AssetBundlesSettingAndBuildingView();
            }
            if (AssetBundleEditorData.currentABConfig != null)
                settingAndBuildingView.Init(visual);
        }

        //创建控制按钮
        private void CreateControllerButton(
            VisualElement visual,
            string buttonName,
            Action action,
            out Label button
        )
        {
            Button changeViewButton = new Button();
            changeViewButton.AddToClassList("ControllerButton");
            changeViewButton.clicked += () => action();

            Label icon = new Label();
            icon.AddToClassList("ControllerButtonIcon");
            changeViewButton.Add(icon);

            Label name = new Label();
            name.text = buttonName;
            name.AddToClassList("ControllerButtonName");
            changeViewButton.Add(name);

            button = icon;

            visual.Add(changeViewButton);
        }
    }
}
