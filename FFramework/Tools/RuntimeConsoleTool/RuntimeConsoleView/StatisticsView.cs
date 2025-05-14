using UnityEngine.UIElements;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// 统计信息视图
/// </summary>
public class StatisticsView : VisualElement
{
    #region 数据
    private float deltaTime = 0.0f;                         // 用于计算帧时间
    private int mainItemCounter = 0;                        // 用于生成唯一的audioItemID
    private int dynamicBatchingItemCounter = 0;
    private int staticBatchingItemCounter = 0;
    private int otherItemCounter = 0;
    //主要信息区域------------------------------------------------------------
    private ProfilerRecorder setPassCallsRecorder;
    private ProfilerRecorder drawCallsRecorder;
    private ProfilerRecorder verticesRecorder;
    private ProfilerRecorder batchesRecorder;
    private ProfilerRecorder trianglesRecorder;
    //动态批处理信息区域--------------------------------------------------------
    private ProfilerRecorder dynamicBatchedDrawCallsRecorder;
    private ProfilerRecorder dynamicBatchedBatchesRecorder;
    private ProfilerRecorder dynamicBatchedTrianglesRecorder;
    private ProfilerRecorder dynamicBatchedVerticesRecorder;
    private ProfilerRecorder dynamicBatchedTimeRecorder;
    //静态批处理信息区域--------------------------------------------------------
    private ProfilerRecorder staticBatchedDrawCallsRecorder;
    private ProfilerRecorder staticBatchedBatchesRecorder;
    private ProfilerRecorder staticBatchedTrianglesRecorder;
    private ProfilerRecorder staticBatchedVerticesRecorder;
    //其它信息区域-------------------------------------------------------------
    //GPU实例化
    private ProfilerRecorder instancingDrawCallsRecorder;
    private ProfilerRecorder instancingBatchesRecorder;
    private ProfilerRecorder instancingTrianglesRecorder;
    private ProfilerRecorder instancingVerticesRecorder;
    //Other
    private ProfilerRecorder usedTexturesCountRecorder;
    private ProfilerRecorder usedTexturesBytesRecorder;
    private ProfilerRecorder renderTexturesCountRecorder;
    private ProfilerRecorder renderTexturesBytesRecorder;
    private ProfilerRecorder renderTexturesChangesCountRecorder;
    private ProfilerRecorder usedBuffersCountRecorder;
    private ProfilerRecorder usedBuffersBytesRecorder;
    private ProfilerRecorder vertexBufferToInFrameCountRecorder;
    private ProfilerRecorder vertexBufferToInFrameBytesRecorder;
    private ProfilerRecorder indexBufferToInFrameCountRecorder;
    private ProfilerRecorder indexBufferToInFrameBytesRecorder;
    private ProfilerRecorder shadowCastersCountRecorder;
    #endregion

