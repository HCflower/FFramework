using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FFramework.Kit;

namespace SkillEditor
{
    /// <summary>
    /// 技能摄像机预览器
    /// 负责在编辑器模式下预览摄像机的位置、旋转、视野变化以及震动效果
    /// </summary>
    public class SkillCameraPreviewer : System.IDisposable
    {
        #region 私有字段

        /// <summary>技能所有者游戏对象</summary>
        private SkillRuntimeController skillOwner;

        /// <summary>技能配置数据</summary>
        private SkillConfig skillConfig;

        /// <summary>是否正在预览中</summary>
        private bool isPreviewActive = false;

        /// <summary>目标Camera组件</summary>
        private Camera targetCamera;

        /// <summary>当前预览帧</summary>
        private int currentFrame = 0;

        /// <summary>存储原始摄像机状态（用于恢复）</summary>
        private struct OriginalCameraState
        {
            public Vector3 position;
            public Vector3 rotation;
            public float fieldOfView;
        }

        /// <summary>原始摄像机状态</summary>
        private OriginalCameraState originalCameraState;

        /// <summary>震动相关的随机状态</summary>
        private Dictionary<string, System.Random> vibrationRandomStates = new Dictionary<string, System.Random>();

        /// <summary>存储每个摄像机的原始状态（片段开始前的状态）</summary>
        private Dictionary<Camera, OriginalCameraState> cameraOriginalStates = new Dictionary<Camera, OriginalCameraState>();

        /// <summary>当前帧内活跃的片段集合</summary>
        private HashSet<string> activeClips = new HashSet<string>();

        #endregion

        #region 公共属性

        /// <summary>是否正在预览中</summary>
        public bool IsPreviewActive => isPreviewActive;

        /// <summary>当前预览帧</summary>
        public int CurrentFrame => currentFrame;

        /// <summary>技能所有者</summary>
        public SkillRuntimeController SkillOwner => skillOwner;

        /// <summary>目标摄像机</summary>
        public Camera TargetCamera => targetCamera;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造摄像机预览器
        /// </summary>
        /// <param name="owner">技能所有者</param>
        /// <param name="config">技能配置</param>
        public SkillCameraPreviewer(SkillRuntimeController owner, SkillConfig config)
        {
            skillOwner = owner;
            skillConfig = config;

            // 查找摄像机组件
            FindCamera();

            if (targetCamera != null)
            {
                StoreOriginalCameraState();
            }
        }

        #endregion

        #region 预览控制方法

        /// <summary>
        /// 开始摄像机预览
        /// </summary>
        public void StartPreview()
        {
            if (skillOwner == null || skillConfig == null || targetCamera == null)
            {
                Debug.LogWarning("SkillCameraPreviewer: 无法开始预览，技能所有者、配置或摄像机为空");
                return;
            }

            if (isPreviewActive)
            {
                return;
            }

            isPreviewActive = true;
            currentFrame = 0;

            // 存储原始摄像机状态
            StoreOriginalCameraState();

            // 初始化摄像机轨道
            InitializeCameraTrack();

            // 预览第一帧
            PreviewFrame(0);

            Debug.Log($"摄像机预览已启动 - 目标摄像机: {targetCamera.name}");
        }

        /// <summary>
        /// 停止摄像机预览
        /// </summary>
        public void StopPreview()
        {
            if (!isPreviewActive) return;

            isPreviewActive = false;
            currentFrame = 0;

            // 恢复所有被控制摄像机的原始状态
            RestoreAllCameraStates();

            // 恢复主摄像机的原始状态
            RestoreOriginalCameraState();

            // 清理震动随机状态
            vibrationRandomStates.Clear();

            // 清理活跃片段记录
            activeClips.Clear();

            Debug.Log("摄像机预览已停止");
        }

        /// <summary>
        /// 预览指定帧的摄像机状态
        /// </summary>
        /// <param name="frame">目标帧</param>
        public void PreviewFrame(int frame)
        {
            if (!isPreviewActive || skillConfig == null || targetCamera == null) return;

            int targetFrame = Mathf.Clamp(frame, 0, skillConfig.maxFrames);
            currentFrame = targetFrame;

            // 应用摄像机状态
            ApplyCameraAtFrame(currentFrame);
        }

