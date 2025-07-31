using UnityEngine;
using FFramework.Kit;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能播放器示例 - 展示如何使用 GetTrackDataAtFrame 方法
    /// </summary>
    public class SkillPlayerExample : MonoBehaviour
    {
        [Header("技能配置")]
        [Tooltip("要播放的技能配置")]
        public SkillConfig skillConfig;

        [Header("播放设置")]
        [Tooltip("当前播放帧")] public int currentFrame = 0;
        [Tooltip("自动播放")] public bool autoPlay = false;
        [Tooltip("播放速度(倍数)")] public float playSpeed = 1.0f;

        [Header("调试信息")]
        [Tooltip("显示当前帧数据")] public bool showFrameData = true;

        private float timer = 0f;
        private FrameTrackData currentFrameData;

        void Start()
        {
            if (skillConfig == null)
            {
                Debug.LogWarning("SkillPlayerExample: 请设置技能配置!");
                return;
            }

            // 获取第0帧的数据
            UpdateCurrentFrame();
        }

        void Update()
        {
            if (skillConfig == null) return;

            if (autoPlay)
            {
                // 自动播放
                timer += Time.deltaTime * playSpeed;
                float frameTime = 1.0f / skillConfig.frameRate;

                if (timer >= frameTime)
                {
                    timer = 0f;
                    currentFrame++;

                    // 循环播放
                    if (currentFrame >= skillConfig.maxFrames)
                    {
                        currentFrame = 0;
                    }

                    UpdateCurrentFrame();
                }
            }
        }

        /// <summary>
        /// 更新当前帧数据
        /// </summary>
        void UpdateCurrentFrame()
        {
            if (skillConfig == null) return;

            // 获取当前帧的轨道数据
            currentFrameData = skillConfig.GetTrackDataAtFrame(currentFrame);

            if (showFrameData)
            {
                Debug.Log($"Frame {currentFrame}: {currentFrameData}");
            }

            // 处理各种轨道数据
            ProcessFrameData(currentFrameData);
        }

        /// <summary>
        /// 处理帧数据
        /// </summary>
        void ProcessFrameData(FrameTrackData frameData)
        {
            // 处理动画片段
            foreach (var animClip in frameData.animationClips)
            {
                Debug.Log($"播放动画: {animClip.clipName} (Speed: {animClip.playSpeed})");
                // 在这里添加实际的动画播放逻辑
            }

            // 处理音效片段
            foreach (var audioClip in frameData.audioClips)
            {
                Debug.Log($"播放音效: {audioClip.clipName}");
                // 在这里添加实际的音效播放逻辑
            }

            // 处理特效片段
            foreach (var effectClip in frameData.effectClips)
            {
                Debug.Log($"播放特效: {effectClip.clipName}");
                // 在这里添加实际的特效播放逻辑
            }

            // 处理事件片段
            foreach (var eventClip in frameData.eventClips)
            {
                Debug.Log($"触发事件: {eventClip.clipName}");
                // 在这里添加实际的事件处理逻辑
            }

            // 处理变换片段
            foreach (var transformClip in frameData.transformClips)
            {
                Debug.Log($"应用变换: {transformClip.clipName}");
                // 在这里添加实际的变换应用逻辑
            }

            // 处理摄像机片段
            foreach (var cameraClip in frameData.cameraClips)
            {
                Debug.Log($"摄像机操作: {cameraClip.clipName}");
                // 在这里添加实际的摄像机控制逻辑
            }

            // 处理伤害检测片段
            foreach (var injuryClip in frameData.injuryDetectionClips)
            {
                Debug.Log($"伤害检测: {injuryClip.clipName}");
                // 在这里添加实际的伤害检测逻辑
            }

            // 处理游戏物体片段
            foreach (var gameObjectClip in frameData.gameObjectClips)
            {
                Debug.Log($"游戏物体操作: {gameObjectClip.clipName}");
                // 在这里添加实际的游戏物体操作逻辑
            }
        }

        #region 公共方法 - 用于外部控制

        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        public void JumpToFrame(int frame)
        {
            currentFrame = Mathf.Clamp(frame, 0, skillConfig ? skillConfig.maxFrames - 1 : 0);
            UpdateCurrentFrame();
        }

        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        public void JumpToTime(float time)
        {
            if (skillConfig == null) return;
            int frame = skillConfig.TimeToFrames(time);
            JumpToFrame(frame);
        }

        /// <summary>
        /// 开始播放
        /// </summary>
        public void Play()
        {
            autoPlay = true;
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            autoPlay = false;
        }

        /// <summary>
        /// 停止播放并重置到第0帧
        /// </summary>
        public void Stop()
        {
            autoPlay = false;
            JumpToFrame(0);
        }

        /// <summary>
        /// 获取指定帧范围内的数据预览
        /// </summary>
        public void PreviewFrameRange(int startFrame, int endFrame)
        {
            if (skillConfig == null) return;

            var frameDataList = skillConfig.GetTrackDataInRange(startFrame, endFrame);
            Debug.Log($"帧范围 {startFrame}-{endFrame} 内共有 {frameDataList.Count} 帧包含活跃片段:");

            foreach (var frameData in frameDataList)
            {
                Debug.Log($"  {frameData}");
            }
        }

        #endregion

        #region Unity Editor 辅助方法
#if UNITY_EDITOR
        [Header("编辑器调试")]
        [Tooltip("在编辑器中测试的帧数")] public int testFrame = 0;

        [ContextMenu("测试获取指定帧数据")]
        void TestGetFrameData()
        {
            if (skillConfig != null)
            {
                var frameData = skillConfig.GetTrackDataAtFrame(testFrame);
                Debug.Log($"测试帧 {testFrame}: {frameData}");
            }
        }

        [ContextMenu("预览前10帧数据")]
        void PreviewFirst10Frames()
        {
            PreviewFrameRange(0, 9);
        }
#endif
        #endregion
    }
}
