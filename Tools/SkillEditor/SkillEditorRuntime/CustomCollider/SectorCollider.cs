using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 扇形碰撞体
/// 用于检测扇形区域内的碰撞，支持内外圆半径、角度和厚度设置
/// </summary>
[AddComponentMenu("CustomColliders/Sector Collider")]
public class SectorCollider : CustomCollider
{
    [Header("扇形参数")]
    [Tooltip("扇形内圆半径"), Min(0)] public float innerCircleRadius = 0;
    [Tooltip("扇形外圆半径"), Min(1)] public float outerCircleRadius = 1;
    [Tooltip("扇形角度"), Range(0, 360)] public float sectorAngle = 90;
    [Tooltip("扇形厚度"), Min(0.1f)] public float sectorThickness = 0.1f;

    // 检测碰撞体
    // BUG,内部圆检测会移除已经检测到的碰撞体。
    protected override void DetectColliders()
    {
        hitColliders.Clear(); // 每次检测前清空

        float innerR = innerCircleRadius;
        float outerR = outerCircleRadius;
        float angle = sectorAngle;
        float thickness = sectorThickness;
        float halfAngle = angle * 0.5f;

        Vector3 center = transform.position + centerOffset;
        Quaternion rot = Quaternion.LookRotation(transform.forward, transform.up) * Quaternion.Euler(rotationOffset);

        Vector3 boxSize = new Vector3(outerR * 2, thickness, outerR * 2);
        Vector3 boxCenter = center;
        Quaternion boxRotation = rot;

        Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize * 0.5f, boxRotation, targetLayers);

        Vector3 sectorForward = rot * Vector3.forward;

        foreach (var col in colliders)
        {
            if (col == null) continue;
            if (col.gameObject == gameObject) continue;

            Vector3 point = col.ClosestPoint(center);
            float distance = Vector3.Distance(point, center);
            if (distance < innerR || distance > outerR) continue;

            Vector3 dir = (point - center).normalized;
            float ang = Vector3.Angle(sectorForward, dir);
            if (ang > halfAngle) continue;

            float upOffset = Mathf.Abs(Vector3.Dot(point - center, rot * Vector3.up));
            if (upOffset > thickness * 0.5f) continue;

            // 命中且去重，HashSet自动去重
            hitColliders.Add(col);
        }
    }
    // 绘制碰撞体区域
    protected override void DrawColliderGizmos()
    {
        float innerR = innerCircleRadius;
        float outerR = outerCircleRadius;
        float angle = sectorAngle;
        float thickness = sectorThickness;
        int segments = Mathf.Max(5, Mathf.CeilToInt(angle / 20f));
        float halfAngle = angle * 0.5f;

        Vector3 center = transform.position + centerOffset;
        Quaternion rot = Quaternion.LookRotation(transform.forward, transform.up) * Quaternion.Euler(rotationOffset);
        Vector3 halfUpOffset = rot * Vector3.up * (thickness * 0.5f);

        // 优化：圆弧绘制方法
        void DrawArc(float radius, Vector3 offset)
        {
            Vector3 lastBottom = center + rot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * radius - offset;
            Vector3 lastTop = center + rot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * radius + offset;
            for (int i = 1; i <= segments; i++)
            {
                float a = -halfAngle + angle / segments * i;
                Vector3 nextBottom = center + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * radius - offset;
                Vector3 nextTop = center + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * radius + offset;
                Gizmos.color = gizmosColor;
                Gizmos.DrawLine(lastBottom, nextBottom);
                Gizmos.DrawLine(lastTop, nextTop);
                lastBottom = nextBottom;
                lastTop = nextTop;
            }
        }

        // 绘制外圆弧
        DrawArc(outerR, halfUpOffset);
        // 绘制内圆弧
        DrawArc(innerR, halfUpOffset);

        // 两条边（底面和顶面）
        Vector3 leftOuterBottom = center + rot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * outerR - halfUpOffset;
        Vector3 leftInnerBottom = center + rot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * innerR - halfUpOffset;
        Vector3 rightOuterBottom = center + rot * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward * outerR - halfUpOffset;
        Vector3 rightInnerBottom = center + rot * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward * innerR - halfUpOffset;

        Vector3 leftOuterTop = center + rot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * outerR + halfUpOffset;
        Vector3 leftInnerTop = center + rot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * innerR + halfUpOffset;
        Vector3 rightOuterTop = center + rot * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward * outerR + halfUpOffset;
        Vector3 rightInnerTop = center + rot * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward * innerR + halfUpOffset;

        Gizmos.DrawLine(leftInnerBottom, leftOuterBottom);
        Gizmos.DrawLine(rightInnerBottom, rightOuterBottom);
        Gizmos.DrawLine(leftInnerTop, leftOuterTop);
        Gizmos.DrawLine(rightInnerTop, rightOuterTop);

        // 连接四个角
        Gizmos.DrawLine(leftInnerBottom, leftInnerTop);
        Gizmos.DrawLine(leftOuterBottom, leftOuterTop);
        Gizmos.DrawLine(rightInnerBottom, rightInnerTop);
        Gizmos.DrawLine(rightOuterBottom, rightOuterTop);

        // 对称连接上下端点和内外圆端点
        int step = 2; // 每隔2个分段
        for (int i = 0; i <= segments; i += step)
        {
            float a = -halfAngle + angle / segments * i;
            Vector3 outerBottom = center + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * outerR - halfUpOffset;
            Vector3 outerTop = center + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * outerR + halfUpOffset;
            Vector3 innerBottom = center + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * innerR - halfUpOffset;
            Vector3 innerTop = center + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * innerR + halfUpOffset;

            Gizmos.color = gizmosColor;
            Gizmos.DrawLine(outerBottom, outerTop);
            Gizmos.DrawLine(innerBottom, innerTop);
            Gizmos.DrawLine(innerBottom, outerBottom); // 底面内外圆端点
            Gizmos.DrawLine(innerTop, outerTop);       // 顶面内外圆端点

            // 对称连接另一侧
            if (i != segments - i && i != 0)
            {
                float aSym = -halfAngle + angle / segments * (segments - i);
                Vector3 outerBottomSym = center + rot * Quaternion.Euler(0, aSym, 0) * Vector3.forward * outerR - halfUpOffset;
                Vector3 outerTopSym = center + rot * Quaternion.Euler(0, aSym, 0) * Vector3.forward * outerR + halfUpOffset;
                Vector3 innerBottomSym = center + rot * Quaternion.Euler(0, aSym, 0) * Vector3.forward * innerR - halfUpOffset;
                Vector3 innerTopSym = center + rot * Quaternion.Euler(0, aSym, 0) * Vector3.forward * innerR + halfUpOffset;

                Gizmos.DrawLine(outerBottomSym, outerTopSym);
                Gizmos.DrawLine(innerBottomSym, innerTopSym);
                Gizmos.DrawLine(innerBottomSym, outerBottomSym);
                Gizmos.DrawLine(innerTopSym, outerTopSym);
            }
        }
    }

}