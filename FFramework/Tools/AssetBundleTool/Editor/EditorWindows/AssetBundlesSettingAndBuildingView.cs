using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System;
using FFramework;

namespace AssetBundleToolEditor
{
    /// <summary>
    /// AB包构建界面
    /// </summary>
    public class AssetBundlesSettingAndBuildingView : VisualElement
    {
        #region  VisualElement
        private VisualElement mainContent;
        private VisualElement localSettingContent;
        private VisualElement remoteSettingContent;
        private Label assetBundlesSavePathField;
        private VisualElement globalSettingContent;
        #endregion

        public void Init(VisualElement visual)
        {
            mainContent = new VisualElement();
            mainContent.styleSheets.Add(Resources.Load<StyleSheet>("USS/AssetBundlesSettingAndBuildingView"));
            mainContent.AddToClassList("MainContent");

            CreateLocalSettingContent(mainContent);
            CreateRemoteSettingContent(mainContent);
            CreateGlobalSettingContent(mainContent);
            visual.Add(mainContent);
        }

        #region  本地包设置

        //创建本地AssetBundle设置内容
        private void CreateLocalSettingContent(VisualElement visual)
        {
            localSettingContent = new VisualElement();
            localSettingContent.AddToClassList("SettingContent");
            localSettingContent.AddToClassList("LocalSettingContent");
            //区域标题
            Label contentTitle = new Label("LocalSetting");
            contentTitle.AddToClassList("ContentTitle");
            localSettingContent.Add(contentTitle);
            //保存路径显示
            AssetBundleSavePath(localSettingContent, "LocalSavePath:", AssetBundleEditorData.currentABConfig.LocalSavePath);

            //构建本地AssetBundles
            CreateButton(localSettingContent, () =>
            {
                if (AssetBundleEditorData.isClearFolderWhenBuild)
                    AssetBundleEditorData.currentABConfig.ClearLocalAssetBundlesFolder();
                //构建AssetBundles
                AssetBundleEditorData.currentABConfig.CreateLocalAssetBundle();
            }, "构建本地AssetBundles", "Setting");

            //构建资源对比文件
            CreateButton(localSettingContent, () =>
            {
                AssetBundleEditorData.currentABConfig.CreateLocalAssetBundleInfoFile();
            }, "创建本地资源对比文件", "TextAssetIcon");

            //打开本地Application.persistentDataPath文件夹 
            CreateButton(localSettingContent, () =>
            {
                string path = Application.persistentDataPath + "/";
                UnityEditor.EditorUtility.RevealInFinder(path);
            }, "打开持久数据文件夹", "SelectFolder");

            visual.Add(localSettingContent);
        }

        #endregion

        #region  远端包设置

        //创建远端AssetBundle设置内容
        private void CreateRemoteSettingContent(VisualElement visual)
        {
            remoteSettingContent = new VisualElement();
            remoteSettingContent.AddToClassList("SettingContent");
            remoteSettingContent.AddToClassList("RemoteSettingContent");
            //区域标题
            Label contentTitle = new Label("RemoteSetting");
            contentTitle.AddToClassList("ContentTitle");
            remoteSettingContent.Add(contentTitle);

            //保存路径显示
            AssetBundleSavePath(remoteSettingContent, "UpdateResPath:", AssetBundleEditorData.currentABConfig.RemoteSavePath);

            //压缩格式选择
            CreateEnumField(remoteSettingContent, "压缩格式选择",
            AssetBundleEditorData.currentABConfig.RemoteCompressionType, (newvalue) =>
            {
                AssetBundleEditorData.currentABConfig.RemoteCompressionType = (CompressionType)newvalue;
            });

            //构建依赖文件
            CreateButton(remoteSettingContent, () =>
            {
                Debug.Log("(资源可寻址加载)施工中~");
            }, "创建依赖关系文件", "TextAssetIcon");

            //构建远端AssetBundles
            CreateButton(remoteSettingContent, () =>
            {
                if (AssetBundleEditorData.isClearFolderWhenBuild)
                    AssetBundleEditorData.currentABConfig.ClearRemoteAssetBundlesFolder();
                //构建AssetBundles
                AssetBundleEditorData.currentABConfig.CreateRemoteAssetBundle();
            }, "构建远端AssetBundles", "Setting");

            //构建资源对比文件
            CreateButton(remoteSettingContent, () =>
            {
                AssetBundleEditorData.currentABConfig.CreateRemoteAssetBundleInfoFile();
            }, "创建远端资源对比文件", "TextAssetIcon");

            visual.Add(remoteSettingContent);
        }

