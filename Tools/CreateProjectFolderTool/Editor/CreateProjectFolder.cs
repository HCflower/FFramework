using UnityEditor;
using UnityEngine;

namespace CreateProjectFolder
{
    /// <summary>
    /// 创建项目文件夹
    /// TODO:加入HybridCLR支持后,需要修改目录结构
    /// </summary>
    public class CreateProjectFolder : EditorWindow
    {
        [MenuItem("FFramework/📁CreateGemeFolder #A", priority = 2)]
        public static void DoCreateProjectFolder()
        {
            //代码
            CreateFolderByName("Scripts/Command");
            CreateFolderByName("Scripts/ViewController");
            CreateFolderByName("Scripts/ViewController/UI");
            CreateFolderByName("Scripts/Model");
            CreateFolderByName("Scripts/System");
            CreateFolderByName("Scripts/Command");
            CreateFolderByName("Scripts/Utility");
            //游戏资源
            CreateFolderByName("GameRes/Resources");
            CreateFolderByName("GameRes/Resources/UI");
            CreateFolderByName("GameRes/Resources/Audio");
            CreateFolderByName("GameRes/Prefab");
            CreateFolderByName("GameRes/Image");
            CreateFolderByName("GameRes/Animation");
            CreateFolderByName("GameRes/Scenes");
            CreateFolderByName("GameRes/Shader");
            CreateFolderByName("GameRes/Font");
            CreateFolderByName("GameRes/Material");
            CreateFolderByName("GameRes/Texture");
            CreateFolderByName("GameRes/Model");
            CreateFolderByName("GameRes/VFX");
            //可热更新资源
            CreateFolderByName("HotUpdate/AssetBundles");
            //设置
            CreateFolderByName("Settings");
            //数据保存
            CreateFolderByName("StreamingAssets");
            //测试
            CreateFolderByName("Test");
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
                    Debug.Log($"<color=green>文件夹创建成功:</color> {nextPath}");
                }
                else
                {
                    Debug.Log($"<color=yellow>文件夹已存在:</color> {nextPath}");
                }
                currentPath = nextPath;
            }
        }
    }
}