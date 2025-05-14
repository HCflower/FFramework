using System.Collections.Generic;
using System;

///<summary>
/// UI数据VM类，实现属性通知
/// 支持多播委托
/// </summary>
public class ViewModelBase
{
    // 存储每个属性的回调
    private readonly Dictionary<string, Delegate> propertyHandlerDic = new Dictionary<string, Delegate>();

    /// <summary>
    /// 注册属性监听
    /// </summary>
    public void RegisterPropertyChanged<T>(string propertyName, Action<T> callback)
    {
        if (callback == null) return;
        if (!propertyHandlerDic.ContainsKey(propertyName))
            propertyHandlerDic[propertyName] = callback;
        else
            propertyHandlerDic[propertyName] = Delegate.Combine(propertyHandlerDic[propertyName], callback);
    }

    /// <summary>
    /// 取消注册属性监听
    /// </summary>
    public void UnregisterPropertyChanged<T>(string propertyName, Action<T> callback)
    {
        if (callback == null || !propertyHandlerDic.ContainsKey(propertyName)) return;
        propertyHandlerDic[propertyName] = Delegate.Remove(propertyHandlerDic[propertyName], callback);
        // 如果没有回调了，移除该属性的记录
        if (propertyHandlerDic[propertyName] == null)
        {
            propertyHandlerDic.Remove(propertyName);
        }
    }

    /// <summary>
    /// 清除所有属性监听
    /// </summary>
    public void ClearAllPropertyChanged()
    {
        propertyHandlerDic.Clear();
    }

    /// <summary>
    /// 触发属性变更
    /// </summary>
    protected void OnPropertyChanged<T>(string propertyName, T newValue)
    {
        if (propertyHandlerDic.TryGetValue(propertyName, out var handler))
        {
            (handler as Action<T>)?.Invoke(newValue);
        }
    }
}