        #endregion

        #region  构建设置

        //创建全局AssetBundle设置内容
        private void CreateGlobalSettingContent(VisualElement visual)
        {
            globalSettingContent = new VisualElement();
            globalSettingContent.AddToClassList("SettingContent");
            globalSettingContent.AddToClassList("BuildingSettingContent");
            //区域标题
            Label contentTitle = new Label("BuildingSetting");
            contentTitle.AddToClassList("ContentTitle");
            globalSettingContent.Add(contentTitle);
            //构建平台选择
            CreateEnumField(globalSettingContent, "构建平台选择",
            AssetBundleEditorData.currentABConfig.BuildTarget,
            (newvalue) =>
            {
                AssetBundleEditorData.currentABConfig.BuildTarget = (BuildTarget)newvalue;
                // 根据平台自动更新远端保存路径
                AssetBundleEditorData.currentABConfig.RemoteSavePath =
                    $"HotUpdateRes/{AssetBundleEditorData.currentABConfig.BuildTarget}";
                // 更新路径显示
                if (assetBundlesSavePathField != null)
                {
                    assetBundlesSavePathField.text = AssetBundleEditorData.currentABConfig.RemoteSavePath;
                }
            });

            //构建AssetBundle包时是否清理文件夹
            CreateToggle(globalSettingContent, "是否清理文件夹",
            AssetBundleEditorData.isClearFolderWhenBuild,
            (newValue) =>
            {
                AssetBundleEditorData.isClearFolderWhenBuild = newValue;
                Debug.Log($"构建AssetBundles时是否清理文件夹:<color=yellow> {AssetBundleEditorData.isClearFolderWhenBuild}</color>");
            });

            //版本号
            CreateTextField(globalSettingContent, "设置版本号",
            AssetBundleEditorData.currentABConfig.Version,
            (newValue) =>
            {
                //TODO:关联到项目设置中的版本号
                AssetBundleEditorData.currentABConfig.Version = newValue;
            });

            //下载服务器选择
            CreateEnumField(globalSettingContent, "网络传输协议",
            AssetBundleEditorData.currentABConfig.NetworkProtocolsType,
            (newvalue) =>
            {
                AssetBundleEditorData.currentABConfig.NetworkProtocolsType = (NetworkProtocolsType)newvalue;
            });

            //服务器地址
            CreateTextField(globalSettingContent, "服务器地址",
            AssetBundleEditorData.currentABConfig.ResServerPath,
            (newValue) =>
            {
                AssetBundleEditorData.currentABConfig.ResServerPath = newValue;
            });

            //主文件夹名称
            CreateTextField(globalSettingContent, "主文件夹",
            AssetBundleEditorData.currentABConfig.MainFolderName,
            (newValue) =>
            {
                AssetBundleEditorData.currentABConfig.MainFolderName = newValue;
            });

            //用户ID
            CreateTextField(globalSettingContent, "用户ID",
            AssetBundleEditorData.currentABConfig.ID,
            (newValue) =>
            {
                AssetBundleEditorData.currentABConfig.ID = newValue;
            });

            //用户名密码
            CreateTextField(globalSettingContent, "用户名密码",
            AssetBundleEditorData.currentABConfig.Password,
            (newValue) =>
            {
                AssetBundleEditorData.currentABConfig.Password = newValue;
            });

            //添加资源下载管理器
            CreateButton(globalSettingContent, () =>
            {
                Debug.Log("添加资源下载管理器");
                // 在当前场景中查找
                ABResDownLoadManager manager = GameObject.FindObjectOfType<ABResDownLoadManager>();
                //设置资源地址
                string resServer = AssetBundleEditorData.currentABConfig.ResServerPath?.TrimEnd('/');
                string mainFolderName = AssetBundleEditorData.currentABConfig.MainFolderName?.TrimStart('/');
                string buildTarget = AssetBundleEditorData.currentABConfig.BuildTarget.ToString();

                string resServerPath = $"{resServer}/{mainFolderName}/{buildTarget}";
                if (manager != null)
                {
                    Debug.Log($"已存在ABResDownLoadManager,位于 {manager.gameObject.name}");
                    manager.ResServerPath = resServerPath;
                    Debug.Log($"设置资源服务器路径: {resServerPath}");
                    Selection.activeObject = manager.gameObject;
                    return;
                }
                else
                {
                    GameObject managerObj = new GameObject("ABResDownLoadManager");
                    managerObj.transform.SetParent(null);
                    manager = managerObj.AddComponent<ABResDownLoadManager>();
                    manager.ResServerPath = resServerPath;
                    Debug.Log($"设置资源服务器路径: {resServerPath}");
                    Debug.Log("已创建资源下载管理器", managerObj);
                    Selection.activeObject = managerObj;
                }
            }, "添加资源下载管理器", "Setting");

            //添加资源加载管理器
            CreateButton(globalSettingContent, () =>
            {
                Debug.Log("添加资源加载管理器");
                // 在当前场景中查找
                AssetBundlesResLoader manager = GameObject.FindObjectOfType<AssetBundlesResLoader>();
                //设置资源地址
                string resServer = AssetBundleEditorData.currentABConfig.ResServerPath?.TrimEnd('/');
                string mainFolderName = AssetBundleEditorData.currentABConfig.MainFolderName?.TrimStart('/');
                string buildTarget = AssetBundleEditorData.currentABConfig.BuildTarget.ToString();

                if (manager != null)
                {
                    Debug.Log($"已存在ABResDownLoadManager,位于 {manager.gameObject.name}");
                    manager.mainAssetBundleName = buildTarget;
                    Debug.Log($"设置主包名: {buildTarget}");
                    Selection.activeObject = manager.gameObject;

                    return;
                }
                else
                {
                    GameObject managerObj = new GameObject("AssetBundlesResLoader");
                    managerObj.transform.SetParent(null);
                    manager = managerObj.AddComponent<AssetBundlesResLoader>();
                    manager.mainAssetBundleName = buildTarget;
                    Selection.activeObject = managerObj;
                    Debug.Log($"设置主包名: {buildTarget}");
                    Debug.Log("已创建资源加载管理器", managerObj);
                }
            }, "添加资源加载管理器", "Setting");

            //构建所有远端AssetBundles
            CreateButton(globalSettingContent, () =>
            {
                if (AssetBundleEditorData.isClearFolderWhenBuild)
                {
                    AssetBundleEditorData.currentABConfig.ClearLocalAssetBundlesFolder();
                    AssetBundleEditorData.currentABConfig.ClearRemoteAssetBundlesFolder();
                }
                //构建AssetBundles
                AssetBundleEditorData.currentABConfig.CreateLocalAssetBundle();
                AssetBundleEditorData.currentABConfig.CreateRemoteAssetBundle();
            }, "构建所有AssetBundles", "Setting");

            visual.Add(globalSettingContent);
        }

