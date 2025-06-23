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
        private bool isCreateCSV = false;                          //是否创建CSV文件
        private string localizationDataSavePath = "Assets/";       //localizationData文件保存路径,默认为项目路径
        private string csvSavePath = "Assets/";                    //csv文件保存路径,默认为项目路径
        private LocalizationData currentLocalizationData;          //当前localizationData
        private List<LocalizationData> localizationDataList = new List<LocalizationData>();
        private TextField currentDataNameField;                    //数据文件名输入框
        private TextField csvSaveField;                            //CSV文件保存路径
        private TextField dataSaveField;                           //数据文件保存路径
        private ScrollView scrollView;                             //数据列表
        private ObjectField csvSelectField;                        //CSV文件选择框

        [MenuItem("FFramework/🌐LocalizationKit &E", priority = 1)]
        public static void SkillEditorCreateWindow()
        {
            LocalizationEditor window = GetWindow<LocalizationEditor>();
            window.minSize = new Vector2(450, 330);
            window.titleContent = new GUIContent("LocalizationEditor");
            window.Show();
        }

        private void OnEnable()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/LocalizationEditor"));
            DataNameField(rootVisualElement);
            CreateCSVFileField(rootVisualElement);
            DataSaveFolderField(rootVisualElement);
            CSVSaveFolderField(rootVisualElement);
            DataSOScrollview(rootVisualElement);
            ControlButtonContent(rootVisualElement);
            UpdateLocalizationDataSOItemView();
        }

        private void OnDisable()
        {
            currentLocalizationData = null;
            localizationDataList.Clear();
            scrollView.Clear(); //清空数据列表
        }

        //数据文件名输入框
        private void DataNameField(VisualElement visual)
        {
            Label label = new Label();
            label.AddToClassList("InputFieldContent");

            Label title = new Label();
            title.AddToClassList("LableTitle");
            title.text = "本地化数据文件名";

            currentDataNameField = new TextField();
            currentDataNameField.AddToClassList("TextInputField");
            currentDataNameField.value = currentDataName;
            currentDataNameField.RegisterValueChangedCallback((evt) => { currentDataName = evt.newValue; });
            Button createDataAddCSVButton = new Button();
            createDataAddCSVButton.AddToClassList("ControlButton");
            createDataAddCSVButton.AddToClassList("CreateDataAddCSVButton");
            createDataAddCSVButton.clicked += () =>
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
                UpdateLocalizationDataSOItemView();
            };
            label.Add(title);
            label.Add(currentDataNameField);
            label.Add(createDataAddCSVButton);
            visual.Add(label);
        }

        //创建CSV保存文件夹选择区域
        private void CSVSaveFolderField(VisualElement visual)
        {
            Label label = new Label();
            label.AddToClassList("InputFieldContent");

            Label title = new Label();
            title.AddToClassList("LableTitle");
            title.text = "CSV文件保存路径";

            csvSaveField = new TextField();
            csvSaveField.AddToClassList("TextInputField");
            csvSaveField.value = csvSavePath;
            csvSaveField.RegisterValueChangedCallback((evt) => { csvSavePath = evt.newValue; });
            Button selectCSVSavePathButton = new Button();
            selectCSVSavePathButton.AddToClassList("ControlButton");
            selectCSVSavePathButton.AddToClassList("SelectFolderButton");
            selectCSVSavePathButton.clicked += () => GetFolderPath(csvSaveField);

            label.Add(title);
            label.Add(csvSaveField);
            label.Add(selectCSVSavePathButton);
            visual.Add(label);
        }

        //创建CSV文件选择区域
        private void CreateCSVFileField(VisualElement visual)
        {
            Label label = new Label();
            label.AddToClassList("InputFieldContent");
            Label title = new Label();

            title.AddToClassList("LableTitle");
            title.text = "CSV文件";

            Toggle isCreateCSVToggle = new Toggle();
            isCreateCSVToggle.value = this.isCreateCSV;
            isCreateCSVToggle.AddToClassList("ToggleButton");
            isCreateCSVToggle.RegisterValueChangedCallback((evt) =>
            {
                this.isCreateCSV = evt.newValue;
                Debug.Log($"是否创建CSV文件:{isCreateCSV}");
            });
            title.Add(isCreateCSVToggle);

            csvSelectField = new ObjectField();
            csvSelectField.AddToClassList("CSVFileSelectField");
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
            Button ChangeSOCSVButton = new Button();
            ChangeSOCSVButton.AddToClassList("ControlButton");
            ChangeSOCSVButton.AddToClassList("ChangeCSVDataButton");
            ChangeSOCSVButton.clicked += () =>
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

                if (!string.IsNullOrEmpty(csvSaveField.value))
                {
                    // 获取CSV文件路径
                    string assetPath = AssetDatabase.GetAssetPath(csvSelectField.value);
                    Debug.Log($"{currentLocalizationData.name}文件的CSVPath切换为-><color=yellow>{assetPath}</color>(请确保路径正确,且CSV文件存在!)");
                }
                UpdateLocalizationDataSOItemView();
            };

            label.Add(title);
            label.Add(csvSelectField);
            label.Add(ChangeSOCSVButton);
            visual.Add(label);
        }

        //创建Data保存文件夹选择区域
        private void DataSaveFolderField(VisualElement visual)
        {
            Label label = new Label();
            label.AddToClassList("InputFieldContent");
            Label title = new Label();

            title.AddToClassList("LableTitle");
            title.text = "数据文件保存路径";

            dataSaveField = new TextField();
            dataSaveField.AddToClassList("TextInputField");
            dataSaveField.value = localizationDataSavePath;
            dataSaveField.RegisterValueChangedCallback((evt) =>
            {
                localizationDataSavePath = evt.newValue;
            });
            Button selectDataSavePathButton = new Button();
            selectDataSavePathButton.AddToClassList("ControlButton");
            selectDataSavePathButton.AddToClassList("SelectFolderButton");
            selectDataSavePathButton.clicked += () => GetFolderPath(dataSaveField);

            label.Add(title);
            label.Add(dataSaveField);
            label.Add(selectDataSavePathButton);
            visual.Add(label);
        }

        //获取文件夹路径
        private string GetFolderPath(TextField textField)
        {
            // 打开文件夹选择面板
            string exportPath = EditorUtility.OpenFolderPanel("Select export path", "Assets", "");
            // 确保选择的是相对 Assets 目录的路径
            if (!string.IsNullOrEmpty(exportPath) && exportPath.StartsWith(Application.dataPath))
            {
                // 取得相对路径
                exportPath = "Assets" + exportPath.Substring(Application.dataPath.Length);
                textField.value = exportPath;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No directory path was selected within the Assets folder.", "Ok");
            }
            return exportPath;
        }

        //创建Data文件选择区域
        private void DataSOScrollview(VisualElement visual)
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
            mainContent.AddToClassList("MainContent");

            Label soContent = new Label();
            soContent.AddToClassList("SelectItemButtonContent");

            Label soIcon = new Label();
            soIcon.AddToClassList("DataSOIcon");

            Button soButton = new Button();
            soButton.AddToClassList("ControlButton");
            soButton.AddToClassList("SelectSODataFileButton");

            string assetPath = AssetDatabase.GetAssetPath(data);
            // 根据路径加载CSV文件
            TextAsset csvObject = data.localizationDataFile;

            // 添加点击事件处理
            soButton.clicked += () =>
            {
                currentLocalizationData = data;
                // 聚焦到对象
                EditorGUIUtility.PingObject(data);
                currentDataName = data.name;
                currentDataNameField.value = data.name;
                //加载CSV文件
                if (csvObject != null) csvSelectField.value = csvObject;
                else csvSelectField.value = null;
            };

            Label soTitle = new Label();
            soTitle.AddToClassList("ItemTitle");
            soTitle.text = data.name;
            soContent.Add(soButton);
            soButton.Add(soIcon);
            soButton.Add(soTitle);

            Label linkIcon = new Label();
            linkIcon.AddToClassList("LinkIcon");

            Label csvContent = new Label();
            csvContent.AddToClassList("SelectItemButtonContent");

            Button csvButton = new Button();
            csvButton.AddToClassList("ControlButton");
            csvButton.AddToClassList("SelectCSVFileButton");
            csvButton.clicked += () =>
            {
                // 聚焦到对象
                EditorGUIUtility.PingObject(csvObject);
            };

            Label csvIcon = new Label();
            csvIcon.AddToClassList("CSVIcon");

            Label csvTitle = new Label();
            csvTitle.AddToClassList("ItemTitle");
            if (csvObject == null) csvTitle.text = "Null";
            else csvTitle.text = csvObject.name;
            csvContent.Add(csvButton);
            csvButton.Add(csvIcon);
            csvButton.Add(csvTitle);

            Button import = new Button();
            import.AddToClassList("ControlButton");
            import.AddToClassList("CSVImportToSOButton");
            import.clicked += () => LocalizationEditorHandler.ImportOrUpdateCSVToSO(data);

            mainContent.Add(soContent);
            mainContent.Add(linkIcon);
            mainContent.Add(csvContent);
            soContent.Add(import);
            visual.Add(mainContent);
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
            controllerButton.AddToClassList("ControlDataButton");
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