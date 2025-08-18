using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 摄像机轨道实现
    /// 专门处理摄像机的创建、显示和配置管理
    /// </summary>
    public class CameraSkillEditorTrack : SkillEditorTrackBase
    {
        #region 私有字段

        private List<CameraTrackItem> cameraTrackItems = new List<CameraTrackItem>();

        #endregion

        #region 构造函数

        public CameraSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.CameraTrack, width, skillConfig, trackIndex)
        { }

        #endregion

        #region 抽象方法实现

        protected override bool CanAcceptDraggedObject(Object obj)
        {
            return obj is Camera || (obj is GameObject gameObject && gameObject.GetComponent<Camera>() != null);
        }

        protected override TrackItemViewBase CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            string itemName = ExtractCameraName(resource);
            if (string.IsNullOrEmpty(itemName)) return null;

            var newItem = CreateCameraTrackItem(itemName, startFrame, 5, addToConfig);

            if (addToConfig)
            {
                AddTrackItemDataToConfig(itemName, startFrame, 5);
                SkillEditorEvent.TriggerRefreshRequested();
            }

            return newItem;
        }

        protected override void ApplySpecificTrackStyle()
        {
            // 摄像机轨道特有的样式设置
            trackArea.AddToClassList("TrackArea-Camera");
        }

        #endregion

        #region 公共方法

        public CameraTrackItem CreateCameraTrackItem(string cameraName, int startFrame, int frameCount = 5, bool addToConfig = true)
        {
            var cameraItem = new CameraTrackItem(trackArea, cameraName, frameCount, startFrame, trackIndex);

            cameraTrackItems.Add(cameraItem);
            trackItems.Add(cameraItem);

            if (addToConfig)
            {
                AddTrackItemDataToConfig(cameraName, startFrame, frameCount);
                SkillEditorEvent.TriggerRefreshRequested();
            }

            return cameraItem;
        }

        public override void RefreshTrackItems()
        {
            base.RefreshTrackItems();
        }

        public static void CreateTrackItemsFromConfig(CameraSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var cameraTrack = skillConfig.trackContainer.cameraTrack;
            if (cameraTrack?.cameraClips == null || trackIndex != 0) return;

            foreach (var clip in cameraTrack.cameraClips)
            {
                var cameraTrackItem = track.CreateCameraTrackItem(clip.clipName, clip.startFrame, clip.durationFrame, false);

                if (clip.durationFrame > 0)
                {
                    cameraTrackItem?.UpdateFrameCount(clip.durationFrame);
                }
            }
        }

        #endregion

        #region 私有方法

        private string ExtractCameraName(object resource)
        {
            return resource switch
            {
                Camera camera => $"Camera {camera.name}",
                GameObject gameObject when gameObject.GetComponent<Camera>() != null => $"Camera {gameObject.name}",
                string name => name,
                _ => string.Empty
            };
        }

        private void AddTrackItemDataToConfig(string cameraName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            EnsureCameraTrackExists();

            var cameraTrack = skillConfig.trackContainer.cameraTrack;

            if (cameraTrack.cameraClips == null)
            {
                cameraTrack.cameraClips = new List<FFramework.Kit.CameraTrack.CameraClip>();
            }

            string finalName = cameraName;
            int suffix = 1;
            while (cameraTrack.cameraClips.Any(c => c.clipName == finalName))
            {
                finalName = $"{cameraName}_{suffix++}";
            }

            var configCameraClip = new FFramework.Kit.CameraTrack.CameraClip
            {
                clipName = finalName,
                startFrame = startFrame,
                durationFrame = frameCount,
            };

            cameraTrack.cameraClips.Add(configCameraClip);

#if UNITY_EDITOR
            EditorUtility.SetDirty(cameraTrack);
            EditorUtility.SetDirty(skillConfig);
#endif
        }

        private void EnsureCameraTrackExists()
        {
            if (skillConfig.trackContainer.cameraTrack != null) return;

            var cameraTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.CameraTrackSO>();
            cameraTrackSO.trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.CameraTrack, 0);
            cameraTrackSO.cameraClips = new List<FFramework.Kit.CameraTrack.CameraClip>();
            skillConfig.trackContainer.cameraTrack = cameraTrackSO;

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(cameraTrackSO, skillConfig);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        #endregion
    }
}
