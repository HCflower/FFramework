using UnityEngine.UIElements;
using UnityEngine;
using System;

/// <summary>
/// 调试视图
/// </summary>
public class DebugView : VisualElement
{
    private VisualElement mainContent;
    private ScrollView logInfoScrollView;
    private ScrollView logDetailsScrollView;
    private Label logDetailsText;
    private int logItemCounter = 0; // 用于生成唯一的LogItemID

    public void Init(VisualElement visual)
    {
        mainContent = new VisualElement();
        mainContent.styleSheets.Add(Resources.Load<StyleSheet>("USS/DebugView"));
        mainContent.AddToClassList("MainContent");
        //创建Log控制区域
        CreateLogControllerContent(mainContent);
        //创建Log信息区域
        CreateLogInfoScrollView(mainContent);
        //创建Log详情信息区域
        CreateLogDetailsScrollView(mainContent);
        visual.Add(mainContent);
        // 立即更新LogInfo视图
        UpdateLogInfoView();
    }

    public void UpdateLogView()
    {
        mainContent.Clear();
        //创建Log控制区域
        CreateLogControllerContent(mainContent);
        //创建Log信息区域
        CreateLogInfoScrollView(mainContent);
        //创建Log详情信息区域
        CreateLogDetailsScrollView(mainContent);
        // 立即更新LogInfo视图
        UpdateLogInfoView();
    }

    //创建Log控制区域
    private void CreateLogControllerContent(VisualElement visual)
    {
        VisualElement logControllerContent = new VisualElement();
        logControllerContent.AddToClassList("LogControllerContent");

        //创建显示NormalLog控制按钮
        CreateLogContentButton(logControllerContent, "LogNormalIcon", "NormalLog", () =>
        {
            RuntimeConsoleData.showNormalLog = !RuntimeConsoleData.showNormalLog;
            UpdateLogView();
        }, out Button showNormalLogBtn);
        if (RuntimeConsoleData.showNormalLog) showNormalLogBtn.AddToClassList("LogControllerButton-Selected");

        //创建显示WarningLog控制按钮
        CreateLogContentButton(logControllerContent, "LogWarningIcon", "WarningLog", () =>
        {
            RuntimeConsoleData.showWarningLog = !RuntimeConsoleData.showWarningLog;
            UpdateLogView();
        }, out Button showWarningLogBtn);
        if (RuntimeConsoleData.showWarningLog) showWarningLogBtn.AddToClassList("LogControllerButton-Selected");

        //创建显示ErrorLog控制按钮
        CreateLogContentButton(logControllerContent, "LogErrorIcon", "ErrorLog", () =>
        {
            RuntimeConsoleData.showErrorLog = !RuntimeConsoleData.showErrorLog;
            UpdateLogView();
        }, out Button showErrorLogBtn);
        if (RuntimeConsoleData.showErrorLog) showErrorLogBtn.AddToClassList("LogControllerButton-Selected");

        //创建是否折叠信息按钮
        CreateLogContentButton(logControllerContent, "FoldLogIcon", "FoldLog", () =>
        {
            RuntimeConsoleData.isFoldLogInfo = !RuntimeConsoleData.isFoldLogInfo;
            UpdateLogView();
        }, out Button isFoldLogInfo);
        if (RuntimeConsoleData.isFoldLogInfo) isFoldLogInfo.AddToClassList("LogControllerButton-Selected");

        //创建清理信息按钮
        CreateLogContentButton(logControllerContent, "ClearAllLogInfoIcon", "ClearAllInfo", () =>
        {
            RuntimeConsoleData.logQueue.Clear();
            UpdateLogView();
        }, out Button _);

        visual.Add(logControllerContent);
    }

    //创建Log控制按钮
    private void CreateLogContentButton(VisualElement visual, string iconPath, string title, Action action, out Button logContentButton)
    {
        logContentButton = new Button();
        logContentButton.AddToClassList("LogControllerButton");
        logContentButton.text = title;

        logContentButton.clicked += () => { action?.Invoke(); };
        //Icon
        Image icon = new Image();
        icon.AddToClassList("LogControllerButton-Icon");
        icon.style.backgroundImage = Resources.Load<Texture2D>($"Icon/{iconPath}");
        logContentButton.Add(icon);

        visual.Add(logContentButton);
    }

    //创建Log信息区域
    private void CreateLogInfoScrollView(VisualElement visual)
    {
        logInfoScrollView = new ScrollView();
        logInfoScrollView.AddToClassList("LogInfoScrollView");
        visual.Add(logInfoScrollView);
    }