    #region 视觉元素
    private VisualElement mainContent;
    private ScrollView mainInfoScrollView;
    private ScrollView dynamicBatchingInfoScrollView;
    private ScrollView staticBatchingInfoScrollView;
    private ScrollView otherInfoScrollView;
    //主要信息区域------------------------------------------------------------
    private Label fps_Text;                         //帧率
    private Label drawCalls_Text;                   //绘制调用次数
    private Label batches_Text;                     //批次
    private Label triangles_Text;                   //三角形数量
    private Label vertices_Text;                    //顶点数量
    private Label setPassCalls_Text;                //SetPass调用次数
    //动态批处理信息区域--------------------------------------------------------
    private Label dynamicBatchedDrawCalls_Text;     //动态批次的绘制调用数 
    private Label dynamicBatchedBatches_Text;       //动态批次数
    private Label dynamicBatchedTriangles_Text;     //三角形数
    private Label dynamicBatchedVertices_Text;      //顶点数
    private Label dynamicBatchedTime_Text;          //SetPass调用次数
    //静态批处理信息区域--------------------------------------------------------
    private Label staticBatchedDrawCalls_Text;      //静态批次的绘制调用数
    private Label staticBatchedBatches_Text;        //静态批次数
    private Label staticBatchedTriangles_Text;      //三角形数
    private Label staticBatchedVertices_Text;       //顶点数
    //其它信息区域-------------------------------------------------------------
    //GPU实例化
    private Label instancingDrawCalls_Text;         //实例化批次的绘制调用数
    private Label instancingBatches_Text;           //实例化批次数
    private Label instancingTriangles_Text;         //三角形数
    private Label instancingVertices_Text;          //顶点数
    //Other
    private Label usedTexturesCount_Text;           //纹理使用数量
    private Label usedTexturesBytes_Text;           //纹理使用内存量
    private Label renderTexturesCount_Text;         //渲染材质数量
    private Label renderTexturesBytes_Text;         //渲染材质内存量
    private Label renderTexturesChangesCount_Text;  //帧期间将一个或多个 RenderTextures 设置为渲染目标的次数
    private Label usedBuffersCount_Text;            //CPU缓冲区和内存的总数
    private Label usedBuffersBytes_Text;            //CPU缓冲区和内存
    private Label vertexBufferToInFrameCount_Text;  //CPU在帧中上传到GPU的几何体数量(顶点/法线/texcoord数据)
    private Label vertexBufferToInFrameBytes_Text;  //CPU在帧中上传到GPU的几何体内存量
    private Label indexBufferToInFrameCount_Text;   //CPU在帧中上传到GPU的几何体数量(三角形索引数据)
    private Label indexBufferToInFrameBytes_Text;   //CPU在帧中上传到GPU的几何体内存量
    private Label shadowCastersCount_Text;          //投射阴影的游戏对象的数量
    //------------------------------------------------------------

    #endregion

    //初始化状态信息界面
    public void Init(VisualElement visual)
    {
        mainContent = new VisualElement();
        mainContent.styleSheets.Add(Resources.Load<StyleSheet>("USS/StatisticsView"));
        mainContent.AddToClassList("MainContent");
        //初始化MainInfo性能记录器
        InitMainInfoProfilerRecorder();
        //初始化动态批处理信息性能记录器
        InitDynamicBatchingInfoProfilerRecorder();
        //初始化静态批处理信息性能记录器
        InitStaticBatchingInfoProfilerRecorder();
        //初始化其它信息性能记录器
        InitOtherInfoProfilerRecorder();
        visual.Add(mainContent);
        //创建Statistics界面视图
        CreateStatisticsView();
    }

    //初始化Main信息性能记录器
    private void InitMainInfoProfilerRecorder()
    {
        setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
    }

    //初始化动态批处理信息性能记录器
    private void InitDynamicBatchingInfoProfilerRecorder()
    {
        dynamicBatchedDrawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Dynamic Batched Draw Calls Count");
        dynamicBatchedBatchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Dynamic Batches Count");
        dynamicBatchedTrianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Dynamic Batched Triangles Count");
        dynamicBatchedVerticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Dynamic Batched Vertices Count");
        dynamicBatchedTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Dynamic Batching Time");
    }

    //初始化静态批处理信息性能记录器
    private void InitStaticBatchingInfoProfilerRecorder()
    {
        staticBatchedDrawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Static Batched Draw Calls Count");
        staticBatchedBatchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Static Batches Count");
        staticBatchedTrianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Static Batched Triangles Count");
        staticBatchedVerticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Static Batched Vertices Count");
    }
    //初始化其它信息性能记录器
    private void InitOtherInfoProfilerRecorder()
    {
        //GPU实例化
        instancingDrawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Instanced Batched Draw Calls Count");
        instancingBatchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Instanced Batches Count");
        instancingTrianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Instanced Batched Triangles Count");
        instancingVerticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Instanced Batched Vertices Count");
        //其它
        usedTexturesCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Used Textures Count");
        usedTexturesBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Used Textures Bytes");
        renderTexturesCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Textures Count");
        renderTexturesBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Textures Bytes");
        renderTexturesChangesCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Textures Changes Count");
        usedBuffersCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Used Buffers Count");
        usedBuffersBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Used Buffers Bytes");
        vertexBufferToInFrameCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertex Buffer Upload In Frame Count");
        vertexBufferToInFrameBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertex Buffer Upload In Frame Bytes");
        indexBufferToInFrameCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Index Buffer Upload In Frame Count");
        indexBufferToInFrameBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Index Buffer Upload In Frame Bytes");
        shadowCastersCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
    }

