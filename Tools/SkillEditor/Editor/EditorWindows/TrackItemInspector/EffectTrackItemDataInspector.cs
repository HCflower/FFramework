using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(EffectTrackItemData))]
    public class EffectTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private EffectTrackItemData effectTargetData;

        protected override string TrackItemTypeName => "Effect";
        protected override string TrackItemDisplayTitle => "特效轨道项信息";
        protected override string DeleteButtonText => "删除特效轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            effectTargetData = target as EffectTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 特效预制体字段
            CreateObjectField<GameObject>("特效预制体:", "effectPrefab", OnEffectPrefabChanged);

            // Transform 设置
            CreateVector3Field("特效位置:", "position", OnPositionChanged);
            CreateVector3Field("特效旋转:", "rotation", OnRotationChanged);
            CreateVector3Field("特效缩放:", "scale", OnScaleChanged);
        }

        protected override void PerformDelete()
        {
            // 实现特效轨道项的删除逻辑
            Debug.Log($"删除特效轨道项: {effectTargetData.trackItemName}");
            // TODO: 添加具体的删除逻辑，包括从配置中移除
        }

        #region 事件处理方法

        private void OnEffectPrefabChanged(GameObject newPrefab)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "特效预制体更新");
        }

        private void OnPositionChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "特效位置更新");
        }

        private void OnRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "特效旋转更新");
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "特效缩放更新");
        }

        #endregion
    }
}
