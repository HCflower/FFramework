# FFramework.Kit DataSaveKit 数据保存系统文档

## 1. 简介

`DataSaveKit` 是一个用于 Unity 项目的通用数据保存与加载工具，支持二进制和 JSON 两种格式，适用于存档、配置、数据序列化等场景。

---

## 2. 主要功能

### 二进制存储（不安全，需序列化标记）

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

### JSON 存储（推荐，安全且可读）

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

### 保存数据到 JSON

```csharp
MyData data = new MyData();
DataSaveKit.SaveDataToJson("my_save", data);
```

### 加载数据

```csharp
MyData loaded = DataSaveKit.LoadDataFromJson<MyData>("my_save");
```

### 检查文件是否存在

```csharp
bool exists = DataSaveKit.CheckDataExists("my_save.json");
```

### 删除存档

```csharp
DataSaveKit.DeleteJsonData("my_save.json");
```

### 二进制保存/加载

```csharp
DataSaveKit.SaveDataToBinary("my_save", data);
MyData loaded = DataSaveKit.LoadDataFromBinary<MyData>("my_save");
```

---

## 4. 注意事项

- 二进制序列化需数据类型标记 `[Serializable]`，且字段需为可序列化类型。
- JSON 序列化推荐使用 Unity 的 `JsonUtility`，只支持字段，不支持属性。
- 文件路径均为 `Application.persistentDataPath` 下。
- 二进制存储不安全，建议仅用于本地存档。

---

## 5. API 参考

详见 `DataSaveKit.cs` 源码，每个方法均有注释说明。

---

如需扩展更多格式或加密，可在此基础上自定义实现。
