using System.Collections.Generic;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 对话系统事件数据
    /// </summary>
    public static class DialogueActionCache
    {
        // 添加静态字典来缓存已找到的类型
        public static Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        // 添加静态字典来缓存已创建的实例
        public static Dictionary<Type, IBranchAction> typeInstanceCache = new Dictionary<Type, IBranchAction>();
    }
}