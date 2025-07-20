# FFramework

FFramework 是一个用于游戏开发的 C# 框架，旨在提高开发效率、简化项目结构，并支持模块化扩展。
本框架适合中小型 Unity 项目，也可根据实际需求进行定制和扩展。
参考->QFramework [https://github.com/liangxiegame/QFramework]。

## 一、架构规范

FFramework 采用模块化架构，主要分为以下几个层次：

1. **ViewController**：负责视图层的控制，处理用户交互和界面逻辑。
2. **System**：系统层，管理游戏的核心逻辑和功能模块。
3. **Model**：数据层，负责数据的存储和管理。
4. **Utility**：工具层，提供通用的功能支持，如音频管理、UI 管理等。

## 二、核心功能模块

### 1. AudioKit

- 提供音频管理功能，包括背景音乐和音效的播放。
- 支持音量调节和资源加载。

### 2. DataSaveKit

- 提供数据存储和加载功能。
- 支持本地存储和云端同步。

### 3. DialogueKit

- 实现对话系统，支持分支对话和自动播放。

### 4. FSMKit

- 提供有限状态机功能，便于管理复杂的状态逻辑。

### 5. LoadAssetKit

- 提供资源加载功能，支持异步加载和缓存。

### 6. LoadSceneKit

- 管理场景加载和切换。

### 7. LocalizationKit

- 提供本地化支持，管理多语言资源。

### 8. PoolKit

- 对象池管理，优化资源使用。

### 9. TimerKit

- 提供计时器功能，支持倒计时和定时任务。

### 10. UIKit

- UI 管理工具，支持 UI 面板的加载、显示和层级管理。

### 11. RedDotKit

- 红点系统，管理提示红点的显示逻辑。

## 三、编辑器工具

### 1. AssetBundleTool

- 提供资源打包和管理功能。
- 支持本地和远端资源的构建与配置。

### 2. CreateProjectFolderTool

- 自动创建项目文件夹结构，便于快速初始化项目。

### 3. RuntimeConsoleTool

- 提供运行时控制台，便于调试和日志查看。

### 4. SkillEditor

- 技能编辑器，支持技能时间轴的编辑和管理。

## 四、使用说明

1. 下载并导入 FFramework。
2. 根据项目需求，选择需要的功能模块和工具。
3. 参考示例代码和文档，快速集成到项目中。
