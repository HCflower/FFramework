// =============================================================
// 描述：数据保存系统
// 作者：HCFlower
// 创建时间：2025-11-15 18:49:00
// 版本：1.0.0
// =============================================================
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.IO;
using System;

namespace FFramework.Utility
{
    public static class DataSave
    {
        private static string savePath = Application.persistentDataPath + "/";

        #region  二进制(不安全)

        ///<summary>
        /// 保存数据到二进制文件
        /// T -> 数据类型
        /// fileName -> 文件名称
        /// </summary>
        public static bool SaveDataToBinary<T>(string fileName, T data) where T : class
        {
            if (data == null) return false;
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                System.IO.FileStream file = System.IO.File.Create(savePath + fileName + ".dat");
                formatter.Serialize(file, data);
                file.Close();
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 从二进制文件加载数据
        /// T -> 数据类型
        /// fileName -> 文件名称
        /// </summary>
        public static T LoadDataFromBinary<T>(string fileName) where T : class
        {
            if (!File.Exists(savePath + fileName + ".dat"))
            {
                Debug.Log($"{fileName} not Found!");
                return null;
            }
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file = File.Open(savePath + fileName + ".dat", FileMode.Open);
                T data = formatter.Deserialize(file) as T;
                file.Close();
                Debug.Log($"The save was successful:{Application.persistentDataPath}/{fileName}.dat");
                return data;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return null;
            }

        }

        /// <summary>
        /// 检查二进制文件是否存在
        /// fileName -> 文件名称
        /// </summary>
        public static bool CheckBinaryDataExists(string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 删除存档文件
        /// fileName -> 文件名称 
        /// </summary>
        public static bool DeleteBinaryData(string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName + ".dat");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("File not found: " + filePath);
                return false;
            }
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 二进制序列化
        /// data -> 要序列化的数据
        /// </summary>
        public static byte[] SerializeToBytes<T>(T data) where T : class
        {
            if (data == null)
            {
                Debug.LogError("Cannot serialize null data");
                return null;
            }
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, data);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return null;
            }

        }

        /// <summary>
        /// 二进制反序列化
        /// bytes -> 要反序列化的字节数组
        /// </summary>
        public static T DeserializeFromBytes<T>(byte[] bytes) where T : class
        {
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError("Cannot deserialize empty or null byte array");
                return null;
            }
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    return (T)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return null;
            }
        }

        #endregion

        #region JSON

        ///<summary>
        /// 保存数据到Json文件
        /// fileName -> 文件名称
        /// T -> 数据类型
        /// prettyPrint -> 是否格式化输出
        /// </summary>
        public static bool SaveDataToJson<T>(string fileName, T data, bool prettyPrint = true)
        {
            if (data == null) return false;

            string filePath = Path.Combine(Application.persistentDataPath, fileName + ".json");
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint);
                File.WriteAllText(filePath, json);
                Debug.Log($"The save was successful:{Application.persistentDataPath}/{fileName}.json");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return false;
            }
        }

        ///<summary>
        /// 从JSON文件加载数据
        /// T -> 数据类型
        /// fileName -> 文件名称
        /// </summary>
        public static T LoadDataFromJson<T>(string fileName) where T : class
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName + ".json");

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"JSON file not found: {filePath}");
                return null;
            }
            try
            {
                string json = File.ReadAllText(filePath);
                T data = JsonUtility.FromJson<T>(json);
                Debug.Log($"The data load is successful:{Application.persistentDataPath}/{fileName}.json");
                return data;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return null;
            }
        }

        ///<summary>
        /// 检查JSON文件是否存在
        /// fileName -> 文件名称
        /// </summary>
        public static bool CheckDataExists(string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            return File.Exists(filePath);
        }

        ///<summary>
        /// 删除JSON文件
        /// fileName -> 文件名称
        /// </summary>
        public static bool DeleteJsonData(string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning("JSON file not found: " + filePath);
                return false;
            }

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return false;
            }
        }

        ///<summary>
        /// 将对象转换为JSON字符串
        /// T -> 数据类型
        /// data -> 要转换的对象
        /// </summary>
        public static string SerializeToJson<T>(T data, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(data, prettyPrint);
        }

        ///<summary>
        /// 从JSON字符串解析对象
        /// T -> 数据类型
        /// json -> 要解析的JSON字符串
        /// </summary>
        public static T DeserializeFromJson<T>(string json) where T : class
        {
            return JsonUtility.FromJson<T>(json);
        }

        #endregion
    }
}