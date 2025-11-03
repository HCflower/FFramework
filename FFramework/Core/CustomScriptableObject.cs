using UnityEngine;

namespace FFramework.Architecture
{
    /// <summary>
    /// 自定义ScriptableObject(用于扩展编辑器功能)
    /// </summary>
    public class CustomScriptableObject : ScriptableObject
    {
#if UNITY_EDITOR
        [Button("保存资源", ButtonColor.Green)]
        public void SaveAsset()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
    }
}
