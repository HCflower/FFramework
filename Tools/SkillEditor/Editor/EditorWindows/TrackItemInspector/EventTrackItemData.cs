using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 事件轨道项数据类
    /// 用于存储事件轨道项的相关数据，与SkillConfig中的EventClip结构对应
    /// </summary>
    public class EventTrackItemData : BaseTrackItemData
    {
        [Header("事件参数")]
        [Tooltip("事件类型")] public string eventType;
        [Tooltip("事件参数")] public string eventParameters;
    }
}
