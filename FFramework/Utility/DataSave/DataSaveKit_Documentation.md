# DataSaveKit 数据保存系统文档

## 目录

- [一、简介](#一简介)
- [二、优势](#二优势)
- [三、API 介绍](#三api介绍)
  - [1. 二进制存储 API](#1-二进制存储-api)
  - [2. JSON 存储 API](#2-json-存储-api)
- [四、核心功能](#四核心功能)
- [五、快速上手](#五快速上手)
  - [1. 保存数据到 JSON](#1-保存数据到-json)
  - [2. 加载数据](#2-加载数据)
  - [3. 检查文件是否存在](#3-检查文件是否存在)
  - [4. 删除存档](#4-删除存档)
  - [5. 二进制保存加载](#5-二进制保存加载)
- [六、使用场景示例](#六使用场景示例)
- [七、性能优化](#七性能优化)

---

## 一、简介

`DataSaveKit` 是一个为 Unity 项目设计的通用数据保存与加载工具，支持二进制和 JSON 两种格式。它适用于存档、配置、数据序列化等场景，提供了简单易用的接口，帮助开发者快速实现数据持久化。

---

## 二、优势

1. **多格式支持**：支持二进制和 JSON 两种存储格式，满足不同场景需求。
2. **易用性**：提供简单的 API，开发者无需关心底层实现。
3. **跨平台**：基于 Unity 的 `Application.persistentDataPath`，支持多平台数据存储。
4. **安全性**：支持数据加密和校验（需开发者自行扩展）。
5. **性能优化**：支持大数据分块保存和异步操作，减少性能开销。

---

## 三、API 介绍

### 1. 二进制存储 API

- `SaveDataToBinary<T>(string fileName, T data)`
  - 保存数据到二进制文件（`.dat`）。
- `LoadDataFromBinary<T>(string fileName)`
  - 从二进制文件加载数据。
- `CheckBinaryDataExists(string fileName)`
  - 检查二进制文件是否存在。
- `DeleteBinaryData(string fileName)`
  - 删除二进制存档文件。
- `SerializeToBytes<T>(T data)`
  - 将对象序列化为字节数组。
- `DeserializeFromBytes<T>(byte[] bytes)`
  - 从字节数组反序列化对象。

### 2. JSON 存储 API

- `SaveDataToJson<T>(string fileName, T data, bool prettyPrint = true)`
  - 保存数据到 JSON 文件（`.json`）。
- `LoadDataFromJson<T>(string fileName)`
  - 从 JSON 文件加载数据。
- `CheckDataExists(string fileName)`
  - 检查 JSON 文件是否存在。
- `DeleteJsonData(string fileName)`
  - 删除 JSON 文件。
- `SerializeToJson<T>(T data, bool prettyPrint = true)`
  - 将对象转换为 JSON 字符串。
- `DeserializeFromJson<T>(string json)`
  - 从 JSON 字符串解析对象。

---

## 四、核心功能

1. **二进制存储**：

   - 高效但不安全，适合存储非敏感数据。
   - 支持序列化和反序列化。

2. **JSON 存储**：

   - 可读性强，推荐使用。
   - 支持格式化输出，便于调试。

3. **文件管理**：

   - 检查文件是否存在。
   - 删除指定文件。

4. **跨平台支持**：

   - 自动适配不同平台的存储路径。

---

## 五、快速上手

### 1. 保存数据到 JSON

```csharp
MyData data = new MyData();
DataSaveKit.SaveDataToJson("my_save", data);
```

### 2. 加载数据

```csharp
MyData loaded = DataSaveKit.LoadDataFromJson<MyData>("my_save");
```

### 3. 检查文件是否存在

```csharp
bool exists = DataSaveKit.CheckDataExists("my_save.json");
```

### 4. 删除存档

```csharp
DataSaveKit.DeleteJsonData("my_save.json");
```

### 5. 二进制保存/加载

```csharp
DataSaveKit.SaveDataToBinary("my_save", data);
MyData loaded = DataSaveKit.LoadDataFromBinary<MyData>("my_save");
```

---

## 六、使用场景示例

1. **游戏存档**：

   - 使用 JSON 存储玩家进度，便于调试和修改。
   - 使用二进制存储大数据量的关卡信息。

2. **配置管理**：

   - 保存游戏设置（如音量、分辨率）。

3. **数据缓存**：

   - 将网络请求结果缓存到本地，减少重复请求。

4. **工具开发**：

   - 用于编辑器工具的数据持久化。

---

## 七、性能优化

1. **合并保存**：

   - 频繁读写的小数据可合并到一个文件中，减少 IO 操作。

2. **分块操作**：

   - 对于大数据结构，分块保存和加载，避免内存占用过高。

3. **异步操作**：

   - 使用异步方法保存非关键数据，避免阻塞主线程。

4. **缓存机制**：

   - 对常用数据进行内存缓存，减少文件读取次数。

5. **文件压缩**：

   - 对大文件进行压缩存储，减少磁盘占用。
