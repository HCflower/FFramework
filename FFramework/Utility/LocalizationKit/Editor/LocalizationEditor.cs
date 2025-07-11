using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System;

namespace LocalizationEditor
{
    /// <summary>
    /// 本地化编辑器
    /// </summary>
    public class LocalizationEditor : EditorWindow
    {
        private string currentDataName = "LocalizationData";       //当前localizationData文件名,默认为LocalizationData
        private bool isCreateCSV = true;                           //是否创建CSV文件
        private string localizationDataSavePath = "Assets/";       //localizationData文件保存路径,默认为项目路径
        private string csvSavePath = "Assets/";                    //csv文件保存路径,默认为项目路径
        private LocalizationData currentLocalizationData;          //当前localizationData
        private List<LocalizationData> localizationDataList = new List<LocalizationData>();
        private TextField nameInputField;                          //数据文件名输入框
        private TextField dataSaveField;                           //数据文件保存路径
        private TextField csvFileSaveField;                        //CSV文件保存路径
        private ScrollView scrollView;                             //数据列表
        private ObjectField csvSelectField;                        //CSV文件选择框

        [MenuItem("FFramework/🌐LocalizationKit &E", priority = 1)]
        public static void SkillEditorCreateWindow()
        {
            LocalizationEditor window = GetWindow<LocalizationEditor>();
            window.minSize = new Vector2(450, 325);
            window.titleContent = new GUIContent("LocalizationEditor");
            window.Show();
        }

        private void OnEnable()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/LocalizationEditor"));
            // 文件名控制区域
            TextInputControlField(rootVisualElement, ref nameInputField, "本地化数据名称:", "+", currentDataName,
            () =>
            {
                foreach (var item in localizationDataList)
                {
                    if (item.name == currentDataName)
                    {
                        Debug.Log($"本地化数据文件-><color=yellow>{currentDataName}已存在!</color>请重新输入本地化数据文件名.");
                        return;
                    }
                }
                LocalizationData newData = LocalizationEditorHandler.CreateLocalizationDataAndCSV(currentDataName, localizationDataSavePath, csvSavePath, isCreateCSV);
                newData.localizationDataPath = csvSavePath + currentDataName + ".csv";
                UpdateLocalizationDataSOItemView();
            });
            // CSV文件控制区域
            CreateCSVFileField(rootVisualElement);
            // 数据文件保存路径选择区域
            TextInputControlField(rootVisualElement, ref dataSaveField, "本地化数据保存路径:", "S", localizationDataSavePath, () => GetFolderPath(dataSaveField));
            // CSV文件保存路径选择区域
            TextInputControlField(rootVisualElement, ref csvFileSaveField, "CSV文件保存路径:", "S", csvSavePath, () => GetFolderPath(csvFileSaveField));
            // 数据项显示区域
            localizationDataScrollview(rootVisualElement);
            ControlButtonContent(rootVisualElement);
            UpdateLocalizationDataSOItemView();
        }

        private void OnDisable()
        {
            currentLocalizationData = null;
            // 保存所有数据
            foreach (var localizationData in localizationDataList)
            {
                AssetDatabase.SaveAssetIfDirty(localizationData);
            }
            localizationDataList.Clear();
            scrollView.Clear(); //清空数据列表
        }

        //数据文件名输入框
        private void TextInputControlField(VisualElement visual, ref TextField textInputField, string titleName, string buttonText, string textContent = null, Action action = null)
        {
            // 显示项区域
            Label label = new Label();
            label.AddToClassList("InputFieldContent");
            // 功能标题
            Label title = new Label();
            title.AddToClassList("LableTitle");
            title.text = titleName;
            label.Add(title);
            //输入框
            textInputField = new TextField();
            textInputField.AddToClassList("InputField");
            textInputField.value = textContent;
            textInputField.RegisterValueChangedCallback((evt) => { textContent = evt.newValue; });
            label.Add(textInputField);
            //功能按钮
            CreateFunctionButton(label, buttonText, action, out Button _);
            //添加到显示项区域
            visual.Add(label);
        }

