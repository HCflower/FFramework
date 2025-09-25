using System;
using System.IO;
using FFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
            mainContent.styleSheets.Add(
                Resources.Load<StyleSheet>("USS/AssetBundlesSettingAndBuildingView")
            );
            mainContent.AddToClassList("MainContent");
            UpdateSettingView(mainContent);
            visual.Add(mainContent);
        }

        // 更新设置界面
        private void UpdateSettingView(VisualElement visual)
        {
            visual.Clear();
            //创建设置区域
            CreateLocalSettingContent(mainContent);
            CreateRemoteSettingContent(mainContent);
            CreateGlobalSettingContent(mainContent);
        }

        #region  本地包设置

        //创建本地AssetBundle设置内容
        private void CreateLocalSettingContent(VisualElement visual)
        {
            localSettingContent = new VisualElement();
            localSettingContent.AddToClassList("SettingContent");
            localSettingContent.AddToClassList("LocalSettingContent");
            //区域标题
            Label contentTitle = new Label("LocalBuildingSetting");
            contentTitle.AddToClassList("ContentTitle");
            localSettingContent.Add(contentTitle);
            // 本地路径
            AssetBundleSavePath(
                localSettingContent,
                "LocalSavePath:",
                AssetBundleEditorData.currentABConfig.LocalSavePath,
                (newPath) => AssetBundleEditorData.currentABConfig.LocalSavePath = newPath
            );

            //构建本地AssetBundles
            CreateButton(
                localSettingContent,
                () =>
                {
                    if (AssetBundleEditorData.isClearFolderWhenBuild)
                        AssetBundleEditorData.currentABConfig.ClearLocalAssetBundlesFolder();
                    //构建AssetBundles
                    AssetBundleEditorData.currentABConfig.CreateLocalAssetBundle();

                },
                "构建本地AssetBundles",
                "Setting"
            );

            //构建资源对比文件
            CreateButton(
                localSettingContent,
                () =>
                {
                    AssetBundleEditorData.currentABConfig.CreateLocalAssetBundleInfoFile();
                },
                "创建本地资源对比文件",
                "TextAssetIcon"
            );

            //打开本地Application.persistentDataPath文件夹
            CreateButton(
                localSettingContent,
                () =>
                {
                    string path = Application.persistentDataPath + "/";
                    UnityEditor.EditorUtility.RevealInFinder(path);
                },
                "打开持久数据文件夹",
                "SelectFolder"
            );

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
            Label contentTitle = new Label("RemoteBuildingSetting");
            contentTitle.AddToClassList("ContentTitle");
            remoteSettingContent.Add(contentTitle);

            // 远程路径
            AssetBundleSavePath(
                remoteSettingContent,
                "UpdateResPath:",
                AssetBundleEditorData.currentABConfig.RemoteSavePath,
                (newPath) => AssetBundleEditorData.currentABConfig.RemoteSavePath = newPath
            );

            //构建远端AssetBundles
            CreateButton(
            remoteSettingContent,
            () =>
            {
                if (AssetBundleEditorData.isClearFolderWhenBuild)
                    AssetBundleEditorData.currentABConfig.ClearRemoteAssetBundlesFolder();
                //构建AssetBundles
                AssetBundleEditorData.currentABConfig.CreateRemoteAssetBundle();
            },
            "构建远端AssetBundles",
            "Setting"
            );

            //构建资源对比文件
            CreateButton(
            remoteSettingContent,
            () =>
            {
                AssetBundleEditorData.currentABConfig.CreateRemoteAssetBundleInfoFile();
            },
            "创建远端资源对比文件",
            "TextAssetIcon"
            );

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
            Label contentTitle = new Label("GlobalBuildingSetting");
            contentTitle.AddToClassList("ContentTitle");
            globalSettingContent.Add(contentTitle);
            //构建平台选择
            CreateEnumField(
                globalSettingContent,
                "AB包构建平台选择",
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
                        assetBundlesSavePathField.text = AssetBundleEditorData
                            .currentABConfig
                            .RemoteSavePath;
                    }
                }
            );

            //压缩格式选择
            CreateEnumField(
                globalSettingContent,
                "AB包压缩格式选择",
                AssetBundleEditorData.currentABConfig.CompressionType,
                (newvalue) =>
                {
                    AssetBundleEditorData.currentABConfig.CompressionType =
                        (CompressionType)newvalue;
                }
            );

            //构建AssetBundle包时是否清理文件夹
            CreateToggle(
                globalSettingContent,
                "构建时是否清理文件夹",
                AssetBundleEditorData.isClearFolderWhenBuild,
                (newValue) =>
                {
                    AssetBundleEditorData.isClearFolderWhenBuild = newValue;
                    Debug.Log(
                        $"构建AssetBundles时是否清理文件夹:<color=yellow> {AssetBundleEditorData.isClearFolderWhenBuild}</color>"
                    );
                }
            );

            //版本号
            CreateTextField(
                globalSettingContent,
                "设置版本号",
                AssetBundleEditorData.currentABConfig.VersionID,
                (newValue) =>
                {
                    //TODO:关联到项目设置中的版本号
                    AssetBundleEditorData.currentABConfig.VersionID = newValue;
                }
            );

            //下载服务器选择
            CreateEnumField(
                globalSettingContent,
                "网络传输协议",
                AssetBundleEditorData.currentABConfig.NetworkProtocolsType,
                (newvalue) =>
                {
                    AssetBundleEditorData.currentABConfig.NetworkProtocolsType =
                        (NetworkProtocolsType)newvalue;
                    UpdateSettingView(mainContent);
                }
            );

            if (
                AssetBundleEditorData.currentABConfig.NetworkProtocolsType
                == NetworkProtocolsType.FTP
            )
            {
                //用户ID
                CreateTextField(
                    globalSettingContent,
                    "用户ID",
                    AssetBundleEditorData.currentABConfig.Account,
                    (newValue) =>
                    {
                        AssetBundleEditorData.currentABConfig.Account = newValue;
                    }
                );

                //用户名密码
                CreateTextField(
                    globalSettingContent,
                    "用户名密码",
                    AssetBundleEditorData.currentABConfig.Password,
                    (newValue) =>
                    {
                        AssetBundleEditorData.currentABConfig.Password = newValue;
                    }
                );
            }

            //服务器地址
            CreateTextField(
                globalSettingContent,
                "服务器地址",
                AssetBundleEditorData.currentABConfig.ResServerPath,
                (newValue) =>
                {
                    AssetBundleEditorData.currentABConfig.ResServerPath = newValue;
                }
            );

            //主文件夹名称
            CreateTextField(
                globalSettingContent,
                "主文件夹名称",
                AssetBundleEditorData.currentABConfig.MainFolderName,
                (newValue) =>
                {
                    AssetBundleEditorData.currentABConfig.MainFolderName = newValue;
                }
            );

            //添加资源下载管理器
            CreateButton(
                globalSettingContent,
                () =>
                {
                    Debug.Log("添加资源下载管理器");
                    // 在当前场景中查找
                    AssetBundlesDownLoadHandler handler =
                        GameObject.FindObjectOfType<AssetBundlesDownLoadHandler>();
                    //设置资源地址
                    string resServer = AssetBundleEditorData.currentABConfig.ResServerPath?.TrimEnd(
                        '/'
                    );
                    string mainFolderName =
                        AssetBundleEditorData.currentABConfig.MainFolderName?.TrimStart('/');
                    string buildTarget =
                        AssetBundleEditorData.currentABConfig.BuildTarget.ToString();

                    string resServerPath = $"{resServer}/{mainFolderName}/{buildTarget}";
                    string account = AssetBundleEditorData.currentABConfig.Account;
                    string password = AssetBundleEditorData.currentABConfig.Password;
                    if (handler != null)
                    {
                        Debug.Log(
                            $"已存在AssetBundlesDownLoadHandler,位于 {handler.gameObject.name}"
                        );
                        handler.ResServerPath = resServerPath;
                        handler.Account = account;
                        handler.Password = password;
                        Debug.Log($"设置资源服务器路径: {resServerPath}");
                        Selection.activeObject = handler.gameObject;
                        return;
                    }
                    else
                    {
                        GameObject managerObj = new GameObject("AssetBundlesDownLoadHandler");
                        managerObj.transform.SetParent(null);
                        handler = managerObj.AddComponent<AssetBundlesDownLoadHandler>();
                        handler.ResServerPath = resServerPath;
                        handler.Account = account;
                        handler.Password = password;
                        Debug.Log($"设置资源服务器路径: {resServerPath}");
                        Debug.Log("已创建资源下载管理器", managerObj);
                        Selection.activeObject = managerObj;
                    }
                },
                "添加资源下载管理器",
                "Setting"
            );

            //构建所有远端AssetBundles
            CreateButton(
                globalSettingContent,
                () =>
                {
                    if (AssetBundleEditorData.isClearFolderWhenBuild)
                    {
                        AssetBundleEditorData.currentABConfig.ClearLocalAssetBundlesFolder();
                        AssetBundleEditorData.currentABConfig.ClearRemoteAssetBundlesFolder();
                    }
                    //构建AssetBundles
                    AssetBundleEditorData.currentABConfig.CreateLocalAssetBundle();
                    AssetBundleEditorData.currentABConfig.CreateRemoteAssetBundle();
                },
                "构建所有AssetBundles",
                "Setting"
            );

            visual.Add(globalSettingContent);
        }

        #endregion

        #region  Common

        // 资源保存路径
        private Label AssetBundleSavePath(
            VisualElement visual,
            string titleText,
            string pathText,
            System.Action<string> updateAction
        )
        {
            VisualElement pathContent = new VisualElement();
            pathContent.AddToClassList("SettingItemContent");

            //titleButton
            Button titleButton = new Button();
            titleButton.text = titleText;
            titleButton.AddToClassList("Title");
            pathContent.Add(titleButton);

            //content
            Label pathField = new Label();
            pathField.text = pathText;
            pathField.AddToClassList("PathViewContent");
            pathContent.Add(pathField);

            //点击事件=>选择文件夹
            titleButton.clicked += () =>
            {
                //如果文件夹正确则从当前文件夹开始
                string selectedPath = SelectAssetFolder(pathText);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    pathField.text = selectedPath;
                    updateAction(selectedPath); // 直接调用更新函数
                    EditorUtility.SetDirty(AssetBundleEditorData.currentABConfig);
                }
            };

            visual.Add(pathContent);
            return pathField;
        }

        // 文件夹选择
        private string SelectAssetFolder(string currentPath = "")
        {
            // 确定起始路径,默认从Assets开始
            string startPath = Application.dataPath;

            // 如果当前路径不为空且是Assets开头，尝试转换为绝对路径
            if (!string.IsNullOrEmpty(currentPath) && currentPath.StartsWith("Assets"))
            {
                string absolutePath = currentPath.Replace("Assets", Application.dataPath);
                if (Directory.Exists(absolutePath))
                {
                    // 路径存在就用这个作为起始路径
                    startPath = absolutePath;
                }
            }

            // 使用Unity内置的资源选择器
            string selectedPath = EditorUtility.OpenFolderPanel(
                "选择AssetBundle保存路径",
                startPath,
                ""
            );

            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 转换为Unity项目相对路径
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    string relativePath =
                        "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    Debug.Log($"<color=green>已选择路径: {relativePath}</color>");
                    return relativePath;
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "路径错误",
                        "请选择项目Assets文件夹内的目录!",
                        "确定"
                    );
                }
            }

            return string.Empty;
        }

        //创建控制按钮
        private void CreateButton(
            VisualElement visual,
            Action buttonAction,
            string buttonTitle,
            string iconPath = null
        )
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
        private void CreateEnumField(
            VisualElement visual,
            string labelText,
            Enum defaultEnumValue,
            Action<Enum> onValueChanged
        )
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
        private void CreateToggle(
            VisualElement visual,
            string labelText,
            bool initialValue,
            Action<bool> onValueChanged
        )
        {
            // 创建容器
            VisualElement toggleContainer = new VisualElement();
            toggleContainer.AddToClassList("SettingItemContent");
            // 添加标签
            Label titleLabel = new Label(labelText);
            titleLabel.AddToClassList("Title");
            toggleContainer.Add(titleLabel);
            // 创建Toggle控件
            Toggle toggle = new Toggle() { value = initialValue };
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
        private void CreateTextField(
            VisualElement visual,
            string labelText,
            string initialValue,
            Action<string> onValueChanged
        )
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
            TextField textField = new TextField { value = initialValue ?? string.Empty };
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
