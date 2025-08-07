using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 扇形碰撞体
/// 用于检测扇形区域内的碰撞，支持内外圆半径、角度和厚度设置
/// </summary>
[AddComponentMenu("CustomColliders/3D/Sector Collider")]
public class SectorCollider : CustomCollider
{
    [Header("扇形参数")]
    [Tooltip("扇形内圆半径"), Min(0)] public float innerCircleRadius = 0;
    [Tooltip("扇形外圆半径"), Min(1)] public float outerCircleRadius = 1;
    [Tooltip("扇形角度"), Range(0, 360)] public float sectorAngle = 90;
    [Tooltip("扇形厚度"), Min(0.1f)] public float sectorThickness = 0.1f;

    [Header("检测设置")]
    [Tooltip("检测层级")] public LayerMask detectionLayerMask = -1;

    // 缓存的计算值，避免重复计算
    private float cachedInnerRadiusSqr;
    private float cachedOuterRadiusSqr;
    private float cachedHalfThickness;
    private float cachedHalfAngleCos;
    private bool cacheValid = false;

    // 缓存关键点数组，避免重复分配
    private Vector3[] cachedKeyPoints;
    private bool keyPointsCacheValid = false;

    // 额外的性能优化缓存
    private Vector3 cachedSectorCenter;
    private Vector3 cachedSectorForward;
    private Vector3 cachedSectorUp;
    private bool transformCacheValid = false;

    #region 扇形特有属性 - 重写基类属性以提供更明确的命名

    /// <summary>
    /// 获取扇形的实际中心点位置
    /// </summary>
    public Vector3 SectorCenter => ColliderCenter;

    /// <summary>
    /// 获取扇形的实际前向方向
    /// </summary>
    public Vector3 SectorForward => ColliderForward;

    /// <summary>
    /// 获取扇形的实际向上方向
    /// </summary>
    public Vector3 SectorUp => ColliderUp;

    /// <summary>
    /// 获取扇形的实际右向方向
    /// </summary>
    public Vector3 SectorRight => ColliderRight;

    #endregion

    #region 扇形特有设置方法 - 提供更明确的命名

    public void SetSectorCenterWorldPosition(Vector3 worldPosition) => SetColliderCenterWorldPosition(worldPosition);
    public void SetSectorCenterLocalOffset(Vector3 localOffset) => SetColliderCenterLocalOffset(localOffset);
    public void ResetSectorCenterToTransform() => ResetColliderCenterToTransform();
    public void SetSectorWorldRotation(Quaternion worldRotation) => SetColliderWorldRotation(worldRotation);
    public void SetSectorWorldRotation(Vector3 worldEulerAngles) => SetColliderWorldRotation(worldEulerAngles);
    public void SetSectorRotationLocalOffset(Vector3 localEulerOffset) => SetColliderRotationLocalOffset(localEulerOffset);
    public void SetSectorLookDirection(Vector3 worldDirection) => SetColliderLookDirection(worldDirection);
    public void SetSectorLookAtPosition(Vector3 worldPosition) => SetColliderLookAtPosition(worldPosition);
    public void ResetSectorRotationToTransform() => ResetColliderRotationToTransform();

    #endregion

    #region 缓存管理

    /// <summary>
    /// 更新缓存的计算值
    /// </summary>
    private void UpdateCache()
    {
        if (cacheValid) return;

        cachedInnerRadiusSqr = innerCircleRadius * innerCircleRadius;
        cachedOuterRadiusSqr = outerCircleRadius * outerCircleRadius;
        cachedHalfThickness = sectorThickness * 0.5f;
        cachedHalfAngleCos = Mathf.Cos(sectorAngle * 0.5f * Mathf.Deg2Rad);
        cacheValid = true;
    }

    /// <summary>
    /// 更新Transform相关的缓存
    /// </summary>
    private void UpdateTransformCache()
    {
        if (transformCacheValid) return;

        cachedSectorCenter = SectorCenter;
        cachedSectorForward = SectorForward;
        cachedSectorUp = SectorUp;
        transformCacheValid = true;
    }

    /// <summary>
    /// 使缓存失效
    /// </summary>
    private void InvalidateCache()
    {
        cacheValid = false;
        keyPointsCacheValid = false;
        transformCacheValid = false;
    }

    #endregion

    #region 实现抽象接口

    /// <summary>
    /// 检查指定位置是否在扇形范围内
    /// </summary>
    public override bool IsPointInRange(Vector3 worldPosition)
    {
        return IsPointInSector(worldPosition);
    }

