using FFramework.Kit;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 事件管理器
    /// </summary>
    public class SkillEditorEvent
    {
        public Action<int> OnCurrentFrameChanged;
        public Action<int> OnMaxFrameChanged;
        public Action<bool> OnGlobalControlToggled;
        public Action OnRefreshRequested;
        public Action<SkillConfig> OnSkillConfigChanged;
        public Action<bool> OnPlayStateChanged;
        public Action OnTimelineZoomChanged; // 添加缩放事件

        public void TriggerCurrentFrameChanged(int frame)
        {
            OnCurrentFrameChanged?.Invoke(frame);
        }

        public void TriggerMaxFrameChanged(int maxFrame)
        {
            OnMaxFrameChanged?.Invoke(maxFrame);
        }

        public void TriggerGlobalControlToggled(bool isShow)
        {
            OnGlobalControlToggled?.Invoke(isShow);
        }

        public void TriggerRefreshRequested()
        {
            OnRefreshRequested?.Invoke();
        }

        public void TriggerSkillConfigChanged(SkillConfig config)
        {
            OnSkillConfigChanged?.Invoke(config);
        }

        public void TriggerPlayStateChanged(bool isPlaying)
        {
            OnPlayStateChanged?.Invoke(isPlaying);
        }

        public void TriggerTimelineZoomChanged()
        {
            OnTimelineZoomChanged?.Invoke();
        }

        public void Cleanup()
        {
            OnCurrentFrameChanged = null;
            OnMaxFrameChanged = null;
            OnGlobalControlToggled = null;
            OnRefreshRequested = null;
            OnSkillConfigChanged = null;
            OnPlayStateChanged = null;
            OnTimelineZoomChanged = null; // 添加清理
        }
    }
}
