using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;

namespace AssetBundleToolEditor
{
    [CreateAssetMenu(fileName = nameof(AssetBundleConfig), menuName = "Localization/AssetBundleConfig")]
    public class AssetBundleConfig : ScriptableObject
    {
        [Tooltip("AssetBundle配置文件")] public TextAsset JsonAsset;
        [Tooltip("Json文件路径")][ShowOnly] public string JosnPath;
        [Tooltip("Json文件GUID")][ShowOnly] public string JosnGuid;

        [Header("Setting")]
        [Tooltip("本地AssetBundle包保存路径")][ShowOnly] public string LocalSavePath = "StreamingAssets";
        [Tooltip("热更新AssetBundle包保存路径")][ShowOnly] public string RemoteSavePath;
        [Tooltip("AssetBundle包压缩格式")] public CompressionType CompressionType = CompressionType.Lz4;
        [Tooltip("AssetBundle包构建平台")] public BuildTarget BuildTarget = BuildTarget.Windows;

        [Header("ResServer")]
        [Tooltip("AssetBundle包传输方式")] public NetworkProtocolsType NetworkProtocolsType = NetworkProtocolsType.FTP;
        [Tooltip("资源服务器地址")] public string ResServerPath = "127.0.0.1";
        [Tooltip("主文件夹")] public string MainFolderName;
        [Tooltip("用户ID")] public string ID;
        [Tooltip("用户密码")] public string Password;
        [Tooltip("版本号")] public string VersionID;
        [Header("AssetBundles")]
        [Tooltip("AssetBundles包列表")] public List<AssetBundleGroup> AssetBundleList = new List<AssetBundleGroup>();

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
            if (!Directory.Exists(folderPath)) return;

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
            // 确保输出目录存在
            string outputPath = LocalSavePath;
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
            string outputPath = RemoteSavePath;
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

            // 添加isBuild限制，只选择isBuild为true的组
            var filteredGroups = AssetBundleList
                .Where(g => g.buildPathType == pathType && g.isBuild)
                .ToList();

            if (filteredGroups.Count == 0)
            {
                Debug.Log($"<color=yellow>没有可构建的{pathType}AB包（所有包的isBuild都为false或不匹配构建类型）</color>");
                return;
            }

            try
            {
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                BuildAssetBundleOptions options = BuildAssetBundleOptions.None;
                // 只有远端包才应用压缩设置
                // if (pathType == BuildPathType.Remote)
                // {
                switch (CompressionType)
                {
                    case CompressionType.None:
                        options |= BuildAssetBundleOptions.UncompressedAssetBundle;
                        break;
                    case CompressionType.Lzma:
                        break;
                    case CompressionType.Lz4:
                        options |= BuildAssetBundleOptions.ChunkBasedCompression;
                        break;
                }
                // }

                var builds = new List<AssetBundleBuild>();
                foreach (var group in filteredGroups)
                {
                    if (string.IsNullOrEmpty(group.assetBundleName))
                    {
                        Debug.LogWarning($"跳过未命名的AB包组");
                        continue;
                    }

                    // 可以在这里再次检查isBuild状态（可选的双重保险）
                    if (!group.isBuild)
                    {
                        Debug.LogWarning($"跳过isBuild为false的AB包组: {group.assetBundleName}");
                        continue;
                    }

                    builds.Add(new AssetBundleBuild
                    {
                        assetBundleName = group.assetBundleName,
                        assetNames = group.assets
                        .Where(a => !string.IsNullOrEmpty(a.assetPath))
                        .Select(a => a.assetPath)
                        .ToArray()
                    });
                }

                if (builds.Count == 0)
                {
                    Debug.LogError("没有有效的AB包可构建");
                    return;
                }

                var manifest = BuildPipeline.BuildAssetBundles(
                    savePath,
                    builds.ToArray(),
                    options,
                    EditorUserBuildSettings.activeBuildTarget
                );

                if (manifest == null)
                {
                    Debug.LogError("构建AssetBundle失败");
                    return;
                }
                Debug.Log($"成功构建{manifest.GetAllAssetBundles().Length}个{pathType} AssetBundle,保存路径: {savePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"构建AssetBundle出错: {e.Message}");
            }
            AssetDatabase.Refresh();
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
                        assetBundleCompareInfo += info.Name + "/" + info.Length + "/" + CreateAssetsBundlesHandles.GetMD5(info.FullName);
                        //添加分隔符
                        assetBundleCompareInfo += "|";
                    }
                }
                //去除最后一个分隔符
                assetBundleCompareInfo = assetBundleCompareInfo.Substring(0, assetBundleCompareInfo.Length - 1);
                File.WriteAllText(Path + "/AssetBundleCompareInfo.txt", assetBundleCompareInfo, Encoding.UTF8);
                Debug.Log($"AssetBundle对比文件创建成功:{Path + "/AssetBundleCompareInfo.txt"}");
            }
        }
    }

    // AB包组和数据类 
    [System.Serializable]
    public class AssetBundleGroup
    {
        [Tooltip("AssetBundle包名")] public string assetBundleName = string.Empty;
        [Tooltip("构建路径类型")] public BuildPathType buildPathType = BuildPathType.Local;
        [Tooltip("是否构建")] public bool isBuild = true;
        public List<AssetBundleAssetsData> assets;
    }

    [System.Serializable]
    public class AssetBundleAssetsData
    {
        [SerializeField] private UnityEngine.Object assetsObject;
        public UnityEngine.Object AssetsObject
        {
            get => assetsObject;
            set
            {
                assetsObject = value;
                UpdateAssetInfo();
            }
        }
        [ShowOnly] public string assetName;
        [ShowOnly] public string assetPath;
        [ShowOnly] public string assetGuid;

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