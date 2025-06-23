using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;

namespace DialogueToolEditor
{
    /// <summary>
    /// 对话工具编辑器
    /// </summary>
    public class DialogueTool : EditorWindow
    {
        [MenuItem("FFramework/💬CreateDialogueDataFile &D", priority = 2)]
        public static void CreateDialogueDataFile()
        {
            string path = "Assets/[Dialogue]Data.csv";
            if (!File.Exists(path))
            {
                using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
                {
                    writer.WriteLine("Type,ID,Name,Icon,pos,Content,To,Action");
                    writer.WriteLine("Dialogue,0,,,Left,1,");
                    writer.WriteLine("Branch,1,,,Right,2,");
                    writer.WriteLine("End,2,,,,,");
                }
                AssetDatabase.Refresh();
                Debug.Log($"对话数据文件已创建: {path}");
            }
            else
            {
                Debug.LogWarning($"文件已存在: {path}");
            }

            //聚焦到文件位置
            var csvFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Selection.activeObject = csvFile;
            EditorGUIUtility.PingObject(csvFile);
        }
    }
}
