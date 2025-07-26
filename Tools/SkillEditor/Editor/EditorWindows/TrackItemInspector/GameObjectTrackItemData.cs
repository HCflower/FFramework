using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 游戏物体轨道项数据
    /// 存储游戏物体轨道项的所有配置信息
    /// </summary>
    public class GameObjectTrackItemData : BaseTrackItemData
    {
        [Header("游戏物体设置")]
        public GameObject prefab;                   // 预制体
        public bool autoDestroy = true;             // 是否自动销毁
        public Vector3 positionOffset = Vector3.zero; // 生成位置偏移
        public Vector3 rotationOffset = Vector3.zero; // 生成旋转偏移
        public Vector3 scale = Vector3.one;         // 生成缩放

        [Header("父对象设置")]
        public bool useParent = false;              // 是否作为子对象
        public string parentName = "";              // 父对象名称

        [Header("生命周期设置")]
        public float destroyDelay = -1f;            // 延迟销毁时间(秒), -1表示不销毁

        [Header("运行时信息")]
        public GameObject instantiatedObject;       // 运行时生成的对象实例
    }
}
