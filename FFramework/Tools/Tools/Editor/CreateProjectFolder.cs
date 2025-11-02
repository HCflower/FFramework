using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace FFramework.Tools
{
    /// <summary>
    /// 创建项目文件夹
    /// 支持自定义文件夹结构的现代化编辑器界面
    /// </summary>
    public class CreateProjectFolder : EditorWindow
    {
        #region 数据结构
        [System.Serializable]
        public class FolderItem
        {
            public string name;
            public bool enabled = true;
            public List<FolderItem> children = new List<FolderItem>();
            public bool foldout = true;

            public FolderItem(string name)
            {
                this.name = name;
            }
        }
        #endregion

        #region 字段
        private List<FolderItem> folderItems = new List<FolderItem>();
        private Vector2 scrollPosition;
        private bool showPresets = true;
        private bool showCustomFolder = false;
        private string newFolderName = "";
        private string gameRootName = "Game";
        private GUIStyle headerStyle;
        private GUIStyle foldoutStyle;
        private bool stylesInitialized = false;
        #endregion

        #region Unity生命周期
        [MenuItem("FFramework/Tools/项目文件夹管理器", priority = 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<CreateProjectFolder>("项目文件夹管理器");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeDefaultFolders();
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.Space(1);
            DrawHeader();
            EditorGUILayout.Space(1);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawGameRootSettings();
            EditorGUILayout.Space(1);

            DrawPresetSection();
            EditorGUILayout.Space(1);

            DrawCustomFolderSection();
            EditorGUILayout.Space(1);

            DrawFolderStructure();
            EditorGUILayout.Space(1);

            EditorGUILayout.EndScrollView();

            DrawActionButtons();
        }
        #endregion

        #region 初始化
        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            stylesInitialized = true;
        }

        private void InitializeDefaultFolders()
        {
            if (folderItems.Count > 0) return;

            folderItems = new List<FolderItem>
            {
                CreateScriptsFolder(),
                CreateGameResFolder(),
                new FolderItem("Test")
            };
        }

        private FolderItem CreateScriptsFolder()
        {
            var scripts = new FolderItem("Scripts");
            scripts.children.AddRange(new[]
            {
                new FolderItem("ViewController")
                {
                    children = { new FolderItem("UI") }
                },
                new FolderItem("Architecture"),
                new FolderItem("Models"),
                new FolderItem("Utility"),
                new FolderItem("Manager"),
                new FolderItem("System")
            });
            return scripts;
        }

        private FolderItem CreateGameResFolder()
        {
            var gameRes = new FolderItem("GameRes");
            gameRes.children.AddRange(new[]
            {
                new FolderItem("Resources")
                {
                    children = { new FolderItem("UI") },
                },
                new FolderItem("Image"),
                new FolderItem("Animation"),
                new FolderItem("Font"),
                new FolderItem("Material"),
                new FolderItem("Models"),
                new FolderItem("VFX"),
                new FolderItem("Audio"),
                new FolderItem("Prefabs"),
            });
            return gameRes;
        }
        #endregion

        #region UI绘制
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("项目文件夹管理器", headerStyle);
            EditorGUILayout.LabelField("自定义和管理您的Unity项目文件夹结构", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawGameRootSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("根目录设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("根文件夹名称:", GUILayout.Width(100));
            gameRootName = EditorGUILayout.TextField(gameRootName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox($"所有文件夹将在 Assets/{gameRootName}/ 下创建", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showPresets = EditorGUILayout.Foldout(showPresets, "预设模板", foldoutStyle);

            if (showPresets)
            {
                EditorGUILayout.Space(2);

                // 竖向排列按钮
                GUILayout.BeginVertical();
                if (GUILayout.Button(new GUIContent("空白自定义模板", "空白自定义模板"), GUILayout.Height(26)))
                {
                    LoadEmptyTemplate();
                }
                if (GUILayout.Button(new GUIContent("默认游戏项目", "默认游戏项目结构"), GUILayout.Height(26)))
                {
                    LoadDefaultGamePreset();
                }
                if (GUILayout.Button(new GUIContent("2D游戏项目", "2D游戏项目结构"), GUILayout.Height(26)))
                {
                    Load2DGamePreset();
                }
                if (GUILayout.Button(new GUIContent("网络游戏项目", "网络游戏项目结构"), GUILayout.Height(26)))
                {
                    LoadNetworkGamePreset();
                }
                GUILayout.EndVertical();

                // 温馨提示
                EditorGUILayout.HelpBox("选择模板后可继续自定义结构，或直接点击下方按钮生成文件夹。", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCustomFolderSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showCustomFolder = EditorGUILayout.Foldout(showCustomFolder, "添加自定义文件夹", foldoutStyle);

            if (showCustomFolder)
            {
                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("文件夹路径:", GUILayout.Width(80));
                newFolderName = EditorGUILayout.TextField(newFolderName);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("支持多级路径，例如: Game/Config 或 Scripts/Network/Protocol", MessageType.Info);

                EditorGUILayout.Space(5);

                EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(newFolderName));
                if (GUILayout.Button("添加文件夹", GUILayout.Height(25)))
                {
                    AddCustomFolder();
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawFolderStructure()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("文件夹结构", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // 添加全选/取消全选按钮
            if (GUILayout.Button("全选", GUILayout.Width(40), GUILayout.Height(20)))
            {
                SetAllFoldersEnabled(true);
            }
            if (GUILayout.Button("取消", GUILayout.Width(40), GUILayout.Height(20)))
            {
                SetAllFoldersEnabled(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(1);

            for (int i = 0; i < folderItems.Count; i++)
            {
                DrawFolderItem(folderItems[i], 0, i);
            }
            EditorGUILayout.EndVertical();
        }

        private void SetAllFoldersEnabled(bool enabled)
        {
            foreach (var item in folderItems)
            {
                SetFolderEnabledRecursive(item, enabled);
            }
        }

        private void SetFolderEnabledRecursive(FolderItem item, bool enabled)
        {
            item.enabled = enabled;
            foreach (var child in item.children)
            {
                SetFolderEnabledRecursive(child, enabled);
            }
        }

        private void DrawFolderItem(FolderItem item, int depth, int index = -1)
        {
            EditorGUILayout.BeginHorizontal();

            // 缩进
            GUILayout.Space(depth * 20);

            // 启用/禁用复选框
            item.enabled = EditorGUILayout.Toggle(item.enabled, GUILayout.Width(15));
            Texture iconTex = EditorGUIUtility.IconContent("Folder Icon").image;
            if (item.children.Count > 0)
            {

                GUILayout.Label(iconTex, GUILayout.Width(18), GUILayout.Height(18));
                item.foldout = EditorGUILayout.Foldout(item.foldout, item.name);
            }
            else
            {
                GUILayout.Label(iconTex, GUILayout.Width(18), GUILayout.Height(18));
                EditorGUILayout.LabelField(item.name);
            }

            GUILayout.FlexibleSpace();

            // 删除按钮 (仅对根级别项目)
            if (depth == 0 && index >= 0)
            {
                if (GUILayout.Button("移除", GUILayout.Width(60), GUILayout.Height(18)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", $"确定要删除文件夹 '{item.name}' 吗？", "删除", "取消"))
                    {
                        folderItems.RemoveAt(index);
                        GUI.FocusControl(null);
                        return;
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            // 绘制子文件夹
            if (item.foldout && item.children.Count > 0)
            {
                foreach (var child in item.children)
                {
                    DrawFolderItem(child, depth + 1);
                }
            }
        }

        private void DrawActionButtons()
        {
            // 只用一个按钮并设置ExpandWidth为true即可100%宽度
            if (GUILayout.Button("创建文件夹", GUILayout.Height(26), GUILayout.ExpandWidth(true)))
            {
                CreateAllFolders();
            }
        }

        #endregion

        #region 预设加载

        // 加载空白模板
        private void LoadEmptyTemplate()
        {
            if (EditorUtility.DisplayDialog("确认操作",
                "这将清空当前所有文件夹配置，开始一个全新的空白模板。\n\n您可以通过\"添加自定义文件夹\"来构建自己的项目结构。\n\n确定要继续吗？",
                "确定", "取消"))
            {
                folderItems.Clear();
                // 自动展开自定义文件夹部分，方便用户立即开始添加
                showCustomFolder = true;
            }
        }

        // 加载默认游戏预设
        private void LoadDefaultGamePreset()
        {
            folderItems.Clear();
            InitializeDefaultFolders();
        }

        // 加载2D游戏预设
        private void Load2DGamePreset()
        {
            folderItems.Clear();
            folderItems.AddRange(new[]
            {
                CreateScriptsFolder(),
                Create2DGameResFolder(),
                new FolderItem("HotUpdate"),
                new FolderItem("StreamingAssets"),
                new FolderItem("Test")
            });
        }

        // 加载网络游戏预设
        private void LoadNetworkGamePreset()
        {
            folderItems.Clear();
            var scripts = CreateScriptsFolder();
            scripts.children.AddRange(new[]
            {
                new FolderItem("Network"),
                new FolderItem("Protocol"),
                new FolderItem("Server")
            });

            folderItems.AddRange(new[]
            {
                scripts,
                CreateGameResFolder(),
                new FolderItem("HotUpdate"),
                new FolderItem("StreamingAssets"),
                new FolderItem("Config"),
                new FolderItem("Test")
            });
        }

        // 创建2D游戏资源文件夹结构
        private FolderItem Create2DGameResFolder()
        {
            var gameRes = new FolderItem("GameRes");
            gameRes.children.AddRange(new[]
            {
                new FolderItem("Sprites"),
                new FolderItem("Animation"),
                new FolderItem("Tilemap"),
                new FolderItem("UI"),
                new FolderItem("Audio"),
                new FolderItem("Font")
            });
            return gameRes;
        }

        #endregion

        #region 功能方法
        private void AddCustomFolder()
        {
            if (string.IsNullOrWhiteSpace(newFolderName)) return;

            // 处理带路径的文件夹名称
            if (newFolderName.Contains("/"))
            {
                AddNestedFolder(newFolderName);
            }
            else
            {
                folderItems.Add(new FolderItem(newFolderName));
            }

            newFolderName = "";
        }

        private void AddNestedFolder(string folderPath)
        {
            string[] pathParts = folderPath.Split('/');
            if (pathParts.Length == 0) return;

            FolderItem currentParent = null;
            List<FolderItem> currentList = folderItems;

            // 遍历路径的每一部分
            for (int i = 0; i < pathParts.Length; i++)
            {
                string partName = pathParts[i].Trim();
                if (string.IsNullOrEmpty(partName)) continue;

                // 查找是否已存在这个文件夹（支持模糊匹配）
                var existingFolder = FindBestMatchFolder(currentList, partName);

                if (existingFolder != null)
                {
                    // 如果已存在，使用现有的文件夹
                    currentParent = existingFolder;
                    currentList = existingFolder.children;
                }
                else
                {
                    // 如果不存在，创建新文件夹
                    var newFolder = new FolderItem(partName);
                    currentList.Add(newFolder);
                    currentParent = newFolder;
                    currentList = newFolder.children;
                }
            }
        }

        // 新增：智能匹配文件夹的方法
        private FolderItem FindBestMatchFolder(List<FolderItem> folderList, string targetName)
        {
            // 1. 完全匹配
            var exactMatch = folderList.FirstOrDefault(f =>
                f.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return exactMatch;

            // 2. 包含匹配（例如：输入"Res"匹配"GameRes"或"Resources"）
            var containsMatch = folderList.FirstOrDefault(f =>
                f.name.Contains(targetName, System.StringComparison.OrdinalIgnoreCase) ||
                targetName.Contains(f.name, System.StringComparison.OrdinalIgnoreCase));
            if (containsMatch != null) return containsMatch;

            // 3. 常见别名匹配
            var aliasMatch = folderList.FirstOrDefault(f => IsAlias(f.name, targetName));
            if (aliasMatch != null) return aliasMatch;

            return null;
        }

        // 新增：常见文件夹别名匹配
        private bool IsAlias(string existingName, string inputName)
        {
            var aliases = new Dictionary<string, string[]>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "GameRes", new[] { "Res", "Resource", "Resources" } },
                { "Resources", new[] { "Res", "Resource" } },
                { "Scripts", new[] { "Script", "Code", "Src" } },
                { "Prefabs", new[] { "Prefab" } },
                { "Animation", new[] { "Anim", "Animations" } },
                { "Materials", new[] { "Material", "Mat" } },
                { "Textures", new[] { "Texture", "Tex", "Image", "Images" } }
            };

            // 检查是否为已知别名
            foreach (var kvp in aliases)
            {
                if (existingName.Equals(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.Any(alias => alias.Equals(inputName, System.StringComparison.OrdinalIgnoreCase));
                }
            }

            return false;
        }

        private void PreviewFolderStructure()
        {
            var preview = "将要创建的文件夹结构预览:\n\n";
            preview += $"Assets/{gameRootName}/\n";

            foreach (var item in folderItems.Where(f => f.enabled))
            {
                preview += BuildPreviewString(item, 1);
            }

            EditorUtility.DisplayDialog("文件夹结构预览", preview, "确定");
        }

        private string BuildPreviewString(FolderItem item, int depth)
        {
            if (!item.enabled) return "";

            var indent = new string(' ', depth * 2);
            var result = $"{indent}├── {item.name}/\n";

            foreach (var child in item.children.Where(c => c.enabled))
            {
                result += BuildPreviewString(child, depth + 1);
            }

            return result;
        }

        private void CreateAllFolders()
        {
            var enabledFolders = folderItems.Where(f => f.enabled).ToList();
            if (enabledFolders.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请至少选择一个文件夹进行创建", "确定");
                return;
            }

            var totalFolders = CountEnabledFolders();
            var createdCount = 0;

            foreach (var item in enabledFolders)
            {
                CreateFolderRecursive(item, "", ref createdCount, totalFolders);
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("完成", $"成功创建了 {createdCount} 个文件夹！", "确定");
            AssetDatabase.Refresh();
        }

        private int CountEnabledFolders()
        {
            int count = 0;
            foreach (var item in folderItems.Where(f => f.enabled))
            {
                count += CountFoldersRecursive(item);
            }
            return count;
        }

        private int CountFoldersRecursive(FolderItem item)
        {
            if (!item.enabled) return 0;

            int count = 1;
            foreach (var child in item.children.Where(c => c.enabled))
            {
                count += CountFoldersRecursive(child);
            }
            return count;
        }

        private void CreateFolderRecursive(FolderItem item, string parentPath, ref int createdCount, int totalCount)
        {
            if (!item.enabled) return;

            var currentPath = string.IsNullOrEmpty(parentPath) ? item.name : $"{parentPath}/{item.name}";

            EditorUtility.DisplayProgressBar("创建文件夹", $"正在创建: {currentPath}", (float)createdCount / totalCount);

            CreateFolderByName(currentPath);
            createdCount++;

            foreach (var child in item.children.Where(c => c.enabled))
            {
                CreateFolderRecursive(child, currentPath, ref createdCount, totalCount);
            }
        }
        #endregion

        #region 创建文件夹核心方法
        //创建文件夹 - 实例方法，支持自定义根目录
        private void CreateFolderByName(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            // 使用自定义的根目录名称
            string gameRootPath = $"Assets/{gameRootName}";
            if (!AssetDatabase.IsValidFolder(gameRootPath))
            {
                string error = AssetDatabase.CreateFolder("Assets", gameRootName);
                if (!string.IsNullOrEmpty(error) && !AssetDatabase.IsValidFolder(gameRootPath))
                {
                    Debug.LogError($"<color=red>游戏根文件夹({gameRootName})创建失败:</color> {error}");
                    return;
                }
                AssetDatabase.Refresh();
            }

            // 处理多级目录
            string[] pathParts = folderPath.Split('/');
            string currentPath = gameRootPath;

            foreach (string part in pathParts)
            {
                string nextPath = System.IO.Path.Combine(currentPath, part).Replace('\\', '/');

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string createResult = AssetDatabase.CreateFolder(currentPath, part);
                    if (!string.IsNullOrEmpty(createResult) && !AssetDatabase.IsValidFolder(nextPath))
                    {
                        Debug.LogError($"<color=red>创建文件夹失败:</color> {nextPath} - {createResult}");
                        return;
                    }
                    Debug.Log($"<color=green>文件夹创建成功:</color> {nextPath}");
                }
                currentPath = nextPath;
            }
        }

        //创建文件夹 - 静态方法，用于向后兼容
        private static void CreateFolderByNameStatic(string folderPath)
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
                string nextPath = System.IO.Path.Combine(currentPath, part).Replace('\\', '/');

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
        #endregion
    }
}