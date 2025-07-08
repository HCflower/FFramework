using System.Collections.Generic;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.Net;
using System.IO;
using System;

namespace FFramework
{
    /// <summary>
    /// AssetBundles资源下载管理器
    /// </summary>
    public class AssetBundlesDownLoadHandler : SingletonMono<AssetBundlesDownLoadHandler>
    {
        public string ResServerPath = "127.0.0.1";
        //远端AssetBundles信息字典
        private Dictionary<string, AssetBundleInfo> remoteAssetBundlesInfoDic = new Dictionary<string, AssetBundleInfo>();
        //本地AssetBundles信息字典
        private Dictionary<string, AssetBundleInfo> localAssetBundlesInfoDic = new Dictionary<string, AssetBundleInfo>();
        //待下载AssetBundle列表,存储AssetBundle的名称
        private List<string> downloadResList = new List<string>();
        //已下载成功的资源列表
        private List<string> downloadedAssets = new List<string>();

        //AssetBundle信息类
        public class AssetBundleInfo
        {
            public string assetBundleName;
            public long size;
            public string md5;

            public AssetBundleInfo(string assetBundleName, string size, string md5)
            {
                this.assetBundleName = assetBundleName;
                this.size = long.Parse(size);
                this.md5 = md5;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            CheckResUpdate((resUpdateSuccess) =>
            {
                if (resUpdateSuccess)
                {
                    Debug.Log("资源更新完成.");
                }
                else
                {
                    Debug.Log("资源更新失败.");
                }
            },
            (info) =>
            {
                Debug.Log($"<color=yellow>{info}</color>");
            });
        }

        /// <summary>
        /// 检查更新
        /// resUpdateSuccess -> 更新成功回调
        /// </summary>
        public async void CheckResUpdate(Action<bool> resUpdateSuccess, Action<string> resUpdateInfo)
        {
            try
            {
                // 1-下载远端资源对比文件
                bool downloadResult = await DownLoadAssetBundlesCompareFileAsync();
                if (!downloadResult)
                {
                    resUpdateSuccess(false);
                    return;
                }

                resUpdateInfo("资源对比文件下载完成.");
                string remoteInfo = File.ReadAllText(Application.persistentDataPath + "/AssetBundleCompareInfo_TMP.txt");
                resUpdateInfo("解析远端资源对比文件.");

                // 2-解析远端资源对比文件
                ParseResCompareFile(remoteInfo, remoteAssetBundlesInfoDic);
                resUpdateInfo("远端资源对比文件解析完成.");

                // 3-加载本地资源对比文件
                bool localFileResult = await GetLocalResCompareFileAsync();
                if (!localFileResult)
                {
                    resUpdateSuccess(false);
                    return;
                }

                resUpdateInfo("本地资源对比文件解析完成.");

                // 4-对比并构建下载列表
                BuildDownloadList(resUpdateInfo);

                // 5-下载AssetBundles文件
                bool downloadAllResult = await DownloadAssetBundlesFileAsync(
                    (downloadProgress, downloadedSizeStr, totalSizeStr) =>
                    {
                        resUpdateInfo($"总下载进度: <color=yellow>{downloadProgress:P1}</color> ({downloadedSizeStr}/{totalSizeStr})");
                    });

                if (downloadAllResult)
                {
                    //下载完成,更新本地资源对比文件
                    File.WriteAllText(Application.persistentDataPath + "/AssetBundleCompareInfo.txt", remoteInfo);
                }

                resUpdateSuccess(downloadAllResult);
                Debug.Log(downloadAllResult ? "<color=green>所有资源下载完成</color>" : "<color=red>资源下载失败!请检查网络连接.</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"资源更新过程中发生错误: {ex.Message}");
                resUpdateSuccess(false);
            }
        }

        // 异步下载资源对比文件
        private async Task<bool> DownLoadAssetBundlesCompareFileAsync()
        {
            bool isOver = false;
            int reDownloadCount = 5;
            string localPath = Application.persistentDataPath;

            while (!isOver && reDownloadCount > 0)
            {
                isOver = await Task.Run(() =>
                {
                    return DownloadFile(ResServerPath, "AssetBundleCompareInfo.txt", localPath + "/AssetBundleCompareInfo_TMP.txt");
                });
                reDownloadCount -= 1;
            }

            return isOver;
        }


        //解析资源对比文件
        private void ParseResCompareFile(string info, Dictionary<string, AssetBundleInfo> infoDic)
        {
            //拆分对比文件信息
            string[] strs = info.Split("|");
            string[] infos = null;
            foreach (string str in strs)
            {
                infos = str.Split("/");
                //添加到远端AssetBundles信息字典 - 记录下载的信息
                infoDic.Add(infos[0], new AssetBundleInfo(infos[0], infos[1], infos[2]));
            }
        }

        // 异步获取本地资源对比文件
        private async Task<bool> GetLocalResCompareFileAsync()
        {
            // 先判断persistentDataPath文件夹中是否有资源对比文件
            if (File.Exists(Application.persistentDataPath + "/AssetBundleCompareInfo.txt"))
            {
                return await GetLocalResCompareFileFromPathAsync("file:///" + Application.persistentDataPath + "/AssetBundleCompareInfo.txt");
            }
            // 读取streamingAssetsPath中的资源对比文件(第一次进入游戏)
            else if (File.Exists(Application.streamingAssetsPath + "/AssetBundleCompareInfo.txt"))
            {
                string path =
#if UNITY_ANDROID
            Application.streamingAssetsPath
#else
                    "file:///" + Application.streamingAssetsPath;
#endif
                return await GetLocalResCompareFileFromPathAsync(path + "/AssetBundleCompareInfo.txt");
            }
            // 没有资源对比文件
            else
            {
                return true;
            }
        }

        // 从指定路径异步获取本地资源对比文件
        private async Task<bool> GetLocalResCompareFileFromPathAsync(string filePath)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(GetLocalResCompareFileCoroutine(filePath, tcs));
            return await tcs.Task;
        }

