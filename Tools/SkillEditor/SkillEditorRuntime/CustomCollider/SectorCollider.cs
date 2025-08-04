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
        Vector3 localPosition = worldPosition - sectorCenter;

        // 快速距离检查（使用平方距离避免开方计算）
        float sqrDistance = localPosition.sqrMagnitude;
        if (sqrDistance < innerCircleRadius * innerCircleRadius || sqrDistance > outerCircleRadius * outerCircleRadius)
            return false;

        // 检查高度范围（厚度）
        float heightOffset = Vector3.Dot(localPosition, sectorUp);
        if (Mathf.Abs(heightOffset) > sectorThickness * 0.5f)
            return false;

        // 如果角度为360度，跳过角度检查
        if (sectorAngle >= 360f)
            return true;

        // 投影到扇形平面
        Vector3 projectedPos = localPosition - heightOffset * sectorUp;

        // 检查角度范围（使用点积比较，避免Vector3.Angle的反三角函数计算）
        float dot = Vector3.Dot(sectorForward.normalized, projectedPos.normalized);
        float halfAngleCos = Mathf.Cos(sectorAngle * 0.5f * Mathf.Deg2Rad);
        return dot >= halfAngleCos;
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

        // 使用基类方法获取附近的碰撞体
        Collider[] nearbyColliders = GetNearbyColliders();

        foreach (var collider in nearbyColliders)
        {
            if (collider == null || collider.transform == transform) continue;

            // 使用基类方法检查碰撞体是否在范围内
            if (IsAnyCornerInRange(collider))
            {
                tempColliderList.Add(collider);
            }
        }

        return tempColliderList;
    }

    /// <summary>
    /// 检查碰撞体是否在扇形范围内
    /// </summary>
    /// <param name="collider">目标碰撞体</param>
    /// <returns>是否在扇形内</returns>
    public bool IsColliderInSector(Collider collider)
    {
        return IsAnyCornerInRange(collider);
    }

    #endregion

    #region 参数验证

    /// <summary>
    /// 验证扇形参数合法性
    /// </summary>
    protected override void ValidateParameters()
    {
        base.ValidateParameters();

        // 确保外圆半径大于内圆半径
        if (outerCircleRadius <= innerCircleRadius)
        {
            outerCircleRadius = innerCircleRadius + 0.1f;
        }

        // 确保角度在合理范围内
        sectorAngle = Mathf.Clamp(sectorAngle, 0f, 360f);

        // 确保厚度为正值
        sectorThickness = Mathf.Max(sectorThickness, 0.1f);
    }

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