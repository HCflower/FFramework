using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 攻击轨道项数据类
    /// 用于存储攻击轨道项的相关数据，与SkillConfig中的InjuryDetectionClip结构对应
    /// </summary>
    public class InjuryDetectionTrackItemData : TrackItemDataBase
    {
        public GameObject hitEffectPrefab;                  //击中特效
        public LayerMask targetLayers = -1;                 //目标层级
        public bool enableAllCollisionGroups = false;       //启用所有碰撞体
        public int collisionGroupId = 0;                    //碰撞检测组ID
        public string injuryDetectionEventName;             //伤害检测事件名
    }
}