        //创建CSV文件选择区域
        private void CreateCSVFileField(VisualElement visual)
        {
            Label label = new Label();
            label.AddToClassList("InputFieldContent");
            Label title = new Label();

            // 功能标题
            title.AddToClassList("LableTitle");
            title.text = "CSV文件";
            label.Add(title);

            // 创建CSV文件选择框
            csvSelectField = new ObjectField();
            csvSelectField.AddToClassList("InputField");
            csvSelectField.objectType = typeof(UnityEngine.Object);
            // 监听ObjectField的值变化
            csvSelectField.RegisterValueChangedCallback(evt =>
            {
                UnityEngine.Object selectedObject = evt.newValue as UnityEngine.Object;
                if (selectedObject != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                    if (!assetPath.EndsWith(".csv"))
                    {
                        Debug.Log("<color=yellow>请选择CSV文件</color> (.csv)");
                        csvSelectField.value = null;
                    }
                }
            });
            label.Add(csvSelectField);

            // 控制按钮
            CreateFunctionButton(label, "R",
            () =>
            {
                if (currentLocalizationData == null)
                {
                    Debug.Log("<color=yellow>请先选择一个Data文件</color>");
                    return;
                }
                if (csvSelectField.value == null)
                {
                    Debug.Log("<color=yellow>请先选择一个CSV文件</color>");
                    return;
                }

                if (!string.IsNullOrEmpty(csvFileSaveField.value))
                {
                    // 获取CSV文件路径
                    string assetPath = AssetDatabase.GetAssetPath(csvSelectField.value);
                    if (currentLocalizationData != null)
                    {
                        currentLocalizationData.localizationDataPath = assetPath;
                        // 标记为脏数据并保存
                        EditorUtility.SetDirty(currentLocalizationData);
                        AssetDatabase.SaveAssetIfDirty(currentLocalizationData);
                        Debug.Log($"路径已更新为: {assetPath}");
                    }
                }
                UpdateLocalizationDataSOItemView();
            }, out Button _);

            // 是否创建CSV文件单选框
            Toggle isCreateCSVToggle = new Toggle();
            isCreateCSVToggle.value = this.isCreateCSV;
            isCreateCSVToggle.AddToClassList("ToggleButton");
            isCreateCSVToggle.tooltip = "是否创建CSV文件";
            isCreateCSVToggle.RegisterValueChangedCallback((evt) =>
            {
                this.isCreateCSV = evt.newValue;
                Debug.Log($"是否创建CSV文件:{isCreateCSV}");
            });
            var textElement = isCreateCSVToggle.Q<VisualElement>(className: "unity-toggle__checkmark");
            textElement.style.width = 18;
            textElement.style.height = 18;
            label.Add(isCreateCSVToggle);

            visual.Add(label);
        }

        //获取文件夹路径
        private string GetFolderPath(TextField textField)
        {
            // 打开文件夹选择面板
            string exportPath = EditorUtility.OpenFolderPanel("选择导出路径", "Assets", "");
            // 确保选择的是相对 Assets 目录的路径
            if (!string.IsNullOrEmpty(exportPath) && exportPath.StartsWith(Application.dataPath))
            {
                // 取得相对路径
                exportPath = "Assets" + exportPath.Substring(Application.dataPath.Length);
                textField.value = exportPath;
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未在 Assets 文件夹中选择任何目录路径。", "Ok");
            }
            return exportPath;
        }

        //创建本地化Data文件选择区域
        private void localizationDataScrollview(VisualElement visual)
        {
            scrollView = new ScrollView();
            scrollView.AddToClassList("DataSOScrollview");
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            visual.Add(scrollView);
        }

        //刷新Data数据列表
        private void UpdateLocalizationDataSOItemView()
        {
            if (scrollView == null)
            {
                Debug.Log("<color=yellow>scrollView is null</color>");
                return;
            }
            localizationDataList.Clear();
            scrollView.Clear(); //清空数据列表
            localizationDataList = LocalizationEditorHandler.GetLocalizationDataSOList();
            foreach (var item in localizationDataList)
            {
                AddDataSOItem(item, scrollView);
            }
        }

