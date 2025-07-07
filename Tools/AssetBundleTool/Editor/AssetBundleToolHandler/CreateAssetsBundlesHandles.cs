using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Net;
using System.IO;
using System;

namespace AssetBundleToolEditor
{
    /// <summary>
    /// 创建AssetBundle工具类
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
        /// 获取所有配置
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
        /// 获取指定文件夹下所有AssetBundle文件并生成MD5码
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns></returns>
        public static string GetMD5(string path)
        {
            using (FileStream file = new FileStream(path, FileMode.Open))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] md5Info = md5.ComputeHash(file);
                //关闭文件流
                file.Close();
                //转换为16进制并拼接字符串
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < md5Info.Length; i++)
                {
                    stringBuilder.Append(md5Info[i].ToString("X2"));
                }
                return stringBuilder.ToString();
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
                Debug.LogError($"<color=red>服务器连接失败，无法上传资源！</color>");
                return;
            }

            Debug.Log($"<color=green>服务器连接成功，开始上传资源...</color>");

            // 获取所有文件
            var files = Directory.GetFiles(path);
            switch (networkProtocolsType)
            {
                case NetworkProtocolsType.FTP:
                    foreach (var filePath in files)
                    {
                        var fileInfo = new FileInfo(filePath);
                        // 过滤文件类型
                        if (string.IsNullOrEmpty(fileInfo.Extension) || fileInfo.Extension == ".txt")
                        {
                            // 传递完整文件路径
                            FtpUploadFile(filePath, fileInfo.Name, resServerPath, ftpUser, ftpPwd);
                        }
                    }
                    break;
                case NetworkProtocolsType.HTTP:
                    Debug.Log("<color=yellow>HTTP上传施工中~请使用FTP网络协议.</color>");
                    break;
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
                    // 简单的FTP连接测试
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverPath);
                    request.Method = WebRequestMethods.Ftp.ListDirectory;
                    request.Credentials = new NetworkCredential(userName, password);
                    request.Timeout = 5000; // 5秒超时

                    using (var response = request.GetResponse())
                    {
                        return true;
                    }
                }
                return false; // TODO:其他协议暂时返回false
            }
            catch
            {
                return false;
            }
        }


        // FTP上传文件
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
    }
}