        #endregion

        #region  Common

        //资源保存路径
        private Label AssetBundleSavePath(VisualElement visual, string titleText, string pathText)
        {
            VisualElement pathContent = new VisualElement();
            pathContent.AddToClassList("SettingItemContent");
            //titleButton
            Button titleButton = new Button();
            titleButton.text = titleText;
            titleButton.AddToClassList("Title");
            pathContent.Add(titleButton);
            //content
            assetBundlesSavePathField = new Label();
            assetBundlesSavePathField.text = pathText;
            assetBundlesSavePathField.AddToClassList("PathViewContent");
            pathContent.Add(assetBundlesSavePathField);
            //点击事件=>打开文件夹
            titleButton.clicked += () =>
            {
                Debug.Log($"打开文件夹{pathText}");
                // 转换为完整Asset路径
                string assetPath = $"Assets/{pathText.TrimStart('/')}";
                // 获取文件夹对象
                UnityEngine.Object folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (folderObj != null)
                {
                    // 聚焦并选中文件夹
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = folderObj;
                    EditorGUIUtility.PingObject(folderObj);
                }
                else
                    Debug.LogWarning($"文件夹不存在: {assetPath}");
            };
            visual.Add(pathContent);
            return assetBundlesSavePathField;
        }

        //创建控制按钮
        private void CreateButton(VisualElement visual, Action buttonAction, string buttonTitle, string iconPath = null)
        {
            Button button = new Button();
            button.AddToClassList("ControllButton");
            button.text = buttonTitle;
            button.clicked += buttonAction;
            //图标
            Image buttonIcon = new Image();
            buttonIcon.AddToClassList("ControllButtonIcon");
            buttonIcon.style.backgroundImage = Resources.Load<Texture2D>($"Icon/{iconPath}");
            button.Add(buttonIcon);

            visual.Add(button);
        }

