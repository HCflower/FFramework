using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;

namespace LocalizationEditor
{
    /// <summary>
    /// 本地化编辑器
    /// </summary>
    public class LocalizationEditor : EditorWindow
    {
        private string currentDataName = "LocalizationData";       //当前localizationData文件名,默认为LocalizationData
        private bool isCreateExcel = false;                        //是否创建Excel文件
        private string localizationDataSavePath = "Assets/";       //localizationData文件保存路径,默认为项目路径
        private string excelSavePath = "Assets/";                  //excel文件保存路径,默认为项目路径
        private LocalizationData currentLocalizationData;          //当前localizationData
        private List<LocalizationData> localizationDataList = new List<LocalizationData>();
        private TextField currentDataNameField;                    //数据文件名输入框
        private TextField excelSaveField;                          //Excel文件保存路径
        private TextField dataSaveField;                           //数据文件保存路径
        private ScrollView scrollView;                             //数据列表
        private ObjectField excelSelectField;                      //Excel文件选择框

        [MenuItem("FFramework/LocalizationKit &E", priority = 1)]
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
            CreateExcelFileField(rootVisualElement);
            DataSaveFolderField(rootVisualElement);
            ExcelSaveFolderField(rootVisualElement);
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
            Button createDataAddExcelButton = new Button();
            createDataAddExcelButton.AddToClassList("ControlButton");
            createDataAddExcelButton.AddToClassList("CreateDataAddExcelButton");
            createDataAddExcelButton.clicked += () =>
            {
                foreach (var item in localizationDataList)
                {
                    if (item.name == currentDataName)
                    {
                        Debug.Log($"本地化数据文件-><color=yellow>{currentDataName}已存在!</color>请重新输入本地化数据文件名.");
                        return;
                    }
                }
                LocalizationData newData = LocalizationEditorHandler.CreateLocalizationDataAndExcel(currentDataName, localizationDataSavePath, excelSavePath, isCreateExcel);
                UpdateLocalizationDataSOItemView();
            };
            label.Add(title);
            label.Add(currentDataNameField);
            label.Add(createDataAddExcelButton);
            visual.Add(label);
        }

        //创建Excel保存文件夹选择区域
        private void ExcelSaveFolderField(VisualElement visual)
        {
            Label label = new Label();
            label.AddToClassList("InputFieldContent");

            Label title = new Label();
            title.AddToClassList("LableTitle");
            title.text = "Excel文件保存路径";

            excelSaveField = new TextField();
            excelSaveField.AddToClassList("TextInputField");
            excelSaveField.value = excelSavePath;
            excelSaveField.RegisterValueChangedCallback((evt) => { excelSavePath = evt.newValue; });
            Button selectExcelSavePathButton = new Button();
            selectExcelSavePathButton.AddToClassList("ControlButton");
            selectExcelSavePathButton.AddToClassList("SelectFolderButton");
            selectExcelSavePathButton.clicked += () => GetFolderPath(excelSaveField);

            label.Add(title);
            label.Add(excelSaveField);
            label.Add(selectExcelSavePathButton);
            visual.Add(label);
        }

