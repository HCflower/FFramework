using UnityEngine.UIElements;
using UnityEngine;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RuntimeConsoleView : MonoBehaviour
{
    #region 数据
    [SerializeField] private bool isMin = true;         // 是否最小化
    private float timer = 0;                            // 数据刷新间隔 
    #endregion

    #region 视觉元素
    private VisualElement root;                         // 根 
    private VisualElement mainContent;                  // 主内容
    private Button leastView;                           // 最小视图
    private VisualElement infoArea;                     // 消息区域
    private DebugView debugView;
    private StatisticsView statusView;
    #endregion

    private void OnEnable()
    {
        InitRootView();
        CreateLeastView(root);
        // 注册日志接收事件
        Application.logMessageReceived += HandleLog;
    }

    void Update()
    {
        if (RuntimeConsoleData.runtimeConsoleDataType == RuntimeConsoleDataType.Statistics && statusView != null)
        {
            timer += Time.deltaTime;
            if (timer > 0.1f)
            {
                timer = 0;
                statusView.UpdateStatisticsInfoView();
            }
        }
    }

    private void OnDisable()
    {
        // 清除现有内容
        root.Clear();
        // 注销日志接收事件
        Application.logMessageReceived -= HandleLog;
    }

    private void InitRootView()
    {
        // 创建UI文档
        var uiDocument = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
        // 获取根VisualElement
        root = uiDocument.rootVisualElement;
        root.styleSheets.Add(Resources.Load<StyleSheet>("USS/RuntimeConsoleView"));
        root.AddToClassList("Root");
        // 清除现有内容
        root.Clear();
    }

    private void CreateRuntimeConsoleView(VisualElement visual)
    {
        mainContent = new VisualElement();
        mainContent.AddToClassList("MainContent");
        ControllerBar(mainContent);
        CreateInfoArea(mainContent);
        visual.Add(mainContent);
    }

    private void CreateLeastView(VisualElement visual)
    {
        leastView = new Button();
        leastView.AddToClassList("LeastView");
        leastView.text = "RuntimeConsole";
        leastView.clicked += () =>
        {
            if (isMin)
            {
                isMin = !isMin;
                CreateRuntimeConsoleView(root);
                //默认显示DebugView
                if (RuntimeConsoleData.runtimeConsoleDataType == RuntimeConsoleDataType.Log) CreateDebugInfoArea(infoArea);
                else if (RuntimeConsoleData.runtimeConsoleDataType == RuntimeConsoleDataType.Statistics) CreateStatusInfoArea(infoArea);
                root.Remove(leastView);
            }
        };
        visual.Add(leastView);
    }

    private void ControllerBar(VisualElement visual)
    {
        VisualElement toolbar = new VisualElement();
        toolbar.AddToClassList("ControllerBar");

        //当前选择类型信息Icon
        Image icon = new Image();
        icon.AddToClassList("TitleIcon");
        toolbar.Add(icon);


        CreateTypeSelectEnumField(toolbar, out Label typeIcon);
        typeIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/Type");

        CreateControllerButton(toolbar, "CopyToClipboard", () =>
        {
            CopyLogItemInfoToClipboard();
        }, out Label clipboardIcon);
        clipboardIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/CopyToClipboard");

        CreateControllerButton(toolbar, "SaveAllInfoToLocal", () =>
        {
            SaveAllLogItemInfoToLocal();
        }, out Label emailIcon);
        emailIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/SaveInfoToLocal");

        CreateControllerButton(toolbar, "UpdateView", () =>
        {
            if (debugView != null && RuntimeConsoleData.runtimeConsoleDataType == RuntimeConsoleDataType.Log)
            {

                CreateDebugInfoArea(infoArea);
            }
            else if (statusView != null && RuntimeConsoleData.runtimeConsoleDataType == RuntimeConsoleDataType.Statistics)
            {
                CreateStatusInfoArea(infoArea);
            }
        }, out Label updateIcon);
        updateIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/Log-Refresh");
        CreateControllerButton(toolbar, "LeastView", () =>
        {
            if (!isMin)
            {
                isMin = !isMin;
                CreateLeastView(root);
                root.Remove(mainContent);

            }
        }, out Label leastViewIcon);
        leastViewIcon.style.backgroundImage = Resources.Load<Texture2D>("Icon/LeastView");

        visual.Add(toolbar);
    }

    //创建控制按钮
    private void CreateControllerButton(VisualElement visual, string buttonTitle, Action action, out Label icon)
    {
        Button changeViewButton = new Button();
        changeViewButton.AddToClassList("ControllerBar-Button");
        changeViewButton.clicked += () => action();

        Label mIcon = new Label();
        mIcon.AddToClassList("Icon");
        changeViewButton.Add(mIcon);

        Label name = new Label();
        name.text = buttonTitle;
        name.AddToClassList("ControllerButton-Title");
        changeViewButton.Add(name);

        icon = mIcon;

        visual.Add(changeViewButton);
    }

    //创建类型选择下拉列表
    private void CreateTypeSelectEnumField(VisualElement visual, out Label icon)
    {
        EnumField typeSelectEnumField = new EnumField(RuntimeConsoleDataType.Log)
        {
            value = RuntimeConsoleData.runtimeConsoleDataType
        };
        typeSelectEnumField.RegisterValueChangedCallback(evt =>
        {
            RuntimeConsoleData.runtimeConsoleDataType = (RuntimeConsoleDataType)evt.newValue;
            if (RuntimeConsoleData.runtimeConsoleDataType == RuntimeConsoleDataType.Log)
            {

                CreateDebugInfoArea(infoArea);
            }
            else if (RuntimeConsoleData.runtimeConsoleDataType == RuntimeConsoleDataType.Statistics)
            {
                CreateStatusInfoArea(infoArea);
            }
        });
        typeSelectEnumField.AddToClassList("ControllerBar-Button");
        typeSelectEnumField.AddToClassList("ControllerBar-EnumField");
        //Icon
        Label mIcon = new Label();
        mIcon.AddToClassList("Icon");
        typeSelectEnumField.Add(mIcon);
        icon = mIcon;

        visual.Add(typeSelectEnumField);
    }

    //创建信息区域
    private void CreateInfoArea(VisualElement visual)
    {
        infoArea = new VisualElement();
        infoArea.AddToClassList("InfoArea");
        visual.Add(infoArea);
    }

    //清理信息区域
    private void CleanInfoArea()
    {
        infoArea.Clear();
    }

    //创建Debug信息区域
    private void CreateDebugInfoArea(VisualElement visual)
    {
        CleanInfoArea();
        debugView = new DebugView();
        debugView.Init(visual);
        debugView.UpdateLogInfoView();
    }

    //创建状态信息区域
    private void CreateStatusInfoArea(VisualElement visual)
    {
        CleanInfoArea();
        statusView = new StatisticsView();
        statusView.Init(visual);
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 强制获取完整堆栈信息
        {

            // if (string.IsNullOrEmpty(stackTrace) || !stackTrace.Contains("(at "))
            // {
            //     try
            //     {
            //         // 使用System.Diagnostics获取更详细的堆栈信息
            //         var st = new System.Diagnostics.StackTrace(1, true);
            //         var frames = st.GetFrames();
            //         if (frames != null && frames.Length > 0)
            //         {
            //             var sb = new System.Text.StringBuilder();
            //             foreach (var frame in frames)
            //             {
            //                 var method = frame.GetMethod();
            //                 var fileName = frame.GetFileName();
            //                 var lineNumber = frame.GetFileLineNumber();

            //                 if (!string.IsNullOrEmpty(fileName) && lineNumber > 0)
            //                 {
            //                     sb.AppendFormat("{0}.{1} (at {2}:{3})",
            //                         method.DeclaringType,
            //                         method.Name,
            //                         fileName.Replace(Application.dataPath, "Assets"),
            //                         lineNumber);
            //                 }
            //                 else
            //                 {
            //                     sb.AppendFormat("{0}.{1}", method.DeclaringType, method.Name);
            //                 }
            //                 sb.AppendLine();
            //             }
            //             stackTrace = sb.ToString();
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         stackTrace = "Failed to get detailed stack trace: " + ex.Message;
            //     }
            // }
        }
        // 将日志加入队列
        RuntimeConsoleData.logQueue.Enqueue(new LogItem($"{logString}", stackTrace, type));
        //刷新数据显示
        if (debugView != null) debugView.UpdateLogView();
    }

    //复制当前信息到剪切板
    private void CopyLogItemInfoToClipboard()
    {
        if (RuntimeConsoleData.currentLogItem == null)
        {
            Debug.LogWarning("No selected log entries can be copied!");
            return;
        }

        try
        {
            // 格式化日志信息
            string logInfo = $"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                             $"类型: {RuntimeConsoleData.currentLogItem.LogType}\n" +
                             $"内容: {RuntimeConsoleData.currentLogItem.Message}\n" +
                             $"堆栈: {RuntimeConsoleData.currentLogItem.StackTrace}";

            // 复制到剪贴板
            GUIUtility.systemCopyBuffer = logInfo;
            Debug.Log("The log information has been copied to the clipboard");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to copy log information: {e.Message}");
        }
    }

    //保存所有Log信息到本地
    private void SaveAllLogItemInfoToLocal()
    {
        //生成文件名：年(yyyy)月(MM)日(dd)时(HH)分(mm)秒(ss)_Log.txt
        //示例：20251019120000_Log.txt 表示2025年10月19日12点00分00秒
        string fileName = $"{DateTime.Now:yyyy-MM-dd HH_mm_ss}_Log.txt";
        string path = "";

#if UNITY_EDITOR
        //编辑器环境下使用保存文件对话框
        path = EditorUtility.SaveFilePanel("保存日志文件", Application.dataPath, fileName, "txt");
#else
        //非编辑器环境下使用固定可读写文件夹路径
        path = Path.Combine(Application.persistentDataPath, fileName);
#endif

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Unsave the log file.");
            return;
        }

        try
        {
            //创建或覆盖文件
            using (StreamWriter writer = new StreamWriter(path))
            {
                //写入所有日志项
                foreach (var logItem in RuntimeConsoleData.logQueue)
                {
                    writer.WriteLine($"[{logItem.LogType}] {logItem.Message}");
                    if (!string.IsNullOrEmpty(logItem.StackTrace))
                    {
                        writer.WriteLine(logItem.StackTrace);
                    }
                    //空行分隔
                    writer.WriteLine();
                }
            }
            Debug.Log($"The log has been saved to: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save log file: {e.Message}");
        }
    }

    //TODO:发送所有Log信息到邮箱
}
