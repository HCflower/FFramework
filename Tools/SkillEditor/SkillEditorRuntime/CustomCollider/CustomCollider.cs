using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自定义碰撞体基类
/// 提供通用的碰撞检测功能和可视化功能
/// </summary>
public abstract class CustomCollider : MonoBehaviour
{
    [Header("碰撞检测")]
    [Tooltip("是否启用碰撞检测")] public bool enableCollisionDetection = true;
    [Tooltip("检测的图层")] public LayerMask targetLayers = -1;
    [Tooltip("检测频率(帧数间隔)"), Min(1)] public int frameInterval = 1;
    [Tooltip("自定义中心点偏移")] public Vector3 centerOffset = Vector3.zero;
    [Tooltip("自定义旋转偏移")] public Vector3 rotationOffset = Vector3.zero;

    [Header("调试")]
    [Tooltip("是否显示碰撞区域")] public bool showGizmosInScene = true;
    [Tooltip("碰撞区域颜色")] public Color gizmosColor = Color.red;

    // 缓存的碰撞对象列表
    protected HashSet<Collider> collidersInRange = new HashSet<Collider>();
    protected List<Collider> tempColliderList = new List<Collider>();

    // 性能优化相关
    private int frameCounter = 0;
    private Vector3[] cachedCorners = new Vector3[8]; // 预分配角点数组
    private Vector3 lastDetectionPosition;
    private Quaternion lastDetectionRotation;
    private float positionThreshold = 0.1f;
    private float rotationThreshold = 5f;

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

        // 性能优化：检测频率控制
        if (frameInterval > 1)
        {
            frameCounter++;
            if (frameCounter < frameInterval)
                return;
            frameCounter = 0;
        }

        // 性能优化：位置变化检测
        if (HasSignificantTransformChange())
        {
            collidersInRange.Clear();
            var currentColliders = GetCollidersInRange();

            // 添加所有检测到的碰撞体
            foreach (var collider in currentColliders)
            {
                collidersInRange.Add(collider);
            }

            // 更新缓存的变换信息
            lastDetectionPosition = ColliderCenter;
            lastDetectionRotation = Quaternion.LookRotation(ColliderForward, ColliderUp);
        }
    }

    /// <summary>
    /// 检查变换是否有显著变化
    /// </summary>
    private bool HasSignificantTransformChange()
    {
        Vector3 currentPosition = ColliderCenter;
        Quaternion currentRotation = Quaternion.LookRotation(ColliderForward, ColliderUp);

        bool positionChanged = Vector3.Distance(currentPosition, lastDetectionPosition) > positionThreshold;
        bool rotationChanged = Quaternion.Angle(currentRotation, lastDetectionRotation) > rotationThreshold;

        return positionChanged || rotationChanged;
    }

    /// <summary>
    /// 获取当前帧检测到的碰撞体数量
    /// </summary>
    public virtual int GetColliderCount()
    {
        return collidersInRange.Count;
    }

    /// <summary>
    /// 获取当前缓存的碰撞体列表（性能优化版本）
    /// </summary>
    /// <returns>碰撞体HashSet（只读）</returns>
    public virtual HashSet<Collider> GetCachedColliders()
    {
        return collidersInRange; // 直接返回引用，避免创建新对象
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
    /// 检查碰撞体边界框的所有角点是否有任何一个在范围内（性能优化版本）
    /// </summary>
    /// <param name="collider">目标碰撞体</param>
    /// <returns>是否有角点在范围内</returns>
    protected virtual bool IsAnyCornerInRange(Collider collider)
    {
        if (collider == null) return false;

        // 先检查碰撞体的中心点（最快的检测）
        if (IsPointInRange(collider.bounds.center))
            return true;

        // 获取边界框信息
        Bounds bounds = collider.bounds;

        // 快速边界检查：如果碰撞体边界与检测区域完全不相交，直接返回false
        float detectionRadius = GetDetectionRadius();
        Vector3 center = ColliderCenter;
        if (Vector3.Distance(bounds.center, center) > detectionRadius + bounds.size.magnitude * 0.5f)
            return false;

        // 使用预分配的数组存储8个角点
        cachedCorners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        cachedCorners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        cachedCorners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        cachedCorners[3] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        cachedCorners[4] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        cachedCorners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        cachedCorners[6] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        cachedCorners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        // 检查角点（如果有任意一个在范围内就早期返回）
        for (int i = 0; i < 8; i++)
        {
            if (IsPointInRange(cachedCorners[i]))
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

    /// <summary>
    /// 编辑器菜单：设置旋转为对象本地旋转
    /// </summary>
    [UnityEngine.ContextMenu("设置旋转为对象本地旋转")]
    protected virtual void EditorSetRotationToLocalRotation()
    {
        // 将旋转偏移设置为对象的本地旋转（相对于父物体的旋转）
        if (transform.parent != null)
        {
            // 如果有父物体，计算相对于父物体的旋转
            Quaternion localRotation = transform.localRotation;
            Vector3 eulerAngles = localRotation.eulerAngles;

            // 处理浮点数精度问题，将接近0的值设置为0
            eulerAngles.x = Mathf.Abs(eulerAngles.x) < 0.001f ? 0f : eulerAngles.x;
            eulerAngles.y = Mathf.Abs(eulerAngles.y) < 0.001f ? 0f : eulerAngles.y;
            eulerAngles.z = Mathf.Abs(eulerAngles.z) < 0.001f ? 0f : eulerAngles.z;

            rotationOffset = eulerAngles;
        }
        else
        {
            // 如果没有父物体，本地旋转就是世界旋转
            Vector3 eulerAngles = transform.rotation.eulerAngles;

            // 处理浮点数精度问题，将接近0的值设置为0
            eulerAngles.x = Mathf.Abs(eulerAngles.x) < 0.001f ? 0f : eulerAngles.x;
            eulerAngles.y = Mathf.Abs(eulerAngles.y) < 0.001f ? 0f : eulerAngles.y;
            eulerAngles.z = Mathf.Abs(eulerAngles.z) < 0.001f ? 0f : eulerAngles.z;

            rotationOffset = eulerAngles;
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }

#endif

    #endregion

    #region Unity生命周期

    protected virtual void Start()
    {
        // 子类可以重写此方法进行初始化
        ValidateParameters();

        // 初始化缓存的变换信息
        lastDetectionPosition = ColliderCenter;
        lastDetectionRotation = Quaternion.LookRotation(ColliderForward, ColliderUp);
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
        // 如果组件是未激活状态就不需要绘制
        if (!showGizmosInScene || !enabled) return;
        DrawColliderGizmos();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // 避免重复绘制，如果组件是未激活状态就不需要绘制
        if (showGizmosInScene || !enabled) return;
        DrawColliderGizmos();
    }

    /// <summary>
    /// 绘制碰撞体区域（子类必须实现）
    /// 子类应在方法开始时检查组件是否激活：if (!enabled) return;
    /// </summary>
    protected abstract void DrawColliderGizmos();

    /// <summary>
    /// 绘制通用的调试信息
    /// </summary>
    protected virtual void DrawCommonGizmos()
    {
        // 如果组件是未激活状态就不需要绘制
        if (!enabled) return;

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