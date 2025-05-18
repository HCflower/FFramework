using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 资源加载工具
    /// TODO:待测试 
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
        public static T LoadAssetFromRes<T>(string resPath, Action<T> callback = null, bool isCache = false) where T : UnityEngine.Object
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
                CoroutineRunner.StartStaticCoroutine(LoadAssetAsync(resPath, callback, isCache));
                return null;
            }
        }

        // 异步加载协程
        private static IEnumerator LoadAssetAsync<T>(string resPath, Action<T> callback, bool isCache = false) where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(resPath);
            yield return request;

            if (request.asset == null)
            {
                Debug.LogError($"[ResourceLoader]:Asynchronous load failure:{resPath} (Type: {typeof(T)})");
                callback?.Invoke(null);
                yield break;
            }

            if (isCache) assetCacheDic[resPath] = request.asset;
            callback?.Invoke(request.asset as T);
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
        private static string loadPathUrl => Application.persistentDataPath + "/";
        //AssetBundle包管理字典
        private static readonly Dictionary<string, AssetBundleData> assetBundleDic = new Dictionary<string, AssetBundleData>();

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
            CoroutineRunner.StartStaticCoroutine(ReallyLoadResAsync(abName, resName, callBack));
        }

        //异步加载资源
        private static IEnumerator ReallyLoadResAsync(string abName, string resName, Action<UnityEngine.Object> callBack)
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync(resName);
            yield return requestObj;
            callBack(requestObj.asset);
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
            CoroutineRunner.StartStaticCoroutine(ReallyLoadResAsync(abName, resName, type, callBack));
        }

        //异步加载资源
        private static IEnumerator ReallyLoadResAsync(string abName, string resName, System.Type type, Action<UnityEngine.Object> callBack)
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync(resName, type);
            yield return requestObj;
            callBack(requestObj.asset);
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
            CoroutineRunner.StartStaticCoroutine(ReallyLoadResAsync<T>(abName, resName, callBack));
        }

        //异步加载资源
        private static IEnumerator ReallyLoadResAsync<T>(string abName, string resName, Action<T> callBack) where T : UnityEngine.Object
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync<T>(resName);
            yield return requestObj;
            callBack(requestObj.asset as T);
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
        }

        #endregion
        #endregion
    }
}