        //创建Data数据列表项
        private void AddDataSOItem(LocalizationData data, VisualElement visual)
        {
            Label mainContent = new Label();
            mainContent.AddToClassList("DataViewContent");
            // 资源路径
            string assetPath = AssetDatabase.GetAssetPath(data);
            // 根据路径加载CSV文件
            TextAsset csvObject = data.localizationDataFile;
            // 数据文件
            CreateFunctionButton(mainContent, "", () =>
            {
                currentLocalizationData = data;
                // 聚焦到对象
                EditorGUIUtility.PingObject(data);
                dataSaveField.value = AssetDatabase.GetAssetPath(currentLocalizationData);
                currentDataName = data.name;
                nameInputField.value = data.name;
                //加载CSV文件
                if (csvObject != null)
                    csvSelectField.value = csvObject;
                else
                    csvSelectField.value = null;
            }, out Button soButton, "SelectItemButtonContent");
            // Data文件图标
            Label soIcon = new Label();
            soIcon.AddToClassList("DataSOIcon");
            soButton.Add(soIcon);
            // Data文件名称
            Label soTitle = new Label();
            soTitle.AddToClassList("ItemTitle");
            soTitle.text = data.name;
            soButton.Add(soTitle);

            // CSV文件
            CreateFunctionButton(mainContent, "", () =>
            {
                // 聚焦到对象
                EditorGUIUtility.PingObject(csvObject);
                csvFileSaveField.value = currentLocalizationData.localizationDataPath;
            }, out Button csvButton, "SelectItemButtonContent");
            // CSV文件图标
            Label csvIcon = new Label();
            csvIcon.AddToClassList("CSVIcon");
            csvButton.Add(csvIcon);
            // CSV文件名称
            Label csvTitle = new Label();
            csvTitle.AddToClassList("ItemTitle");
            if (csvObject == null) csvTitle.text = "Null";
            else csvTitle.text = csvObject.name;
            csvButton.Add(csvTitle);
            // 导入CSV文件数据
            CreateFunctionButton(mainContent, "I", () =>
            {
                LocalizationEditorHandler.ImportOrUpdateCSVToSO(data);
            }, out Button importCSVButton, "");
            // 删除当前的数据文件
            CreateFunctionButton(mainContent, "<color=red>X</color>", () =>
            {
                LocalizationEditorHandler.DeleteCurrentDataFile(data);

                UpdateLocalizationDataSOItemView();
            }, out Button deleteButton, "");
            visual.Add(mainContent);
        }

        //创建功能按钮
        private void CreateFunctionButton(VisualElement visual, string buttonText, Action action, out Button functionButton, string buttonStyle = null)
        {
            functionButton = new Button();
            functionButton.AddToClassList(!string.IsNullOrEmpty(buttonStyle) ? buttonStyle : "ControlButton");
            functionButton.text = buttonText;
            functionButton.clicked += action;
            visual.Add(functionButton);
        }

        //控制按钮区域
        private void ControlButtonContent(VisualElement visual)
        {
            Label controlButtonContent = new Label();
            controlButtonContent.AddToClassList("ControlButtonContent");
            //保存所有数据按钮
            CreateControllerButton(controlButtonContent, "Save All Data",
            () =>
            {
                foreach (var item in localizationDataList)
                {
                    AssetDatabase.SaveAssetIfDirty(item);
                    AssetDatabase.Refresh();
                }
            }, "Save");
            //更新所有数据按钮
            CreateControllerButton(controlButtonContent, "Update All Data",
            () =>
            {
                foreach (var item in localizationDataList)
                {
                    LocalizationEditorHandler.ImportOrUpdateCSVToSO(item);
                }
            }, "Refresh");
            //刷新视图按钮
            CreateControllerButton(controlButtonContent, "Refresh View",
            () =>
            {
                UpdateLocalizationDataSOItemView();
            }, "Refresh");

            visual.Add(controlButtonContent);
        }

        //创建控制按钮
        private void CreateControllerButton(VisualElement visual, string buttonName, Action action, string iconPath)
        {
            Button controllerButton = new Button();
            controllerButton.AddToClassList("GlobalControlButton");
            controllerButton.text = buttonName;
            controllerButton.clicked += () =>
            {
                action?.Invoke();
            };
            //图标
            Image buttonIcon = new Image();
            buttonIcon.AddToClassList("ControllButtonIcon");
            buttonIcon.style.backgroundImage = Resources.Load<Texture2D>($"Icon/{iconPath}");
            controllerButton.Add(buttonIcon);

            visual.Add(controllerButton);
        }
    }
}