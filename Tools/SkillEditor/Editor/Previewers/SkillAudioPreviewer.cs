using System.Reflection;
using UnityEngine;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 音频播放工具类
    /// </summary>
    public static class EditorAudioUtility
    {
        private static MethodInfo playClipMethodInfo;
        private static MethodInfo stopAllClipMethodInfo;

        static EditorAudioUtility()
        {
            Assembly editorAssembly = typeof(UnityEditor.AudioImporter).Assembly;
            Type utilClassType = editorAssembly.GetType("UnityEditor.AudioUtil");
            // UnityEditor.AudioUtil
            playClipMethodInfo = utilClassType.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            stopAllClipMethodInfo = utilClassType.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
        }

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="start">0-1的播放进度</param>
        public static void PlayAudio(AudioClip clip, float start)
        {
            playClipMethodInfo?.Invoke(null, new object[] { clip, (int)(start * 50000), false });
        }

        /// <summary>
        /// 停止所有音频
        /// </summary>
        public static void StopAllAudio()
        {
            stopAllClipMethodInfo?.Invoke(null, null);
        }
    }

    /// <summary>
    /// 音频预览器类，负责在编辑器模式下预览音频
    /// 
    /// 修复说明：
    /// 1. OnPlay方法：只在点击音频片段开始帧时播放，避免在片段内任意位置都触发播放
    /// 2. OnAudioClipClicked方法：专门处理点击音频片段内部的播放逻辑
    /// 3. OnTimelineClicked方法：统一处理时间轴点击，区分是否点击在音频片段上
    /// 4. IsFrameInAudioClip方法：检查帧是否在音频片段范围内
    /// 
    /// 使用方法：
    /// - 点击空白时间轴：调用OnTimelineClicked(frame, false)
    /// - 点击音频片段：调用OnTimelineClicked(frame, true, audioClip, startFrame)
    /// </summary>
    public class SkillAudioPreviewer : System.IDisposable
    {
        #region 私有字段

        /// <summary>当前播放的音频剪辑</summary>
        private AudioClip currentClip;

        /// <summary>是否正在播放</summary>
        private bool isPlaying;

        /// <summary>是否处于预览激活状态</summary>
        private bool isPreviewActive;

        /// <summary>当前播放的起始时间进度</summary>
        private float currentStartProgress;

        #endregion

        #region 公共属性

        /// <summary>
        /// 是否正在预览
        /// </summary>
        public bool IsPreviewActive => isPreviewActive;

        /// <summary>
        /// 当前播放的音频剪辑
        /// </summary>
        public AudioClip CurrentClip => currentClip;

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始音频预览
        /// </summary>
        public void StartPreview()
        {
            isPreviewActive = true;
        }

        /// <summary>
        /// 停止音频预览
        /// </summary>
        public void StopPreview()
        {
            StopAudio();
            isPreviewActive = false;
        }

        /// <summary>
        /// 预览音频轨道的音频片段
        /// </summary>
        /// <param name="audioClip">要预览的音频剪辑</param>
        /// <param name="currentFrame">当前帧数</param>
        /// <param name="frameRate">帧率</param>
        /// <param name="audioStartFrame">音频片段开始帧（可选）</param>
        /// <param name="audioEndFrame">音频片段结束帧（可选）</param>
        public void PreviewAudio(AudioClip audioClip, int currentFrame, int frameRate = 60, int audioStartFrame = -1, int audioEndFrame = -1)
        {
            if (audioClip == null)
            {
                return;
            }

            if (!isPreviewActive)
            {
                return;
            }

            // 如果指定了音频片段的帧范围，检查当前帧是否在范围内
            if (audioStartFrame >= 0 && audioEndFrame >= 0)
            {
                if (currentFrame < audioStartFrame || currentFrame > audioEndFrame)
                {
                    // 当前帧超出音频片段区域，停止播放
                    if (isPlaying && currentClip == audioClip)
                    {
                        StopAudio();
                    }
                    return;
                }
            }

            // 计算音频播放进度
            float startProgress;

            // 如果指定了音频片段范围，则基于片段内的相对位置计算播放进度
            if (audioStartFrame >= 0 && audioEndFrame >= 0)
            {
                // 计算在音频片段内的相对位置
                float relativeFrame = currentFrame - audioStartFrame;
                float totalSegmentFrames = audioEndFrame - audioStartFrame;

                // 基于片段进度来计算音频播放进度
                float segmentProgress = relativeFrame / totalSegmentFrames;
                startProgress = Mathf.Clamp01(segmentProgress);
            }
            else
            {
                // 基于整个时间轴的位置计算播放进度（非片段模式）
                float currentTime = currentFrame / (float)frameRate;
                startProgress = Mathf.Clamp01(currentTime / audioClip.length);
            }

            // 特殊处理：如果当前帧是音频片段的第一帧，强制从头播放
            bool isFirstFrameOfSegment = (audioStartFrame >= 0 && currentFrame == audioStartFrame);

            // 如果是同一个音频剪辑且播放进度差异很小，不重新播放（除非是片段第一帧）
            if (currentClip == audioClip && isPlaying && !isFirstFrameOfSegment)
            {
                float progressDifference = Mathf.Abs(currentStartProgress - startProgress);
                if (progressDifference < 0.02f) // 缩小误差范围到2%，提高精确度
                {
                    return;
                }
            }

            // 播放音频
            PlayAudio(audioClip, startProgress);
            currentClip = audioClip;
            currentStartProgress = startProgress;
            isPlaying = true;
        }

        /// <summary>
        /// 预览指定帧的音频
        /// </summary>
        /// <param name="frame">帧数</param>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="frameRate">帧率</param>
        /// <param name="audioStartFrame">音频片段开始帧（可选）</param>
        /// <param name="audioEndFrame">音频片段结束帧（可选）</param>
        public void PreviewFrame(int frame, AudioClip audioClip = null, int frameRate = 60, int audioStartFrame = -1, int audioEndFrame = -1)
        {
            if (!isPreviewActive) return;

            if (audioClip != null)
            {
                PreviewAudio(audioClip, frame, frameRate, audioStartFrame, audioEndFrame);
            }
        }

        /// <summary>
        /// 基于时间范围预览音频
        /// </summary>
        /// <param name="audioClip">要预览的音频剪辑</param>
        /// <param name="currentFrame">当前帧数</param>
        /// <param name="audioStartTime">音频片段开始时间（秒）</param>
        /// <param name="audioEndTime">音频片段结束时间（秒）</param>
        /// <param name="frameRate">帧率</param>
        public void PreviewAudioWithTimeRange(AudioClip audioClip, int currentFrame, float audioStartTime, float audioEndTime, int frameRate = 60)
        {
            int audioStartFrame = Mathf.RoundToInt(audioStartTime * frameRate);
            int audioEndFrame = Mathf.RoundToInt(audioEndTime * frameRate);
            PreviewAudio(audioClip, currentFrame, frameRate, audioStartFrame, audioEndFrame);
        }

        /// <summary>
        /// 停止音频播放
        /// </summary>
        public void StopAudio()
        {
            if (isPlaying)
            {
#if UNITY_EDITOR
                EditorAudioUtility.StopAllAudio();
#endif
                isPlaying = false;
                currentClip = null;
                currentStartProgress = 0f;
            }
        }

        /// <summary>
        /// 检查指定帧是否在某个音频片段内
        /// </summary>
        /// <param name="frameIndex">帧索引</param>
        /// <param name="audioStartFrame">音频开始帧</param>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="frameRate">帧率</param>
        /// <returns>是否在音频片段内</returns>
        public bool IsFrameInAudioClip(int frameIndex, int audioStartFrame, AudioClip audioClip, int frameRate = 60)
        {
            if (audioClip == null) return false;

            int audioFrameCount = (int)(audioClip.length * frameRate);
            int audioEndFrame = audioStartFrame + audioFrameCount;

            return frameIndex >= audioStartFrame && frameIndex <= audioEndFrame;
        }

        /// <summary>
        /// 处理时间轴点击事件（区分是否点击在音频片段上）
        /// </summary>
        /// <param name="clickedFrame">点击的帧位置</param>
        /// <param name="isClickOnAudioClip">是否点击在音频片段上</param>
        /// <param name="targetAudioClip">目标音频剪辑（如果点击在音频片段上）</param>
        /// <param name="audioStartFrame">音频开始帧（如果点击在音频片段上）</param>
        /// <param name="frameRate">帧率</param>
        public void OnTimelineClicked(int clickedFrame, bool isClickOnAudioClip = false, AudioClip targetAudioClip = null, int audioStartFrame = -1, int frameRate = 60)
        {
            if (!isPreviewActive) return;

            if (isClickOnAudioClip && targetAudioClip != null && audioStartFrame >= 0)
            {
                // 点击在音频片段上，播放对应位置的音频
                OnAudioClipClicked(targetAudioClip, clickedFrame, audioStartFrame, frameRate);
            }
            else
            {
                // 点击在空白时间轴上，检查是否有音频需要播放
                OnPlay(clickedFrame);
            }
        }

        /// <summary>
        /// 检查音频是否在播放
        /// </summary>
        /// <returns>是否正在播放</returns>
        public bool IsPlaying()
        {
            return isPlaying;
        }

        /// <summary>
        /// 播放指定帧的音频
        /// </summary>
        /// <param name="startFrameIndex">开始帧索引</param>
        public void OnPlay(int startFrameIndex)
        {
            if (!isPreviewActive) return;

            if (SkillEditorData.CurrentSkillConfig?.trackContainer?.audioTrack?.audioTracks != null)
            {
                foreach (var audioTrack in SkillEditorData.CurrentSkillConfig.trackContainer.audioTrack.audioTracks)
                {
                    // 检查轨道是否启用
                    if (!audioTrack.isEnabled) continue;

                    if (audioTrack?.audioClips != null)
                    {
                        foreach (var skillAudioEvent in audioTrack.audioClips)
                        {
                            if (skillAudioEvent.clip == null) continue;

                            // 只有当点击的帧正好是音频片段的开始帧时才播放
                            if (skillAudioEvent.startFrame == startFrameIndex)
                            {
                                // 播放音频,重头开始
                                EditorAudioUtility.PlayAudio(skillAudioEvent.clip, 0);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 点击音频片段时的播放处理（用于直接点击音频片段）
        /// </summary>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="clickedFrame">点击的帧位置</param>
        /// <param name="audioStartFrame">音频片段开始帧</param>
        /// <param name="frameRate">帧率</param>
        public void OnAudioClipClicked(AudioClip audioClip, int clickedFrame, int audioStartFrame, int frameRate = 60)
        {
            if (!isPreviewActive || audioClip == null) return;

            // 计算点击位置在音频片段内的相对进度
            int relativeFrame = clickedFrame - audioStartFrame;
            int audioFrameCount = (int)(audioClip.length * frameRate);

            // 确保相对帧数在有效范围内
            relativeFrame = Mathf.Clamp(relativeFrame, 0, audioFrameCount);

            // 计算播放进度
            float playProgress = (float)relativeFrame / audioFrameCount;
            playProgress = Mathf.Clamp01(playProgress);

            // 播放音频
            PlayAudio(audioClip, playProgress);
            currentClip = audioClip;
            currentStartProgress = playProgress;
            isPlaying = true;

            Debug.Log($"点击音频片段播放：进度 {playProgress:F2}，相对帧 {relativeFrame}/{audioFrameCount}");
        }

        /// <summary>
        /// 播放时的帧更新
        /// </summary>
        /// <param name="frameIndex">当前帧索引</param>
        public void TickView(int frameIndex)
        {
            if (!isPreviewActive) return;

            // 如果是运行模式直接在经过起始帧时播放音频即可
            if (SkillEditorData.IsPlaying)
            {
                if (SkillEditorData.CurrentSkillConfig?.trackContainer?.audioTrack?.audioTracks != null)
                {
                    foreach (var audioTrack in SkillEditorData.CurrentSkillConfig.trackContainer.audioTrack.audioTracks)
                    {
                        // 检查轨道是否启用
                        if (!audioTrack.isEnabled) continue;

                        if (audioTrack?.audioClips != null)
                        {
                            foreach (var skillAudioEvent in audioTrack.audioClips)
                            {
                                if (skillAudioEvent.clip != null && skillAudioEvent.startFrame == frameIndex)
                                {
                                    // 播放音频,重头开始
                                    EditorAudioUtility.PlayAudio(skillAudioEvent.clip, 0);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="clip">音频剪辑</param>
        /// <param name="startProgress">开始播放的进度（0-1）</param>
        private void PlayAudio(AudioClip clip, float startProgress)
        {
            try
            {
#if UNITY_EDITOR
                // 先停止所有音频
                EditorAudioUtility.StopAllAudio();

                // 播放新音频
                EditorAudioUtility.PlayAudio(clip, startProgress);
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"播放音频失败: {e.Message}");
            }
        }

        #endregion

        #region IDisposable实现

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            StopPreview();
        }

        #endregion
    }
}
