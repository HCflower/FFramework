#if DOTWEEN_ENABLED
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    public static class AnimationSequencerEditorGUIUtility
    {
        private static Dictionary<Type, GUIContent> cachedTypeToDisplayName;
        public static Dictionary<Type, GUIContent> TypeToDisplayName
        {
            get
            {
                CacheDisplayTypes();
                return cachedTypeToDisplayName;
            }
        }
        
        private static Dictionary<Type, GUIContent> cachedTypeToInstance;
        public static Dictionary<Type, GUIContent> TypeToParentDisplay
        {
            get
            {
                CacheDisplayTypes();
                return cachedTypeToInstance;
            }
        }

        
        private static Dictionary<Type, TweenActionBase> typeToInstanceCache;
        public static Dictionary<Type, TweenActionBase> TypeToInstanceCache
        {
            get
            {
                CacheDisplayTypes();
                return typeToInstanceCache;
            }
        }
        
        private static TweenActionsAdvancedDropdown cachedTweenActionsDropdown;
        public static TweenActionsAdvancedDropdown TweenActionsDropdown
        {
            get
            {
                if (cachedTweenActionsDropdown == null)
                    cachedTweenActionsDropdown = new TweenActionsAdvancedDropdown(new AdvancedDropdownState());

                return cachedTweenActionsDropdown;
            }
        }
        

        public static GUIContent GetTypeDisplayName(Type targetBaseDOTweenType)
        {
            if (TypeToDisplayName.TryGetValue(targetBaseDOTweenType, out GUIContent result))
                return result;

            return new GUIContent(targetBaseDOTweenType.Name);
        }

        private static void CacheDisplayTypes()
        {
            if (cachedTypeToDisplayName != null)
                return;

            cachedTypeToDisplayName = new Dictionary<Type, GUIContent>();
            cachedTypeToInstance = new Dictionary<Type, GUIContent>();
            typeToInstanceCache = new Dictionary<Type, TweenActionBase>();
            
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom(typeof(TweenActionBase));
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                if (type.IsAbstract)
                    continue;
                
                TweenActionBase tweenActionBaseInstance = Activator.CreateInstance(type) as TweenActionBase;
                if (tweenActionBaseInstance == null)
                    continue;
                GUIContent guiContent = new GUIContent(tweenActionBaseInstance.DisplayName);
                if (tweenActionBaseInstance.TargetComponentType != null)
                {
                    GUIContent targetComponentGUIContent = EditorGUIUtility.ObjectContent(null, tweenActionBaseInstance.TargetComponentType);
                    guiContent.image = targetComponentGUIContent.image;
                    GUIContent parentGUIContent = new GUIContent(tweenActionBaseInstance.TargetComponentType.Name)
                    {
                        image = targetComponentGUIContent.image
                    };
                    cachedTypeToInstance.Add(type, parentGUIContent);
                }
                
                cachedTypeToDisplayName.Add(type, guiContent);
                typeToInstanceCache.Add(type, tweenActionBaseInstance);
            }
        }
        
        public static bool CanActionBeAppliedToTarget(Type targetActionType, GameObject targetGameObject)
        {
            if (targetGameObject == null)
                return false;

            if (TypeToInstanceCache.TryGetValue(targetActionType, out TweenActionBase actionBaseInstance))
            {
                Type requiredComponent = actionBaseInstance.TargetComponentType;
                
                if (requiredComponent == typeof(Transform))
                    return true;
                    
                if (requiredComponent == typeof(RectTransform))
                    return targetGameObject.transform is RectTransform;

                return targetGameObject.GetComponent(requiredComponent) != null;
            }
            return false;
        }

        private static GUIContent cachedRewindButtonGUIContent;
        internal static GUIContent RewindButtonGUIContent
        {
            get
            {
                if (cachedRewindButtonGUIContent == null)
                {
                    cachedRewindButtonGUIContent = EditorGUIUtility.IconContent("Animation.FirstKey");
                    cachedRewindButtonGUIContent.tooltip = "Rewind";
                }

                return cachedRewindButtonGUIContent;
            }
        }
        
        private static GUIContent cachedStepBackGUIContent;
        internal static GUIContent StepBackGUIContent
        {
            get
            {
                if (cachedStepBackGUIContent == null)
                {
                    cachedStepBackGUIContent = EditorGUIUtility.IconContent("Animation.PrevKey");
                    cachedStepBackGUIContent.tooltip = "Step Back";
                }

                return cachedStepBackGUIContent;
            }
        }
        
        private static GUIContent cachedStepNextGUIContent;
        internal static GUIContent StepNextGUIContent
        {
            get
            {
                if (cachedStepNextGUIContent == null)
                {
                    cachedStepNextGUIContent = EditorGUIUtility.IconContent("Animation.NextKey");
                    cachedStepNextGUIContent.tooltip = "Step Next";
                }

                return cachedStepNextGUIContent;
            }
        }
        
        private static GUIContent cachedStopButtonGUIContent;
        internal static GUIContent StopButtonGUIContent
        {
            get
            {
                if (cachedStopButtonGUIContent == null)
                {
                    cachedStopButtonGUIContent = EditorGUIUtility.IconContent("animationdopesheetkeyframe");
                    cachedStopButtonGUIContent.tooltip = "Stop";
                }
                return cachedStopButtonGUIContent;
            }
        }
        
        private static GUIContent cachedForwardButtonGUIContent;
        internal static GUIContent ForwardButtonGUIContent
        {
            get
            {
                if (cachedForwardButtonGUIContent == null)
                {
                    cachedForwardButtonGUIContent = EditorGUIUtility.IconContent("Animation.LastKey");
                    cachedForwardButtonGUIContent.tooltip = "Fast Forward";
                }
                return cachedForwardButtonGUIContent;
            }
        }
        
        private static GUIContent cachedPauseButtonGUIContent;
        internal static GUIContent PauseButtonGUIContent
        {
            get
            {
                if (cachedPauseButtonGUIContent == null)
                {
                    cachedPauseButtonGUIContent = EditorGUIUtility.IconContent("PauseButton");
                    cachedPauseButtonGUIContent.tooltip = "Pause";
                }
                return cachedPauseButtonGUIContent;
            }
        }
        
        private static GUIContent cachedPlayForwardButtonGUIContent;
        internal static GUIContent PlayForwardButtonGUIContent
        {
            get
            {
                if (cachedPlayForwardButtonGUIContent == null)
                {
                    cachedPlayForwardButtonGUIContent = EditorGUIUtility.IconContent("Animation.Play");
                    cachedPlayForwardButtonGUIContent.tooltip = "Play Forward";
                }
                return cachedPlayForwardButtonGUIContent;
            }
        }

        private static GUIContent cachedPlayBackwardsButtonGUIContent;
        internal static GUIContent PlayBackwardsButtonGUIContent
        {
            get
            {
                if (cachedPlayBackwardsButtonGUIContent == null)
                {
                    string iconName = $"{(EditorGUIUtility.isProSkin ? "d_" : "")}PlayBackward";
                    cachedPlayBackwardsButtonGUIContent = new GUIContent(IconLoader.LoadIcon(iconName), "Play Backwards");
                }
                return cachedPlayBackwardsButtonGUIContent;
            }
        }

        private static GUIContent cachedSaveAsDefaultGUIContent;
        internal static GUIContent SaveAsDefaultButtonGUIContent
        {
            get
            {
                if (cachedSaveAsDefaultGUIContent == null)
                {
                    cachedSaveAsDefaultGUIContent = EditorGUIUtility.IconContent("d_SaveAs");
                    cachedSaveAsDefaultGUIContent.tooltip = "Save as Default";
                }
                return cachedSaveAsDefaultGUIContent;
            }
        }
    }
}
#endif