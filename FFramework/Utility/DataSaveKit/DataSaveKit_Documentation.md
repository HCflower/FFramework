# FFramework.Kit DataSaveKit 数据保存系统文档

## 目录

- [1. 简介](#1-简介)
- [2. 主要功能](#2-主要功能)
  - [2.1 二进制存储](#21-二进制存储不安全需序列化标记)
  - [2.2 JSON 存储](#22-json-存储推荐安全且可读)
- [3. 使用示例](#3-使用示例)
  - [3.1 保存数据到 JSON](#31-保存数据到-json)
  - [3.2 加载数据](#32-加载数据)
  - [3.3 检查文件是否存在](#33-检查文件是否存在)
  - [3.4 删除存档](#34-删除存档)
  - [3.5 二进制保存/加载](#35-二进制保存加载)
- [4. API 参考](#4-api-参考)
- [5. 注意事项](#5-注意事项)

## 1. 简介

`DataSaveKit` 是一个用于 Unity 项目的通用数据保存与加载工具，支持二进制和 JSON 两种格式，适用于存档、配置、数据序列化等场景。

---

## 2. 主要功能

### 2.1 二进制存储（不安全，需序列化标记）

- `SaveDataToBinary<T>(string fileName, T data)`
  - 保存数据到二进制文件（.dat）。
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

### 2.2 JSON 存储（推荐，安全且可读）

- `SaveDataToJson<T>(string fileName, T data, bool prettyPrint = true)`
  - 保存数据到 JSON 文件（.json）。
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

## 3. 使用示例

### 3.1 保存数据到 JSON

```csharp
MyData data = new MyData();
DataSaveKit.SaveDataToJson("my_save", data);
```

### 3.2 加载数据

```csharp
MyData loaded = DataSaveKit.LoadDataFromJson<MyData>("my_save");
```

### 3.3 检查文件是否存在

```csharp
bool exists = DataSaveKit.CheckDataExists("my_save.json");
```

### 3.4 删除存档

```csharp
DataSaveKit.DeleteJsonData("my_save.json");
```

### 3.5 二进制保存/加载

```csharp
DataSaveKit.SaveDataToBinary("my_save", data);
MyData loaded = DataSaveKit.LoadDataFromBinary<MyData>("my_save");
```

---

## 4. API 参考

以下是 `DataSaveKit` 提供的所有 API 方法及其说明：

### 4.1 二进制存储 API

- `SaveDataToBinary<T>(string fileName, T data)`
  - 将数据保存到二进制文件（`.dat`）。
  - **参数**：
    - `fileName`：文件名（不含路径）。
    - `data`：要保存的对象，需标记 `[Serializable]`。
- `LoadDataFromBinary<T>(string fileName)`
  - 从二进制文件加载数据。
  - **参数**：
    - `fileName`：文件名（不含路径）。
  - **返回值**：反序列化后的对象。
- `CheckBinaryDataExists(string fileName)`
  - 检查指定的二进制文件是否存在。
  - **参数**：
    - `fileName`：文件名（不含路径）。
  - **返回值**：布尔值，表示文件是否存在。
- `DeleteBinaryData(string fileName)`
  - 删除指定的二进制文件。
  - **参数**：
    - `fileName`：文件名（不含路径）。
- `SerializeToBytes<T>(T data)`
  - 将对象序列化为字节数组。
  - **参数**：
    - `data`：要序列化的对象。
  - **返回值**：字节数组。
- `DeserializeFromBytes<T>(byte[] bytes)`
  - 从字节数组反序列化对象。
  - **参数**：
    - `bytes`：字节数组。
  - **返回值**：反序列化后的对象。

### 4.2 JSON 存储 API

- `SaveDataToJson<T>(string fileName, T data, bool prettyPrint = true)`
  - 将数据保存到 JSON 文件（`.json`）。
  - **参数**：
    - `fileName`：文件名（不含路径）。
    - `data`：要保存的对象。
    - `prettyPrint`：是否格式化输出（默认为 `true`）。
- `LoadDataFromJson<T>(string fileName)`
  - 从 JSON 文件加载数据。
  - **参数**：
    - `fileName`：文件名（不含路径）。
  - **返回值**：反序列化后的对象。
- `CheckDataExists(string fileName)`
  - 检查指定的 JSON 文件是否存在。
  - **参数**：
    - `fileName`：文件名（不含路径）。
  - **返回值**：布尔值，表示文件是否存在。
- `DeleteJsonData(string fileName)`
  - 删除指定的 JSON 文件。
  - **参数**：
    - `fileName`：文件名（不含路径）。
- `SerializeToJson<T>(T data, bool prettyPrint = true)`
  - 将对象转换为 JSON 字符串。
  - **参数**：
    - `data`：要序列化的对象。
    - `prettyPrint`：是否格式化输出（默认为 `true`）。
  - **返回值**：JSON 字符串。
- `DeserializeFromJson<T>(string json)`
  - 从 JSON 字符串解析对象。
  - **参数**：
    - `json`：JSON 字符串。
  - **返回值**：反序列化后的对象。

---

## 5. 注意事项

### 5.1 序列化要求

- 二进制序列化需数据类型标记 `[Serializable]`，且字段需为可序列化类型。
- 避免序列化复杂的引用关系，可能导致循环引用问题。
- 不支持接口类型、抽象类和多态序列化，需要自行处理类型转换。

### 5.2 JSON 特性

- JSON 序列化推荐使用 Unity 的 `JsonUtility`，只支持字段，不支持属性。
- 私有字段需要标记 `[SerializeField]` 才能被序列化。
- 不支持字典类型的直接序列化，需转换为键值对列表。
- 嵌套类需要标记 `[Serializable]` 属性。

### 5.3 文件管理

- 文件路径均为 `Application.persistentDataPath` 下。
- 平台差异：iOS、Android、Windows 的路径规则不同。
- 文件命名建议避免特殊字符和空格。
- 大文件读写应考虑异步操作，避免卡顿。

### 5.4 安全性考虑

- 二进制存储不安全，建议仅用于本地非敏感数据存档。
- 重要数据应考虑加密或哈希校验，防止数据被篡改。
- 避免存储账号密码等敏感信息。
- 考虑数据备份机制，防止存档损坏。

### 5.5 性能优化

- 频繁读写的小数据可考虑合并保存，减少 IO 操作。
- 大型数据结构考虑分块保存和加载，提高响应速度。
- 利用缓存机制减少重复读取操作。
- 非关键数据可使用异步方法进行保存。