    /// <summary>
    /// 检查碰撞体是否在扇形范围内
    /// </summary>
    public override bool IsColliderInRange(Collider collider)
    {
        return IsColliderInSector(collider);
    }

    /// <summary>
    /// 获取当前扇形范围内的所有碰撞体
    /// </summary>
    public override List<Collider> GetCollidersInRange()
    {
        return GetCollidersInSector();
    }

    /// <summary>
    /// 获取用于初步筛选的检测半径
    /// </summary>
    protected override float GetDetectionRadius()
    {
        return outerCircleRadius;
    }

    #endregion

    #region 扇形特有的检测方法

    /// <summary>
    /// 检查指定位置是否在扇形范围内
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <returns>是否在扇形内</returns>
    public bool IsPointInSector(Vector3 worldPosition)
    {
        return IsPointInSector(worldPosition, SectorCenter, SectorForward, SectorUp);
    }

    /// <summary>
    /// 检查指定位置是否在扇形范围内（自定义扇形参数）
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <param name="sectorCenter">扇形中心点</param>
    /// <param name="sectorForward">扇形前向方向</param>
    /// <param name="sectorUp">扇形向上方向</param>
    /// <returns>是否在扇形内</returns>
    public bool IsPointInSector(Vector3 worldPosition, Vector3 sectorCenter, Vector3 sectorForward, Vector3 sectorUp)
    {
        UpdateCache(); // 确保缓存是最新的

        Vector3 localPosition = worldPosition - sectorCenter;

        // 快速距离检查（使用缓存的平方距离值）
        float sqrDistance = localPosition.sqrMagnitude;
        if (sqrDistance < cachedInnerRadiusSqr || sqrDistance > cachedOuterRadiusSqr)
            return false;

        // 检查高度范围（厚度，使用缓存值）
        float heightOffset = Vector3.Dot(localPosition, sectorUp);
        if (Mathf.Abs(heightOffset) > cachedHalfThickness)
            return false;

        // 如果角度为360度，跳过角度检查
        if (sectorAngle >= 360f)
            return true;

        // 投影到扇形平面并检查角度范围（使用缓存的余弦值）
        Vector3 projectedPos = localPosition - heightOffset * sectorUp;

        // 对于零向量，认为在扇形内（中心点）
        if (projectedPos.sqrMagnitude < 0.0001f)
            return true;

        float dot = Vector3.Dot(sectorForward.normalized, projectedPos.normalized);
        return dot >= cachedHalfAngleCos;
    }

    /// <summary>
    /// 获取当前扇形范围内的所有碰撞体
    /// </summary>
    /// <returns>碰撞体列表</returns>
    public List<Collider> GetCollidersInSector()
    {
        tempColliderList.Clear();

        if (!enableCollisionDetection)
            return tempColliderList;

        // 使用 Physics.OverlapSphere 进行初步筛选，考虑检测层级
        Collider[] nearbyColliders = Physics.OverlapSphere(SectorCenter, outerCircleRadius, detectionLayerMask);

        foreach (var collider in nearbyColliders)
        {
            if (collider == null || collider.transform == transform) continue;

            // 使用扇形特有的检测方法检查碰撞体是否在扇形范围内
            if (IsColliderInSectorRange(collider))
            {
                tempColliderList.Add(collider);
            }
        }

        return tempColliderList;
    }

