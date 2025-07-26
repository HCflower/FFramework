using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(GameObjectTrackItemData))]
    public class GameObjectTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private GameObjectTrackItemData gameObjectTargetData;

        protected override string TrackItemTypeName => "GameObject";
        protected override string TrackItemDisplayTitle => "游戏物体轨道项信息";
        protected override string DeleteButtonText => "删除游戏物体轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            gameObjectTargetData = target as GameObjectTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 预制体字段
            CreateObjectField<GameObject>("预制体:", "prefab", OnPrefabChanged);

            // 自动销毁字段
            CreateToggleField("自动销毁:", "autoDestroy", OnAutoDestroyChanged);

            // 位置偏移字段
            CreateVector3Field("位置偏移:", "positionOffset", OnPositionOffsetChanged);

            // 旋转偏移字段
            CreateVector3Field("旋转偏移:", "rotationOffset", OnRotationOffsetChanged);

            // 缩放字段
            CreateVector3Field("缩放:", "scale", OnScaleChanged);

            // 父对象设置
            CreateToggleField("使用父对象:", "useParent", OnUseParentChanged);

            // 父对象名称字段
            CreateTextField("父对象名称:", "parentName", OnParentNameChanged);

            // 销毁延迟字段
            CreateFloatField("销毁延迟(秒):", "destroyDelay", OnDestroyDelayChanged);
        }

        protected override void PerformDelete()
        {
            // TODO: 实现删除逻辑，需要从技能配置中移除对应的游戏物体片段
            Debug.Log($"删除游戏物体轨道项: {gameObjectTargetData.trackItemName}");
        }

        #region 字段变化回调

        private void OnPrefabChanged(GameObject newPrefab)
        {
            SafeExecute(() =>
           {
               // TODO: 同步到配置文件
               MarkSkillConfigDirty();
           }, "预制体改变");
        }

        private void OnAutoDestroyChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "自动销毁");
        }

        private void OnPositionOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "位置变换启用状态更新");
        }

        private void OnRotationOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "旋转变换启用状态更新");
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "缩放变换启用状态更新");
        }

        private void OnUseParentChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "使用父级变换");
        }

        private void OnParentNameChanged(string newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "父级名称");
        }

        private void OnDestroyDelayChanged(float newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "延迟销毁");
        }

        #endregion
    }
}