    //创建Statistics界面视图
    private void CreateStatisticsView()
    {
        //创建主要信息区域
        CreateMainInfoArea();
        //创建动态批处理信息区域
        CreateDynamicBatchingInfoArea();
        //创建静态批处理信息区域
        CreateStaticBatchingInfoArea();
        //创建其它信息区域
        CreateOtherInfoArea();
    }

    //创建主要信息区域
    private void CreateMainInfoArea()
    {
        //创建主要信息区域
        CreateInfoViewArea(mainContent, out VisualElement mainInfoArea);
        //创建标题
        CreateInfoTitle(mainInfoArea, "MainInfoicon", "Main Info");
        CreateInfoScrollView(mainInfoArea, out mainInfoScrollView);
        //添加项
        CreateStatisticsItem(mainInfoScrollView, ref mainItemCounter, "StatisticsItem", out fps_Text);
        CreateStatisticsItem(mainInfoScrollView, ref mainItemCounter, "StatisticsItem", out drawCalls_Text);
        CreateStatisticsItem(mainInfoScrollView, ref mainItemCounter, "StatisticsItem", out setPassCalls_Text);
        CreateStatisticsItem(mainInfoScrollView, ref mainItemCounter, "StatisticsItem", out batches_Text);
        CreateStatisticsItem(mainInfoScrollView, ref mainItemCounter, "StatisticsItem", out triangles_Text);
        CreateStatisticsItem(mainInfoScrollView, ref mainItemCounter, "StatisticsItem", out vertices_Text);
    }

    //创建动态批处理信息区域
    private void CreateDynamicBatchingInfoArea()
    {
        //创建Dynamic Batching信息区域
        CreateInfoViewArea(mainContent, out VisualElement dynamicBatchingArea);
        //创建标题
        CreateInfoTitle(dynamicBatchingArea, "BatchedIcon", "Dynamic Batching");
        CreateInfoScrollView(dynamicBatchingArea, out dynamicBatchingInfoScrollView);
        //添加项
        CreateStatisticsItem(dynamicBatchingInfoScrollView, ref dynamicBatchingItemCounter, "StatisticsItem", out dynamicBatchedDrawCalls_Text);
        CreateStatisticsItem(dynamicBatchingInfoScrollView, ref dynamicBatchingItemCounter, "StatisticsItem", out dynamicBatchedBatches_Text);
        CreateStatisticsItem(dynamicBatchingInfoScrollView, ref dynamicBatchingItemCounter, "StatisticsItem", out dynamicBatchedTriangles_Text);
        CreateStatisticsItem(dynamicBatchingInfoScrollView, ref dynamicBatchingItemCounter, "StatisticsItem", out dynamicBatchedVertices_Text);
        CreateStatisticsItem(dynamicBatchingInfoScrollView, ref dynamicBatchingItemCounter, "StatisticsItem", out dynamicBatchedTime_Text);
    }

    //创建静态批处理信息区域
    private void CreateStaticBatchingInfoArea()
    {
        //创建Static Batching信息区域
        CreateInfoViewArea(mainContent, out VisualElement staticBatchingArea);
        //创建标题
        CreateInfoTitle(staticBatchingArea, "BatchedIcon", "Static Batching");
        CreateInfoScrollView(staticBatchingArea, out staticBatchingInfoScrollView);
        //添加项
        CreateStatisticsItem(staticBatchingInfoScrollView, ref staticBatchingItemCounter, "StatisticsItem", out staticBatchedDrawCalls_Text);
        CreateStatisticsItem(staticBatchingInfoScrollView, ref staticBatchingItemCounter, "StatisticsItem", out staticBatchedBatches_Text);
        CreateStatisticsItem(staticBatchingInfoScrollView, ref staticBatchingItemCounter, "StatisticsItem", out staticBatchedTriangles_Text);
        CreateStatisticsItem(staticBatchingInfoScrollView, ref staticBatchingItemCounter, "StatisticsItem", out staticBatchedVertices_Text);
    }