    //创建Log详情信息区域
    private void CreateLogDetailsScrollView(VisualElement visual)
    {
        logDetailsScrollView = new ScrollView();
        logDetailsScrollView.AddToClassList("LogDetailsScrollView");

        logDetailsText = new Label();
        logDetailsText.AddToClassList("LogDetailsText");
        logDetailsText.text = "Please select Log Info (^_^)";
        logDetailsScrollView.Add(logDetailsText);
        visual.Add(logDetailsScrollView);
    }

    //创建Log信息项
    private void CreateLogInfoItem(VisualElement visual, LogItem logItem, LogType logType = LogType.Log)
    {
        //log信息项
        Button logInfoItem = new Button();
        logInfoItem.AddToClassList("LogInfoItemDefault");

        // 添加奇偶行交替样式
        if (logItemCounter % 2 == 0) logInfoItem.AddToClassList("LogInfoItemColor-1");
        else logInfoItem.AddToClassList("LogInfoItemColor-2");

        //log图标        
        Image icon = new Image();
        icon.AddToClassList("LogIcon");
        logInfoItem.Add(icon);

        if (RuntimeConsoleData.isFoldLogInfo)
        {
            //折叠信息
            Label foldLogInfoCount = new Label();
            foldLogInfoCount.AddToClassList("FoldLogInfoCount");
            logInfoItem.Add(foldLogInfoCount);
            //TODO:实现折叠后计数功能
            foldLogInfoCount.text = "~";
            // 检查是否已存在相同日志项
            foreach (var child in logInfoScrollView.Children())
            {
                if (child is Button existingItem &&
                existingItem.text == logItem.Message &&
                (existingItem.ClassListContains($"Log{logType}") ||
                 (logType == LogType.Log && existingItem.ClassListContains("LogNormal"))))
                {
                    //只保留一个相同项
                    return;
                }
            }
        }
        logItemCounter++;
        visual.Add(logInfoItem);
        // 根据日志类型添加样式类
        switch (logType)
        {
            case LogType.Warning:
                logInfoItem.AddToClassList("LogWarning");
                icon.AddToClassList("LogWarning-Icon");
                break;
            case LogType.Error:
                logInfoItem.AddToClassList("LogError");
                icon.AddToClassList("LogError-Icon");
                break;
            default:
                logInfoItem.AddToClassList("LogNormal");
                icon.AddToClassList("LogNormal-Icon");
                break;
        }
        logInfoItem.text = logItem.Message;

        // 保存参数到局部变量避免闭包问题
        var detailsText = logDetailsText;
        var trace = logItem.StackTrace;
        var type = logType;
        logInfoItem.clicked += () =>
        {
            //设置当前日志项
            RuntimeConsoleData.currentLogItem = logItem;
            // 清除所有日志类型样式
            detailsText.RemoveFromClassList("LogNormal-Details");
            detailsText.RemoveFromClassList("LogWarning-Details");
            detailsText.RemoveFromClassList("LogError-Details");
            // 根据日志类型添加对应样式
            switch (type)
            {
                case LogType.Warning:
                    detailsText.AddToClassList("LogWarning-Details");
                    break;
                case LogType.Error:
                    detailsText.AddToClassList("LogError-Details");
                    break;
                default:
                    detailsText.AddToClassList("LogNormal-Details");
                    break;
            }
            if (!string.IsNullOrEmpty(trace))
            {
                detailsText.text = trace;
            }
        };
    }

    public void AddLogInfoItem(LogItem logItem, LogType logType = LogType.Log)
    {
        CreateLogInfoItem(logInfoScrollView, logItem, logType);
    }

    public void UpdateLogInfoView()
    {
        // 先清空UI
        logInfoScrollView.Clear();
        // 重置计数器
        logItemCounter = 0;

        // 遍历队列但不移除元素
        foreach (var logItem in RuntimeConsoleData.logQueue)
        {
            // 根据日志类型判断是否需要显示
            bool shouldShow = false;
            switch (logItem.LogType)
            {
                case LogType.Log:
                    shouldShow = RuntimeConsoleData.showNormalLog;
                    break;
                case LogType.Warning:
                    shouldShow = RuntimeConsoleData.showWarningLog;
                    break;
                case LogType.Error:
                    shouldShow = RuntimeConsoleData.showErrorLog;
                    break;
            }
            if (shouldShow)
            {
                AddLogInfoItem(logItem, logItem.LogType);
            }
        }
    }
}