    /// <summary>
    /// 获取扇形的关键点，用于反向碰撞检测（使用缓存优化）
    /// </summary>
    /// <returns>扇形关键点数组</returns>
    private Vector3[] GetSectorKeyPoints()
    {
        if (keyPointsCacheValid && cachedKeyPoints != null)
            return cachedKeyPoints;

        UpdateCache(); // 确保基础缓存是最新的
        UpdateTransformCache(); // 确保Transform缓存是最新的

        Vector3 center = cachedSectorCenter;
        Vector3 forward = cachedSectorForward;
        Vector3 up = cachedSectorUp;
        Vector3 upOffset = up * cachedHalfThickness;

        // 使用更紧凑的计算策略
        if (sectorAngle < 360f)
        {
            // 非360度扇形，使用精简的关键点
            int pointCount = innerCircleRadius > 0 ? 6 : 4; // 减少关键点数量
            cachedKeyPoints = new Vector3[pointCount];

            int index = 0;
            // 添加扇形中心点（上下两个）
            cachedKeyPoints[index++] = center + upOffset;
            cachedKeyPoints[index++] = center - upOffset;

            // 只添加前向的外圆点
            Vector3 forwardOuter = center + forward * outerCircleRadius;
            cachedKeyPoints[index++] = forwardOuter + upOffset;
            cachedKeyPoints[index++] = forwardOuter - upOffset;

            // 如果有内圆，添加前向内圆点
            if (innerCircleRadius > 0)
            {
                Vector3 forwardInner = center + forward * innerCircleRadius;
                cachedKeyPoints[index++] = forwardInner + upOffset;
                cachedKeyPoints[index++] = forwardInner - upOffset;
            }
        }
        else
        {
            // 360度扇形，使用四个方向
            int pointCount = innerCircleRadius > 0 ? 12 : 8;
            cachedKeyPoints = new Vector3[pointCount];

            Vector3 right = Vector3.Cross(forward, up).normalized;
            Vector3[] directions = { forward, right, -forward, -right };

            int index = 0;
            foreach (var direction in directions)
            {
                if (innerCircleRadius > 0)
                {
                    Vector3 innerPoint = center + direction * innerCircleRadius;
                    cachedKeyPoints[index++] = innerPoint + upOffset;
                    cachedKeyPoints[index++] = innerPoint - upOffset;
                }
                Vector3 outerPoint = center + direction * outerCircleRadius;
                cachedKeyPoints[index++] = outerPoint + upOffset;
                cachedKeyPoints[index++] = outerPoint - upOffset;
            }
        }

        keyPointsCacheValid = true;
        return cachedKeyPoints;
    }

    /// <summary>
    /// 检查碰撞体是否在扇形范围内（优化版本）
    /// </summary>
    /// <param name="collider">目标碰撞体</param>
    /// <returns>是否在扇形内</returns>
    private bool IsColliderInSectorRange(Collider collider)
    {
        UpdateTransformCache(); // 确保Transform缓存最新

        Bounds bounds = collider.bounds;

        // 快速预检查：使用包围盒中心距离进行初步筛选
        Vector3 centerPos = bounds.center;
        float centerDistanceSqr = (centerPos - cachedSectorCenter).sqrMagnitude;

        // 如果包围盒中心距离超出外圆+包围盒扩展范围，直接排除
        float boundsExtent = bounds.size.magnitude * 0.5f;
        float maxCheckDistance = outerCircleRadius + boundsExtent;
        if (centerDistanceSqr > maxCheckDistance * maxCheckDistance)
            return false;

        // 检查碰撞体中心点
        if (IsPointInSector(centerPos))
            return true;

        // 智能角点检测：只检查最可能在扇形内的角点
        Vector3 sectorToCenter = centerPos - cachedSectorCenter;

        // 基于方向性选择要检查的角点（减少不必要的检查）
        bool checkMinX = Vector3.Dot(sectorToCenter, Vector3.right) < 0;
        bool checkMinY = Vector3.Dot(sectorToCenter, Vector3.up) < 0;
        bool checkMinZ = Vector3.Dot(sectorToCenter, cachedSectorForward) < 0;

        // 只检查最相关的4个角点而不是全部8个
        Vector3[] relevantCorners = new Vector3[4];
        relevantCorners[0] = new Vector3(checkMinX ? bounds.min.x : bounds.max.x, checkMinY ? bounds.min.y : bounds.max.y, checkMinZ ? bounds.min.z : bounds.max.z);
        relevantCorners[1] = new Vector3(!checkMinX ? bounds.min.x : bounds.max.x, checkMinY ? bounds.min.y : bounds.max.y, checkMinZ ? bounds.min.z : bounds.max.z);
        relevantCorners[2] = new Vector3(checkMinX ? bounds.min.x : bounds.max.x, !checkMinY ? bounds.min.y : bounds.max.y, checkMinZ ? bounds.min.z : bounds.max.z);
        relevantCorners[3] = new Vector3(checkMinX ? bounds.min.x : bounds.max.x, checkMinY ? bounds.min.y : bounds.max.y, !checkMinZ ? bounds.min.z : bounds.max.z);

        // 检查选择的角点
        foreach (var corner in relevantCorners)
        {
            if (IsPointInSector(corner))
                return true;
        }

        // 优化的反向检测：只在必要时进行
        // 如果碰撞体很小，跳过反向检测
        if (boundsExtent < outerCircleRadius * 0.3f)
            return false;

        // 使用简化的反向检测：只检查几个关键点
        return PerformOptimizedReverseCheck(collider, bounds);
    }

