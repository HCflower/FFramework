using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System;

namespace AssetBundleToolEditor
{
    /// <summary>
    /// 创建AssetBundle工具类,
    /// </summary>
    public static class CreateAssetsBundlesHandles
    {
        //创建配置数据
        public static void CreateConfigureData(string savePath, string configureName)
        {
            //创建本地化数据SO文件
            AssetBundleConfig data = ScriptableObject.CreateInstance<AssetBundleConfig>();
            data.name = configureName;
            string assetPath = $"{savePath}/{configureName}.asset";
            AssetDatabase.CreateAsset(data, assetPath);
        }

        /// <summary>
        /// 获取所有配置文件
        /// </summary>
        /// <returns></returns>
        public static List<AssetBundleConfig> GetAllConfigureDataSOList()
        {

            List<AssetBundleConfig> configureDataSOList = new List<AssetBundleConfig>();
            // 获取项目中所有 LocalizationData 类型的资产 GUID
            string[] guids = AssetDatabase.FindAssets("t:AssetBundleConfig");

            foreach (string guid in guids)
            {
                // 通过 GUID 获取资产路径
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 加载资产
                AssetBundleConfig data = AssetDatabase.LoadAssetAtPath<AssetBundleConfig>(assetPath);

                if (data != null)
                {
                    configureDataSOList.Add(data);
                }
            }
            return configureDataSOList;
        }

        /// <summary>
        /// 获取所有文件夹的MD5码
        /// </summary>
        /// <param name="rootPath">根目录路径</param>
        /// <returns>格式: 文件夹名称-MD5码</returns>
        public static string GetAllFolderMD5(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                Debug.LogError($"根目录不存在: {rootPath}");
                return string.Empty;
            }

            var dirs = Directory.GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly);
            if (dirs.Length == 0) return string.Empty;

