using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 资源加载工具
    /// </summary>
    public static class LoadAssetKit
    {
        #region 加载Resource文件夹资源
        //资产缓存字典
        private static readonly Dictionary<string, UnityEngine.Object> assetCacheDic = new Dictionary<string, UnityEngine.Object>();

        /// <summary>
        /// 卸载指定资源
        /// </summary>
        public static void UnloadAsset(string resPath)
        {
            if (assetCacheDic.TryGetValue(resPath, out var asset))
            {
                if (asset != null) Resources.UnloadAsset(asset);
                assetCacheDic.Remove(resPath);
            }
        }

        /// <summary>
        /// 清理所有缓存资源
        /// </summary>
        public static void ClearCache()
        {
            foreach (var asset in assetCacheDic.Values)
            {
                if (asset != null) Resources.UnloadAsset(asset);
            }
            assetCacheDic.Clear();
        }

        /// <summary>
        /// 从Resource文件夹加载资源 - 同步
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resPath">资源路径</param>
        /// <param name="callback">完成后回调</param>
        public static T LoadAssetFromRes<T>(string resPath, Action<T> callback = null, bool isCache = true) where T : UnityEngine.Object
        {
            // 参数检查
            if (string.IsNullOrEmpty(resPath))
            {
                Debug.LogError("[LoadAssetKit]:The resource path cannot be empty!");
                return HandleResult(null, callback);
            }

            // 检查缓存
            if (assetCacheDic.TryGetValue(resPath, out var cachedAsset))
            {
                return HandleResult(cachedAsset as T, callback);
            }

            // 同步加载模式
            if (callback == null)
            {
                T asset = Resources.Load<T>(resPath);
                if (asset == null)
                {
                    Debug.LogError($"[ResourceLoader]:Resource load failed:{resPath} (Type: {typeof(T)}).");
                    return null;
                }
                if (isCache) assetCacheDic[resPath] = asset;
                return asset;
            }
            // 异步加载模式
            else
            {
                LoadAssetAsyncFromRes(resPath, callback, isCache, CancellationToken.None).Forget();
                return null;
            }
        }

        /// <summary>
        /// 从Resource文件夹异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resPath">资源路径</param>
        /// <param name="isCache">是否缓存</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>加载的资源</returns>
        public static async UniTask<T> LoadAssetFromResAsync<T>(string resPath, bool isCache = true, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            // 参数检查
            if (string.IsNullOrEmpty(resPath))
            {
                Debug.LogError("[LoadAssetKit]:The resource path cannot be empty!");
                return null;
            }

            // 检查缓存
            if (assetCacheDic.TryGetValue(resPath, out var cachedAsset))
            {
                return cachedAsset as T;
            }

            var asset = await LoadAssetAsyncFromRes<T>(resPath, null, isCache, cancellationToken);
            return asset;
        }

        // 异步加载协程
        private static async UniTask<T> LoadAssetAsyncFromRes<T>(string resPath, Action<T> callback, bool isCache = true, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(resPath);
            await request.ToUniTask(cancellationToken: cancellationToken);

            if (request.asset == null)
            {
                Debug.LogError($"[ResourceLoader]:Asynchronous load failure:{resPath} (Type: {typeof(T)})");
                callback?.Invoke(null);
                return null;
            }

            if (isCache) assetCacheDic[resPath] = request.asset;
            var result = request.asset as T;
            callback?.Invoke(result);
            return result;
        }

        // 统一处理结果返回
        private static T HandleResult<T>(T asset, Action<T> callback) where T : UnityEngine.Object
        {
            callback?.Invoke(asset);
            return asset;
        }

        #endregion

        #region 加载AssetBundle资源

        //是否加载主包资源
        private static bool isLoadMainAssetBundle = false;
        //主包
        private static AssetBundle mainAssetBundle = null;
        //包依赖
        private static AssetBundleManifest manifest = null;
        //AB加载路径
        private static string loadPathUrl = Application.persistentDataPath + "/";
        //AssetBundle包管理字典
        private static readonly Dictionary<string, AssetBundleData> assetBundleDic = new Dictionary<string, AssetBundleData>();

        /// <summary>
        /// 设置AssetBundle加载路径
        /// </summary>
        /// <param name="path">新的加载路径（需要以 "/" 结尾）</param>
        public static void SetAssetBundleLoadPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[LoadAssetKit]: AssetBundle load path cannot be null or empty!");
                return;
            }

            // 确保路径以 "/" 结尾
            if (!path.EndsWith("/"))
            {
                path += "/";
            }

            loadPathUrl = path;
            Debug.Log($"[LoadAssetKit]: AssetBundle load path set to: {loadPathUrl}");
        }

        /// <summary>
        /// 获取当前AssetBundle加载路径
        /// </summary>
        /// <returns>当前加载路径</returns>
        public static string GetAssetBundleLoadPath()
        {
            return loadPathUrl;
        }

        //AssetBundle包数据类
        public class AssetBundleData
        {
            // 私有字段
            private AssetBundle assetBundle;
            private int referenceCount;
            private float lastUsedTime;

            // 公共属性
            public AssetBundle AssetBundle => assetBundle;
            public int ReferenceCount => referenceCount;
            public float LastUsedTime => lastUsedTime;

            // 构造函数
            public AssetBundleData(AssetBundle assetBundle)
            {
                this.assetBundle = assetBundle;
                referenceCount = 1;
                lastUsedTime = Time.realtimeSinceStartup;
            }

            // 增加引用计数
            public void AddReference()
            {
                referenceCount++;
                lastUsedTime = Time.realtimeSinceStartup;
            }

            // 减少引用计数并返回是否可以卸载
            public bool RemoveReference()
            {
                referenceCount--;
                lastUsedTime = Time.realtimeSinceStartup;
                return referenceCount <= 0;
            }

            // 卸载资源
            public void Unload(bool unloadAllLoadedObjects)
            {
                if (assetBundle != null)
                {
                    assetBundle.Unload(unloadAllLoadedObjects);
                    assetBundle = null;
                }
                referenceCount = 0;
            }

            // 检查是否可以卸载
            public bool CanUnload()
            {
                return referenceCount <= 0;
            }
        }

        /// <summary>
        /// 加载加载主包资源
        /// </summary>
        public static bool LoadMainAssetBundle(string mainAssetBundleName)
        {
            if (isLoadMainAssetBundle) return true;

            //加载依赖信息包
            if (mainAssetBundle == null)
            {
                mainAssetBundle = AssetBundle.LoadFromFile(loadPathUrl + mainAssetBundleName);

                if (mainAssetBundle == null)
                {
                    Debug.LogError($"[LoadAssetKit]: Failed to load main AssetBundle.: {mainAssetBundleName}");
                    return false;
                }

                manifest = mainAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

                if (manifest == null)
                {
                    Debug.LogError("[LoadAssetKit]: Failed to load AssetBundleManifest from main bundle.");
                    mainAssetBundle.Unload(true);
                    mainAssetBundle = null;
                    return false;
                }
            }

            isLoadMainAssetBundle = true;
            return true;
        }

        //加载AssetBundle
        private static void LoadAssetBundle(string abName)
        {
            if (!isLoadMainAssetBundle)
            {
                Debug.LogError("Please load the main AssetBundle first.");
                return;
            }
            //获取依赖信息
            AssetBundle assetBundle = null;
            string[] dependencies = manifest.GetAllDependencies(abName);
            //加载依赖
            foreach (string dependency in dependencies)
            {
                if (!assetBundleDic.ContainsKey(dependency))
                {
                    assetBundle = AssetBundle.LoadFromFile(loadPathUrl + dependency);
                    if (assetBundle == null)
                    {
                        Debug.LogError($"[LoadAssetKit]:Failed to load {dependency}.");
                        continue;
                    }
                    assetBundleDic.Add(dependency, new AssetBundleData(assetBundle));
                }
                else
                {
                    assetBundleDic[dependency].AddReference();
                }
            }
            //加载资源主包
            if (!assetBundleDic.ContainsKey(abName))
            {
                assetBundle = AssetBundle.LoadFromFile(loadPathUrl + abName);
                assetBundleDic.Add(abName, new AssetBundleData(assetBundle));
            }
            else
            {
                assetBundleDic[abName].AddReference();
            }
        }

        #region  同步加载方法

        /// <summary>
        /// 同步加载资源
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// </summary>
        public static UnityEngine.Object LoadAssetFromAssetBundle(string abName, string resName)
        {
            LoadAssetBundle(abName);
            //加载资源
            UnityEngine.Object obj = assetBundleDic[abName].AssetBundle.LoadAsset(resName);
            return obj;
        }

        /// <summary>
        /// 同步加载资源(Lua中常用)
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// type -> 资源类型
        /// </summary>
        public static UnityEngine.Object LoadAssetFromAssetBundle(string abName, string resName, System.Type type)
        {
            LoadAssetBundle(abName);
            //加载资源
            UnityEngine.Object obj = assetBundleDic[abName].AssetBundle.LoadAsset(resName, type);
            return obj;
        }


        /// <summary>
        /// 同步加载资源(C#中常用)
        /// T -> 资源类型
        /// abName -> AssetBundle包名
        /// resName -> 需要加载资源名
        /// </summary>
        public static T LoadAssetFromAssetBundle<T>(string abName, string resName) where T : UnityEngine.Object
        {
            LoadAssetBundle(abName);
            //加载资源
            T obj = assetBundleDic[abName].AssetBundle.LoadAsset<T>(resName);
            return obj;
        }

        #endregion

        #region  异步加载方法

        /// <summary>
        /// 异步加载方法
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// callBack -> 回调函数
        /// </summary>
        public static void LoadResAsync(string abName, string resName, Action<UnityEngine.Object> callBack)
        {
            ReallyLoadResAsync(abName, resName, callBack, CancellationToken.None).Forget();
        }

        /// <summary>
        /// 异步加载方法
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// cancellationToken -> 取消令牌
        /// </summary>
        public static async UniTask<UnityEngine.Object> LoadResAsync(string abName, string resName, CancellationToken cancellationToken = default)
        {
            return await ReallyLoadResAsync(abName, resName, null, cancellationToken);
        }

        //异步加载资源
        private static async UniTask<UnityEngine.Object> ReallyLoadResAsync(string abName, string resName, Action<UnityEngine.Object> callBack, CancellationToken cancellationToken = default)
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync(resName);
            await requestObj.ToUniTask(cancellationToken: cancellationToken);
            callBack?.Invoke(requestObj.asset);
            return requestObj.asset;
        }

        /// <summary>
        /// 异步加载方法(Lua中常用)
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// type -> 资源类型 
        /// callBack -> 回调函数
        /// </summary>
        public static void LoadResAsync(string abName, string resName, System.Type type, Action<UnityEngine.Object> callBack)
        {
            ReallyLoadResAsync(abName, resName, type, callBack, CancellationToken.None).Forget();
        }

        /// <summary>
        /// 异步加载方法(Lua中常用)
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// type -> 资源类型 
        /// cancellationToken -> 取消令牌
        /// </summary>
        public static async UniTask<UnityEngine.Object> LoadResAsync(string abName, string resName, System.Type type, CancellationToken cancellationToken = default)
        {
            return await ReallyLoadResAsync(abName, resName, type, null, cancellationToken);
        }

        //异步加载资源
        private static async UniTask<UnityEngine.Object> ReallyLoadResAsync(string abName, string resName, System.Type type, Action<UnityEngine.Object> callBack, CancellationToken cancellationToken = default)
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync(resName, type);
            await requestObj.ToUniTask(cancellationToken: cancellationToken);
            callBack?.Invoke(requestObj.asset);
            return requestObj.asset;
        }

        /// <summary>
        /// 异步加载方法(C#中常用)
        /// T -> 资源类型
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// callBack -> 回调函数
        /// </summary>
        public static void LoadResAsync<T>(string abName, string resName, Action<T> callBack) where T : UnityEngine.Object
        {
            ReallyLoadResAsync<T>(abName, resName, callBack, CancellationToken.None).Forget();
        }

        /// <summary>
        /// 异步加载方法(C#中常用)
        /// T -> 资源类型
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// cancellationToken -> 取消令牌
        /// </summary>
        public static async UniTask<T> LoadResAsync<T>(string abName, string resName, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return await ReallyLoadResAsync<T>(abName, resName, null, cancellationToken);
        }

        //异步加载资源
        private static async UniTask<T> ReallyLoadResAsync<T>(string abName, string resName, Action<T> callBack, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync<T>(resName);
            await requestObj.ToUniTask(cancellationToken: cancellationToken);
            var result = requestObj.asset as T;
            callBack?.Invoke(result);
            return result;
        }

        #endregion

        #region  AssetBundle卸载方法

        /// <summary>
        /// 卸载单个包
        /// 自动卸载引用为0的依赖包
        /// abName -> AssetBundle包名
        /// </summary>
        public static void UnLoadAssetBundle(string abName)
        {
            if (assetBundleDic.ContainsKey(abName))
            {
                // 减少主包的引用计数
                if (assetBundleDic[abName].RemoveReference())
                {
                    assetBundleDic[abName].Unload(false);
                    assetBundleDic.Remove(abName);
                }

                // 减少依赖包的引用计数
                string[] dependencies = manifest.GetAllDependencies(abName);
                foreach (string dependency in dependencies)
                {
                    if (assetBundleDic.ContainsKey(dependency))
                    {
                        if (assetBundleDic[dependency].RemoveReference())
                        {
                            assetBundleDic[dependency].Unload(false);
                            assetBundleDic.Remove(dependency);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 卸载所有包
        /// </summary>
        public static void UnLoadAllAssetBundle()
        {
            AssetBundle.UnloadAllAssetBundles(false);
            assetBundleDic.Clear();
            mainAssetBundle = null;
            manifest = null;
            isLoadMainAssetBundle = false;
        }

        /// <summary>
        /// 清理所有缓存（包括Resources和AssetBundle）
        /// </summary>
        public static void ClearAllCache()
        {
            // 清理Resources缓存
            ClearCache();

            // 清理AssetBundle缓存
            UnLoadAllAssetBundle();

            Debug.Log("[LoadAssetKit]: All caches cleared.");
        }

        #endregion

        #endregion
    }
}
