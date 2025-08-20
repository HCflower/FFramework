using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自定义碰撞体基类
/// 提供通用的碰撞检测功能和可视化功能
/// </summary>
public abstract class CustomCollider : MonoBehaviour
{
    [Header("碰撞检测")]
    [Tooltip("检测的图层")] public LayerMask targetLayers = -1;
    [Tooltip("自定义中心点偏移")] public Vector3 centerOffset = Vector3.zero;
    [Tooltip("自定义旋转偏移")] public Vector3 rotationOffset = Vector3.zero;
    [Tooltip("碰撞区域颜色")] public Color gizmosColor = new Color(0, 255, 0, 1);
    [Tooltip("是否显示碰撞区域")] public bool showGizmosInScene = true;

    // 缓存的碰撞对象列表
    protected HashSet<Collider> hitColliders = new HashSet<Collider>();
    void Update()
    {
        DetectColliders();
    }

    void OnDisable()
    {
        hitColliders.Clear(); // 每次检测前清空
    }

    // 检测碰撞体
    protected abstract void DetectColliders();

    // 获取命中的碰撞体
    public HashSet<Collider> GetHitColliders() => hitColliders;

#if UNITY_EDITOR
    // 调试绘制 - 子类必须实现
    private void OnDrawGizmos()
    {
        if (!showGizmosInScene || !enabled) return;
        DrawCenterGizmos();
        DrawColliderGizmos();
    }

    // 绘制中心点
    private void DrawCenterGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position + centerOffset, 0.05f);
    }

    // 绘制碰撞体区域
    protected abstract void DrawColliderGizmos();
#endif

}