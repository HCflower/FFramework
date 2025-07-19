using UnityEngine.UIElements;
using UnityEditor;

namespace SkillEditor
{
    /// <summary>
    /// 自定义绘制技能编辑器窗口的Inspector
    /// </summary>
    [CustomEditor(typeof(TrackItemInspector))]
    public class TrackItemInspector : Editor
    {
        public static TrackItemInspector Instance;
        private VisualElement root;                      //显示Inspector根区域
        private TrackType trackType;                     //当前轨道类型
        public static void SetTrackItem(TrackType trackType)
        {
            if (Instance != null)
            {
                Instance.trackType = trackType;
                Instance.UpdateView();
            }
        }

        private void OnDestroy()
        {

        }

        /// <summary>
        /// 绘制Inspector
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            Instance = this;
            root = new VisualElement();
            UpdateView();
            return root;
        }

        /// <summary>
        /// 刷新视图
        /// </summary>
        private void UpdateView()
        {
            if (root == null) return;
            root.Clear();
            Label label = new Label("当前选中的轨道项信息");
            label.style.width = 200f;
            label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            root.Add(label);

            // 显示轨道类型
            Label typeLabel = new Label($"轨道类型: {trackType}");
            typeLabel.style.marginTop = 8;
            root.Add(typeLabel);
        }
    }
}
