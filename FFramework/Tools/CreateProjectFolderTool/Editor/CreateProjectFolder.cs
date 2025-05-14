using UnityEditor;
using UnityEngine;

namespace CreateProjectFolder
{
    /// <summary>
    /// 创建项目文件夹
    /// </summary>
    public class CreateProjectFolder : EditorWindow
    {
        [MenuItem("FFramework/CreateGemeFolder #A", priority = 2)]
        public static void DoCreateProjectFolder()
        {
            CreateFolderByName("Scripts/Command");
            CreateFolderByName("Scripts/ViewController");
            CreateFolderByName("Scripts/Model");
            CreateFolderByName("Scripts/System");
            CreateFolderByName("Scripts/Command");
            CreateFolderByName("Scripts/Utility");
            CreateFolderByName("GameRes/Resources");
            CreateFolderByName("GameRes/Prefab");
            CreateFolderByName("GameRes/Image");
            CreateFolderByName("GameRes/Audio");
            CreateFolderByName("GameRes/Animation");
            CreateFolderByName("GameRes/Scene");
            CreateFolderByName("GameRes/Shader");
            CreateFolderByName("GameRes/Font");
            CreateFolderByName("GameRes/Material");
            CreateFolderByName("GameRes/GameModel");
            CreateFolderByName("GameRes/VFX");
        }

        //创建文件夹
        private static void CreateFolderByName(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            // 确保Game根目录存在
            string gameRootPath = "Assets/Game";
            if (!AssetDatabase.IsValidFolder(gameRootPath))
            {
                string error = AssetDatabase.CreateFolder("Assets", "Game");
                if (!string.IsNullOrEmpty(error) && !AssetDatabase.IsValidFolder(gameRootPath))
                {
                    Debug.LogError($"<color=red>游戏根文件夹(Game)创建失败:</color> {error}");
                    return;
                }
                AssetDatabase.Refresh();
            }

            // 处理多级目录
            string[] pathParts = folderPath.Split('/');
            string currentPath = gameRootPath;

            foreach (string part in pathParts)
            {
                string nextPath = System.IO.Path.Combine(currentPath, part);

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string createResult = AssetDatabase.CreateFolder(currentPath, part);
                    if (!string.IsNullOrEmpty(createResult) && !AssetDatabase.IsValidFolder(nextPath))
                    {
                        Debug.LogError($"<color=red>创建文件夹失败:</color> {nextPath} - {createResult}");
                        return;
                    }
                    AssetDatabase.Refresh();
                }
                currentPath = nextPath;
            }

            Debug.Log($"<color=green>文件夹创建成功:</color>{gameRootPath}/{folderPath}");
        }
    }
}