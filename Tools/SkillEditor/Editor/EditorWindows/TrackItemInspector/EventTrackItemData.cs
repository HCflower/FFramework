using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 事件轨道项数据类
    /// 用于存储事件轨道项的相关数据，与SkillConfig中的EventClip结构对应
    /// </summary>
    public class EventTrackItemData : TrackItemDataBase
    {
        public string eventName;          //事件类型
    }
}
