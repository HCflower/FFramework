using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

namespace FFramework
{
    /// <summary>
    /// AssetBundle加载处理器 
    /// (资源引用计数,同步/异步加载与卸载资源) 
    /// </summary>
    public class LoadAssetBundleHandler
    {
        // 资源包地址
        private string assetPath = Application.streamingAssetsPath;
        // 主包地址
        private string mainBundlePath;
        // 主包名
        private string mainBundleName;
        // 依赖包地址
        private string DependenciesPath = "Dependencies";
        // 主包Manifest           
        private AssetBundleManifest manifest;
        // 资源包信息
        private Dictionary<string, BundleRuntimeInfo> bundleRuntimeInfoDict = new Dictionary<string, BundleRuntimeInfo>();

        /// <summary>
        /// 初始化构造函数
        /// ->(资源包地址 | 主包地址 | 主包名)
        /// </summary>
        /// <param name="assetPath">资源包地址</param>
        /// <param name="mainBundlePath">主包地址</param>
        /// <param name="mainBundleName">主包名</param>
        public LoadAssetBundleHandler(string assetPath, string mainBundlePath, string mainBundleName)
        {
            this.assetPath = assetPath;
            this.mainBundlePath = mainBundlePath;
            this.mainBundleName = mainBundleName;
            LoadManifest();
        }

        #region 加载AB包

        // 加载主包Manifest
        private void LoadManifest()
        {
            string path = $"{assetPath}/{mainBundlePath}/{mainBundleName}";
            AssetBundle mainBundle = AssetBundle.LoadFromFile(path);
            if (mainBundle == null)
            {
                Debug.LogError($"主包加载失败: {mainBundleName}");
                return;
            }
            manifest = mainBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (manifest == null)
            {
                Debug.LogError("Manifest加载失败");
                mainBundle.Unload(false);
                return;
            }
            mainBundle.Unload(false);
        }

        // 加载依赖包（依赖包都在Dependencies文件夹下）
        private BundleRuntimeInfo LoadDependencyBundle(string depBundleName, string dependenciesPath = null)
        {
            if (bundleRuntimeInfoDict.TryGetValue(depBundleName, out var info))
            {
                info.RefCount++;
                return info;
            }

            string depPath = string.IsNullOrEmpty(dependenciesPath) ? DependenciesPath : dependenciesPath;
            string path = $"{assetPath}/{depPath}/{depBundleName}";

            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Debug.LogError($"依赖包加载失败: {depBundleName} 路径: {path}");
                return null;
            }

            info = new BundleRuntimeInfo { Bundle = bundle, RefCount = 1 };
            bundleRuntimeInfoDict[depBundleName] = info;
            return info;
        }

        // 加载单个AB包（已加载则增加引用计数）
        private BundleRuntimeInfo LoadBundleInternal(string bundleName)
        {
            if (bundleRuntimeInfoDict.TryGetValue(bundleName, out var info))
            {
                info.RefCount++;
                return info;
            }

            // 获取主包所在文件夹名
            string folder = mainBundleName.Split('/')[0];
            string path = $"{assetPath}/{folder}/{bundleName}";
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Debug.LogError($"AB包加载失败: {bundleName} 路径: {path}");
                return null;
            }

