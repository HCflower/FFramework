using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace AssetBundleToolEditor
{
    /// <summary>
    /// AssetBundle配置JSON数据类
    /// </summary>
    public static class AssetBundleConfigJSON
    {
        /// <summary>
        /// 根据AssetBundle配置生成JSON文件
        /// </summary>
        public static void GenerateJSON(AssetBundleConfig config)
        {
            var jsonData = new JSONData
            {
                compressionType = config.CompressionType,
                bundles = new List<BundleData>()
            };

            foreach (var group in config.AssetBundleList)
            {
                var bundleData = new BundleData
                {
                    bundleName = group.assetBundleName,
                    assets = new List<AssetData>()
                };

                foreach (var asset in group.assets)
                {
                    asset.UpdateAssetInfo();

                    bundleData.assets.Add(new AssetData
                    {
                        name = asset.assetName,
                        path = asset.assetPath,
                        guid = asset.assetGuid
                    });
                }

                jsonData.bundles.Add(bundleData);
            }

            string jsonPath = GetAssociatedJSONPath(config);
            File.WriteAllText(jsonPath, JsonUtility.ToJson(jsonData, true));
            config.JosnPath = jsonPath;
            config.JosnGuid = AssetDatabase.AssetPathToGUID(jsonPath);
            AssetDatabase.Refresh();
            Debug.Log($"JSON文件生成成功: {jsonPath}");
        }

        /// <summary>
        /// 通过存储的路径/GUID加载JSON数据到SO
        /// </summary>
        public static bool LoadJSONToSO(AssetBundleConfig config)
        {
            // 尝试通过存储的路径加载
            if (!string.IsNullOrEmpty(config.JosnPath) && File.Exists(config.JosnPath))
            {
                return LoadFromSpecificPath(config, config.JosnPath);
            }

            // 尝试通过存储的GUID加载
            if (!string.IsNullOrEmpty(config.JosnGuid))
            {
                string guidPath = AssetDatabase.GUIDToAssetPath(config.JosnGuid);
                if (!string.IsNullOrEmpty(guidPath) && File.Exists(guidPath))
                {
                    Debug.LogWarning($"路径加载失败,通过GUID回退加载: {guidPath}");
                    return LoadFromSpecificPath(config, guidPath);
                }
            }

            // 第三步：终极回退 - 尝试同名JSON文件（兼容旧版本）
            string fallbackPath = GetAssociatedJSONPath(config);
            if (File.Exists(fallbackPath))
            {
                Debug.LogWarning($"使用同名JSON文件回退: {fallbackPath}");
                return LoadFromSpecificPath(config, fallbackPath);
            }

            Debug.LogError($"无法加载JSON文件\n存储路径: {config.JosnPath}\n存储GUID: {config.JosnGuid}");
            return false;
        }

        /// <summary>
        /// 从指定路径加载JSON并更新SO引用
        /// </summary>
        private static bool LoadFromSpecificPath(AssetBundleConfig config, string jsonPath)
        {
            try
            {
                string json = File.ReadAllText(jsonPath);
                ApplyJSONData(config, json);

                // 更新引用（如果路径发生变化）
                if (config.JosnPath != jsonPath ||
                    config.JosnGuid != AssetDatabase.AssetPathToGUID(jsonPath))
                {
                    config.JosnPath = jsonPath;
                    config.JosnGuid = AssetDatabase.AssetPathToGUID(jsonPath);
                    EditorUtility.SetDirty(config);
                    Debug.Log($"<color=yellow>更新JSON引用:</color> {jsonPath}");
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载失败: {jsonPath}\n错误: {e.Message}");
                return false;
            }
        }

        //获取SO同名JSON文件
        private static string GetAssociatedJSONPath(AssetBundleConfig config)
        {
            string soPath = AssetDatabase.GetAssetPath(config);
            return Path.Combine(Path.GetDirectoryName(soPath), $"{Path.GetFileNameWithoutExtension(soPath)}.json");
        }

        //载入JSON数据
        private static void ApplyJSONData(AssetBundleConfig config, string json)
        {
            var jsonData = JsonUtility.FromJson<JSONData>(json);

            config.CompressionType = jsonData.compressionType;
            config.AssetBundleList.Clear();

            foreach (var bundleData in jsonData.bundles)
            {
                var group = new AssetBundleGroup
                {
                    assetBundleName = bundleData.bundleName,
                    assets = new List<AssetBundleAssetsData>()
                };

                foreach (var assetData in bundleData.assets)
                {
                    Object asset = LoadAssetWithFallback(assetData.path, assetData.guid, assetData.name);

                    group.assets.Add(new AssetBundleAssetsData
                    {
                        assetName = assetData.name,
                        assetPath = asset ? AssetDatabase.GetAssetPath(asset) : assetData.path,
                        assetGuid = assetData.guid,
                        AssetsObject = asset
                    });
                }
                config.AssetBundleList.Add(group);
            }
        }

        /// <summary>
        /// 优先路径加载，失败时回退GUID加载
        /// </summary>
        private static Object LoadAssetWithFallback(string path, string guid, string assetName)
        {
            // 1. 优先尝试路径加载
            if (!string.IsNullOrEmpty(path))
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                {
                    Debug.Log($"<color=yellow>[路径加载成功]</color> {assetName} ({path})");
                    return asset;
                }
            }

            // 2. 路径失败时尝试GUID加载
            if (!string.IsNullOrEmpty(guid))
            {
                string guidPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(guidPath) && guidPath != path)
                {
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(guidPath);
                    if (asset != null)
                    {
                        Debug.LogWarning($"<color=yellow>[GUID回退加载]</color> {assetName}\n" + $"原路径: {path}\n" + $"新路径: {guidPath}");
                        return asset;
                    }
                }
            }

            // 3. 双重加载均失败
            Debug.LogError($"<color=red>[加载失败]</color> {assetName}\n" + $"路径: {path}\n" + $"GUID: {guid}");
            return null;
        }

        [System.Serializable]
        private class JSONData
        {
            public CompressionType compressionType;
            public List<BundleData> bundles;
        }

        [System.Serializable]
        private class BundleData
        {
            public string bundleName;
            public List<AssetData> assets;
        }

        [System.Serializable]
        private class AssetData
        {
            public string name;
            public string path;
            public string guid;
        }
    }
}