    /// <summary>
    /// 执行优化的反向检测
    /// </summary>
    /// <param name="collider">碰撞体</param>
    /// <param name="bounds">包围盒</param>
    /// <returns>是否检测到碰撞</returns>
    private bool PerformOptimizedReverseCheck(Collider collider, Bounds bounds)
    {
        // 只检查最关键的3个点：中心和两个主要方向
        Vector3[] criticalPoints = new Vector3[3];
        criticalPoints[0] = cachedSectorCenter; // 扇形中心
        criticalPoints[1] = cachedSectorCenter + cachedSectorForward * (outerCircleRadius * 0.5f); // 前方中点
        criticalPoints[2] = cachedSectorCenter + cachedSectorForward * outerCircleRadius; // 前方边缘

        foreach (var point in criticalPoints)
        {
            Vector3 closestPoint = collider.ClosestPoint(point);
            if (Vector3.Distance(closestPoint, point) < 0.02f) // 稍微放宽容差以提高检测率
                return true;
        }

        return false;
    }    /// <summary>
         /// 检查碰撞体是否在扇形范围内
         /// </summary>
         /// <param name="collider">目标碰撞体</param>
         /// <returns>是否在扇形内</returns>
    public bool IsColliderInSector(Collider collider)
    {
        return IsColliderInSectorRange(collider);
    }

    #endregion

    #region 参数验证