            info = new BundleRuntimeInfo { Bundle = bundle, RefCount = 1 };
            bundleRuntimeInfoDict[bundleName] = info;
            return info;
        }

        /// <summary>
        /// 同步加载单个AB包（自动加载依赖）
        /// </summary>
        public T LoadAsset<T>(string bundleName, string assetName, string dependenciesPath = null) where T : UnityEngine.Object
        {
            // 1. 加载依赖项
            if (manifest != null)
            {
                string[] dependencies = manifest.GetAllDependencies(bundleName);
                foreach (var dep in dependencies)
                {
                    LoadDependencyBundle(dep, dependenciesPath);
                }
            }

            // 2. 加载主资源包
            BundleRuntimeInfo bundleInfo = LoadBundleInternal(bundleName);
            if (bundleInfo == null || bundleInfo.Bundle == null)
            {
                Debug.LogError($"资源包未加载: {bundleName}");
                return null;
            }
            T asset = bundleInfo.Bundle.LoadAsset<T>(assetName);
            if (asset == null)
            {
                Debug.LogError($"资源未找到: {assetName} in {bundleName}");
            }
            return asset;
        }

        // 异步加载依赖包
        private async UniTask<BundleRuntimeInfo> LoadDependencyBundleAsync(string depBundleName, Action<float> progress, Action<bool> isDone, string dependenciesPath = null)
        {
            if (bundleRuntimeInfoDict.TryGetValue(depBundleName, out var info))
            {
                info.RefCount++;
                isDone?.Invoke(true);
                return info;
            }

            string depPath = string.IsNullOrEmpty(dependenciesPath) ? DependenciesPath : dependenciesPath;
            string path = $"{assetPath}/{depPath}/{depBundleName}";
            var request = AssetBundle.LoadFromFileAsync(path);
            while (!request.isDone)
            {
                progress?.Invoke(request.progress);
                await UniTask.Yield();
            }
            AssetBundle bundle = request.assetBundle;
            if (bundle == null)
            {
                Debug.LogError($"异步依赖包加载失败: {depBundleName} 路径: {path}");
                isDone?.Invoke(false);
                return null;
            }

            info = new BundleRuntimeInfo { Bundle = bundle, RefCount = 1 };
            bundleRuntimeInfoDict[depBundleName] = info;
            isDone?.Invoke(true);
            return info;
        }

        // 异步加载单个AB包（已加载则增加引用计数）
        private async UniTask<BundleRuntimeInfo> LoadBundleInternalAsync(string bundleName, Action<float> progress, Action<bool> isDone)
        {
            if (bundleRuntimeInfoDict.TryGetValue(bundleName, out var info))
            {
                info.RefCount++;
                isDone?.Invoke(true);
                return info;
            }

            string folder = mainBundleName.Split('/')[0];
            string path = $"{assetPath}/{folder}/{bundleName}";
            var request = AssetBundle.LoadFromFileAsync(path);
            while (!request.isDone)
            {
                progress?.Invoke(request.progress);
                await UniTask.Yield();
            }
            AssetBundle bundle = request.assetBundle;
            if (bundle == null)
            {
                Debug.LogError($"异步AB包加载失败: {bundleName} 路径: {path}");
                isDone?.Invoke(false);
                return null;
            }

            info = new BundleRuntimeInfo { Bundle = bundle, RefCount = 1 };
            bundleRuntimeInfoDict[bundleName] = info;
            isDone?.Invoke(true);
            return info;
        }

        /// <summary>
        /// 异步加载资源（自动加载依赖）
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(string bundleName, string assetName, string dependenciesPath = null, Action<float> progress = null, Action<bool> isDone = null) where T : UnityEngine.Object
        {
            // 1. 异步加载依赖项
            if (manifest != null)
            {
                string[] dependencies = manifest.GetAllDependencies(bundleName);
                foreach (var dep in dependencies)
                {
                    await LoadDependencyBundleAsync(dep, null, null, dependenciesPath);
                }
            }

            // 2. 异步加载主包
            var bundleInfo = await LoadBundleInternalAsync(bundleName, progress, isDone);
            if (bundleInfo == null || bundleInfo.Bundle == null)
            {
                Debug.LogError($"资源包未加载: {bundleName}");
                isDone?.Invoke(false);
                return null;
            }
            T asset = bundleInfo.Bundle.LoadAsset<T>(assetName);
            if (asset == null)
            {
                Debug.LogError($"资源未找到: {assetName} in {bundleName}");
                isDone?.Invoke(false);
            }
            else
            {
                isDone?.Invoke(true);
            }
            return asset;
        }

        #endregion

        #region 卸载AB包

        /// <summary>
        /// 同步卸载指定资源包及其依赖项（减少引用计数，计数为0时卸载）
        /// </summary>
        public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            // 卸载依赖项
            if (manifest != null)
            {
                string[] dependencies = manifest.GetAllDependencies(bundleName);
                foreach (string dep in dependencies)
                {
                    if (bundleRuntimeInfoDict.TryGetValue(dep, out BundleRuntimeInfo depInfo))
                    {
                        depInfo.RefCount--;
                        if (depInfo.RefCount <= 0)
                        {
                            depInfo.Bundle.Unload(unloadAllLoadedObjects);
                            bundleRuntimeInfoDict.Remove(dep);
                        }
                    }
                }
            }

            // 卸载主包
            if (bundleRuntimeInfoDict.TryGetValue(bundleName, out BundleRuntimeInfo info))
            {
                info.RefCount--;
                if (info.RefCount <= 0)
                {
                    info.Bundle.Unload(unloadAllLoadedObjects);
                    bundleRuntimeInfoDict.Remove(bundleName);
                }
            }
        }

        /// <summary>
        /// 异步卸载指定资源包及其依赖项（减少引用计数，计数为0时卸载）
        /// </summary>
        public async UniTask UnloadBundleAsync(string bundleName, bool unloadAllLoadedObjects = false)
        {
            // 卸载依赖项
            if (manifest != null)
            {
                string[] dependencies = manifest.GetAllDependencies(bundleName);
                foreach (string dep in dependencies)
                {
                    if (bundleRuntimeInfoDict.TryGetValue(dep, out BundleRuntimeInfo depInfo))
                    {
                        depInfo.RefCount--;
                        if (depInfo.RefCount <= 0)
                        {
                            depInfo.Bundle.Unload(unloadAllLoadedObjects);
                            bundleRuntimeInfoDict.Remove(dep);
                            await UniTask.Yield();
                        }
                    }
                }
            }

            // 卸载主包
            if (bundleRuntimeInfoDict.TryGetValue(bundleName, out BundleRuntimeInfo info))
            {
                info.RefCount--;
                if (info.RefCount <= 0)
                {
                    info.Bundle.Unload(unloadAllLoadedObjects);
                    bundleRuntimeInfoDict.Remove(bundleName);
                    await UniTask.Yield();
                }
            }
        }

        /// <summary>
        /// 同步清理所有已加载的资源包（包括依赖包）
        /// </summary>
        public void ClearAllBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var kv in bundleRuntimeInfoDict)
            {
                kv.Value.Bundle.Unload(unloadAllLoadedObjects);
            }
            bundleRuntimeInfoDict.Clear();
        }

        /// <summary>
        /// 异步清理所有已加载的资源包（包括依赖包）
        /// </summary>
        public async UniTask ClearAllBundlesAsync(bool unloadAllLoadedObjects = false, Action<float> progress = null, Action<bool> isDone = null)
        {
            int total = bundleRuntimeInfoDict.Count;
            int current = 0;
            foreach (var kv in bundleRuntimeInfoDict)
            {
                kv.Value.Bundle.Unload(unloadAllLoadedObjects);
                current++;
                progress?.Invoke((float)current / total);
                await UniTask.Yield();
            }
            bundleRuntimeInfoDict.Clear();
            isDone?.Invoke(true);
        }

        #endregion
    }

    /// <summary>
    /// AB包运行时信息类
    /// </summary>
    public class BundleRuntimeInfo
    {
        public AssetBundle Bundle { get; set; }
        public int RefCount { get; set; }
    }
}