        //创建Excel文件选择区域
        private void CreateExcelFileField(VisualElement visual)
        {
            Label label = new Label();
            label.AddToClassList("InputFieldContent");
            Label title = new Label();

            title.AddToClassList("LableTitle");
            title.text = "Excel文件";

            Toggle isCreateExcelToggle = new Toggle();
            isCreateExcelToggle.value = this.isCreateExcel;
            isCreateExcelToggle.AddToClassList("ToggleButton");
            isCreateExcelToggle.RegisterValueChangedCallback((evt) =>
            {
                this.isCreateExcel = evt.newValue;
                Debug.Log($"是否创建Excel文件:{isCreateExcel}");
            });
            title.Add(isCreateExcelToggle);

            excelSelectField = new ObjectField();
            excelSelectField.AddToClassList("ExcelFileSelectField");
            excelSelectField.objectType = typeof(UnityEngine.Object);
            // 监听ObjectField的值变化
            excelSelectField.RegisterValueChangedCallback(evt =>
            {
                UnityEngine.Object selectedObject = evt.newValue as UnityEngine.Object;
                if (selectedObject != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                    if (!assetPath.EndsWith(".xlsx") && !assetPath.EndsWith(".xls"))
                    {
                        Debug.Log("<color=yellow>请选择Excel文件</color> (.xlsx 或 .xls)");
                        excelSelectField.value = null;
                    }
                }
            });
            Button ChangeSOExcelButton = new Button();
            ChangeSOExcelButton.AddToClassList("ControlButton");
            ChangeSOExcelButton.AddToClassList("ChangeExcelDataButton");
            ChangeSOExcelButton.clicked += () =>
            {
                if (currentLocalizationData == null)
                {
                    Debug.Log("<color=yellow>请先选择一个Data文件</color>");
                    return;
                }
                if (excelSelectField.value == null)
                {
                    Debug.Log("<color=yellow>请先选择一个Excel文件</color>");
                    return;
                }

                if (!string.IsNullOrEmpty(excelSaveField.value))
                {
                    // 获取Excel文件路径
                    string assetPath = AssetDatabase.GetAssetPath(excelSelectField.value);
                    currentLocalizationData.ExcelPath = assetPath;
                    Debug.Log($"{currentLocalizationData.name}文件的ExcelPath切换为-><color=yellow>{assetPath}</color>(请确保路径正确,且Excel文件存在!)");
                }
                UpdateLocalizationDataSOItemView();
            };

            label.Add(title);
            label.Add(excelSelectField);
            label.Add(ChangeSOExcelButton);
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
            // 根据路径加载Excel文件
            Object excelObject = AssetDatabase.LoadAssetAtPath<Object>(data.ExcelPath);

            // 添加点击事件处理
            soButton.clicked += () =>
            {
                currentLocalizationData = data;
                // 聚焦到对象
                EditorGUIUtility.PingObject(data);
                currentDataName = data.name;
                currentDataNameField.value = data.name;
                //加载Excel文件
                if (excelObject != null) excelSelectField.value = excelObject;
                else excelSelectField.value = null;
            };
            Label soTitle = new Label();
            soTitle.AddToClassList("ItemTitle");
            soTitle.text = data.name;
            soContent.Add(soButton);
            soButton.Add(soIcon);
            soButton.Add(soTitle);

            Label linkIcon = new Label();
            linkIcon.AddToClassList("LinkIcon");

            Label excelContent = new Label();
            excelContent.AddToClassList("SelectItemButtonContent");

            Button excelButton = new Button();
            excelButton.AddToClassList("ControlButton");
            excelButton.AddToClassList("SelectExcelFileButton");
            excelButton.clicked += () =>
            {
                // 聚焦到对象
                EditorGUIUtility.PingObject(excelObject);
            };

            Label excelIcon = new Label();
            excelIcon.AddToClassList("ExcelIcon");

            Label excelTitle = new Label();
            excelTitle.AddToClassList("ItemTitle");
            if (excelObject == null) excelTitle.text = "Null";
            else excelTitle.text = excelObject.name;
            excelContent.Add(excelButton);
            excelButton.Add(excelIcon);
            excelButton.Add(excelTitle);

            Button import = new Button();
            import.AddToClassList("ControlButton");
            import.AddToClassList("ExcelImportToSOButton");
            import.clicked += () => LocalizationEditorHandler.ImportOrUpdateExcelToSO(data);

            mainContent.Add(soContent);
            mainContent.Add(linkIcon);
            mainContent.Add(excelContent);
            soContent.Add(import);
            visual.Add(mainContent);
        }

        //控制按钮区域
        private void ControlButtonContent(VisualElement visual)
        {
            Label controlButtonContent = new Label();
            controlButtonContent.AddToClassList("ControlButtonContent");
            SaveAllDataButton(controlButtonContent);
            UpdateAllSODataButton(controlButtonContent);
            UpdateItemViewButton(controlButtonContent);
            visual.Add(controlButtonContent);
        }

        //刷新数据视图按钮
        private void UpdateItemViewButton(VisualElement visual)
        {
            Button UpdateItemView = new Button();
            UpdateItemView.AddToClassList("ControlDataButton");
            UpdateItemView.text = "Refresh Data View";
            UpdateItemView.clicked += () =>
            {
                UpdateLocalizationDataSOItemView();
            };
            visual.Add(UpdateItemView);
        }

        //刷新所有Data文件数据
        private void UpdateAllSODataButton(VisualElement visual)
        {
            Button updateAllSOData = new Button();
            updateAllSOData.AddToClassList("ControlDataButton");
            updateAllSOData.text = "Update All Data";
            updateAllSOData.clicked += () =>
            {
                foreach (var item in localizationDataList)
                {
                    LocalizationEditorHandler.ImportOrUpdateExcelToSO(item);
                }
            };
            visual.Add(updateAllSOData);
        }

        //保存存所有Data文件数据
        private void SaveAllDataButton(VisualElement visual)
        {
            Button saveAllSOData = new Button();
            saveAllSOData.AddToClassList("ControlDataButton");
            saveAllSOData.text = "Save All Data";
            saveAllSOData.clicked += () =>
            {
                foreach (var item in localizationDataList)
                {
                    EditorUtility.SetDirty(item);
                    AssetDatabase.SaveAssetIfDirty(item);
                    AssetDatabase.Refresh();
                }
            };
            visual.Add(saveAllSOData);
        }
    }
}