    //创建其它信息区域
    private void CreateOtherInfoArea()
    {
        //创建Other信息区域
        CreateInfoViewArea(mainContent, out VisualElement otherArea);
        //创建标题
        CreateInfoTitle(otherArea, "OtherInfoicon", "Other Info");
        CreateInfoScrollView(otherArea, out otherInfoScrollView);
        //添加项
        //GPU实例化
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out instancingDrawCalls_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out instancingBatches_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out instancingTriangles_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out instancingVertices_Text);
        //Other
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out usedTexturesCount_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out usedTexturesBytes_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out renderTexturesCount_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out renderTexturesBytes_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out renderTexturesChangesCount_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out usedBuffersCount_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out usedBuffersBytes_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out vertexBufferToInFrameCount_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out vertexBufferToInFrameBytes_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out indexBufferToInFrameCount_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out indexBufferToInFrameBytes_Text);
        CreateStatisticsItem(otherInfoScrollView, ref otherItemCounter, "StatisticsItem", out shadowCastersCount_Text);
    }

    //刷新State信息
    public void UpdateStatisticsInfoView()
    {
        //更新帧率
        UpdateFPS();
        //更新性能数据
        UpdateMainInfo();
        //更新动态批处理信息
        UpdateDynamicBatchingInfo();
        //更新静态批处理信息
        UpdateStaticBatchingInfo();
        //更新其它信息
        UpdateOtherInfo();
    }

    //更新帧率
    private void UpdateFPS()
    {
        // 计算帧率
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        if (fps_Text != null) fps_Text.text = $"FPS:<color=orange>{fps.ToString("F1")}</color>";
    }

    //更新主信息
    private void UpdateMainInfo()
    {
        CreateProfilerRecorder(setPassCallsRecorder, "SetPass Calls", ref setPassCalls_Text);
        CreateProfilerRecorder(drawCallsRecorder, "Draw Calls", ref drawCalls_Text);
        CreateProfilerRecorder(verticesRecorder, "Vertices", ref vertices_Text);
        CreateProfilerRecorder(batchesRecorder, "Batches", ref batches_Text);
        CreateProfilerRecorder(trianglesRecorder, "Triangles", ref triangles_Text);
    }

    //更新动态批处理信息
    private void UpdateDynamicBatchingInfo()
    {
        CreateProfilerRecorder(dynamicBatchedDrawCallsRecorder, "Dynamic DrawCalls", ref dynamicBatchedDrawCalls_Text);
        CreateProfilerRecorder(dynamicBatchedBatchesRecorder, "Dynamic Batches", ref dynamicBatchedBatches_Text);
        CreateProfilerRecorder(dynamicBatchedTrianglesRecorder, "Dynamic Triangles", ref dynamicBatchedTriangles_Text);
        CreateProfilerRecorder(dynamicBatchedVerticesRecorder, "Dynamic Vertices", ref dynamicBatchedVertices_Text);
        CreateProfilerRecorder(dynamicBatchedTimeRecorder, "Dynamic BatchedTime ", ref dynamicBatchedTime_Text);
    }

    //更新静态批处理信息
    private void UpdateStaticBatchingInfo()
    {
        CreateProfilerRecorder(staticBatchedDrawCallsRecorder, "Static Batched Draw Calls", ref staticBatchedDrawCalls_Text);
        CreateProfilerRecorder(staticBatchedBatchesRecorder, "Static Batches", ref staticBatchedBatches_Text);
        CreateProfilerRecorder(staticBatchedTrianglesRecorder, "Static Batched Triangles", ref staticBatchedTriangles_Text);
        CreateProfilerRecorder(staticBatchedVerticesRecorder, "Static Batched Vertices", ref staticBatchedVertices_Text);
    }

    //更新其它信息
    private void UpdateOtherInfo()
    {
        //GPU实例化
        CreateProfilerRecorder(instancingDrawCallsRecorder, "Instanced Batched Draw Calls", ref instancingDrawCalls_Text);
        CreateProfilerRecorder(instancingBatchesRecorder, "Instanced Batches", ref instancingBatches_Text);
        CreateProfilerRecorder(instancingTrianglesRecorder, "Instanced Batched Triangles", ref instancingTriangles_Text);
        CreateProfilerRecorder(instancingVerticesRecorder, "Instanced Batched Vertices", ref instancingVertices_Text);
        //Other
        CreateProfilerRecorder(usedTexturesCountRecorder, "Used Textures Count", ref usedTexturesCount_Text);
        CreateProfilerRecorder(usedTexturesBytesRecorder, "Used Textures Bytes", ref usedTexturesBytes_Text);
        CreateProfilerRecorder(renderTexturesCountRecorder, "Render Textures Count", ref renderTexturesCount_Text);
        CreateProfilerRecorder(renderTexturesBytesRecorder, "Render Textures Bytes", ref renderTexturesBytes_Text);
        CreateProfilerRecorder(renderTexturesChangesCountRecorder, "Render Textures Changes Count", ref renderTexturesChangesCount_Text);
        CreateProfilerRecorder(usedBuffersCountRecorder, "Used Buffers Count", ref usedBuffersCount_Text);
        CreateProfilerRecorder(usedBuffersBytesRecorder, "Used Buffers Bytes", ref usedBuffersBytes_Text);
        CreateProfilerRecorder(vertexBufferToInFrameCountRecorder, "Vertex Buffer To In Frame Count", ref vertexBufferToInFrameCount_Text);
        CreateProfilerRecorder(vertexBufferToInFrameBytesRecorder, "Vertex Buffer To In Frame Bytes", ref vertexBufferToInFrameBytes_Text);
        CreateProfilerRecorder(indexBufferToInFrameCountRecorder, "Index Buffer To In Frame Count", ref indexBufferToInFrameCount_Text);
        CreateProfilerRecorder(indexBufferToInFrameBytesRecorder, "Index Buffer To In Frame Bytes", ref indexBufferToInFrameBytes_Text);
        CreateProfilerRecorder(shadowCastersCountRecorder, "Shadow Casters Count", ref shadowCastersCount_Text);
    }

    //创建信息记录器
    private void CreateProfilerRecorder(ProfilerRecorder profilerRecorder, string title, ref Label text)
    {
        if (profilerRecorder.Valid && text != null) text.text = $"{title}: <color=yellow>{profilerRecorder.LastValue}</color>";
    }

    //创建信息区域 
    private void CreateInfoViewArea(VisualElement visual, out VisualElement infoViewArea)
    {
        infoViewArea = new VisualElement();
        infoViewArea.AddToClassList("StatisticsInfoViewArea");
        visual.Add(infoViewArea);
    }

    //创建信息区域滚动视图
    private void CreateInfoScrollView(VisualElement visual, out ScrollView scrollView)
    {
        scrollView = new ScrollView();
        scrollView.AddToClassList("StatisticsInfoScrollView");
        visual.Add(scrollView);
    }

    //创建信息标题区域
    private void CreateInfoTitle(VisualElement visual, string iconPath, string title)
    {
        Label titleText = new Label(title);
        titleText.AddToClassList("StatisticsInfoTitle");
        //Icon
        Image icon = new Image();
        icon.AddToClassList("StatisticsInfoTitle-Icon");
        icon.style.backgroundImage = Resources.Load<Texture2D>($"Icon/{iconPath}");
        titleText.Add(icon);

        visual.Add(titleText);
    }

    //创建状态信息项
    private void CreateStatisticsItem(VisualElement visual, ref int counter, string iconPath, out Label text)
    {
        VisualElement statusItem = new VisualElement();
        statusItem.AddToClassList("StatisticsItem");
        // 添加奇偶行交替样式
        if (counter % 2 == 0) statusItem.AddToClassList("StatisticsItemColor-1");
        else statusItem.AddToClassList("StatisticsItemColor-2");
        counter++;
        //Icon
        Image icon = new Image();
        icon.AddToClassList("StatisticsItem-Icon");
        icon.style.backgroundImage = Resources.Load<Texture2D>($"Icon/{iconPath}");
        statusItem.Add(icon);
        //Text
        text = new Label();
        text.AddToClassList("StatisticsItem-Text");
        statusItem.Add(text);

        visual.Add(statusItem);
    }

}
