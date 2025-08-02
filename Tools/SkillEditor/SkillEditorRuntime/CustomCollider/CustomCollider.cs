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
    [Tooltip("是否启用碰撞检测")] public bool enableCollisionDetection = true;
    [Tooltip("碰撞检测索引")] public int injuryDetectionIndex = 0;

    [Header("空间变换")]
    [Tooltip("自定义中心点偏移")] public Vector3 centerOffset = Vector3.zero;
    [Tooltip("自定义旋转偏移")] public Vector3 rotationOffset = Vector3.zero;

    [Header("调试")]
    [Tooltip("是否显示碰撞区域")] public bool showGizmosInScene = true;
    [Tooltip("碰撞区域颜色")] public Color gizmosColor = Color.red;

    // 缓存的碰撞对象列表
    protected HashSet<Collider> collidersInRange = new HashSet<Collider>();
    protected List<Collider> tempColliderList = new List<Collider>();

    #region 通用属性

    /// <summary>
    /// 获取碰撞体的实际中心点位置
    /// </summary>
    public virtual Vector3 ColliderCenter
    {
        get
        {
            if (centerOffset != Vector3.zero)
            {
                // 使用自定义中心点：对象位置 + 偏移量（考虑对象的旋转）
                return transform.position + transform.TransformDirection(centerOffset);
            }
            else
            {
                // 使用对象自身位置作为中心点
                return transform.position;
            }
        }
    }

    /// <summary>
    /// 获取碰撞体的实际前向方向
    /// </summary>
    public virtual Vector3 ColliderForward
    {
        get
        {
            if (rotationOffset != Vector3.zero)
            {
                // 使用自定义旋转：对象旋转 + 旋转偏移
                Quaternion customRotation = transform.rotation * Quaternion.Euler(rotationOffset);
                return customRotation * Vector3.forward;
            }
            else
            {
                // 使用对象自身的前向方向
                return transform.forward;
            }
        }
    }

    /// <summary>
    /// 获取碰撞体的实际向上方向
    /// </summary>
    public virtual Vector3 ColliderUp
    {
        get
        {
            if (rotationOffset != Vector3.zero)
            {
                // 使用自定义旋转：对象旋转 + 旋转偏移
                Quaternion customRotation = transform.rotation * Quaternion.Euler(rotationOffset);
                return customRotation * Vector3.up;
            }
            else
            {
                // 使用对象自身的向上方向
                return transform.up;
            }
        }
    }

    /// <summary>
    /// 获取碰撞体的实际右向方向
    /// </summary>
    public virtual Vector3 ColliderRight
    {
        get
        {
            if (rotationOffset != Vector3.zero)
            {
                // 使用自定义旋转：对象旋转 + 旋转偏移
                Quaternion customRotation = transform.rotation * Quaternion.Euler(rotationOffset);
                return customRotation * Vector3.right;
            }
            else
            {
                // 使用对象自身的右向方向
                return transform.right;
            }
        }
    }

    #endregion

    #region 通用设置方法

    /// <summary>
    /// 设置碰撞体中心点的世界坐标位置
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    public virtual void SetColliderCenterWorldPosition(Vector3 worldPosition)
    {
        // 计算相对于当前对象的偏移量
        centerOffset = transform.InverseTransformDirection(worldPosition - transform.position);
    }

    /// <summary>
    /// 设置碰撞体中心点的本地偏移量
    /// </summary>
    /// <param name="localOffset">本地偏移量</param>
    public virtual void SetColliderCenterLocalOffset(Vector3 localOffset)
    {
        centerOffset = localOffset;
    }

    /// <summary>
    /// 重置碰撞体中心点为对象位置
    /// </summary>
    public virtual void ResetColliderCenterToTransform()
    {
        centerOffset = Vector3.zero;
    }

    /// <summary>
    /// 设置碰撞体的世界旋转
    /// </summary>
    /// <param name="worldRotation">世界旋转（四元数）</param>
    public virtual void SetColliderWorldRotation(Quaternion worldRotation)
    {
        // 计算相对于当前对象的旋转偏移
        Quaternion relativeRotation = Quaternion.Inverse(transform.rotation) * worldRotation;
        rotationOffset = relativeRotation.eulerAngles;
    }

    /// <summary>
    /// 设置碰撞体的世界旋转（欧拉角）
    /// </summary>
    /// <param name="worldEulerAngles">世界旋转（欧拉角）</param>
    public virtual void SetColliderWorldRotation(Vector3 worldEulerAngles)
    {
        SetColliderWorldRotation(Quaternion.Euler(worldEulerAngles));
    }

    /// <summary>
    /// 设置碰撞体旋转的本地偏移量
    /// </summary>
    /// <param name="localEulerOffset">本地旋转偏移（欧拉角）</param>
    public virtual void SetColliderRotationLocalOffset(Vector3 localEulerOffset)
    {
        rotationOffset = localEulerOffset;
    }

    /// <summary>
    /// 让碰撞体朝向指定的世界方向
    /// </summary>
    /// <param name="worldDirection">世界方向向量</param>
    public virtual void SetColliderLookDirection(Vector3 worldDirection)
    {
        Quaternion lookRotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        SetColliderWorldRotation(lookRotation);
    }

    /// <summary>
    /// 让碰撞体朝向指定的世界位置
    /// </summary>
    /// <param name="worldPosition">世界位置</param>
    public virtual void SetColliderLookAtPosition(Vector3 worldPosition)
    {
        Vector3 direction = (worldPosition - ColliderCenter).normalized;
        SetColliderLookDirection(direction);
    }

    /// <summary>
    /// 重置碰撞体旋转为对象旋转
    /// </summary>
    public virtual void ResetColliderRotationToTransform()
    {
        rotationOffset = Vector3.zero;
    }

    #endregion

    #region 抽象接口 - 子类必须实现

    /// <summary>
    /// 检查指定位置是否在碰撞范围内
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <returns>是否在范围内</returns>
    public abstract bool IsPointInRange(Vector3 worldPosition);

    /// <summary>
    /// 检查碰撞体是否在范围内
    /// </summary>
    /// <param name="collider">目标碰撞体</param>
    /// <returns>是否在范围内</returns>
    public abstract bool IsColliderInRange(Collider collider);

    /// <summary>
    /// 获取当前范围内的所有碰撞体
    /// </summary>
    /// <returns>碰撞体列表</returns>
    public abstract List<Collider> GetCollidersInRange();

    /// <summary>
    /// 获取用于初步筛选的检测半径（用于Physics.OverlapSphere等方法）
    /// </summary>
    /// <returns>检测半径</returns>
    protected abstract float GetDetectionRadius();

    #endregion

    #region 通用碰撞检测功能

    /// <summary>
    /// 更新碰撞检测（通常在Update中调用）
    /// </summary>
    public virtual void UpdateCollisionDetection()
    {
        if (!enableCollisionDetection) return;

        collidersInRange.Clear();
        var currentColliders = GetCollidersInRange();

        foreach (var collider in currentColliders)
        {
            collidersInRange.Add(collider);
        }
    }

    /// <summary>
    /// 获取当前帧检测到的碰撞体数量
    /// </summary>
    public virtual int GetColliderCount()
    {
        return collidersInRange.Count;
    }

    /// <summary>
    /// 获取当前缓存的碰撞体列表
    /// </summary>
    /// <returns>碰撞体HashSet</returns>
    public virtual HashSet<Collider> GetCachedColliders()
    {
        return new HashSet<Collider>(collidersInRange);
    }

    /// <summary>
    /// 检查指定碰撞体是否在当前缓存中
    /// </summary>
    /// <param name="collider">目标碰撞体</param>
    /// <returns>是否在缓存中</returns>
    public virtual bool ContainsCollider(Collider collider)
    {
        return collidersInRange.Contains(collider);
    }

    /// <summary>
    /// 清除碰撞体缓存
    /// </summary>
    public virtual void ClearColliderCache()
    {
        collidersInRange.Clear();
    }

    #endregion

    #region 通用工具方法

    /// <summary>
    /// 检查碰撞体边界框的所有角点是否有任何一个在范围内
    /// </summary>
    /// <param name="collider">目标碰撞体</param>
    /// <returns>是否有角点在范围内</returns>
    protected virtual bool IsAnyCornerInRange(Collider collider)
    {
        if (collider == null) return false;

        // 检查碰撞体的中心点
        if (IsPointInRange(collider.bounds.center))
            return true;

        // 检查碰撞体边界的8个角点
        Bounds bounds = collider.bounds;
        Vector3[] corners = new Vector3[8];

        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[4] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        // 检查任意一个角点是否在范围内
        foreach (var corner in corners)
        {
            if (IsPointInRange(corner))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 使用球形检测获取附近的碰撞体
    /// </summary>
    /// <returns>附近的碰撞体数组</returns>
    protected virtual Collider[] GetNearbyColliders()
    {
        return Physics.OverlapSphere(ColliderCenter, GetDetectionRadius(), targetLayers);
    }

    #endregion

    #region 编辑器功能

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器菜单：重置中心点为对象位置
    /// </summary>
    [UnityEngine.ContextMenu("重置中心点为对象位置")]
    protected virtual void EditorResetCenterToTransform()
    {
        ResetColliderCenterToTransform();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// 编辑器菜单：重置旋转为对象旋转
    /// </summary>
    [UnityEngine.ContextMenu("重置旋转为对象旋转")]
    protected virtual void EditorResetRotationToTransform()
    {
        ResetColliderRotationToTransform();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// 编辑器菜单：重置所有设置为对象状态
    /// </summary>
    [UnityEngine.ContextMenu("重置所有设置为对象状态")]
    protected virtual void EditorResetAllToTransform()
    {
        ResetColliderCenterToTransform();
        ResetColliderRotationToTransform();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    #endregion

    #region Unity生命周期

    protected virtual void Start()
    {
        // 子类可以重写此方法进行初始化
        ValidateParameters();
    }

    protected virtual void Update()
    {
        if (enableCollisionDetection)
        {
            UpdateCollisionDetection();
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        ValidateParameters();
    }
#endif

    #endregion

    #region 参数验证 - 子类可重写

    /// <summary>
    /// 验证参数合法性（子类可重写）
    /// </summary>
    protected virtual void ValidateParameters()
    {
        // 基类默认不做验证，子类可以重写此方法
    }

    #endregion

    #region 调试绘制 - 子类必须实现

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        if (!showGizmosInScene) return;
        DrawColliderGizmos();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (showGizmosInScene) return; // 避免重复绘制
        DrawColliderGizmos();
    }

    /// <summary>
    /// 绘制碰撞体区域（子类必须实现）
    /// </summary>
    protected abstract void DrawColliderGizmos();

    /// <summary>
    /// 绘制通用的调试信息
    /// </summary>
    protected virtual void DrawCommonGizmos()
    {
        Vector3 center = ColliderCenter;
        Vector3 forward = ColliderForward;

        // 绘制中心点
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.05f);

        // 绘制朝向指示器
        Gizmos.color = Color.green;
        Gizmos.DrawLine(center, center + forward * 0.5f);

        // 如果使用自定义中心点，绘制连接线
        if (centerOffset != Vector3.zero && Vector3.Distance(transform.position, center) > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, center);

            // 绘制对象位置点
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.03f);
        }

        // 如果使用自定义旋转，绘制参考线
        if (rotationOffset != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Vector3 objectForward = transform.forward;
            Gizmos.DrawLine(center, center + objectForward * 0.3f);

            // 在参考线末端绘制小球
            Gizmos.DrawWireSphere(center + objectForward * 0.3f, 0.02f);
        }
    }
#endif

    #endregion
}