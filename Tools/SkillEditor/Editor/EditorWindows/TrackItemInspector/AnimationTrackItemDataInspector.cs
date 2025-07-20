using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace SkillEditor
{
    [CustomEditor(typeof(AnimationTrackItemData))]
    public class AnimationTrackItemDataInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var data = target as AnimationTrackItemData;
            var root = new VisualElement();

            // 标题样式 - 居中对齐
            var title = new Label("动画轨道项信息");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 8;
            root.Add(title);

            // 轨道名称 - 左对齐
            var nameLabel = new Label($"轨道名称: {data.trackName}");
            nameLabel.style.marginLeft = 0;
            nameLabel.style.marginBottom = 4;
            root.Add(nameLabel);            // 帧数信息 - 左对齐
            var frameLabel = new Label($"帧数: {data.frameCount}");
            frameLabel.style.marginLeft = 0;
            frameLabel.style.marginBottom = 4;
            root.Add(frameLabel);

            // 动画片段字段 - 左对齐
            var clipField = new ObjectField("动画片段") { objectType = typeof(AnimationClip), value = data.animationClip };
            clipField.style.marginLeft = 0;
            clipField.style.marginBottom = 4;
            root.Add(clipField);

            // 循环状态 - 左对齐
            var loopLabel = new Label($"是否循环: {(data.isLoop ? "是" : "否")}");
            loopLabel.style.marginLeft = 0;
            loopLabel.style.marginBottom = 4;
            root.Add(loopLabel);

            // 可扩展更多动画相关属性
            return root;
        }
    }
}
