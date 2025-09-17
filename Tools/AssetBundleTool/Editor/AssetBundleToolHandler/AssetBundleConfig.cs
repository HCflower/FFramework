using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleToolEditor
{
    [CreateAssetMenu(
        fileName = nameof(AssetBundleConfig),
        menuName = "FFramework/AssetBundleConfig",
        order = 0
    )]
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

            // 获取并删除所有文件
            DirectoryInfo directory = new DirectoryInfo(folderPath);
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
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

        // 根据路径类型创建AssetBundle
        private void CreateAssetBundleInternal(string savePath, BuildPathType pathType)
        {
            if (string.IsNullOrEmpty(savePath))
            {
                Debug.LogError("保存路径不能为空");
                return;
            }

            // 过滤可构建的组
            var filteredGroups = AssetBundleList
                .Where(g => g.buildPathType == pathType && g.isBuild)
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

                // 检查是否分割文件
                if (group.isSplitFile)
                {
                    // 一个资源文件一个AB包
                    foreach (var asset in validAssets)
                    {
                        string assetBundleName;
                        // 是否添加前缀
                        if (group.prefixIsAssetBundleName)
                            assetBundleName = $"{group.assetBundleName}_{asset.assetName}";
                        else
                            assetBundleName = asset.assetName;

                        builds.Add(new AssetBundleBuild
                        {
                            assetBundleName = assetBundleName,
                            assetNames = new[] { asset.assetPath }
                        });
                    }
                    Debug.Log($"<color=cyan>AB包组 '{group.assetBundleName}' 启用文件分割，生成了 {validAssets.Length} 个独立AB包</color>");
                }
                else
                {
                    // 所有资源打包到一个AB包
                    builds.Add(new AssetBundleBuild
                    {
                        assetBundleName = group.assetBundleName,
                        assetNames = validAssets.Select(a => a.assetPath).ToArray()
                    });
                    Debug.Log($"<color=cyan>AB包组 '{group.assetBundleName}' 将 {validAssets.Length} 个资源打包到单个AB包</color>");
                }
            }

            return builds;
        }

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
                        assetBundleCompareInfo +=
                            info.Name
                            + "/"
                            + info.Length
                            + "/"
                            + CreateAssetsBundlesHandles.GetMD5(info.FullName);
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
    }

    // AB包组和数据类
    [System.Serializable]
    public class AssetBundleGroup
    {
        [Tooltip("AssetBundle包名")]
        public string assetBundleName = string.Empty;

        [Tooltip("构建路径类型")]
        public BuildPathType buildPathType = BuildPathType.Local;

        [Tooltip("是否分割文件")]
        public bool isSplitFile = false;

        [Tooltip("是添加前缀 - 启用文件分割时应用")]
        public bool prefixIsAssetBundleName = false;

        [Tooltip("是否构建")]
        public bool isBuild = true;
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

        [ShowOnly]
        public string assetName;

        [ShowOnly]
        public string assetPath;

        [ShowOnly]
        public string assetGuid;

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
