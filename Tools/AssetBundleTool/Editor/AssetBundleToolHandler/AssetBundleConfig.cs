using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace AssetBundleToolEditor
{
    [CreateAssetMenu(fileName = nameof(AssetBundleConfig), menuName = "FFramework/AssetBundleConfig", order = 0)]
    public class AssetBundleConfig : ScriptableObject
    {
        [Tooltip("AssetBundle配置文件")]
        public TextAsset JsonAsset;

        [Tooltip("Json文件路径")]
        [ShowOnly]
        public string JosnPath;

        [Tooltip("Json文件GUID")]
        [ShowOnly]
        public string JosnGuid;

        [Header("Setting")]
        [Tooltip("本地AssetBundle包保存路径")]
        [ShowOnly]
        public string LocalSavePath = "StreamingAssets";

        [Tooltip("热更新AssetBundle包保存路径")]
        [ShowOnly]
        public string RemoteSavePath;

        [Tooltip("AssetBundle包压缩格式")]
        public CompressionType CompressionType = CompressionType.Lz4;

        [Tooltip("AssetBundle包构建平台")]
        public BuildTarget BuildTarget = BuildTarget.Windows;

        [Header("ResServer")]
        [Tooltip("AssetBundle包传输方式")]
        public NetworkProtocolsType NetworkProtocolsType = NetworkProtocolsType.FTP;

        [Tooltip("资源服务器地址")]
        public string ResServerPath = "127.0.0.1";

        [Tooltip("主文件夹")]
        public string MainFolderName;

        [Tooltip("用户ID")]
        public string ID;

        [Tooltip("用户密码")]
        public string Password;

        [Tooltip("版本号")]
        public string VersionID;

        [Header("AssetBundles")]
        [Tooltip("AssetBundles包列表")]
        public List<AssetBundleGroup> AssetBundleList = new List<AssetBundleGroup>();

        // 全局依赖项跟踪，防止跨组重复打包
        private static Dictionary<string, string> globalDependencyTracker = new Dictionary<string, string>();

        [Button("更新数据")]
        private void UpdateAndSaveData()
        {
            foreach (var group in AssetBundleList)
            {
                foreach (var assetData in group.assets)
                {
                    assetData.UpdateAssetInfo();
                }
            }
            GenerateJSON();
        }

        [Button("生成JSON文件")]
        public void GenerateJSON()
        {
            AssetBundleConfigJSON.GenerateJSON(this);
        }

        [Button("从JSON加载数据")]
        public void LoadFromJSON()
        {
            if (AssetBundleConfigJSON.LoadJSONToSO(this))
            {
                SaveData();
            }
        }

        //清理文件夹
        private void ClearDirectory(string folderPath)
        {
            // 确保目录存在
            if (!Directory.Exists(folderPath))
                return;

            DirectoryInfo directory = new DirectoryInfo(folderPath);

            // 删除所有文件
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }

            // 删除所有子文件夹及其内容
            foreach (DirectoryInfo subDir in directory.GetDirectories())
            {
                subDir.Delete(true); // true表示递归删除
            }
        }

        //清理本地AssetBundles文件夹
        public void ClearLocalAssetBundlesFolder()
        {
            ClearDirectory(LocalSavePath);
        }

        //清理远端AssetBundles文件夹
        public void ClearRemoteAssetBundlesFolder()
        {
            ClearDirectory(RemoteSavePath);
        }

        [Button("构建本地AssetBundle")]
        public void CreateLocalAssetBundle()
        {
            // 确保输出目录存在 - 应该是 Assets 内部的路径
            string outputPath = Path.Combine(Application.dataPath, LocalSavePath);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                Debug.Log($"创建输出目录: {outputPath}");
            }
            CreateAssetBundleInternal(outputPath, BuildPathType.Local);
        }

        [Button("构建远端AssetBundle")]
        public void CreateRemoteAssetBundle()
        {
            // 确保输出目录存在
            string outputPath = Path.Combine(Application.dataPath, RemoteSavePath);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                Debug.Log($"创建输出目录: {outputPath}");
            }
            CreateAssetBundleInternal(outputPath, BuildPathType.Remote);
        }

        /// <summary>
        /// 测试AssetBundle组依赖项收集
        /// </summary>
        /// <param name="group">AB包组</param>
        public void DebugDependencyCollection(AssetBundleGroup group)
        {
            if (group.isEnableBuild == false)
            {
                Debug.Log($"组 '{group.assetBundleName}' 未启用构建");
                return;
            }
            Debug.Log($"=== 调试AB包组: {group.assetBundleName} ===");
            Debug.Log($"可寻址模式: {group.isEnableAddressable}");
            Debug.Log($"分割文件: {group.isEnablePackSeparately}");

            foreach (var asset in group.assets)
            {
                // 只处理启用构建的资源
                if (!asset.isEnableBuild || string.IsNullOrEmpty(asset.assetPath)) continue;

                Debug.Log($"--- 主资源: {asset.assetName} ---");

                // 获取所有依赖项（包括间接依赖）
                string[] allDeps = AssetDatabase.GetDependencies(asset.assetPath, true);
                Debug.Log($"所有依赖项 ({allDeps.Length}): {string.Join(", ", allDeps)}");

                // 获取直接依赖项
                string[] directDeps = AssetDatabase.GetDependencies(asset.assetPath, false);
                Debug.Log($"直接依赖项 ({directDeps.Length}): {string.Join(", ", directDeps)}");

                // 检查哪些依赖项会被过滤
                foreach (string dep in directDeps)
                {
                    string status = "";

                    if (!IsValidAssetForPacking(dep))
                    {
                        status = "无效依赖项";
                    }
                    else if (IsBuiltInAsset(dep))
                    {
                        status = "内置依赖项";
                    }
                    else
                    {
                        if (globalDependencyTracker.ContainsKey(dep))
                        {
                            status = $"重复依赖项 (已被组 '{globalDependencyTracker[dep]}' 使用)";
                        }
                        else
                        {
                            status = "有效依赖项";
                        }
                    }

                    Debug.Log($"{status}: {dep}");
                }

                // 模拟收集过程，显示实际会收集的依赖项
                if (group.isEnableAddressable)
                {
                    var collectedAssets = new HashSet<string>();
                    var processedAssets = new HashSet<string>();
                    CollectAssetAndDependencies(asset.assetPath, collectedAssets, processedAssets);

                    var mainAssetPath = asset.assetPath;
                    var dependencyAssets = collectedAssets.Where(path => !path.Equals(mainAssetPath)).ToList();

                    Debug.Log($"<color=yellow>实际收集的依赖项 ({dependencyAssets.Count})</color>:\n{string.Join("\n", dependencyAssets)}");
                }
            }

            // 显示全局依赖项跟踪器状态
            Debug.Log($"=== 全局依赖项跟踪器状态 ===");
            foreach (var kvp in globalDependencyTracker)
            {
                Debug.Log($"{kvp.Key} -> 被组 '{kvp.Value}' 使用");
            }
        }

        // 根据路径类型创建AssetBundle
        // 在 CreateAssetBundleInternal 方法中，构建完成后添加清理调用
        private void CreateAssetBundleInternal(string savePath, BuildPathType pathType)
        {
            // 清理全局依赖项跟踪器
            globalDependencyTracker.Clear();
            if (string.IsNullOrEmpty(savePath))
            {
                Debug.LogError("保存路径不能为空");
                return;
            }

            // 过滤可构建的组
            var filteredGroups = AssetBundleList
                .Where(g => g.buildPathType == pathType && g.isEnableBuild)
                .ToList();

            if (filteredGroups.Count == 0)
            {
                Debug.Log($"<color=yellow>没有可构建的{pathType}AB包(所有包的isBuild都为false或不匹配构建类型)</color>");
                return;
            }

            try
            {
                // 确保目录存在
                Directory.CreateDirectory(savePath);

                // 配置构建选项
                var options = GetBuildOptions();

                // 构建AssetBundleBuild列表
                var builds = CreateAssetBundleBuilds(filteredGroups);

                if (builds.Count == 0)
                {
                    Debug.LogError("没有有效的AB包可构建");
                    return;
                }

                // 执行构建
                var manifest = BuildPipeline.BuildAssetBundles(
                    savePath,
                    builds.ToArray(),
                    options,
                    EditorUserBuildSettings.activeBuildTarget
                );

                // 检查构建结果
                if (manifest == null)
                {
                    Debug.LogError("构建AssetBundle失败");
                    return;
                }

                // 清理多余的manifest文件，只保留主manifest
                CleanupExtraManifestFiles(savePath);

                Debug.Log($"<color=green>成功构建{manifest.GetAllAssetBundles().Length}个{pathType} AssetBundle</color>\n保存路径: {savePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"构建AssetBundle出错: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 清理多余的manifest文件，只保留主manifest文件
        /// </summary>
        private void CleanupExtraManifestFiles(string outputPath)
        {
            try
            {
                // 获取输出目录名（主manifest的名称）
                string mainManifestName = Path.GetFileName(outputPath);
                string mainManifestFile = Path.Combine(outputPath, $"{mainManifestName}.manifest");

                // 获取所有manifest文件
                string[] manifestFiles = Directory.GetFiles(outputPath, "*.manifest");

                int deletedCount = 0;
                foreach (string manifestFile in manifestFiles)
                {
                    // 跳过主manifest文件
                    if (manifestFile.Equals(mainManifestFile, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"<color=cyan>保留主manifest文件: {Path.GetFileName(manifestFile)}</color>");
                        continue;
                    }

                    // 删除其他manifest文件
                    File.Delete(manifestFile);
                    deletedCount++;
                    Debug.Log($"<color=yellow>删除manifest文件: {Path.GetFileName(manifestFile)}</color>");
                }

                Debug.Log($"<color=green>清理完成：保留1个主manifest文件，删除了{deletedCount}个多余的manifest文件</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"清理manifest文件时出错: {e.Message}");
            }
        }

        // 获取构建选项
        private BuildAssetBundleOptions GetBuildOptions()
        {
            var options = BuildAssetBundleOptions.None;

            switch (CompressionType)
            {
                case CompressionType.None:
                    options |= BuildAssetBundleOptions.UncompressedAssetBundle;
                    break;
                case CompressionType.Lzma:
                    // LZMA是默认压缩方式，无需额外设置
                    break;
                case CompressionType.Lz4:
                    options |= BuildAssetBundleOptions.ChunkBasedCompression;
                    break;
            }

            return options;
        }

        // 创建AssetBundleBuild列表
        private List<AssetBundleBuild> CreateAssetBundleBuilds(List<AssetBundleGroup> groups)
        {
            var builds = new List<AssetBundleBuild>();

            foreach (var group in groups)
            {
                if (string.IsNullOrEmpty(group.assetBundleName))
                {
                    Debug.LogWarning("跳过未命名的AB包组");
                    continue;
                }

                var validAssets = group.assets
                    .Where(a => !string.IsNullOrEmpty(a.assetPath))
                    .ToArray();

                if (validAssets.Length == 0)
                {
                    Debug.LogWarning($"AB包组 '{group.assetBundleName}' 没有有效资源，跳过构建");
                    continue;
                }

                // 收集所有需要打包的资源（包括依赖项）
                var allAssetPaths = CollectAssetsWithDependencies(group, validAssets);

                if (allAssetPaths.Count == 0)
                {
                    Debug.LogWarning($"AB包组 '{group.assetBundleName}' 没有有效资源，跳过构建");
                    continue;
                }

                // 检查是否分割文件
                if (group.isEnablePackSeparately)
                {
                    CreateSeparateAssetBundles(group, allAssetPaths, builds);
                }
                else
                {
                    CreateSingleAssetBundle(group, allAssetPaths, builds);
                }
            }

            return builds;
        }

        /// <summary>
        /// 收集资源及其依赖项
        /// </summary>
        private List<string> CollectAssetsWithDependencies(AssetBundleGroup group, AssetBundleAssetsData[] validAssets)
        {
            var allAssetPaths = new HashSet<string>();
            var processedAssets = new HashSet<string>();

            foreach (var asset in validAssets)
            {
                // 只处理启用构建的资源
                if (!asset.isEnableBuild || string.IsNullOrEmpty(asset.assetPath))
                    continue;

                if (group.isEnableAddressable)
                {
                    // 启用可寻址资源定位系统 - 收集所有依赖项
                    CollectAssetAndDependencies(asset.assetPath, allAssetPaths, processedAssets);
                }
                else
                {
                    // 普通模式 - 只添加资源本身
                    allAssetPaths.Add(asset.assetPath);
                }
            }

            return allAssetPaths.ToList();
        }

        /// <summary>
        /// 增强的依赖项收集，添加更多验证
        /// </summary>
        private void CollectAssetAndDependencies(string assetPath, HashSet<string> allAssets, HashSet<string> processedAssets)
        {
            if (processedAssets.Contains(assetPath))
                return;

            processedAssets.Add(assetPath);

            // 添加主资源
            if (IsValidAssetForPacking(assetPath))
            {
                allAssets.Add(assetPath);
            }

            // 获取直接依赖项
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

            foreach (string dependency in dependencies)
            {
                // 跳过脚本文件和其他不支持的资源
                if (!IsValidAssetForPacking(dependency))
                    continue;

                // 跳过Unity内置资源
                if (IsBuiltInAsset(dependency))
                    continue;

                // 避免同一路径的循环引用
                if (dependency.Equals(assetPath, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // 递归收集依赖项的依赖项
                CollectAssetAndDependencies(dependency, allAssets, processedAssets);
            }
        }

        /// <summary>
        /// 检查资源是否适合打包到AssetBundle
        /// </summary>
        private bool IsValidAssetForPacking(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            // 检查是否是文件夹
            if (AssetDatabase.IsValidFolder(assetPath))
                return false;

            // 检查文件扩展名
            string extension = Path.GetExtension(assetPath).ToLower();
            if (UnsupportedExtensions.Contains(extension))
                return false;

            return true;
        }

        /// <summary>
        /// 检查是否是Unity内置资源
        /// </summary>
        private bool IsBuiltInAsset(string assetPath)
        {
            return assetPath.StartsWith("Resources/unity_builtin_extra") ||
                   assetPath.StartsWith("Library/unity default resources") ||
                   assetPath.StartsWith("Library/unity_builtin_extra");
        }

        /// <summary>
        /// 创建分离的AssetBundle包（每个资源单独打包）
        /// </summary>
        private void CreateSeparateAssetBundles(AssetBundleGroup group, List<string> assetPaths, List<AssetBundleBuild> builds)
        {
            if (group.isEnableAddressable)
            {
                var mainAssets = group.assets.Where(a => a.isEnableBuild && !string.IsNullOrEmpty(a.assetPath)).Select(a => a.assetPath).ToHashSet();
                var dependencyAssets = assetPaths.Where(path => !mainAssets.Contains(path)).ToList();

                // 为每个主资源创建独立包（只处理 isEnableBuild 为 true 的资源）
                foreach (var asset in group.assets.Where(a => a.isEnableBuild && !string.IsNullOrEmpty(a.assetPath)))
                {
                    string assetBundleName = group.prefixIsAssetBundleName
                        ? $"{group.assetBundleName}_{asset.assetName}"
                        : asset.assetName;

                    builds.Add(new AssetBundleBuild
                    {
                        assetBundleName = assetBundleName,
                        assetNames = new[] { asset.assetPath }
                    });

                    Debug.Log($"<color=cyan>主资源包 '{assetBundleName}' 已创建</color>");
                }

                // 为每个依赖项创建独立包
                foreach (string dependencyPath in dependencyAssets)
                {
                    // 检查依赖资源是否在资源列表中且 isEnableBuild=false
                    var assetData = group.assets.FirstOrDefault(a => a.assetPath == dependencyPath);
                    if (assetData != null && assetData.isEnableBuild == false)
                        continue; // 跳过未启用构建的依赖资源

                    if (globalDependencyTracker.ContainsKey(dependencyPath))
                    {
                        Debug.Log($"<color=yellow>依赖项 '{dependencyPath}' 已被组 '{globalDependencyTracker[dependencyPath]}' 打包，跳过重复打包</color>");
                        continue;
                    }

                    var dependencyAssetName = Path.GetFileNameWithoutExtension(dependencyPath);
                    var dependencyAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dependencyPath);
                    string assetTypeName = GetAssetTypeName(dependencyAsset);

                    string dependencyBundleName = group.prefixIsAssetBundleName
                        ? $"{group.assetBundleName}_{dependencyAssetName}_{assetTypeName}"
                        : $"{dependencyAssetName}_{assetTypeName}";

                    builds.Add(new AssetBundleBuild
                    {
                        assetBundleName = dependencyBundleName,
                        assetNames = new[] { dependencyPath }
                    });

                    globalDependencyTracker[dependencyPath] = group.assetBundleName;

                    Debug.Log($"<color=green>依赖项包 '{dependencyBundleName}' 已创建，路径: {dependencyPath}</color>");
                }
            }
            else
            {
                // 普通分离模式 - 只处理 isEnableBuild 为 true 的资源
                var enabledAssetPaths = group.assets.Where(a => a.isEnableBuild && !string.IsNullOrEmpty(a.assetPath)).Select(a => a.assetPath).ToList();
                foreach (string assetPath in enabledAssetPaths)
                {
                    var assetName = Path.GetFileNameWithoutExtension(assetPath);
                    string assetBundleName = group.prefixIsAssetBundleName
                        ? $"{group.assetBundleName}_{assetName}"
                        : assetName;

                    builds.Add(new AssetBundleBuild
                    {
                        assetBundleName = assetBundleName,
                        assetNames = new[] { assetPath }
                    });
                }

                Debug.Log($"<color=cyan>AB包组 '{group.assetBundleName}' 启用文件分割，生成了 {enabledAssetPaths.Count} 个独立AB包</color>");
            }
        }

        /// <summary>
        /// 创建单个AssetBundle包
        /// </summary>
        private void CreateSingleAssetBundle(AssetBundleGroup group, List<string> assetPaths, List<AssetBundleBuild> builds)
        {
            builds.Add(new AssetBundleBuild
            {
                assetBundleName = group.assetBundleName,
                assetNames = assetPaths.ToArray()
            });

            if (group.isEnableAddressable)
            {
                Debug.Log($"<color=cyan>可寻址AB包组 '{group.assetBundleName}' 将 {group.assets.Count} 个主资源和所有依赖项(共{assetPaths.Count}个资源)打包到单个AB包</color>");
            }
            else
            {
                Debug.Log($"<color=cyan>AB包组 '{group.assetBundleName}' 将 {assetPaths.Count} 个资源打包到单个AB包</color>");
            }
        }

        /// <summary>
        /// 获取资源类型名称（用于分组）
        /// </summary>
        private string GetAssetTypeName(UnityEngine.Object asset)
        {
            var type = asset.GetType();

            // 对于一些特殊类型，使用更通用的名称
            if (type == typeof(Texture2D) || type == typeof(Sprite))
                return "Texture";
            if (typeof(ScriptableObject).IsAssignableFrom(type))
                return "ScriptableObject";

            return type.Name;
        }

        // 更新不支持的扩展名常量
        private static readonly string[] UnsupportedExtensions = { ".cs", ".shader", ".asmdef", ".unity", ".meta" };

        //保存数据
        public void SaveData()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"{this.name}-><color=green>保存数据成功</color>");
        }

        //构建本地资源对比文件
        public void CreateLocalAssetBundleInfoFile()
        {
            //创建资源对比文件
            CreateAssetBundleInfoFile(LocalSavePath);
        }

        //构建远端资源对比文件
        public void CreateRemoteAssetBundleInfoFile()
        {
            //创建资源对比文件
            CreateAssetBundleInfoFile(RemoteSavePath);
        }

        //AssetBundle构建完成后创建资源对比文件
        public void CreateAssetBundleInfoFile(string path)
        {
            string Path = Application.dataPath + $"/{path}";
            if (Directory.Exists(Path))
            {
                //获取文件夹信息
                DirectoryInfo assetBundlesPath = Directory.CreateDirectory(Path);
                //获取该文件夹下所有文件
                FileInfo[] fileInfos = assetBundlesPath.GetFiles();
                //存储AssetBundle信息
                string assetBundleCompareInfo = "";
                foreach (FileInfo info in fileInfos)
                {
                    //注意:AssetBundle没有后缀
                    if (info.Extension == "")
                    {
                        assetBundleCompareInfo += info.Name + "/" + info.Length + "/" + CreateAssetsBundlesHandles.GetMD5(info.FullName);
                        //添加分隔符
                        assetBundleCompareInfo += "|";
                    }
                }
                //去除最后一个分隔符
                assetBundleCompareInfo = assetBundleCompareInfo.Substring(
                    0,
                    assetBundleCompareInfo.Length - 1
                );
                File.WriteAllText(
                    Path + "/AssetBundleCompareInfo.txt",
                    assetBundleCompareInfo,
                    Encoding.UTF8
                );
                Debug.Log($"AssetBundle对比文件创建成功:{Path + "/AssetBundleCompareInfo.txt"}");
            }
        }

        /// <summary>
        /// 清空AB包组数据
        /// </summary>
        /// <param name="group">AB包组</param>
        public void ClearABGroupData(AssetBundleGroup group)
        {
            foreach (var ABGroup in AssetBundleList)
            {
                if (ABGroup.assetBundleName == group.assetBundleName)
                {
                    ABGroup.assets.Clear();
                }
            }
        }

    }

    // AB包组和数据类
    [System.Serializable]
    public class AssetBundleGroup
    {
        [Tooltip("AssetBundle包名")]
        public string assetBundleName = string.Empty;

        [Tooltip("构建路径类型")]
        public BuildPathType buildPathType = BuildPathType.Local;

        [Tooltip("是否启用构建")]
        public bool isEnableBuild = true;

        [Tooltip("是否拆分文件")]
        public bool isEnablePackSeparately = false;

        [Tooltip("是添加前缀 - 启用文件分割时应用")]
        public bool prefixIsAssetBundleName = false;

        [Tooltip("是否启用可寻址资源定位系统")]
        public bool isEnableAddressable = false;

        [Tooltip("包资源列表")]
        public List<AssetBundleAssetsData> assets;
    }

    [System.Serializable]
    public class AssetBundleAssetsData
    {
        [SerializeField]
        private UnityEngine.Object assetsObject;
        public UnityEngine.Object AssetsObject
        {
            get => assetsObject;
            set
            {
                assetsObject = value;
                UpdateAssetInfo();
            }
        }

        [ShowOnly] public bool isEnableBuild = true; // 是否参与构建AB包(只有启用拆分文件时才有效)
        [ShowOnly] public string assetName;          // 资源名称
        [ShowOnly] public string assetPath;          // 资源路径
        [ShowOnly] public string assetGuid;          // 资源GUID

        public void UpdateAssetInfo()
        {
            if (assetsObject != null)
            {
                assetPath = AssetDatabase.GetAssetPath(assetsObject);
                assetName = assetsObject.name;
                assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            }
            else
            {
                assetPath = string.Empty;
                assetName = string.Empty;
                assetGuid = string.Empty;
            }
        }
    }
}
