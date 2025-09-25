using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using System.Net;
using System.IO;
using System;

namespace FFramework
{
    /// <summary>
    /// AssetBundles资源下载管理器
    /// 支持断点续传,增量更新
    /// </summary>
    public class AssetBundlesDownLoadHandler : SingletonMono<AssetBundlesDownLoadHandler>
    {
        public string ResServerPath = "127.0.0.1";
        [Header("FTP")]
        public string Account;
        public string Password;
        //远端AssetBundles信息字典
        private Dictionary<string, AssetBundleInfo> remoteAssetBundlesInfoDic = new Dictionary<string, AssetBundleInfo>();
        //本地AssetBundles信息字典
        private Dictionary<string, AssetBundleInfo> localAssetBundlesInfoDic = new Dictionary<string, AssetBundleInfo>();
        //待下载AssetBundle列表,存储AssetBundle的名称
        private List<string> downloadResList = new List<string>();
        //已下载成功的资源列表
        private List<string> downloadedAssets = new List<string>();
        //需要更新的文件夹列表
        private List<string> updateFoldersList = new List<string>();

        //是否更新完成
        public bool IsUpdateCompleted = false;

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
                    IsUpdateCompleted = true;
                }
                else
                {
                    Debug.Log("资源更新失败.");
                    IsUpdateCompleted = false;
                }
            },
            (info) =>
            {
                Debug.Log($"<color=yellow>{info}</color>");
            }).Forget();
        }

        /// <summary>
        /// 检查更新
        /// resUpdateSuccess -> 更新成功回调
        /// </summary>
        public async UniTaskVoid CheckResUpdate(Action<bool> resUpdateSuccess, Action<string> resUpdateInfo)
        {
            try
            {
                // 1-下载远端主要资源对比文件
                bool downloadResult = await DownLoadRemoteMainCompareFileAsync();
                if (!downloadResult)
                {
                    resUpdateSuccess(false);
                    return;
                }

                resUpdateInfo("主要资源对比文件下载完成.");

                // 2-对比主要资源对比文件，确定需要更新的文件夹
                bool isFirstUpdate = await CompareMainFileAndGetUpdateFoldersAsync(resUpdateInfo);

                if (updateFoldersList.Count == 0 && !isFirstUpdate)
                {
                    resUpdateInfo("没有需要更新的资源.");
                    resUpdateSuccess(true);
                    return;
                }

                if (isFirstUpdate)
                {
                    resUpdateInfo("检测到首次更新，准备全量下载.");
                }
                else
                {
                    resUpdateInfo($"检测到 {updateFoldersList.Count} 个文件夹需要更新.");
                }

                // 3-下载需要更新文件夹的详细对比文件并解析
                bool parseResult = await DownloadAndParseDetailCompareFilesAsync(resUpdateInfo, isFirstUpdate);
                if (!parseResult)
                {
                    resUpdateSuccess(false);
                    return;
                }

                // 4-下载AssetBundles文件
                bool downloadAllResult = await DownloadAssetBundlesFileAsync(
                    (downloadProgress, downloadedSizeStr, totalSizeStr) =>
                    {
                        resUpdateInfo($"总下载进度: <color=yellow>{downloadProgress:P1}</color> ({downloadedSizeStr}/{totalSizeStr})");
                    });

                if (downloadAllResult)
                {
                    //下载完成,更新本地主要资源对比文件
                    string remoteMainInfo = File.ReadAllText(Application.persistentDataPath + "/RemoteAssetBundleCompareInfo_TMP.txt");
                    File.WriteAllText(Application.persistentDataPath + "/RemoteAssetBundleCompareInfo.txt", remoteMainInfo);

                    // 删除临时文件
                    string tmpFilePath = Application.persistentDataPath + "/RemoteAssetBundleCompareInfo_TMP.txt";
                    if (File.Exists(tmpFilePath))
                    {
                        File.Delete(tmpFilePath);
                        Debug.Log("<color=grey>删除主要资源对比临时文件</color>");
                    }
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

        // 下载远端主要资源对比文件
        private async UniTask<bool> DownLoadRemoteMainCompareFileAsync()
        {
            bool isOver = false;
            int reDownloadCount = 5;
            string localPath = Application.persistentDataPath; // 在主线程中获取路径

            while (!isOver && reDownloadCount > 0)
            {
                // 将路径传递给子线程
                string localFilePath = localPath + "/RemoteAssetBundleCompareInfo_TMP.txt";

                isOver = await UniTask.RunOnThreadPool(() =>
                {
                    return DownloadFile(ResServerPath, "RemoteAssetBundleCompareInfo.txt", localFilePath);
                });
                reDownloadCount -= 1;
            }

            return isOver;
        }

        // 对比主要资源对比文件，确定需要更新的文件夹
        private async UniTask<bool> CompareMainFileAndGetUpdateFoldersAsync(Action<string> resUpdateInfo)
        {
            string remoteFolderInfo = File.ReadAllText(Application.persistentDataPath + "/RemoteAssetBundleCompareInfo_TMP.txt");
            string localFolderInfo = "";
            bool isFirstUpdate = false;

            // 检查本地是否有主要资源对比文件
            string localMainFilePath = Application.persistentDataPath + "/RemoteAssetBundleCompareInfo.txt";
            if (File.Exists(localMainFilePath))
            {
                localFolderInfo = File.ReadAllText(localMainFilePath);
            }
            else
            {
                // 第一次更新
                isFirstUpdate = true;
                resUpdateInfo("检测到首次更新.");

                // 解析所有远端文件夹，添加到更新列表,Folder_用于占位
                string[] remoteFolderInfos = remoteFolderInfo.Split('|');
                for (int i = 0; i < remoteFolderInfos.Length; i++)
                {
                    if (!string.IsNullOrEmpty(remoteFolderInfos[i]))
                    {
                        updateFoldersList.Add($"Folder_{i}");
                    }
                }
                return true;
            }

            // 对比远端和本地的文件夹信息
            string[] remoteFolderArray = remoteFolderInfo.Split('|');
            string[] localFolderArray = localFolderInfo.Split('|');

            // 找出需要更新的文件夹
            for (int i = 0; i < remoteFolderArray.Length; i++)
            {
                if (i >= localFolderArray.Length ||
                    !string.IsNullOrEmpty(remoteFolderArray[i]) &&
                    remoteFolderArray[i] != localFolderArray[i])
                {
                    updateFoldersList.Add($"Folder_{i}");
                }
            }

            await UniTask.Yield();
            return isFirstUpdate;
        }

        // 下载并解析详细对比文件
        private async UniTask<bool> DownloadAndParseDetailCompareFilesAsync(Action<string> resUpdateInfo, bool isFirstUpdate)
        {
            // 从远端主要资源对比文件中解析文件夹名称
            string remoteFolderInfo = File.ReadAllText(Application.persistentDataPath + "/RemoteAssetBundleCompareInfo_TMP.txt");
            List<string> folderNames = new List<string>();

            // 解析格式：AB_Group1-MD5码|AB_Group2-MD5码
            string[] folderInfos = remoteFolderInfo.Split('|');
            foreach (string info in folderInfos)
            {
                if (!string.IsNullOrEmpty(info))
                {
                    string[] parts = info.Split('-');
                    if (parts.Length >= 2)
                    {
                        string folderName = parts[0];
                        folderNames.Add(folderName);
                    }
                }
            }

            if (isFirstUpdate)
            {
                // 第一次更新，下载所有文件夹的详细对比文件
                foreach (string folderName in folderNames)
                {
                    bool downloadResult = await DownloadFolderCompareFileAsync(folderName);
                    if (downloadResult)
                    {
                        string folderCompareContent = File.ReadAllText(Application.persistentDataPath + $"/{folderName}CompareInfo_TMP.txt");
                        ParseFolderCompareFile(folderCompareContent, folderName);
                        resUpdateInfo($"文件夹 {folderName} 资源信息解析完成.");
                    }
                }
            }
            else
            {
                // 增量更新，只下载需要更新的文件夹的详细对比文件
                foreach (string folderIndex in updateFoldersList)
                {
                    // 从文件夹索引获取实际文件夹名
                    string folderName = GetFolderNameFromIndex(folderIndex, folderNames);

                    if (!string.IsNullOrEmpty(folderName))
                    {
                        bool downloadResult = await DownloadFolderCompareFileAsync(folderName);
                        if (downloadResult)
                        {
                            string folderCompareContent = File.ReadAllText(Application.persistentDataPath + $"/{folderName}CompareInfo_TMP.txt");
                            ParseFolderCompareFile(folderCompareContent, folderName);
                            resUpdateInfo($"文件夹 {folderName} 资源信息解析完成.");
                        }
                    }
                }
            }

            // 构建下载列表
            BuildDownloadList(resUpdateInfo);

            return true;
        }

        // 下载指定文件夹的对比文件
        private async UniTask<bool> DownloadFolderCompareFileAsync(string folderName)
        {
            bool isOver = false;
            int reDownloadCount = 3;
            string localPath = Application.persistentDataPath; // 在主线程中获取路径

            while (!isOver && reDownloadCount > 0)
            {
                // 在主线程中构建路径
                string remoteFilePath = $"{folderName}/{folderName}CompareInfo.txt";
                string localFilePath = $"{localPath}/{folderName}CompareInfo_TMP.txt";

                isOver = await UniTask.RunOnThreadPool(() =>
                {
                    return DownloadFile(ResServerPath, remoteFilePath, localFilePath);
                });
                reDownloadCount -= 1;
            }

            return isOver;
        }

        // 根据索引获取文件夹名称
        private string GetFolderNameFromIndex(string folderIndex, List<string> folderNames)
        {
            // 解析索引，格式可能是 "Folder_0", "Folder_1" 等
            if (folderIndex.StartsWith("Folder_"))
            {
                string indexStr = folderIndex.Replace("Folder_", "");
                if (int.TryParse(indexStr, out int index) && index < folderNames.Count)
                {
                    return folderNames[index];
                }
            }

            return folderIndex; // 如果解析失败，直接返回原值
        }

        // 修改下载文件方法，支持文件夹结构
        private bool DownloadFile(string resServerPath, string fileName, string localSavePath)
        {
            try
            {
                // 确保FTP路径格式正确
                string ftpPath = fileName.Replace("\\", "/"); // 统一使用正斜杠
                Uri ftpUri = new Uri($"ftp://{resServerPath}/{ftpPath}");

                Debug.Log($"<color=cyan>尝试下载: {ftpUri}</color>");

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
                request.Credentials = new NetworkCredential(Account, Password);
                request.Proxy = null;
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.UseBinary = true;
                request.Timeout = 30000; // 30秒超时

                // 确保本地目录存在
                string directory = Path.GetDirectoryName(localSavePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Debug.Log($"<color=green>创建本地目录: {directory}</color>");
                }

                // 获取FTP响应流
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (FileStream fileStream = File.Create(localSavePath))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    long totalBytesRead = 0;

                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }

                    Debug.Log($"<color=green>{fileName}下载完成，大小: {totalBytesRead} bytes，保存到: {localSavePath}</color>");
                    return true;
                }
            }
            catch (WebException webEx)
            {
                if (webEx.Response is FtpWebResponse ftpResponse)
                {
                    Debug.LogError($"下载文件失败 {fileName}: FTP错误 {ftpResponse.StatusCode} - {ftpResponse.StatusDescription}");
                }
                else
                {
                    Debug.LogError($"下载文件失败 {fileName}: 网络错误 - {webEx.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"下载文件失败 {fileName}: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        // 修改下载AssetBundle文件的方法
        private async UniTask<bool> DownloadAssetBundlesFileAsync(Action<float, string, string> downloadProgress)
        {
            string localPath = Application.persistentDataPath; // 在主线程中获取路径
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
                List<string> failedDownloads = new List<string>();

                for (int i = 0; i < downloadResList.Count; i++)
                {
                    string assetName = downloadResList[i];

                    // 在主线程中获取文件夹名称和构建路径
                    string folderName = GetFolderNameForAsset(assetName);
                    string remoteFilePath;
                    string localFilePath;

                    if (!string.IsNullOrEmpty(folderName))
                    {
                        // 有文件夹结构的资源
                        remoteFilePath = $"{folderName}/{assetName}";
                        localFilePath = Path.Combine(localPath, folderName, assetName);
                    }
                    else
                    {
                        // 根目录资源
                        remoteFilePath = assetName;
                        localFilePath = Path.Combine(localPath, assetName);
                    }

                    Debug.Log($"<color=yellow>准备下载: {remoteFilePath} -> {localFilePath}</color>");

                    bool isOver = await UniTask.RunOnThreadPool(() =>
                    {
                        return DownloadFile(ResServerPath, remoteFilePath, localFilePath);
                    });

                    if (isOver)
                    {
                        // 更新已下载大小
                        if (remoteAssetBundlesInfoDic.ContainsKey(assetName))
                        {
                            downloadedSize += remoteAssetBundlesInfoDic[assetName].size;
                        }

                        // 计算并显示进度
                        downloadOverCount++;
                        float progress = totalSize > 0 ? (float)downloadedSize / totalSize : (float)downloadOverCount / downloadResList.Count;
                        string totalSizeStr = FormatFileSize(totalSize);
                        string downloadedSizeStr = FormatFileSize(downloadedSize);
                        downloadProgress(progress, downloadedSizeStr, totalSizeStr);

                        // 记录下载成功的资源
                        downloadedAssets.Add(assetName);
                        File.AppendAllText(succeedListPath, assetName + Environment.NewLine);
                    }
                    else
                    {
                        failedDownloads.Add(assetName);
                    }
                }

                // 更新待下载列表，只包含失败的下载
                downloadResList = failedDownloads;
                reDownloadCount -= 1;

                if (downloadResList.Count > 0)
                {
                    Debug.LogWarning($"<color=orange>还有 {downloadResList.Count} 个文件下载失败，剩余重试次数: {reDownloadCount}</color>");
                    await UniTask.Delay(1000); // 等待1秒后重试
                }
            }

            bool allDownloaded = downloadResList.Count == 0;

            // 全部下载完成后删除记录文件并更新本地详细对比文件
            if (allDownloaded && File.Exists(succeedListPath))
            {
                File.Delete(succeedListPath);
                downloadedAssets.Clear();

                // 更新本地详细对比文件
                await UpdateLocalDetailCompareFilesAsync();
            }

            return allDownloaded;
        }

        // 优化获取资源所属文件夹的方法
        private string GetFolderNameForAsset(string assetName)
        {
            // 优先从缓存的映射关系中查找
            if (assetToFolderMap.ContainsKey(assetName))
            {
                return assetToFolderMap[assetName];
            }

            // 在主线程中获取路径
            string persistentDataPath = Application.persistentDataPath;

            // 如果缓存中没有，遍历临时文件查找
            string[] tmpFiles = Directory.GetFiles(persistentDataPath, "*CompareInfo_TMP.txt");
            foreach (string tmpFile in tmpFiles)
            {
                string folderName = Path.GetFileNameWithoutExtension(tmpFile).Replace("CompareInfo_TMP", "");

                if (File.Exists(tmpFile))
                {
                    string content = File.ReadAllText(tmpFile);
                    if (content.Contains($"{assetName}-"))
                    {
                        // 缓存结果
                        assetToFolderMap[assetName] = folderName;
                        return folderName;
                    }
                }
            }

            // 如果还是找不到，尝试从正式的对比文件中查找
            string[] compareFiles = Directory.GetFiles(persistentDataPath, "*CompareInfo.txt", SearchOption.AllDirectories);
            foreach (string compareFile in compareFiles)
            {
                if (compareFile.Contains("RemoteAssetBundleCompareInfo")) continue;

                string folderName = Path.GetFileNameWithoutExtension(compareFile).Replace("CompareInfo", "");

                if (File.Exists(compareFile))
                {
                    string content = File.ReadAllText(compareFile);
                    if (content.Contains($"{assetName}-"))
                    {
                        // 缓存结果
                        assetToFolderMap[assetName] = folderName;
                        return folderName;
                    }
                }
            }

            return null; // 如果找不到，返回null（资源在根目录）
        }

        // 修改构建下载列表方法，记录资源与文件夹的关系
        private Dictionary<string, string> assetToFolderMap = new Dictionary<string, string>();

        private void BuildDownloadList(Action<string> resUpdateInfo)
        {
            // 加载本地资源对比文件（包括StreamingAssets和PersistentDataPath）
            LoadLocalAssetBundleInfo();

            // 清空资源文件夹映射
            assetToFolderMap.Clear();

            // 对比远端和本地资源文件信息,下载需要的AssetBundles
            foreach (string abName in remoteAssetBundlesInfoDic.Keys)
            {
                // 本地没有,下载
                if (!localAssetBundlesInfoDic.ContainsKey(abName))
                {
                    downloadResList.Add(abName);
                    Debug.Log($"<color=orange>需要下载新资源: {abName}</color>");
                }
                else
                {
                    // 本地有,MD5对比,不相同,下载
                    if (localAssetBundlesInfoDic[abName].md5 != remoteAssetBundlesInfoDic[abName].md5)
                    {
                        downloadResList.Add(abName);
                        Debug.Log($"<color=orange>需要更新资源: {abName}</color>");
                    }
                    else
                    {
                        Debug.Log($"<color=green>资源无需更新: {abName}</color>");
                    }
                    // 每次检测完后移出本地资源字典,遍历完还有的AB包直接删除
                    localAssetBundlesInfoDic.Remove(abName);
                }
            }

            // 删除没用的AssetBundles（只删除PersistentDataPath中的，不删除StreamingAssets中的）
            foreach (string abName in localAssetBundlesInfoDic.Keys)
            {
                // 尝试从各个可能的文件夹中删除
                DeleteAssetFromAllFolders(abName, resUpdateInfo);
            }

            resUpdateInfo($"需要下载的AssetBundle数量: {downloadResList.Count}");

            if (downloadResList.Count > 0)
            {
                Debug.Log($"<color=yellow>待下载资源列表: {string.Join(", ", downloadResList)}</color>");
            }
        }

        // 新增：从所有可能的文件夹中删除资源
        private void DeleteAssetFromAllFolders(string abName, Action<string> resUpdateInfo)
        {
            // 尝试从根目录删除
            string rootFilePath = Path.Combine(Application.persistentDataPath, abName);
            if (File.Exists(rootFilePath))
            {
                File.Delete(rootFilePath);
                resUpdateInfo($"<color=red>{abName}删除成功!</color>");
                return;
            }

            // 尝试从各个文件夹中删除
            string[] existingFolders = Directory.GetDirectories(Application.persistentDataPath);
            foreach (string folderPath in existingFolders)
            {
                string folderName = Path.GetFileName(folderPath);
                string abFilePath = Path.Combine(folderPath, abName);
                if (File.Exists(abFilePath))
                {
                    File.Delete(abFilePath);
                    resUpdateInfo($"<color=red>{folderName}/{abName}删除成功!</color>");
                    return;
                }
            }
        }

        // 修改解析方法，记录资源与文件夹的关系
        private void ParseFolderCompareFile(string folderInfo, string folderName, bool isLocal = false)
        {
            if (string.IsNullOrEmpty(folderInfo)) return;

            string[] assetBundles = folderInfo.Split('|');

            foreach (string abInfo in assetBundles)
            {
                if (string.IsNullOrEmpty(abInfo)) continue;

                string[] infos = abInfo.Split('-');
                if (infos.Length < 3)
                {
                    Debug.LogWarning($"跳过无效行(段数不足3): {abInfo}");
                    continue;
                }

                // 最后两个是 size / md5，其前面全部还原为名字（允许名字里有 - ）
                string md5 = infos[infos.Length - 1];
                string size = infos[infos.Length - 2];
                string abName = string.Join("-", infos, 0, infos.Length - 2);

                if (string.IsNullOrEmpty(abName))
                {
                    Debug.LogWarning($"跳过无效资源名: {abInfo}");
                    continue;
                }

                if (!long.TryParse(size, out _))
                {
                    Debug.LogWarning($"大小字段非法: {abInfo}");
                    continue;
                }

                if (!isLocal)
                {
                    assetToFolderMap[abName] = folderName;
                }

                var targetDic = isLocal ? localAssetBundlesInfoDic : remoteAssetBundlesInfoDic;
                if (!targetDic.ContainsKey(abName))
                {
                    targetDic.Add(abName, new AssetBundleInfo(abName, size, md5));
                }
            }
        }

        // 加载本地资源信息
        private void LoadLocalAssetBundleInfo()
        {
            localAssetBundlesInfoDic.Clear();

            // 1. 首先加载 StreamingAssets 中的本地资源对比文件
            LoadStreamingAssetsInfo();

            // 2. 然后加载 PersistentDataPath 中的已下载资源对比文件
            LoadPersistentDataInfo();
        }

        // 加载 StreamingAssets 中的本地资源信息
        private void LoadStreamingAssetsInfo()
        {
            string streamingAssetsCompareFile = Path.Combine(Application.streamingAssetsPath, "LocalAssetBundleCompareInfo.txt");

            if (File.Exists(streamingAssetsCompareFile))
            {
                try
                {
                    string content = File.ReadAllText(streamingAssetsCompareFile);
                    Debug.Log($"<color=cyan>加载StreamingAssets本地资源对比文件: {streamingAssetsCompareFile}</color>");

                    // 解析格式：文件夹名-MD5码|文件夹名-MD5码
                    string[] folderInfos = content.Split('|');
                    foreach (string info in folderInfos)
                    {
                        if (!string.IsNullOrEmpty(info))
                        {
                            string[] parts = info.Split('-');
                            if (parts.Length >= 2)
                            {
                                string folderName = parts[0];

                                // 加载对应文件夹的详细对比文件
                                string folderCompareFile = Path.Combine(Application.streamingAssetsPath, folderName, $"{folderName}CompareInfo.txt");
                                if (File.Exists(folderCompareFile))
                                {
                                    string folderContent = File.ReadAllText(folderCompareFile);
                                    ParseFolderCompareFile(folderContent, folderName, true);
                                    Debug.Log($"<color=green>加载StreamingAssets文件夹资源: {folderName}</color>");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"加载StreamingAssets资源信息失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"StreamingAssets中未找到本地资源对比文件: {streamingAssetsCompareFile}");
            }
        }

        // 加载 PersistentDataPath 中的已下载资源信息
        private void LoadPersistentDataInfo()
        {
            // 1. 扫描根目录中的对比文件
            string[] rootCompareFiles = Directory.GetFiles(Application.persistentDataPath, "*CompareInfo.txt", SearchOption.TopDirectoryOnly);
            foreach (string file in rootCompareFiles)
            {
                // 排除临时文件和主要对比文件
                if (file.Contains("_TMP") || file.Contains("RemoteAssetBundleCompareInfo"))
                    continue;

                if (File.Exists(file))
                {
                    string content = File.ReadAllText(file);
                    string folderName = Path.GetFileNameWithoutExtension(file).Replace("CompareInfo", "");
                    ParseFolderCompareFile(content, folderName, true);
                    Debug.Log($"<color=yellow>加载PersistentData根目录文件夹资源: {folderName}</color>");
                }
            }

            // 2. 扫描各个子文件夹中的对比文件
            string[] subDirectories = Directory.GetDirectories(Application.persistentDataPath);
            foreach (string subDir in subDirectories)
            {
                string folderName = Path.GetFileName(subDir);
                string folderCompareFile = Path.Combine(subDir, $"{folderName}CompareInfo.txt");

                if (File.Exists(folderCompareFile))
                {
                    string content = File.ReadAllText(folderCompareFile);
                    ParseFolderCompareFile(content, folderName, true);
                    Debug.Log($"<color=yellow>加载PersistentData子文件夹资源: {folderName}</color>");
                }
            }
        }

        // 修改更新本地详细对比文件的方法
        private async UniTask UpdateLocalDetailCompareFilesAsync()
        {
            try
            {
                // 将临时下载的详细对比文件转为正式文件
                string[] tmpFiles = Directory.GetFiles(Application.persistentDataPath, "*CompareInfo_TMP.txt");
                foreach (string tmpFile in tmpFiles)
                {
                    string tmpFileName = Path.GetFileName(tmpFile);

                    // 排除主要资源对比文件，只处理文件夹的详细对比文件
                    if (tmpFileName.Contains("RemoteAssetBundleCompareInfo"))
                        continue;

                    string fileName = Path.GetFileNameWithoutExtension(tmpFile).Replace("_TMP", "") + ".txt";
                    string folderName = fileName.Replace("CompareInfo.txt", "");

                    // 确定最终保存位置：在对应的文件夹内
                    string targetDir = Path.Combine(Application.persistentDataPath, folderName);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    string finalFile = Path.Combine(targetDir, fileName);

                    if (File.Exists(tmpFile))
                    {
                        File.Copy(tmpFile, finalFile, true);
                        File.Delete(tmpFile);
                        Debug.Log($"<color=green>更新本地详细对比文件: {folderName}/{fileName}</color>");
                    }
                }

                await UniTask.Yield();
            }
            catch (Exception ex)
            {
                Debug.LogError($"更新本地详细对比文件失败: {ex.Message}");
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