            // 按文件夹名排序
            Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);

            StringBuilder result = new StringBuilder();
            foreach (var dir in dirs)
            {
                string folderName = Path.GetFileName(dir);
                string folderMd5 = ComputeDirectoryMD5(dir);
                // 这里已经是 文件夹名称-MD5码 的格式
                result.Append(folderName).Append("-").Append(folderMd5).Append("|");
            }

            // 去掉最后一个 '|'
            if (result.Length > 0) result.Length--;
            return result.ToString();
        }

        /// <summary>
        /// 计算单个目录的聚合MD5，并在该目录下创建详细的MD5资源对比文件
        /// </summary>
        public static string ComputeDirectoryMD5(string directory)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
                                     .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) &&
                                                !f.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase) &&
                                                !f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) // 排除已有的对比文件
                                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                     .ToArray();

                if (files.Length == 0) return "EMPTY";

                StringBuilder concat = new StringBuilder();
                StringBuilder folderDetailInfo = new StringBuilder(); // 用于存储文件夹内详细信息

                foreach (var f in files)
                {
                    string fileName = Path.GetFileName(f);
                    FileInfo fileInfo = new FileInfo(f);
                    long fileSize = fileInfo.Length;

                    // 获取文件的纯MD5码（不包含文件名和大小）
                    string fileMd5 = GetFileMD5Only(f);

                    // 用于计算文件夹聚合MD5的字符串
                    concat.Append(fileName).Append(":").Append($"{fileName}-{fileSize}-{fileMd5}").Append("|");

                    // 用于文件夹详细信息的字符串：AB包名-资源大小-MD5码
                    folderDetailInfo.Append($"{fileName}-{fileSize}-{fileMd5}").Append("|");
                }

                if (concat.Length > 0) concat.Length--;
                if (folderDetailInfo.Length > 0) folderDetailInfo.Length--;

                // 在当前文件夹下创建详细的MD5资源对比文件
                string folderName = Path.GetFileName(directory);
                string folderDetailFile = Path.Combine(directory, $"{folderName}CompareInfo.txt");
                File.WriteAllText(folderDetailFile, folderDetailInfo.ToString(), Encoding.UTF8);

                string logPath = folderDetailFile.Replace("\\", "/");
                Debug.Log($"<color=cyan>在文件夹内创建详细MD5资源对比文件: {logPath}</color>");
                Debug.Log($"<color=yellow>文件夹 {folderName} 详细信息: {folderDetailInfo.ToString()}</color>");

                // 对拼接字符串再做一次MD5，作为文件夹的聚合MD5
                using (var md5Alg = MD5.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(concat.ToString());
                    byte[] hash = md5Alg.ComputeHash(bytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                        sb.Append(hash[i].ToString("X2"));
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"计算目录MD5失败: {directory} -> {ex.Message}");
                return "ERROR";
            }
        }

        /// <summary>
        /// 只获取文件MD5码（不包含文件名和大小信息）
        /// </summary>
        public static string GetFileMD5Only(string path)
        {
            try
            {
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (var md5 = MD5.Create())
                    {
                        byte[] md5Info = md5.ComputeHash(file);
                        StringBuilder stringBuilder = new StringBuilder();
                        for (int i = 0; i < md5Info.Length; i++)
                        {
                            stringBuilder.Append(md5Info[i].ToString("X2"));
                        }
                        return stringBuilder.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"计算文件MD5失败: {path} -> {ex.Message}");
                return "ERROR";
            }
        }

        /// <summary>
        /// 上传所有AssetBundle文件到服务器
        /// </summary>
        /// <param name="assetsPath">本地资源路径</param>
        /// <param name="resServerPath">服务器路径</param>
        /// <param name="networkProtocolsType">网络协议</param>
        /// <param name="ftpUser">FTP用户名</param>
        /// <param name="ftpPwd">FTP密码</param>
        public static void UploadAllAssetBundlesFile(string assetsPath, string resServerPath, NetworkProtocolsType networkProtocolsType, string ftpUser, string ftpPwd)
        {
            string path = assetsPath;
            // 确保目录存在
            if (!Directory.Exists(path))
            {
                Debug.LogError($"目录不存在: {path}");
                return;
            }

            // 先检测服务器连接
            if (!TestServerConnection(resServerPath, networkProtocolsType, ftpUser, ftpPwd))
            {
                Debug.LogWarning($"<color=yellow>{resServerPath}服务器连接失败，尝试自动创建根目录...</color>");
                // 自动创建resServerPath文件夹
                if (networkProtocolsType == NetworkProtocolsType.FTP)
                {
                    // 确保路径格式正确
                    string ftpRoot = resServerPath.StartsWith("ftp://") ? resServerPath : "ftp://" + resServerPath.TrimStart('/');
                    try
                    {
                        // 只创建根目录
                        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpRoot);
                        request.Method = WebRequestMethods.Ftp.MakeDirectory;
                        request.Credentials = new NetworkCredential(ftpUser, ftpPwd);
                        request.Proxy = null;
                        request.KeepAlive = false;
                        using (var response = request.GetResponse())
                        {
                            Debug.Log($"<color=green>根目录创建成功: {ftpRoot}</color>");
                        }
                    }
                    catch (WebException ex)
                    {
                        // 目录已存在时FTP会返回550错误，可以忽略
                        if (ex.Response is FtpWebResponse ftpResponse &&
                            ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        {
                            Debug.Log($"<color=yellow>根目录已存在: {ftpRoot}</color>");
                        }
                        else
                        {
                            Debug.LogError($"<color=red>根目录创建失败: {ftpRoot} -> {ex.Message}</color>");
                            return;
                        }
                    }
                }

                // 再次检测服务器连接
                if (!TestServerConnection(resServerPath, networkProtocolsType, ftpUser, ftpPwd))
                {
                    Debug.LogError($"<color=red>{resServerPath}服务器连接失败，无法上传资源！</color>");
                    return;
                }
            }

            Debug.Log($"<color=green>服务器连接成功，开始上传资源...</color>");

            switch (networkProtocolsType)
            {
                case NetworkProtocolsType.FTP:
                    // 确保服务器路径格式正确
                    string ftpBase = resServerPath.StartsWith("ftp://") ? resServerPath : "ftp://" + resServerPath.TrimStart('/');

                    // 直接递归上传所有文件夹中的文件，不预先创建平台文件夹
                    // UploadDirectoryRecursively 会自动创建需要的目录结构
                    UploadDirectoryRecursively(path, path, ftpBase, ftpUser, ftpPwd);
                    break;
            }
        }

        /// <summary>
        /// 递归上传目录中的所有文件，保持文件夹结构
        /// </summary>
        /// <param name="localPath">当前本地路径</param>
        /// <param name="rootPath">根路径，用于计算相对路径</param>
        /// <param name="resServerPath">服务器根路径</param>
        /// <param name="ftpUser">FTP用户名</param>
        /// <param name="ftpPwd">FTP密码</param>
        private static void UploadDirectoryRecursively(string localPath, string rootPath, string resServerPath, string ftpUser, string ftpPwd)
        {
            try
            {
                // 上传当前目录中的所有文件
                var files = Directory.GetFiles(localPath)
                    .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (var filePath in files)
                {
                    // 计算相对路径，保持文件夹结构
                    string relativePath = Path.GetRelativePath(rootPath, filePath).Replace("\\", "/");
                    var fileInfo = new FileInfo(filePath);

                    Debug.Log($"<color=cyan>准备上传: {relativePath}</color>");
                    FtpUploadFileWithPath(filePath, relativePath, resServerPath, ftpUser, ftpPwd);
                }

                // 递归处理子文件夹
                var directories = Directory.GetDirectories(localPath);
                foreach (var directory in directories)
                {
                    UploadDirectoryRecursively(directory, rootPath, resServerPath, ftpUser, ftpPwd);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"递归上传目录失败: {localPath} -> {ex.Message}");
            }
        }

        /// <summary>
        /// FTP上传文件，支持路径结构
        /// </summary>
        /// <param name="localFilePath">本地文件完整路径</param>
        /// <param name="remoteFilePath">远程文件相对路径</param>
        /// <param name="resServerPath">服务器根路径</param>
        /// <param name="ftpUser">FTP用户名</param>
        /// <param name="ftpPwd">FTP密码</param>
        private static void FtpUploadFileWithPath(string localFilePath, string remoteFilePath, string resServerPath, string ftpUser, string ftpPwd)
        {
            try
            {
                // 确保服务器路径格式正确
                if (!resServerPath.StartsWith("ftp://"))
                    resServerPath = "ftp://" + resServerPath.TrimStart('/');

                // 创建远程目录（如果需要）
                string remoteDir = Path.GetDirectoryName(remoteFilePath)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(remoteDir))
                {
                    CreateRemoteDirectory(resServerPath, remoteDir, ftpUser, ftpPwd);
                }

                // 构建完整的FTP URI
                Uri ftpUri = new Uri($"{resServerPath.TrimEnd('/')}/{remoteFilePath}");

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
                request.Credentials = new NetworkCredential(ftpUser, ftpPwd);
                request.Proxy = null;
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.UseBinary = true;

                // 上传文件
                using (Stream uploadStream = request.GetRequestStream())
                using (FileStream fileStream = File.OpenRead(localFilePath))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        uploadStream.Write(buffer, 0, bytesRead);
                    }
                }

                Debug.Log($"文件-><color=yellow>{remoteFilePath}</color>-<color=green>上传成功</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"文件-><color=red>{remoteFilePath}</color>-上传失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 在FTP服务器上创建目录结构
        /// </summary>
        /// <param name="ftpBaseUri">FTP基础URI</param>
        /// <param name="remoteDir">要创建的远程目录路径</param>
        /// <param name="ftpUser">FTP用户名</param>
        /// <param name="ftpPwd">FTP密码</param>
        private static void CreateRemoteDirectory(string ftpBaseUri, string remoteDir, string ftpUser, string ftpPwd)
        {
            try
            {
                string[] dirs = remoteDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string currentPath = "";

                foreach (string dir in dirs)
                {
                    currentPath += "/" + dir;
                    Uri dirUri = new Uri($"{ftpBaseUri.TrimEnd('/')}{currentPath}");

                    try
                    {
                        // 尝试创建目录
                        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(dirUri);
                        request.Credentials = new NetworkCredential(ftpUser, ftpPwd);
                        request.Method = WebRequestMethods.Ftp.MakeDirectory;
                        request.Proxy = null;
                        request.KeepAlive = false;

                        using (var response = request.GetResponse())
                        {
                            Debug.Log($"<color=green>创建远程目录成功: {currentPath}</color>");
                        }
                    }
                    catch (WebException ex)
                    {
                        // 目录可能已存在，忽略550错误
                        if (ex.Response is FtpWebResponse ftpResponse &&
                            ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        {
                            // 目录已存在，继续
                            continue;
                        }
                        else
                        {
                            Debug.LogWarning($"创建远程目录失败: {currentPath} -> {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建远程目录结构失败: {remoteDir} -> {ex.Message}");
            }
        }

        // 保留原有的简单上传方法作为备用
        private static void FtpUploadFile(string assetsPath, string fileName, string resServerPath, string ftpUser, string ftpPwd)
        {
            try
            {
                // 创建FTP连接
                Uri ftpUri = new Uri($"ftp://{resServerPath}/{fileName}");

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
                request.Credentials = new NetworkCredential(ftpUser, ftpPwd);
                request.Proxy = null;
                request.KeepAlive = false;
                //上传资源
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.UseBinary = true;

                // 上传文件声明网络流 - 使用正确的本地文件路径
                using (Stream uploadStream = request.GetRequestStream())
                // 这里使用完整文件路径
                using (FileStream fileStream = File.OpenRead(assetsPath))
                {
                    byte[] buffer = new byte[2048];
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        uploadStream.Write(buffer, 0, bytesRead);
                    }
                }

                Debug.Log($"文件-><color=yellow>{fileName}</color>-<color=green>上传成功</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"文件-><color=red>{fileName}</color>-上传失败: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 简化版服务器连接测试
        /// </summary>
        private static bool TestServerConnection(string serverPath, NetworkProtocolsType networkType, string userName, string password)
        {
            try
            {
                if (networkType == NetworkProtocolsType.FTP)
                {
                    // 确保路径格式正确
                    if (!serverPath.StartsWith("ftp://"))
                        serverPath = "ftp://" + serverPath.TrimStart('/');

                    Debug.Log($"Test FTP: {serverPath}");

                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverPath);
                    request.Method = WebRequestMethods.Ftp.ListDirectory;
                    request.Credentials = new NetworkCredential(userName, password);
                    request.Timeout = 5000; // 5秒超时
                    request.UsePassive = true; // 或false，视服务器而定

                    using (var response = request.GetResponse())
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FTP连接测试失败: {ex.Message}");
                return false;
            }
        }

    }
}
