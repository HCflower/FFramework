#if DOTWEEN_ENABLED
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.AnimationSequencer
{
    // Renamed by Pablo Huaxteco
    public sealed class TweenActionAdvancedDropdownItem : AdvancedDropdownItem
    {
        private Type baseTweenActionType;
        public Type BaseTweenActionType => baseTweenActionType;

        public TweenActionAdvancedDropdownItem(Type baseTweenActionType, string displayName) : base(displayName)
        {
            this.baseTweenActionType = baseTweenActionType;
        }
    }

    // Renamed by Pablo Huaxteco
    public sealed class TweenActionsAdvancedDropdown : AdvancedDropdown
    {
        private Action<TweenActionAdvancedDropdownItem> callback;
        private SerializedProperty actionsList;
        private GameObject targetGameObject;

        public TweenActionsAdvancedDropdown(AdvancedDropdownState state) : base(state)
        {
            this.minimumSize = new Vector2(200, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Tween Actions");

            foreach (var typeToDisplayGUI in AnimationSequencerEditorGUIUtility.TypeToDisplayName)
            {
                Type baseTweenActionType = typeToDisplayGUI.Key;

                AdvancedDropdownItem targetFolder = root;

                if (AnimationSequencerEditorGUIUtility.TypeToParentDisplay.TryGetValue(baseTweenActionType, out GUIContent parent))
                {
                    AdvancedDropdownItem item = targetFolder.children.FirstOrDefault(dropdownItem =>
                        dropdownItem.name.Equals(parent.text, StringComparison.Ordinal));

                    if (item == null)
                    {
                        item = new AdvancedDropdownItem(parent.text)
                        {
                            icon = (Texture2D)parent.image
                        };
                        targetFolder.AddChild(item);
                    }

                    targetFolder = item;
                }

                TweenActionAdvancedDropdownItem tweenActionAdvancedDropdownItem =
                    new TweenActionAdvancedDropdownItem(baseTweenActionType, typeToDisplayGUI.Value.text)
                    {
                        enabled = !IsTypeAlreadyInUse(actionsList, baseTweenActionType) && AnimationSequencerEditorGUIUtility.CanActionBeAppliedToTarget(baseTweenActionType, targetGameObject)
                    };

                if (typeToDisplayGUI.Value.image != null)
                {
                    tweenActionAdvancedDropdownItem.icon = (Texture2D)typeToDisplayGUI.Value.image;
                }

                targetFolder.AddChild(tweenActionAdvancedDropdownItem);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            try
            {
                base.ItemSelected(item);
                callback?.Invoke(item as TweenActionAdvancedDropdownItem);
            }
            catch (Exception ex)
            {
                Debug.Log($"Unexpected error when selected new action: {ex}");
            }
        }

        public void Show(Rect rect, SerializedProperty actionsListSerializedProperty, Object targetGameObject, Action<TweenActionAdvancedDropdownItem>
        onActionSelectedCallback)
        {
            callback = onActionSelectedCallback;
            this.actionsList = actionsListSerializedProperty;
            if (targetGameObject is GameObject target)
                this.targetGameObject = target;
            base.Show(rect);
        }

        public bool IsTypeAlreadyInUse(SerializedProperty actionsSerializedProperty, Type targetType)
        {
            if (string.IsNullOrEmpty(targetType.FullName))
                return false;

            for (int i = 0; i < actionsSerializedProperty.arraySize; i++)
            {
                SerializedProperty actionElement = actionsSerializedProperty.GetArrayElementAtIndex(i);
                if (actionElement.managedReferenceFullTypename.IndexOf(targetType.FullName, StringComparison.Ordinal) > -1)
                    return true;
            }

            return false;
        }
    }
}
#endif