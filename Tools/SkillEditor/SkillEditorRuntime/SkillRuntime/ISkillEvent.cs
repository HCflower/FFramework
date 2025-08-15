using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能编辑器事件接口
    /// </summary>    
    public interface ISkillEvent
    {
        void TriggerSkillEvent(string eventName);
        void TriggerSkillEvent<T>(string eventName, T eventData);
    }

    [DisallowMultipleComponent]
    public class SkillEvent : MonoBehaviour, ISkillEvent
    {
        // 按事件名存储回调
        private Dictionary<string, Delegate> eventDic = new Dictionary<string, Delegate>();

        // 注册事件监听（带参数）
        public void AddSkillEventListener<T>(string eventName, Action<T> callback)
        {
            if (!eventDic.ContainsKey(eventName)) eventDic[eventName] = null;
            eventDic[eventName] = Delegate.Combine(eventDic[eventName], callback);
        }

        // 注册监听（无参数）
        public void AddSkillEventListener(string eventName, Action callback)
        {
            if (!eventDic.ContainsKey(eventName)) eventDic[eventName] = null;
            eventDic[eventName] = Delegate.Combine(eventDic[eventName], callback);
        }

        // 移除监听（无参数）
        public void RemoveSkillEventListener(string eventName, Action callback)
        {
            if (eventDic.ContainsKey(eventName))
            {
                eventDic[eventName] = Delegate.Remove(eventDic[eventName], callback);
                if (eventDic[eventName] == null) eventDic.Remove(eventName);
            }
        }

        // 移除事件监听（带参数）
        public void RemoveSkillEventListener<T>(string eventName, Action<T> callback)
        {
            if (eventDic.ContainsKey(eventName))
            {
                eventDic[eventName] = Delegate.Remove(eventDic[eventName], callback);
                if (eventDic[eventName] == null) eventDic.Remove(eventName);
            }
        }

        // 触发事件（无参数）
        public void TriggerSkillEvent(string eventName)
        {
            if (eventDic.TryGetValue(eventName, out var del))
            {
                var action = del as Action;
                action?.Invoke();
            }
        }

        // 触发事件（带参数）
        public virtual void TriggerSkillEvent<T>(string eventName, T eventData)
        {
            if (eventDic.TryGetValue(eventName, out var del))
            {
                var action = del as Action<T>;
                action?.Invoke(eventData);
            }
        }

        private void OnDestroy()
        {
            eventDic.Clear();
        }
    }
}