        /// <summary>
        /// 刷新摄像机数据 - 当轨道项发生变化时调用
        /// </summary>
        public void RefreshCameraData()
        {
            if (!isPreviewActive) return;

            // 重新初始化摄像机轨道
            InitializeCameraTrack();

            // 重置帧数
            currentFrame = 0;

            // 重新预览第0帧
            PreviewFrame(0);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 查找摄像机组件
        /// </summary>
        private void FindCamera()
        {
            if (skillOwner == null) return;

            // 首先尝试从技能所有者身上查找摄像机组件
            targetCamera = skillOwner.GetComponentInChildren<Camera>();

            // 如果没有找到，使用场景中的主摄像机
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            // 如果还是没有找到，使用第一个摄像机
            if (targetCamera == null)
            {
                Camera[] cameras = Object.FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                {
                    targetCamera = cameras[0];
                }
            }
        }

        /// <summary>
        /// 根据片段名称查找摄像机
        /// </summary>
        /// <param name="clipName">片段名称</param>
        /// <returns>找到的摄像机组件</returns>
        private Camera FindCameraByClipName(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return targetCamera;

            // 在场景中查找名称匹配的游戏对象
            GameObject cameraObject = GameObject.Find(clipName);
            if (cameraObject != null)
            {
                Camera camera = cameraObject.GetComponent<Camera>();
                if (camera != null)
                {
                    return camera;
                }
            }

            // 如果没有找到，返回默认摄像机
            return targetCamera;
        }

        /// <summary>
        /// 存储原始摄像机状态
        /// </summary>
        private void StoreOriginalCameraState()
        {
            if (targetCamera == null) return;

            originalCameraState = new OriginalCameraState
            {
                position = targetCamera.transform.position,
                rotation = targetCamera.transform.rotation.eulerAngles,
                fieldOfView = targetCamera.fieldOfView
            };
        }

        /// <summary>
        /// 存储指定摄像机的原始状态（用于片段控制）
        /// </summary>
        /// <param name="camera">要存储状态的摄像机</param>
        private void StoreCameraOriginalState(Camera camera)
        {
            if (camera == null) return;

            // 如果已经存储过这个摄像机的状态，则不重复存储
            if (cameraOriginalStates.ContainsKey(camera)) return;

            cameraOriginalStates[camera] = new OriginalCameraState
            {
                position = camera.transform.position,
                rotation = camera.transform.rotation.eulerAngles,
                fieldOfView = camera.fieldOfView
            };
        }

        /// <summary>
        /// 恢复原始摄像机状态
        /// </summary>
        private void RestoreOriginalCameraState()
        {
            if (targetCamera == null) return;

            targetCamera.transform.position = originalCameraState.position;
            targetCamera.transform.rotation = Quaternion.Euler(originalCameraState.rotation);
            targetCamera.fieldOfView = originalCameraState.fieldOfView;
        }

        /// <summary>
        /// 根据片段键恢复对应摄像机的原始状态
        /// </summary>
        /// <param name="clipKey">片段键</param>
        private void RestoreCameraFromClip(string clipKey)
        {
            if (string.IsNullOrEmpty(clipKey)) return;

            // 从片段键中解析出片段名称
            string[] parts = clipKey.Split('_');
            if (parts.Length < 2) return;

            string clipName = string.Join("_", parts, 0, parts.Length - 1);

            // 查找对应的摄像机
            Camera camera = FindCameraByClipName(clipName);
            if (camera != null && cameraOriginalStates.ContainsKey(camera))
            {
                // 恢复摄像机状态
                var originalState = cameraOriginalStates[camera];
                camera.transform.position = originalState.position;
                camera.transform.rotation = Quaternion.Euler(originalState.rotation);
                camera.fieldOfView = originalState.fieldOfView;

                // 移除已恢复的状态记录
                cameraOriginalStates.Remove(camera);
            }
        }

        /// <summary>
        /// 恢复所有被控制摄像机的原始状态
        /// </summary>
        private void RestoreAllCameraStates()
        {
            foreach (var kvp in cameraOriginalStates)
            {
                Camera camera = kvp.Key;
                OriginalCameraState originalState = kvp.Value;

                if (camera != null)
                {
                    camera.transform.position = originalState.position;
                    camera.transform.rotation = Quaternion.Euler(originalState.rotation);
                    camera.fieldOfView = originalState.fieldOfView;
                }
            }

            // 清理所有状态记录
            cameraOriginalStates.Clear();
        }

        /// <summary>
        /// 初始化摄像机轨道
        /// </summary>
        private void InitializeCameraTrack()
        {
            if (skillConfig?.trackContainer?.cameraTrack == null || targetCamera == null)
                return;

            var cameraTrack = skillConfig.trackContainer.cameraTrack;

            // 初始化轨道中所有片段的初始摄像机状态
            cameraTrack.InitializeCameras(targetCamera);
        }

        /// <summary>
        /// 应用指定帧的摄像机状态
        /// </summary>
        /// <param name="frame">目标帧</param>
        private void ApplyCameraAtFrame(int frame)
        {
            if (skillConfig?.trackContainer?.cameraTrack == null)
                return;

            var cameraTrack = skillConfig.trackContainer.cameraTrack;

            // 检查轨道是否激活
            if (!cameraTrack.isEnabled)
                return;

            // 记录当前帧的活跃片段
            HashSet<string> currentActiveClips = new HashSet<string>();
            HashSet<Camera> currentActiveCameras = new HashSet<Camera>();

            // 遍历所有摄像机片段，找到当前帧内激活的片段
            foreach (var clip in cameraTrack.cameraClips)
            {
                string clipKey = $"{clip.clipName}_{clip.startFrame}";

                if (clip.IsFrameInRange(frame))
                {
                    currentActiveClips.Add(clipKey);

                    // 根据片段名称查找特定摄像机
                    Camera clipCamera = FindCameraByClipName(clip.clipName);
                    if (clipCamera != null)
                    {
                        currentActiveCameras.Add(clipCamera);

                        // 如果这是新激活的片段，存储摄像机的原始状态
                        if (!activeClips.Contains(clipKey))
                        {
                            StoreCameraOriginalState(clipCamera);
                        }

                        // 执行该片段的摄像机控制
                        bool hasCameraControl = ExecuteCameraClipAtFrame(clip, clipCamera, frame);

                        // 应用震动效果
                        ApplyVibrationEffectForClip(clip, clipCamera, frame);

                        if (hasCameraControl)
                        {
                            // 标记场景需要重绘
                            SceneView.RepaintAll();
                        }
                    }
                }
            }

            // 检查哪些片段已经不再活跃，需要还原对应摄像机
            foreach (string previousClipKey in activeClips)
            {
                if (!currentActiveClips.Contains(previousClipKey))
                {
                    // 这个片段已经结束，需要还原对应的摄像机
                    RestoreCameraFromClip(previousClipKey);
                }
            }

            // 更新活跃片段集合
            activeClips = currentActiveClips;
        }

        /// <summary>
        /// 执行单个摄像机片段在指定帧的控制
        /// </summary>
        /// <param name="clip">摄像机片段</param>
        /// <param name="camera">目标摄像机</param>
        /// <param name="frame">当前帧</param>
        /// <returns>是否执行了摄像机控制</returns>
        private bool ExecuteCameraClipAtFrame(CameraTrack.CameraClip clip, Camera camera, int frame)
        {
            if (clip == null || camera == null) return false;

            // 计算片段内的进度
            float progress = clip.durationFrame > 0 ? (float)(frame - clip.startFrame) / clip.durationFrame : 0f;
            progress = Mathf.Clamp01(progress);

            // 应用摄像机状态
            clip.ApplyCamera(camera, progress);

            return true;
        }

        /// <summary>
        /// 为特定片段应用震动效果
        /// </summary>
        /// <param name="clip">摄像机片段</param>
        /// <param name="camera">目标摄像机</param>
        /// <param name="frame">当前帧</param>
        private void ApplyVibrationEffectForClip(CameraTrack.CameraClip clip, Camera camera, int frame)
        {
            if (clip == null || camera == null || !clip.enableVibration) return;

            // 检查当前帧是否在震动片段范围内
            if (!clip.IsFrameInRange(frame)) return;

            // 计算震动进度
            float progress = clip.durationFrame > 0 ? (float)(frame - clip.startFrame) / clip.durationFrame : 0f;
            progress = Mathf.Clamp01(progress);

            // 应用震动
            ApplyVibrationToCamera(clip, camera, progress, frame);
        }

        /// <summary>
        /// 应用震动效果（保留旧方法兼容性）
        /// </summary>
        /// <param name="frame">当前帧</param>
        private void ApplyVibrationEffect(int frame)
        {
            if (skillConfig?.trackContainer?.cameraTrack == null || targetCamera == null)
                return;

            var cameraTrack = skillConfig.trackContainer.cameraTrack;

            // 遍历所有摄像机片段，寻找需要震动的片段
            foreach (var clip in cameraTrack.cameraClips)
            {
                ApplyVibrationEffectForClip(clip, targetCamera, frame);
            }
        }

        /// <summary>
        /// 应用震动到摄像机
        /// </summary>
        /// <param name="clip">摄像机片段</param>
        /// <param name="camera">目标摄像机</param>
        /// <param name="progress">震动进度 (0-1)</param>
        /// <param name="frame">当前帧</param>
        private void ApplyVibrationToCamera(CameraTrack.CameraClip clip, Camera camera, float progress, int frame)
        {
            if (camera == null) return;

            // 获取或创建该片段的随机数生成器
            string clipKey = $"{clip.clipName}_{clip.startFrame}";
            int seed = clip.startFrame.GetHashCode() ^ clip.clipName.GetHashCode();

            if (!vibrationRandomStates.ContainsKey(clipKey))
            {
                // 使用片段的起始帧和名称作为种子，确保震动效果可重现
                vibrationRandomStates[clipKey] = new System.Random(seed);
            }

            var random = vibrationRandomStates[clipKey];

            // 计算震动强度（考虑衰减曲线）
            float decayValue = clip.vibrationDecay.Evaluate(progress);
            float currentIntensity = clip.vibrationIntensity * decayValue;

            // 应用阻尼系数
            currentIntensity *= Mathf.Pow(clip.dampingFactor, frame - clip.startFrame);

            if (currentIntensity <= 0.001f) return; // 震动强度过小时跳过

            // 生成震动偏移
            Vector3 vibrationOffset = Vector3.zero;

            if (clip.randomizeDirection)
            {
                // 随机方向震动 - 使用震动频率控制随机更新速度
                float frequencyTime = frame * clip.vibrationFrequency * 0.1f;
                int frequencyFrame = Mathf.FloorToInt(frequencyTime);

                // 每隔一定帧数更新随机方向（基于频率）
                var frequencyRandom = new System.Random(seed ^ frequencyFrame);

                vibrationOffset = new Vector3(
                    (float)(frequencyRandom.NextDouble() * 2.0 - 1.0) * clip.vibrationDirection.x,
                    (float)(frequencyRandom.NextDouble() * 2.0 - 1.0) * clip.vibrationDirection.y,
                    (float)(frequencyRandom.NextDouble() * 2.0 - 1.0) * clip.vibrationDirection.z
                ) * currentIntensity;
            }
            else
            {
                // 固定方向震动（使用正弦波） - 使用震动频率控制震动速度
                float time = frame * clip.vibrationFrequency * 0.016f; // 0.016约等于1/60，模拟60fps的时间步长
                vibrationOffset = new Vector3(
                    Mathf.Sin(time) * clip.vibrationDirection.x,
                    Mathf.Sin(time * 1.1f) * clip.vibrationDirection.y, // 稍微不同的频率比例
                    Mathf.Sin(time * 0.9f) * clip.vibrationDirection.z  // 稍微不同的频率比例
                ) * currentIntensity;
            }

            // 应用震动到摄像机位置
            camera.transform.position += vibrationOffset;
        }

        /// <summary>
        /// 应用震动到摄像机（兼容旧接口）
        /// </summary>
        /// <param name="clip">摄像机片段</param>
        /// <param name="progress">震动进度 (0-1)</param>
        /// <param name="frame">当前帧</param>
        private void ApplyVibrationToCamera(CameraTrack.CameraClip clip, float progress, int frame)
        {
            ApplyVibrationToCamera(clip, targetCamera, progress, frame);
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            StopPreview();
            skillOwner = null;
            skillConfig = null;
            targetCamera = null;
            vibrationRandomStates.Clear();
            cameraOriginalStates.Clear();
            activeClips.Clear();
        }

        #endregion
    }
}
