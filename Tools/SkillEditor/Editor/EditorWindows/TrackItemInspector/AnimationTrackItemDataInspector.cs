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
            var title = new Label("动画轨道项信息");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginLeft = 0;
            title.style.marginBottom = 8;
            root.Add(title);

            var nameLabel = new Label($"轨道名称: {data.trackName}");
            title.style.marginLeft = 0;

            nameLabel.style.marginBottom = 4;
            root.Add(nameLabel);

            var frameLabel = new Label($"帧数: {data.frameCount}");
            title.style.marginLeft = 0;

            frameLabel.style.marginBottom = 4;
            root.Add(frameLabel);

            var clipField = new ObjectField("动画片段") { objectType = typeof(AnimationClip), value = data.animationClip };
            clipField.style.marginBottom = 4;
            title.style.marginLeft = 0;

            root.Add(clipField);

            var loopLabel = new Label($"是否循环: {(data.isLoop ? "是" : "否")}");
            loopLabel.style.marginBottom = 4;
            title.style.marginLeft = 0;

            root.Add(loopLabel);

            // 可扩展更多动画相关属性
            return root;
        }
    }
}