        //创建枚举类型选择
        private void CreateEnumField(VisualElement visual, string labelText, Enum defaultEnumValue, Action<Enum> onValueChanged)
        {
            // 创建容器
            VisualElement enumContainer = new VisualElement();
            enumContainer.AddToClassList("SettingItemContent");
            enumContainer.AddToClassList("EnumTypeContent");

            // 添加标签
            Label titleLabel = new Label(labelText);
            titleLabel.AddToClassList("Title");
            enumContainer.Add(titleLabel);

            // 创建枚举字段
            EnumField enumField = new EnumField(defaultEnumValue);
            enumField.AddToClassList("EnumType");

            // 注册值变更回调
            enumField.RegisterValueChangedCallback(evt =>
            {
                onValueChanged?.Invoke((Enum)evt.newValue);
            });

            enumContainer.Add(enumField);
            visual.Add(enumContainer);
        }

        //创建Toggle
        private void CreateToggle(VisualElement visual, string labelText, bool initialValue, Action<bool> onValueChanged)
        {
            // 创建容器
            VisualElement toggleContainer = new VisualElement();
            toggleContainer.AddToClassList("SettingItemContent");
            // 添加标签
            Label titleLabel = new Label(labelText);
            titleLabel.AddToClassList("Title");
            toggleContainer.Add(titleLabel);
            // 创建Toggle控件
            Toggle toggle = new Toggle()
            {
                value = initialValue
            };
            toggle.AddToClassList("Toggle");
            // 注册值变更回调
            toggle.RegisterValueChangedCallback(evt =>
            {
                onValueChanged?.Invoke(evt.newValue);
            });

            toggleContainer.Add(toggle);
            visual.Add(toggleContainer);
        }

        //创建文本输入区域
        private void CreateTextField(VisualElement visual, string labelText, string initialValue, Action<string> onValueChanged)
        {
            // 创建容器
            VisualElement textFieldContainer = new VisualElement();
            textFieldContainer.AddToClassList("SettingItemContent");

            // 创建标签
            if (!string.IsNullOrEmpty(labelText))
            {
                Label label = new Label(labelText);
                label.AddToClassList("Title");
                textFieldContainer.Add(label);
            }

            // 创建文本输入框
            TextField textField = new TextField
            {
                value = initialValue ?? string.Empty
            };
            textField.AddToClassList("InputContent");

            // 注册值变更回调
            textField.RegisterValueChangedCallback(evt =>
            {
                onValueChanged?.Invoke(evt.newValue);
            });

            textFieldContainer.Add(textField);
            visual.Add(textFieldContainer);
        }

        #endregion

    }
}