    /// <summary>
    /// 验证扇形参数合法性
    /// </summary>
    protected override void ValidateParameters()
    {
        base.ValidateParameters();

        bool paramsChanged = false;

        // 确保外圆半径大于内圆半径
        if (outerCircleRadius <= innerCircleRadius)
        {
            outerCircleRadius = innerCircleRadius + 0.1f;
            paramsChanged = true;
        }

        // 确保角度在合理范围内
        float clampedAngle = Mathf.Clamp(sectorAngle, 0f, 360f);
        if (clampedAngle != sectorAngle)
        {
            sectorAngle = clampedAngle;
            paramsChanged = true;
        }

        // 确保厚度为正值
        float clampedThickness = Mathf.Max(sectorThickness, 0.1f);
        if (clampedThickness != sectorThickness)
        {
            sectorThickness = clampedThickness;
            paramsChanged = true;
        }

        // 如果参数发生变化，使缓存失效
        if (paramsChanged)
        {
            InvalidateCache();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Unity编辑器中参数变化时调用，确保缓存失效
    /// </summary>
    protected override void OnValidate()
    {
        base.OnValidate();
        InvalidateCache();
    }
#endif

    #endregion

    #region 调试绘制

#if UNITY_EDITOR

    /// <summary>
    /// 绘制扇形区域
    /// </summary>
    protected override void DrawColliderGizmos()
    {
        Gizmos.color = gizmosColor;

        Vector3 center = SectorCenter;
        Vector3 forward = SectorForward;
        Vector3 up = SectorUp;

        // 绘制扇形的上下两个面
        float halfThickness = sectorThickness * 0.5f;
        Vector3 topCenter = center + up * halfThickness;
        Vector3 bottomCenter = center - up * halfThickness;

        DrawSectorFace(topCenter, forward, up, true);
        DrawSectorFace(bottomCenter, forward, up, false);

        // 绘制扇形的侧边连接线
        DrawSectorSides(center, forward, up);

        // 绘制通用调试信息
        DrawCommonGizmos();
    }

    /// <summary>
    /// 绘制扇形的一个面
    /// </summary>
    private void DrawSectorFace(Vector3 center, Vector3 forward, Vector3 up, bool isTop)
    {
        int segments = Mathf.Max(8, Mathf.RoundToInt(sectorAngle / 10f));
        float halfAngle = sectorAngle * 0.5f;

        // 绘制内圆弧
        if (innerCircleRadius > 0)
        {
            DrawArc(center, forward, up, innerCircleRadius, -halfAngle, halfAngle, segments);
        }

        // 绘制外圆弧
        DrawArc(center, forward, up, outerCircleRadius, -halfAngle, halfAngle, segments);

        // 绘制扇形的两条边
        if (sectorAngle < 360f)
        {
            Vector3 leftDirection = Quaternion.AngleAxis(-halfAngle, up) * forward;
            Vector3 rightDirection = Quaternion.AngleAxis(halfAngle, up) * forward;

            Gizmos.DrawLine(center + leftDirection * innerCircleRadius, center + leftDirection * outerCircleRadius);
            Gizmos.DrawLine(center + rightDirection * innerCircleRadius, center + rightDirection * outerCircleRadius);
        }
    }

    /// <summary>
    /// 绘制弧线
    /// </summary>
    private void DrawArc(Vector3 center, Vector3 forward, Vector3 up, float radius, float startAngle, float endAngle, int segments)
    {
        float angleStep = (endAngle - startAngle) / segments;
        Vector3 prevPoint = center + Quaternion.AngleAxis(startAngle, up) * forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 currentPoint = center + Quaternion.AngleAxis(angle, up) * forward * radius;
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }

    /// <summary>
    /// 绘制扇形的侧边连接线
    /// </summary>
    private void DrawSectorSides(Vector3 center, Vector3 forward, Vector3 up)
    {
        float halfThickness = sectorThickness * 0.5f;
        float halfAngle = sectorAngle * 0.5f;

        if (sectorAngle < 360f)
        {
            Vector3 leftDirection = Quaternion.AngleAxis(-halfAngle, up) * forward;
            Vector3 rightDirection = Quaternion.AngleAxis(halfAngle, up) * forward;

            // 左边线的上下连接
            Vector3 leftInner = center + leftDirection * innerCircleRadius;
            Vector3 leftOuter = center + leftDirection * outerCircleRadius;
            Gizmos.DrawLine(leftInner + up * halfThickness, leftInner - up * halfThickness);
            Gizmos.DrawLine(leftOuter + up * halfThickness, leftOuter - up * halfThickness);

            // 右边线的上下连接
            Vector3 rightInner = center + rightDirection * innerCircleRadius;
            Vector3 rightOuter = center + rightDirection * outerCircleRadius;
            Gizmos.DrawLine(rightInner + up * halfThickness, rightInner - up * halfThickness);
            Gizmos.DrawLine(rightOuter + up * halfThickness, rightOuter - up * halfThickness);

            // 在扇形弧线上增加更多竖向连接线
            int additionalLines = Mathf.Max(4, Mathf.RoundToInt(sectorAngle / 60f));
            for (int i = 1; i < additionalLines; i++)
            {
                float ratio = (float)i / additionalLines;
                float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, ratio);
                Vector3 direction = Quaternion.AngleAxis(currentAngle, up) * forward;

                // 内圆弧上的竖向连接线
                if (innerCircleRadius > 0)
                {
                    Vector3 innerPoint = center + direction * innerCircleRadius;
                    Gizmos.DrawLine(innerPoint + up * halfThickness, innerPoint - up * halfThickness);
                }

                // 外圆弧上的竖向连接线
                Vector3 outerPoint = center + direction * outerCircleRadius;
                Gizmos.DrawLine(outerPoint + up * halfThickness, outerPoint - up * halfThickness);

                // 在内外圆之间增加径向连接线（从内圆到外圆）
                if (innerCircleRadius > 0 && outerCircleRadius - innerCircleRadius > 0.5f)
                {
                    Vector3 innerPoint = center + direction * innerCircleRadius;
                    Vector3 outerPointRadial = center + direction * outerCircleRadius;

                    // 绘制上下两个面的径向连接线
                    Gizmos.DrawLine(innerPoint + up * halfThickness, outerPointRadial + up * halfThickness);
                    Gizmos.DrawLine(innerPoint - up * halfThickness, outerPointRadial - up * halfThickness);
                }
            }
        }
        else
        {
            // 360度扇形，绘制更多径向连接线
            int radialLines = Mathf.Max(6, Mathf.RoundToInt(outerCircleRadius));
            for (int i = 0; i < radialLines; i++)
            {
                float angle = 360f * i / radialLines;
                Vector3 direction = Quaternion.AngleAxis(angle, up) * forward;

                // 内圆上的连接线
                if (innerCircleRadius > 0)
                {
                    Vector3 inner = center + direction * innerCircleRadius;
                    Gizmos.DrawLine(inner + up * halfThickness, inner - up * halfThickness);
                }

                // 外圆上的连接线
                Vector3 outer = center + direction * outerCircleRadius;
                Gizmos.DrawLine(outer + up * halfThickness, outer - up * halfThickness);

                // 在内外圆之间增加径向连接线
                if (innerCircleRadius > 0 && outerCircleRadius - innerCircleRadius > 0.5f)
                {
                    Vector3 inner = center + direction * innerCircleRadius;

                    // 绘制上下两个面的径向连接线
                    Gizmos.DrawLine(inner + up * halfThickness, outer + up * halfThickness);
                    Gizmos.DrawLine(inner - up * halfThickness, outer - up * halfThickness);
                }
            }
        }
    }
#endif

    #endregion
}