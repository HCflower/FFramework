using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ResourceCompressionTool : EditorWindow
{
    private enum CompressionPage
    {
        Texture = 0,
        Model = 1,
        Audio = 2
    }

    private CompressionPage currentPage = CompressionPage.Texture;
    private Vector2 scrollPosition;

    // 纹理压缩设置
    private List<Texture2D> selectedTextures = new List<Texture2D>();
    private TextureCompressionSettings textureSettings = new TextureCompressionSettings();

    // 统计信息
    private CompressionResult lastResult;

    // GUI状态
    private bool showTextureList = true;
    private bool showCompressionSettings = true;
    private Dictionary<string, bool> textureItemFoldouts = new Dictionary<string, bool>();

    [MenuItem("Tools/资源压缩工具")]
    public static void ShowWindow()
    {
        ResourceCompressionTool window = GetWindow<ResourceCompressionTool>("资源压缩工具");
        window.minSize = new Vector2(400, 610);
        window.maxSize = new Vector2(500, 1000);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width));
        DrawHeader();
        DrawNavigation();
        DrawCurrentPage();
        DrawFooter();
        EditorGUILayout.EndVertical();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(5);

        // 用HelpBox包裹标题
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("资源压缩工具", titleStyle, GUILayout.Height(32));
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawNavigation()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width));
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("功能模块:", GUILayout.Width(60));

            string[] pageNames = new string[] { "图片压缩", "模型压缩", "音频压缩" };
            int selectedIndex = (int)currentPage;
            int newIndex = EditorGUILayout.Popup(selectedIndex, pageNames, GUILayout.ExpandWidth(true));

            if (newIndex != selectedIndex)
            {
                currentPage = (CompressionPage)newIndex;
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawCurrentPage()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (currentPage)
        {
            case CompressionPage.Texture:
                DrawTextureCompressionPage();
                break;
            case CompressionPage.Model:
                DrawModelCompressionPage();
                break;
            case CompressionPage.Audio:
                DrawAudioCompressionPage();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTextureCompressionPage()
    {
        // 第一部分：拖拽区域
        DrawDragAndDropArea();

        // 第二部分：压缩设置（可折叠）
        DrawCompressionSettings();

        // 第三部分：纹理列表（可折叠）
        DrawTextureListSection();
    }

    private void DrawDragAndDropArea()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.LabelField("拖拽区域", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("将图片文件或文件夹拖拽到下方区域", MessageType.Info);

            // 创建拖拽区域
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "", EditorStyles.helpBox);

            // 居中显示文本
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 11;
            GUI.Label(dropArea, "拖拽图片或文件夹到这里", labelStyle);

            // 处理拖拽事件
            HandleDragAndDrop(dropArea);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawCompressionSettings()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width));
        {
            // 可折叠标题
            EditorGUILayout.BeginHorizontal();
            {
                showCompressionSettings = EditorGUILayout.Foldout(showCompressionSettings, "压缩设置", true);
            }
            EditorGUILayout.EndHorizontal();

            if (showCompressionSettings)
            {
                // 用HelpBox风格包裹参数区域
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("基础设置", EditorStyles.miniBoldLabel);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("最大尺寸:", GUILayout.Width(70));
                        textureSettings.maxTextureSize = EditorGUILayout.IntField(textureSettings.maxTextureSize, GUILayout.Width(95));
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("压缩质量:", GUILayout.Width(70));
                        textureSettings.compressionQuality = EditorGUILayout.Slider(textureSettings.compressionQuality, 0f, 100f);
                    }
                    EditorGUILayout.EndHorizontal();

                    textureSettings.generateMipMaps = EditorGUILayout.Toggle("生成MipMaps", textureSettings.generateMipMaps);

                    EditorGUILayout.Space(3);

                    EditorGUILayout.LabelField("高级设置", EditorStyles.miniBoldLabel);
                    textureSettings.forceCompression = EditorGUILayout.Toggle("强制压缩", textureSettings.forceCompression);

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("压缩模式:", GUILayout.Width(70));
                        textureSettings.compressionMode = (TextureImporterCompression)EditorGUILayout.EnumPopup(textureSettings.compressionMode);
                    }
                    EditorGUILayout.EndHorizontal();

                    textureSettings.overridePlatformSettings = EditorGUILayout.Toggle("覆盖平台设置", textureSettings.overridePlatformSettings);
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawTextureListSection()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.BeginHorizontal();
            {
                showTextureList = EditorGUILayout.Foldout(showTextureList, "已选择的图片", true);

                GUILayout.FlexibleSpace();

                if (selectedTextures.Count > 0)
                {
                    GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel);
                    countStyle.normal.textColor = Color.gray;
                    EditorGUILayout.LabelField($"({selectedTextures.Count}个)", countStyle, GUILayout.Width(40));

                    long totalMemory = 0;
                    foreach (var texture in selectedTextures)
                    {
                        totalMemory += CalculateTextureMemoryUsage(texture, null);
                    }
                    float totalMemoryMB = totalMemory / (1024f * 1024f);

                    GUIStyle memoryStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                    memoryStyle.normal.textColor = totalMemoryMB > 50 ? Color.red : totalMemoryMB > 20 ? Color.yellow : Color.green;
                    EditorGUILayout.LabelField($"{totalMemoryMB:0.0}MB", memoryStyle, GUILayout.Width(60));

                    if (GUILayout.Button("清空", GUILayout.Height(20), GUILayout.Width(50)))
                    {
                        if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有图片吗？", "确定", "取消"))
                        {
                            selectedTextures.Clear();
                            textureItemFoldouts.Clear();
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (showTextureList)
            {
                // 列表内容
                if (selectedTextures.Count == 0)
                {
                    EditorGUILayout.HelpBox("请添加图片文件", MessageType.Info);
                }
                else
                {
                    DrawTextureList();
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawTextureList()
    {
        EditorGUILayout.BeginVertical();
        {
            bool needBreak = false; // 标记是否需要跳出循环
            for (int i = 0; i < selectedTextures.Count; i++)
            {
                Texture2D texture = selectedTextures[i];
                if (texture == null) continue;

                string texturePath = AssetDatabase.GetAssetPath(texture);
                string textureKey = texturePath + i.ToString();

                if (!textureItemFoldouts.ContainsKey(textureKey))
                    textureItemFoldouts[textureKey] = false;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // 主要内容区域 - 整体可点击
                    Rect mainRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));

                    // 主按钮区域（排除移除按钮）
                    Rect buttonRect = new Rect(mainRect.x + 2, mainRect.y + 2, mainRect.width, mainRect.height - 4);

                    // 创建按钮内容
                    Texture2D preview = AssetPreview.GetAssetPreview(texture) ?? Texture2D.whiteTexture;

                    // 计算内存占用
                    long memoryUsage = CalculateTextureMemoryUsage(texture, null);
                    float memoryMB = memoryUsage / (1024f * 1024f);

                    // 构建按钮显示文本 - 先箭头，再名称
                    string arrowText = textureItemFoldouts[textureKey] ? "▼" : "▶";
                    string buttonText = $"{arrowText}  {texture.name}";

                    GUIContent buttonContent = new GUIContent(buttonText, preview);

                    // 创建按钮样式
                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.alignment = TextAnchor.MiddleLeft;
                    buttonStyle.imagePosition = ImagePosition.ImageLeft;
                    buttonStyle.fontStyle = FontStyle.Bold;
                    buttonStyle.padding = new RectOffset(8, 60, 4, 4); // 右边留空间给内存显示

                    // 绘制主按钮
                    if (GUI.Button(buttonRect, buttonContent, buttonStyle))
                    {
                        textureItemFoldouts[textureKey] = !textureItemFoldouts[textureKey];
                    }

                    // 在按钮右侧绘制内存信息
                    Rect memoryRect = new Rect(buttonRect.xMax - 55, buttonRect.y + 6, 50, 16);
                    GUIStyle memoryStyle = new GUIStyle(EditorStyles.miniLabel);
                    memoryStyle.normal.textColor = memoryMB > 10 ? Color.red : memoryMB > 5 ? Color.yellow : Color.green;
                    memoryStyle.alignment = TextAnchor.MiddleRight;
                    memoryStyle.fontStyle = FontStyle.Bold;
                    GUI.Label(memoryRect, $"{memoryMB:0.0}MB", memoryStyle);

                    // 绘制移除按钮
                    GUIStyle removeStyle = new GUIStyle(GUI.skin.button);
                    removeStyle.fontSize = 12;
                    removeStyle.fontStyle = FontStyle.Bold;

                    // 详细信息（折叠内容）
                    if (textureItemFoldouts[textureKey])
                    {
                        EditorGUILayout.Space(2);
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("尺寸:", GUILayout.Width(35));
                            EditorGUILayout.LabelField($"{texture.width}×{texture.height}", GUILayout.Width(65));

                            EditorGUILayout.LabelField("格式:", GUILayout.Width(35));
                            EditorGUILayout.LabelField(texture.format.ToString(), GUILayout.Width(80));

                            EditorGUILayout.LabelField("MipMaps:", GUILayout.Width(70));
                            string mipStatus = texture.mipmapCount > 1 ? $"是({texture.mipmapCount}级)" : "否";
                            EditorGUILayout.LabelField(mipStatus, GUILayout.Width(60));

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("定位", GUILayout.Width(40), GUILayout.Height(18)))
                            {
                                EditorGUIUtility.PingObject(texture);
                            }

                            // 移除按钮
                            GUIStyle removeStyle2 = new GUIStyle(GUI.skin.button);
                            removeStyle2.fontSize = 12;
                            removeStyle2.fontStyle = FontStyle.Bold;
                            if (GUILayout.Button("×", removeStyle2, GUILayout.Width(24), GUILayout.Height(18)))
                            {
                                selectedTextures.RemoveAt(i);
                                textureItemFoldouts.Remove(textureKey);
                                i--;
                                needBreak = true; // 标记跳出
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        // 路径信息单独一行
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("路径:", GUILayout.Width(35));
                            EditorGUILayout.LabelField(Path.GetDirectoryName(texturePath), EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();

                if (needBreak)
                    break; // 跳出循环，保证EndVertical配对
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    // 处理拖拽的对象
                    ProcessDroppedObjects(DragAndDrop.objectReferences);

                    evt.Use();
                }
                break;
        }
    }

    private void ProcessDroppedObjects(UnityEngine.Object[] droppedObjects)
    {
        int addedCount = 0;

        foreach (UnityEngine.Object obj in droppedObjects)
        {
            if (obj == null) continue;

            // 如果是纹理，直接添加
            if (obj is Texture2D texture)
            {
                if (!selectedTextures.Contains(texture))
                {
                    selectedTextures.Add(texture);
                    addedCount++;
                }
            }
            // 如果是文件夹，递归查找所有纹理
            else if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)))
            {
                string folderPath = AssetDatabase.GetAssetPath(obj);
                addedCount += AddTexturesFromFolderPath(folderPath);
            }
            // 如果是其他资源，尝试获取路径并检查是否是纹理
            else
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // 检查文件扩展名
                    string extension = Path.GetExtension(assetPath).ToLower();
                    if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".tga" || extension == ".bmp")
                    {
                        Texture2D loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                        if (loadedTexture != null && !selectedTextures.Contains(loadedTexture))
                        {
                            selectedTextures.Add(loadedTexture);
                            addedCount++;
                        }
                    }
                }
            }
        }

        if (addedCount > 0)
        {
            Debug.Log($"成功添加 {addedCount} 个图片");
            Repaint();
        }
    }

    private long CalculateTextureMemoryUsage(Texture2D texture, TextureImporter importer)
    {
        if (texture == null) return 0;

        int width = texture.width;
        int height = texture.height;
        TextureFormat format = texture.format;
        bool hasMipMaps = texture.mipmapCount > 1;

        // 计算单个像素的字节数
        int bytesPerPixel = GetBytesPerPixel(format);
        if (bytesPerPixel == 0) return 0;

        // 计算基础内存占用
        long memory = width * height * bytesPerPixel;

        // 如果包含MipMaps，需要累加所有级别的内存
        if (hasMipMaps)
        {
            int mipWidth = width;
            int mipHeight = height;

            while (mipWidth > 1 || mipHeight > 1)
            {
                mipWidth = Mathf.Max(1, mipWidth / 2);
                mipHeight = Mathf.Max(1, mipHeight / 2);
                memory += mipWidth * mipHeight * bytesPerPixel;
            }
        }

        return memory;
    }

    private int GetBytesPerPixel(TextureFormat format)
    {
        switch (format)
        {
            case TextureFormat.Alpha8: return 1;
            case TextureFormat.RGB24: return 3;
            case TextureFormat.RGBA32: return 4;
            case TextureFormat.ARGB32: return 4;
            case TextureFormat.RGB565: return 2;
            case TextureFormat.R16: return 2;
            case TextureFormat.DXT1: return 0;
            case TextureFormat.DXT5: return 1;
            case TextureFormat.RGBA4444: return 2;
            case TextureFormat.BGRA32: return 4;
            case TextureFormat.RG16: return 2;
            case TextureFormat.R8: return 1;
            case TextureFormat.ETC_RGB4: return 0;
            case TextureFormat.ETC2_RGBA8: return 1;
            case TextureFormat.ASTC_4x4: return 1;
            case TextureFormat.ASTC_6x6: return 0;
            case TextureFormat.ASTC_8x8: return 0;
            case TextureFormat.ASTC_10x10: return 0;
            case TextureFormat.BC7: return 1;
            default: return 4;
        }
    }

    private void DrawModelCompressionPage()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.LabelField("模型压缩功能开发中...", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("模型压缩功能正在开发中，敬请期待！", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAudioCompressionPage()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.LabelField("音频压缩功能开发中...", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("音频压缩功能正在开发中，敬请期待！", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawFooter()
    {
        if (lastResult != null)
        {
            DrawCompressionResult();
        }

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("开始压缩", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                StartCompression();
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }

    private void DrawCompressionResult()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width));
        {
            EditorGUILayout.LabelField("压缩结果", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("压缩前:", GUILayout.Width(50));
                EditorGUILayout.LabelField($"{lastResult.originalSize:0.00} MB", EditorStyles.boldLabel, GUILayout.Width(80));

                EditorGUILayout.LabelField("压缩后:", GUILayout.Width(50));
                EditorGUILayout.LabelField($"{lastResult.compressedSize:0.00} MB", EditorStyles.boldLabel, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("节省:", GUILayout.Width(50));
                float saved = lastResult.originalSize - lastResult.compressedSize;
                float percentage = (saved / lastResult.originalSize) * 100f;

                GUIStyle savedStyle = new GUIStyle(EditorStyles.boldLabel);
                savedStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField($"{saved:0.00} MB ({percentage:0.0}%)", savedStyle);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox($"完成！处理了 {lastResult.processedCount} 个资源", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
    }

    private int AddTexturesFromFolderPath(string folderPath)
    {
        int addedCount = 0;

        // 获取文件夹内所有图片文件
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null && !selectedTextures.Contains(texture))
            {
                selectedTextures.Add(texture);
                addedCount++;
            }
        }

        // 递归查找子文件夹
        string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
        foreach (string subFolder in subFolders)
        {
            addedCount += AddTexturesFromFolderPath(subFolder);
        }

        if (addedCount > 0)
        {
            Repaint();
        }

        return addedCount;
    }

    private void StartCompression()
    {
        if (currentPage == CompressionPage.Texture)
        {
            CompressTextures();
        }
        else if (currentPage == CompressionPage.Model)
        {
            EditorUtility.DisplayDialog("提示", "模型压缩功能开发中", "确定");
        }
        else if (currentPage == CompressionPage.Audio)
        {
            EditorUtility.DisplayDialog("提示", "音频压缩功能开发中", "确定");
        }
    }

    private void CompressTextures()
    {
        if (selectedTextures.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请先选择要压缩的图片", "确定");
            return;
        }

        // 计算压缩前内存占用
        float originalTotalMemory = 0f;
        Dictionary<string, long> originalMemoryUsage = new Dictionary<string, long>();

        foreach (Texture2D texture in selectedTextures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            long memory = CalculateTextureMemoryUsage(texture, importer);
            originalMemoryUsage[path] = memory;
            originalTotalMemory += memory / (1024f * 1024f);

            Debug.Log($"压缩前 - {Path.GetFileName(path)}: {memory / (1024f * 1024f):0.00} MB ({texture.width}x{texture.height} {texture.format})");
        }

        // 执行压缩
        int processedCount = 0;
        foreach (Texture2D texture in selectedTextures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (CompressSingleTexture(path))
            {
                processedCount++;
            }
        }

        // 等待资源数据库刷新并重新加载纹理
        AssetDatabase.Refresh();
        System.Threading.Thread.Sleep(1500);

        // 重新加载纹理以获取压缩后的内存占用
        foreach (Texture2D texture in selectedTextures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        System.Threading.Thread.Sleep(500);

        // 计算压缩后内存占用
        float compressedTotalMemory = 0f;
        foreach (Texture2D texture in selectedTextures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            // 重新加载纹理获取最新数据
            Texture2D reloadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (reloadedTexture != null)
            {
                long compressedMemory = CalculateTextureMemoryUsage(reloadedTexture, importer);
                compressedTotalMemory += compressedMemory / (1024f * 1024f);

                long originalMemory = originalMemoryUsage[path];
                float reduction = ((originalMemory - compressedMemory) / (float)originalMemory) * 100f;

                Debug.Log($"压缩后 - {Path.GetFileName(path)}: {compressedMemory / (1024f * 1024f):0.00} MB ({reloadedTexture.width}x{reloadedTexture.height} {reloadedTexture.format}) 减少: {reduction:0.0}%");
            }
        }

        // 保存结果
        lastResult = new CompressionResult
        {
            originalSize = originalTotalMemory,
            compressedSize = compressedTotalMemory,
            processedCount = processedCount
        };

        // 刷新界面显示最新数据
        Repaint();

        // 显示结果对话框
        ShowResultDialog();
    }

    private bool CompressSingleTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;

        // 保存原始设置以便比较
        int originalMaxSize = importer.maxTextureSize;
        bool originalMipMap = importer.mipmapEnabled;
        TextureImporterCompression originalCompression = importer.textureCompression;

        // 设置新的压缩参数
        importer.maxTextureSize = textureSettings.maxTextureSize;
        importer.mipmapEnabled = textureSettings.generateMipMaps;

        if (textureSettings.forceCompression)
        {
            importer.textureCompression = textureSettings.compressionMode;
        }

        // 覆盖平台特定设置
        if (textureSettings.overridePlatformSettings)
        {
            // 为所有平台设置压缩
            string[] platforms = new string[] { "Standalone", "Android", "iPhone", "WebGL" };

            foreach (string platform in platforms)
            {
                TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(platform);
                if (platformSettings == null)
                {
                    platformSettings = new TextureImporterPlatformSettings();
                    platformSettings.name = platform;
                }

                platformSettings.overridden = true;
                platformSettings.maxTextureSize = textureSettings.maxTextureSize;
                platformSettings.compressionQuality = Mathf.RoundToInt(textureSettings.compressionQuality);

                // 根据平台设置合适的压缩格式
                if (textureSettings.forceCompression)
                {
                    switch (platform)
                    {
                        case "Android":
                            platformSettings.format = TextureImporterFormat.ETC2_RGBA8;
                            break;
                        case "iPhone":
                            platformSettings.format = TextureImporterFormat.ASTC_4x4;
                            break;
                        default:
                            platformSettings.format = TextureImporterFormat.BC7;
                            break;
                    }
                }

                importer.SetPlatformTextureSettings(platformSettings);
            }
        }

        // 检查设置是否真的改变了
        bool settingsChanged = originalMaxSize != importer.maxTextureSize ||
                              originalMipMap != importer.mipmapEnabled ||
                              originalCompression != importer.textureCompression;

        if (settingsChanged)
        {
            importer.SaveAndReimport();
            Debug.Log($"已压缩纹理: {Path.GetFileName(path)}");
            return true;
        }
        else
        {
            Debug.Log($"纹理设置未改变，跳过: {Path.GetFileName(path)}");
            return false;
        }
    }

    private void ShowResultDialog()
    {
        string message = $"压缩完成！\n\n" +
                        $"压缩前内存: {lastResult.originalSize:0.00} MB\n" +
                        $"压缩后内存: {lastResult.compressedSize:0.00} MB\n" +
                        $"节省内存: {lastResult.originalSize - lastResult.compressedSize:0.00} MB\n" +
                        $"处理资源: {lastResult.processedCount} 个";

        EditorUtility.DisplayDialog("压缩结果", message, "确定");
    }

    [System.Serializable]
    private class TextureCompressionSettings
    {
        public int maxTextureSize = 1024;
        public float compressionQuality = 50f;
        public bool generateMipMaps = false;
        public bool forceCompression = true;
        public TextureImporterCompression compressionMode = TextureImporterCompression.Compressed;
        public bool overridePlatformSettings = true;
    }

    [System.Serializable]
    private class CompressionResult
    {
        public float originalSize;
        public float compressedSize;
        public int processedCount;
    }
}