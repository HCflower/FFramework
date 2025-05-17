using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 日志项数据
/// </summary>
public class LogItem
{
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public LogType LogType { get; set; }

    public LogItem(string message, string stackTrace, LogType logType)
    {
        Message = message;
        StackTrace = stackTrace;
        LogType = logType;
    }
}

/// <summary>
/// 运行时控制台数据
/// </summary>
public static class RuntimeConsoleData
{
    public static RuntimeConsoleDataType runtimeConsoleDataType = RuntimeConsoleDataType.Log;
    //堆栈信息
    public static Queue<LogItem> logQueue = new Queue<LogItem>();

    #region 是否显示对应类型日志
    public static bool showNormalLog = true;        //->普通日志
    public static bool showErrorLog = true;         //->错误日志
    public static bool showWarningLog = true;       //->警告日志
    public static bool isFoldLogInfo = false;       //->是否折叠日志信息
    public static LogItem currentLogItem = null;    //->当前日志项
    #endregion
}

/// <summary>
/// 运行时控制台数据类型
/// </summary>
public enum RuntimeConsoleDataType
{
    Log,           //日志
    Statistics,    //状态
}