        // 协程辅助方法
        private IEnumerator GetLocalResCompareFileCoroutine(string filePath, TaskCompletionSource<bool> tcs)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(filePath);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                ParseResCompareFile(webRequest.downloadHandler.text, localAssetBundlesInfoDic);
                tcs.SetResult(true);
            }
            else
            {
                tcs.SetResult(false);
            }
        }

        // 构建下载列表的独立方法
        private void BuildDownloadList(Action<string> resUpdateInfo)
        {
            // 对比远端和本地资源文件信息,下载需要的AssetBundles
            foreach (string abName in remoteAssetBundlesInfoDic.Keys)
            {
                // 本地没有,下载
                if (!localAssetBundlesInfoDic.ContainsKey(abName))
                {
                    downloadResList.Add(abName);
                }
                else
                {
                    // 本地有,MD5对比,不相同,下载
                    if (localAssetBundlesInfoDic[abName].md5 != remoteAssetBundlesInfoDic[abName].md5)
                    {
                        downloadResList.Add(abName);
                    }
                    // 每次检测完后移出本地资源字典,遍历完还有的AB包直接删除
                    localAssetBundlesInfoDic.Remove(abName);
                }
            }

            // 删除没用的AssetBundles
            foreach (string abName in localAssetBundlesInfoDic.Keys)
            {
                if (File.Exists(Application.persistentDataPath + "/" + abName))
                    File.Delete(Application.persistentDataPath + "/" + abName);
                resUpdateInfo($"<color=red>{abName}删除成功!</color>");
            }
        }



        // 异步下载AssetBundles文件
        private async Task<bool> DownloadAssetBundlesFileAsync(Action<float, string, string> downloadProgress)
        {
            string localPath = Application.persistentDataPath;
            int reDownloadCount = 5;
            int downloadOverCount = 0;

            // 计算总下载大小
            long totalSize = 0;
            foreach (string abName in downloadResList)
            {
                if (remoteAssetBundlesInfoDic.ContainsKey(abName))
                {
                    totalSize += remoteAssetBundlesInfoDic[abName].size;
                }
            }
            long downloadedSize = 0;

            // 加载已下载资源记录
            string succeedListPath = Path.Combine(localPath, "DownloadSucceedResList.txt");
            if (File.Exists(succeedListPath))
            {
                downloadedAssets = File.ReadAllLines(succeedListPath).ToList();
                downloadResList = downloadResList.Except(downloadedAssets).ToList();

                // 计算已下载部分的大小
                foreach (string abName in downloadedAssets)
                {
                    if (remoteAssetBundlesInfoDic.ContainsKey(abName))
                    {
                        downloadedSize += remoteAssetBundlesInfoDic[abName].size;
                    }
                }
            }

            while (downloadResList.Count > 0 && reDownloadCount > 0)
            {
                for (int i = 0; i < downloadResList.Count; i++)
                {
                    bool isOver = await Task.Run(() =>
                    {
                        return DownloadFile(ResServerPath, downloadResList[i], localPath + "/" + downloadResList[i]);
                    });

                    if (isOver)
                    {
                        // 更新已下载大小
                        if (remoteAssetBundlesInfoDic.ContainsKey(downloadResList[i]))
                        {
                            downloadedSize += remoteAssetBundlesInfoDic[downloadResList[i]].size;
                        }

                        // 计算并显示百分比进度
                        float progress = (float)++downloadOverCount / downloadResList.Count;
                        string totalSizeStr = FormatFileSize(totalSize);
                        string downloadedSizeStr = FormatFileSize(downloadedSize);
                        downloadProgress(progress, downloadedSizeStr, totalSizeStr);

                        // 记录下载成功的资源
                        downloadedAssets.Add(downloadResList[i]);
                        File.AppendAllText(succeedListPath, downloadResList[i] + Environment.NewLine);
                    }
                }

                // 更新待下载列表
                downloadResList = downloadResList.Except(downloadedAssets).ToList();
                reDownloadCount -= 1;
            }

            bool allDownloaded = downloadResList.Count == 0;

            // 全部下载完成后删除记录文件
            if (allDownloaded && File.Exists(succeedListPath))
            {
                File.Delete(succeedListPath);
                downloadedAssets.Clear();
            }

            return allDownloaded;
        }

        //下载文件
        private bool DownloadFile(string resServerPath, string fileName, string localSavePath)
        {
            // 创建FTP连接
            Uri ftpUri = new Uri($"ftp://{resServerPath}/{fileName}");
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Credentials = new NetworkCredential("Anonymous", "Anonymous");
            request.Proxy = null;
            request.KeepAlive = false;
            //下载资源
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.UseBinary = true;
            // 确保本地目录存在
            string directory = Path.GetDirectoryName(localSavePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            // 获取FTP响应流
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            // 创建本地文件流
            using (FileStream fileStream = File.Create(localSavePath))
            {
                byte[] buffer = new byte[2048];
                int bytesRead;

                // 从网络流读取并写入本地文件
                while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                }
                Debug.Log($"{fileName}下载完成，保存到: {localSavePath}");
                return true;
            }
        }

        // 格式化文件大小显示
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}
