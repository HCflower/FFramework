using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PrefabPlacerTool : EditorWindow
{
    [MenuItem("Tools/预制体放置工具")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPlacerTool>("预制体放置工具");
    }

    private enum PlacementMode
    {
        SinglePlacement = 0,            // 单个放置
        StraightLineArrangement = 1,    // 直线排列
        RangePlacement = 2              // 圆形范围放置
    }

    private enum DistributionType
    {
        UniformDistribution = 0,        // 均匀分布
        RandomDistribution = 1          // 随机分布
    }

    private enum ObjectType
    {
        Prefab = 0,                     // 预制体
        Mesh = 1                        // 网格    
    }

    private PlacementMode placementMode = PlacementMode.SinglePlacement;
    private DistributionType distributionType = DistributionType.UniformDistribution;
    private ObjectType objectType = ObjectType.Prefab;
    private GameObject prefabToPlace;
    private Mesh meshToPlace;
    private Material meshMaterial;
    private int placementCount = 5;
    private float placementRadius = 5f;
    private float lineSpacing = 2f;
    private float lineRandomOffset = 0.5f;

    private bool randomRotation = false;
    private bool alignToSurface = true;
    private LayerMask surfaceLayer = 1;
    private bool showPreview = true;

    // 放置状态
    private bool isInPlacementMode = false;
    private float placementRotation = 0f;
    private Vector3 lastMousePosition;
    private List<GameObject> placedObjects = new List<GameObject>();
    private Vector2 scrollPosition;

    // 随机种子
    private int randomSeed = 0;
    private System.Random random;
    private System.Random rotationRandom; // 新增：专门用于旋转的随机数生成器

    // 缓存的随机位置，避免预览时重新计算
    private Vector3[] cachedRandomPositions;
    private bool needRecalculateRandomPositions = true;

    // GUI折叠状态
    private bool showAdvancedOptions = true;
    private bool showRandomSettings = false;
    private bool showDirectionSettings = true;

    private void OnGUI()
    {
        DrawHeader();
        DrawSettings();
        DrawPlacementControls();
        DrawHistory();
    }

    private void DrawHeader()
    {
        // 用HelpBox包裹标题
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("预制体摆放工具", titleStyle, GUILayout.Height(32));
            EditorGUILayout.HelpBox("在场景视图中点击放置 \nCtrl+滚轮:旋转角度 | Shift+滚轮:随机种子 | Ctrl+Alt+滚轮:范围缩放", MessageType.Info);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(3);
    }

    private void DrawSettings()
    {
        EditorGUILayout.BeginVertical("helpbox");
        {
            EditorGUILayout.LabelField("基础设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            // 对象类型选择
            objectType = (ObjectType)EditorGUILayout.EnumPopup("对象类型", objectType);

            if (objectType == ObjectType.Prefab)
            {
                prefabToPlace = (GameObject)EditorGUILayout.ObjectField("预制体", prefabToPlace, typeof(GameObject), false);
            }
            else
            {
                meshToPlace = (Mesh)EditorGUILayout.ObjectField("网格", meshToPlace, typeof(Mesh), false);
                meshMaterial = (Material)EditorGUILayout.ObjectField("材质", meshMaterial, typeof(Material), false);
            }

            EditorGUILayout.Space(5);

            // 放置模式选择
            placementMode = (PlacementMode)EditorGUILayout.EnumPopup("放置模式", placementMode);

            // 根据不同模式显示不同参数
            EditorGUILayout.Space(3);
            DrawModeSpecificSettings();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(3);

        // 高级选项(可折叠)
        DrawAdvancedOptions();

        // 随机设置(可折叠)
        if (placementMode != PlacementMode.SinglePlacement)
        {
            DrawRandomSettings();
        }

        // 方向设置(可折叠)
        DrawDirectionSettings();
    }

    private void DrawModeSpecificSettings()
    {
        EditorGUILayout.BeginVertical("helpbox");

        switch (placementMode)
        {
            case PlacementMode.SinglePlacement:
                EditorGUILayout.LabelField("在场景中点击放置单个对象", EditorStyles.miniLabel);
                break;

            case PlacementMode.StraightLineArrangement:
                EditorGUI.BeginChangeCheck();
                placementCount = EditorGUILayout.IntSlider("放置数量", placementCount, 2, 50);
                lineSpacing = EditorGUILayout.Slider("间隔距离", lineSpacing, 0.5f, 10f);
                lineRandomOffset = EditorGUILayout.Slider("随机偏移", lineRandomOffset, 0f, 5f);
                if (EditorGUI.EndChangeCheck())
                {
                    needRecalculateRandomPositions = true;
                }

                if (lineRandomOffset > 0)
                {
                    EditorGUILayout.LabelField("对象将沿直线排列并带有随机偏移", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("对象将沿直线均匀排列", EditorStyles.miniLabel);
                }
                break;

            case PlacementMode.RangePlacement:
                EditorGUI.BeginChangeCheck();
                placementCount = EditorGUILayout.IntSlider("放置数量", placementCount, 2, 100);
                placementRadius = EditorGUILayout.Slider("圆形半径", placementRadius, 1f, 50f);
                distributionType = (DistributionType)EditorGUILayout.EnumPopup("分布类型", distributionType);
                if (EditorGUI.EndChangeCheck())
                {
                    needRecalculateRandomPositions = true;
                }

                if (distributionType == DistributionType.UniformDistribution)
                {
                    EditorGUILayout.LabelField("对象将在圆形范围内均匀分布", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("对象将在圆形范围内随机分布", EditorStyles.miniLabel);
                }
                break;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAdvancedOptions()
    {
        EditorGUILayout.BeginVertical("helpbox");
        {
            EditorGUILayout.BeginHorizontal();
            // 自定义左对齐按钮样式
            GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
            leftButtonStyle.alignment = TextAnchor.MiddleLeft;
            leftButtonStyle.fontSize = 12;
            leftButtonStyle.fontStyle = FontStyle.Bold;

            string advBtnText = showAdvancedOptions ? "▼ 高级选项" : "▶ 高级选项";
            if (GUILayout.Button(advBtnText, leftButtonStyle, GUILayout.Height(22), GUILayout.ExpandWidth(true)))
            {
                showAdvancedOptions = !showAdvancedOptions;
            }
            EditorGUILayout.EndHorizontal();

            if (showAdvancedOptions)
            {
                // 左对齐标签样式
                GUIStyle leftLabel = new GUIStyle(EditorStyles.label);
                leftLabel.alignment = TextAnchor.MiddleLeft;

                randomRotation = EditorGUILayout.ToggleLeft("随机旋转", randomRotation, leftLabel);
                alignToSurface = EditorGUILayout.ToggleLeft("贴合表面", alignToSurface, leftLabel);
                showPreview = EditorGUILayout.ToggleLeft("显示预览", showPreview, leftLabel);

                if (alignToSurface)
                {
                    surfaceLayer = LayerMaskField("表面图层", surfaceLayer);
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawRandomSettings()
    {
        EditorGUILayout.BeginVertical("helpbox");
        {
            EditorGUILayout.BeginHorizontal();
            // 自定义左对齐按钮样式
            GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
            leftButtonStyle.alignment = TextAnchor.MiddleLeft;
            leftButtonStyle.fontSize = 12;
            leftButtonStyle.fontStyle = FontStyle.Bold;

            string randomBtnText = showRandomSettings ? "▼ 随机设置" : "▶ 随机设置";
            if (GUILayout.Button(randomBtnText, leftButtonStyle, GUILayout.Height(22), GUILayout.ExpandWidth(true)))
            {
                showRandomSettings = !showRandomSettings;
            }
            EditorGUILayout.EndHorizontal();

            if (showRandomSettings)
            {
                EditorGUI.BeginChangeCheck();
                randomSeed = EditorGUILayout.IntField("随机种子", randomSeed);
                if (EditorGUI.EndChangeCheck())
                {
                    needRecalculateRandomPositions = true;
                }

                if (GUILayout.Button("重新生成随机数", GUILayout.Height(25)))
                {
                    randomSeed = Random.Range(0, 10000);
                    InitializeRandom();
                    needRecalculateRandomPositions = true;
                }
                EditorGUILayout.HelpBox("相同种子会产生相同的随机分布", MessageType.None);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawDirectionSettings()
    {
        EditorGUILayout.BeginVertical("helpbox");
        {
            EditorGUILayout.BeginHorizontal();
            // 自定义左对齐按钮样式
            GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
            leftButtonStyle.alignment = TextAnchor.MiddleLeft;
            leftButtonStyle.fontSize = 12;
            leftButtonStyle.fontStyle = FontStyle.Bold;

            string dirBtnText = showDirectionSettings ? "▼ 方向设置" : "▶ 方向设置";
            if (GUILayout.Button(dirBtnText, leftButtonStyle, GUILayout.Height(22), GUILayout.ExpandWidth(true)))
            {
                showDirectionSettings = !showDirectionSettings;
            }
            EditorGUILayout.EndHorizontal();

            if (showDirectionSettings)
            {
                EditorGUILayout.BeginVertical("helpbox");
                {
                    placementRotation = EditorGUILayout.Slider("方向角度", placementRotation, 0f, 360f);
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPlacementControls()
    {
        EditorGUILayout.BeginVertical("helpbox");
        {
            EditorGUILayout.LabelField("放置控制", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            {
                GUI.backgroundColor = isInPlacementMode ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);

                if (!isInPlacementMode)
                {
                    if (GUILayout.Button("开始放置", GUILayout.Height(26)))
                    {
                        StartPlacementMode();
                    }
                }
                else
                {
                    if (GUILayout.Button("取消放置", GUILayout.Height(26)))
                    {
                        CancelPlacementMode();
                    }
                }

                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("清除所有", GUILayout.Height(26), GUILayout.Width(100)))
                {
                    ClearAllPlacedObjects();
                }
            }
            EditorGUILayout.EndHorizontal();

            // 放置信息面板
            if ((prefabToPlace != null && objectType == ObjectType.Prefab) ||
                (meshToPlace != null && objectType == ObjectType.Mesh))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginVertical("helpbox");
                {
                    EditorGUILayout.LabelField("当前配置", EditorStyles.miniBoldLabel);

                    string modeText = GetModeDisplayText();
                    EditorGUILayout.LabelField($"模式: {modeText}", EditorStyles.miniLabel);

                    if (placementMode != PlacementMode.SinglePlacement)
                    {
                        EditorGUILayout.LabelField($"数量: {placementCount}", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.LabelField($"方向: {placementRotation:0.0}°", EditorStyles.miniLabel);

                    string objectName = objectType == ObjectType.Prefab ?
                        (prefabToPlace != null ? prefabToPlace.name : "无") :
                        (meshToPlace != null ? meshToPlace.name : "无");
                    EditorGUILayout.LabelField($"对象: {objectName}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private string GetModeDisplayText()
    {
        switch (placementMode)
        {
            case PlacementMode.SinglePlacement:
                return "单个放置";
            case PlacementMode.StraightLineArrangement:
                return $"直线排列 (间隔: {lineSpacing:0.0})";
            case PlacementMode.RangePlacement:
                return $"圆形范围 ({distributionType}) (半径: {placementRadius:0.0})";
            default:
                return "";
        }
    }

    private bool showHistory = true; // 在类字段区添加

    private void DrawHistory()
    {
        // 折叠按钮
        EditorGUILayout.BeginHorizontal();
        // 自定义左对齐按钮样式
        GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
        leftButtonStyle.alignment = TextAnchor.MiddleLeft;
        leftButtonStyle.fontSize = 12;
        leftButtonStyle.fontStyle = FontStyle.Bold;
        string historyBtnText = showHistory ? "▼ 放置历史" : "▶ 放置历史";
        if (GUILayout.Button(historyBtnText, leftButtonStyle, GUILayout.Height(22), GUILayout.ExpandWidth(true)))
        {
            showHistory = !showHistory;
        }
        EditorGUILayout.EndHorizontal();

        if (!showHistory || placedObjects.Count == 0) return;

        EditorGUILayout.Space(3);
        EditorGUILayout.BeginVertical("box");
        {
            EditorGUILayout.LabelField($"放置历史 ({placedObjects.Count} 个对象)", EditorStyles.boldLabel);
            EditorGUILayout.Space(1);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(180));
            {
                for (int i = placedObjects.Count - 1; i >= 0; i--)
                {
                    if (placedObjects[i] == null)
                    {
                        placedObjects.RemoveAt(i);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.ObjectField(placedObjects[i], typeof(GameObject), true);

                        if (GUILayout.Button("定位", GUILayout.Width(50)))
                        {
                            EditorGUIUtility.PingObject(placedObjects[i]);
                            Selection.activeGameObject = placedObjects[i];
                        }

                        if (GUILayout.Button("删除", GUILayout.Width(50)))
                        {
                            DestroyImmediate(placedObjects[i]);
                            placedObjects.RemoveAt(i);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("清除历史", GUILayout.Height(25)))
            {
                ClearAllPlacedObjects();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawRangeIndicator(SceneView sceneView)
    {
        Vector3 placementPosition = GetMouseWorldPosition(lastMousePosition);
        Quaternion rotation = Quaternion.Euler(0, placementRotation, 0);

        switch (placementMode)
        {
            case PlacementMode.SinglePlacement:
                DrawSinglePlacementIndicator(placementPosition, rotation);
                break;
            case PlacementMode.StraightLineArrangement:
                DrawLinearRange(placementPosition, rotation);
                break;
            case PlacementMode.RangePlacement:
                DrawCircularArea(placementPosition, rotation);
                break;
        }

        Handles.color = Color.red;
        Handles.SphereHandleCap(0, placementPosition, Quaternion.identity, 0.1f, EventType.Repaint);

        DrawDirectionIndicator(placementPosition, rotation);

        if (showPreview && placementMode != PlacementMode.SinglePlacement)
        {
            DrawPlacementPreviews(placementPosition, rotation);
        }

        // 显示信息标签
        DrawInfoLabel(placementPosition);
    }

    private void DrawSinglePlacementIndicator(Vector3 center, Quaternion rotation)
    {
        Handles.color = new Color(0, 1, 0, 0.5f);
        Handles.DrawSolidDisc(center, Vector3.up, 0.25f);
        Handles.color = Color.green;
        Handles.DrawWireDisc(center, Vector3.up, 0.25f);
    }

    private void DrawInfoLabel(Vector3 position)
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.fontSize = 12;

        string infoText = GetModeDisplayText();
        if (placementMode != PlacementMode.SinglePlacement)
        {
            infoText += $"\n数量: {placementCount}";
        }
        infoText += $"\n方向: {placementRotation:0.0}°";

        Handles.Label(position + Vector3.up * 2f, infoText, labelStyle);
    }

    private void DrawLinearRange(Vector3 center, Quaternion rotation)
    {
        Handles.color = Color.blue;

        if (lineRandomOffset > 0)
        {
            Handles.color = new Color(1, 0.5f, 0, 0.2f);
            for (int i = 0; i < placementCount; i++)
            {
                Vector3 point = center + rotation * Vector3.right * i * lineSpacing;
                Handles.DrawSolidDisc(point, Vector3.up, lineRandomOffset);
            }
        }
    }

    private void DrawDirectionIndicator(Vector3 center, Quaternion rotation)
    {
        Vector3 direction = rotation * Vector3.right;
        Vector3 arrowEnd = center + direction * 1f;

        Handles.color = Color.yellow;
        Handles.ArrowHandleCap(0, center, Quaternion.LookRotation(direction), 1f, EventType.Repaint);

        Handles.color = new Color(1, 1, 0, 0.5f);
        Handles.DrawDottedLine(center, arrowEnd, 5f);
    }

    private void DrawPlacementPreviews(Vector3 center, Quaternion rotation)
    {
        Vector3[] positions = CalculatePlacementPositions(center, rotation);

        Handles.color = new Color(1, 0, 1, 0.8f);
        foreach (Vector3 pos in positions)
        {
            Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.15f, EventType.Repaint);
        }

        if ((objectType == ObjectType.Prefab && prefabToPlace != null) ||
            (objectType == ObjectType.Mesh && meshToPlace != null))
        {
            Handles.color = new Color(1, 1, 1, 0.3f);
            foreach (Vector3 pos in positions)
            {
                Handles.CubeHandleCap(0, pos, rotation, 0.3f, EventType.Repaint);
            }
        }
    }

    private Vector3 GetMouseWorldPosition(Vector2 mousePosition)
    {
        Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, surfaceLayer) && alignToSurface)
        {
            return hit.point;
        }
        else
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (groundPlane.Raycast(worldRay, out distance))
            {
                return worldRay.GetPoint(distance);
            }
        }

        return Vector3.zero;
    }

    private Vector3 AdjustPositionToSurface(Vector3 position)
    {
        if (alignToSurface)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, Mathf.Infinity, surfaceLayer))
            {
                return hit.point;
            }
        }
        return position;
    }

    private Quaternion[] CalculatePlacementRotations(Quaternion baseRotation)
    {
        List<Quaternion> rotations = new List<Quaternion>();
        int count = placementMode == PlacementMode.SinglePlacement ? 1 : placementCount;

        // 为旋转创建独立的随机数生成器
        if (randomRotation && rotationRandom == null)
        {
            rotationRandom = new System.Random(randomSeed + 1000); // 使用不同的种子偏移
        }

        for (int i = 0; i < count; i++)
        {
            Quaternion rotation = baseRotation;
            if (randomRotation && rotationRandom != null)
            {
                float randomAngle = (float)(rotationRandom.NextDouble() * 360);
                rotation = baseRotation * Quaternion.Euler(0, randomAngle, 0);
            }
            rotations.Add(rotation);
        }

        return rotations.ToArray();
    }

    private void PlaceObjectsAtMousePosition(SceneView sceneView)
    {
        Vector3 placementPosition = GetMouseWorldPosition(lastMousePosition);
        Quaternion baseRotation = Quaternion.Euler(0, placementRotation, 0);
        Vector3[] positions = CalculatePlacementPositions(placementPosition, baseRotation);
        Quaternion[] rotations = CalculatePlacementRotations(baseRotation);

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject newObject = null;

            if (objectType == ObjectType.Prefab && prefabToPlace != null)
            {
                newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);
            }
            else if (objectType == ObjectType.Mesh && meshToPlace != null)
            {
                newObject = new GameObject($"PlacedMesh_{meshToPlace.name}_{placedObjects.Count + i}");
                MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();

                meshFilter.mesh = meshToPlace;
                if (meshMaterial != null)
                {
                    meshRenderer.material = meshMaterial;
                }
                else
                {
                    meshRenderer.material = new Material(Shader.Find("Standard"));
                }
            }

            if (newObject != null)
            {
                newObject.transform.position = positions[i];
                newObject.transform.rotation = rotations[i];

                placedObjects.Add(newObject);
                Undo.RegisterCreatedObjectUndo(newObject, "Place Object");
            }
        }

        Debug.Log($"成功放置 {positions.Length} 个对象，模式: {placementMode}，方向: {placementRotation:0}°");
    }

    private void ClearAllPlacedObjects()
    {
        if (placedObjects.Count == 0) return;

        if (EditorUtility.DisplayDialog("确认清除", $"确定要删除所有 {placedObjects.Count} 个已放置的对象吗?", "确定", "取消"))
        {
            foreach (GameObject obj in placedObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            placedObjects.Clear();
            Debug.Log("已清除所有放置的对象");
        }
    }

    private LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        List<string> layers = new List<string>();
        List<int> layerNumbers = new List<int>();

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (layerName != "")
            {
                layers.Add(layerName);
                layerNumbers.Add(i);
            }
        }

        int maskWithoutEmpty = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                maskWithoutEmpty |= (1 << i);
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
        int mask = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) > 0)
                mask |= (1 << layerNumbers[i]);
        }

        return mask;
    }

    private void DrawCircularArea(Vector3 center, Quaternion rotation)
    {
        Handles.color = new Color(0, 0.5f, 1, 0.3f);
        Handles.DrawSolidDisc(center, Vector3.up, placementRadius);
        Handles.color = new Color(0, 0.5f, 1, 1f);
        Handles.DrawWireDisc(center, Vector3.up, placementRadius);
    }

    private Vector3[] CalculatePlacementPositions(Vector3 center, Quaternion rotation)
    {
        List<Vector3> positions = new List<Vector3>();

        switch (placementMode)
        {
            case PlacementMode.SinglePlacement:
                positions.Add(AdjustPositionToSurface(center));
                break;

            case PlacementMode.StraightLineArrangement:
                // 确保初始化随机数生成器
                if (lineRandomOffset > 0 && random == null)
                {
                    InitializeRandom();
                }

                // 当有随机偏移时，使用缓存位置避免抖动
                if (lineRandomOffset > 0)
                {
                    if (needRecalculateRandomPositions || cachedRandomPositions == null || cachedRandomPositions.Length != placementCount)
                    {
                        RecalculateRandomPositions();
                        needRecalculateRandomPositions = false;
                    }

                    for (int i = 0; i < placementCount; i++)
                    {
                        Vector3 basePos = center + rotation * Vector3.right * i * lineSpacing;
                        Vector3 worldPos = basePos + rotation * cachedRandomPositions[i];
                        positions.Add(AdjustPositionToSurface(worldPos));
                    }
                }
                else
                {
                    // 无随机偏移时直接计算
                    for (int i = 0; i < placementCount; i++)
                    {
                        Vector3 basePos = center + rotation * Vector3.right * i * lineSpacing;
                        positions.Add(AdjustPositionToSurface(basePos));
                    }
                }
                break;

            case PlacementMode.RangePlacement:
                if (distributionType == DistributionType.UniformDistribution)
                {
                    // 圆形均匀分布 - 使用同心圆方法
                    int rings = Mathf.CeilToInt(Mathf.Sqrt(placementCount));
                    int placedCount = 0;

                    for (int ring = 0; ring < rings && placedCount < placementCount; ring++)
                    {
                        float ringRadius = (ring + 1) * placementRadius / rings;
                        int objectsInRing = ring == 0 ? 1 : Mathf.RoundToInt(6 * ring);

                        if (ring == 0)
                        {
                            // 中心点
                            positions.Add(AdjustPositionToSurface(center));
                            placedCount++;
                        }
                        else
                        {
                            for (int i = 0; i < objectsInRing && placedCount < placementCount; i++)
                            {
                                float angle = i * Mathf.PI * 2f / objectsInRing;
                                Vector3 localPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ringRadius;
                                Vector3 worldPos = center + rotation * localPos;
                                positions.Add(AdjustPositionToSurface(worldPos));
                                placedCount++;
                            }
                        }
                    }
                }
                else
                {
                    // 圆形随机分布 - 使用缓存的位置
                    if (needRecalculateRandomPositions || cachedRandomPositions == null || cachedRandomPositions.Length != placementCount)
                    {
                        RecalculateRandomPositions();
                        needRecalculateRandomPositions = false;
                    }

                    // 应用当前的中心位置和旋转到缓存的相对位置
                    for (int i = 0; i < cachedRandomPositions.Length; i++)
                    {
                        Vector3 worldPos = center + rotation * cachedRandomPositions[i];
                        positions.Add(AdjustPositionToSurface(worldPos));
                    }
                }
                break;
        }

        return positions.ToArray();
    }

    private void RecalculateRandomPositions()
    {
        cachedRandomPositions = new Vector3[placementCount];

        if (random != null)
        {
            // 为每次重新计算创建新的随机数生成器，确保使用正确的种子
            System.Random posRandom = new System.Random(randomSeed);

            if (placementMode == PlacementMode.StraightLineArrangement)
            {
                // 直线模式：生成圆形内的随机偏移
                for (int i = 0; i < placementCount; i++)
                {
                    float randomAngle = (float)(posRandom.NextDouble() * Mathf.PI * 2);
                    float randomDistance = (float)(posRandom.NextDouble() * lineRandomOffset);
                    Vector2 randomCircle = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomDistance;
                    cachedRandomPositions[i] = new Vector3(randomCircle.x, 0, randomCircle.y);
                }
            }
            else if (placementMode == PlacementMode.RangePlacement)
            {
                // 范围模式:生成圆形内的随机点
                for (int i = 0; i < placementCount; i++)
                {
                    float randomAngle = (float)(posRandom.NextDouble() * Mathf.PI * 2);
                    float randomRadius = Mathf.Sqrt((float)posRandom.NextDouble()) * placementRadius;

                    cachedRandomPositions[i] = new Vector3(
                        Mathf.Cos(randomAngle) * randomRadius,
                        0,
                        Mathf.Sin(randomAngle) * randomRadius
                    );
                }
            }
        }
    }

    private void StartPlacementMode()
    {
        if (objectType == ObjectType.Prefab && prefabToPlace == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择要放置的预制体", "确定");
            return;
        }

        if (objectType == ObjectType.Mesh && meshToPlace == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择要放置的网格", "确定");
            return;
        }

        isInPlacementMode = true;
        InitializeRandom();
        needRecalculateRandomPositions = true;
        SceneView.duringSceneGui += OnSceneGUI;
        Debug.Log("进入放置模式:在场景视图中点击放置位置,按ESC取消\nCtrl+滚轮:旋转角度 | Shift+滚轮:随机种子 | Ctrl+Alt+滚轮:范围缩放");

        SceneView.RepaintAll();
        Repaint();
    }

    private void CancelPlacementMode()
    {
        isInPlacementMode = false;
        SceneView.duringSceneGui -= OnSceneGUI;
        Debug.Log("已取消放置模式");

        SceneView.RepaintAll();
        Repaint();
    }

    private void InitializeRandom()
    {
        random = new System.Random(randomSeed);
        rotationRandom = new System.Random(randomSeed + 1000); // 旋转使用不同的种子
        needRecalculateRandomPositions = true;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // Ctrl + Alt + 鼠标滚轮 = 范围缩放 (优先级最高，先判断)
        if (e.type == EventType.ScrollWheel && e.control && e.alt)
        {
            float scaleDelta = -e.delta.y * 0.2f;

            if (placementMode == PlacementMode.StraightLineArrangement)
            {
                lineSpacing = Mathf.Clamp(lineSpacing + scaleDelta, 0.5f, 10f);
            }
            else if (placementMode == PlacementMode.RangePlacement)
            {
                placementRadius = Mathf.Clamp(placementRadius + scaleDelta, 1f, 50f);
            }

            needRecalculateRandomPositions = true;
            e.Use();
            Repaint();
            sceneView.Repaint();
            return;
        }

        // Ctrl + 鼠标滚轮 = 方向旋转
        if (e.type == EventType.ScrollWheel && e.control && !e.alt && !e.shift)
        {
            float rotationDelta = -e.delta.y * 2f;
            placementRotation += rotationDelta;
            placementRotation %= 360f;
            if (placementRotation < 0) placementRotation += 360f;

            e.Use();
            Repaint();
            sceneView.Repaint();
            return;
        }

        // Shift + 鼠标滚轮 = 改变随机种子并立即重新计算随机位置
        if (e.type == EventType.ScrollWheel && e.shift && !e.control && !e.alt)
        {
            float scrollDelta = Mathf.Abs(e.delta.y) > 0 ? e.delta.y : e.delta.x;
            if (Mathf.Abs(scrollDelta) > 0)
            {
                randomSeed += (int)(-scrollDelta * 10); // 每次滚动种子变化
                InitializeRandom();
                needRecalculateRandomPositions = true;
                Debug.Log($"随机种子:{randomSeed}, 滚轮:{scrollDelta}");
            }

            e.Use();
            Repaint();
            sceneView.Repaint();
            return;
        }

        // 鼠标移动时更新位置
        if (e.type == EventType.MouseMove)
        {
            lastMousePosition = e.mousePosition;
        }

        // 左键点击放置对象
        if (e.type == EventType.MouseDown && e.button == 0 && !e.control)
        {
            PlaceObjectsAtMousePosition(sceneView);
            e.Use();
            return;
        }

        DrawRangeIndicator(sceneView);

        if (e.type == EventType.Repaint || e.type == EventType.Layout)
        {
            sceneView.Repaint();
        }
    }
}