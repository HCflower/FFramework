using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable once RedundantNullableDirective
#nullable enable

namespace Plugins.Neonalig.SceneToolbar.Attributes
{
    [CustomPropertyDrawer(typeof(SceneTagCollection))]
    sealed class SceneTagCollectionDrawer : PropertyDrawer
    {

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent lbl) => base.GetPropertyHeight(prop, lbl);

        #region Overrides of PropertyDrawer

        /// <inheritdoc />
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent lbl)
        {
            lbl = EditorGUIUtility.TrTextContent(lbl.text, lbl.tooltip, "FilterByLabel");

            // Iterate properties as usual, but if we reach _Tags, we'll draw it differently.
            SerializedProperty iterator = prop.Copy();
            SerializedProperty end = prop.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                if (iterator.name == "m_Script") { continue; }
                enterChildren = false;
                if (iterator.name == "_Tags")
                {
                    DrawTags(ref pos, iterator, lbl);
                }
                else
                {
                    EditorGUI.PropertyField(pos, iterator, true);
                }
                pos.y += EditorGUI.GetPropertyHeight(iterator, true);
            }
        }

        #endregion

        private static void DrawTags(ref Rect pos, SerializedProperty prop, GUIContent lbl)
        {
            string propPath = prop.propertyPath; // Store the property path, to re-fetch the property later.

            string[] tags = IterateStrings(prop).ToArray();
            EditorUtilities.DrawChipBar(pos, lbl, tags, OnRemove, OnAdd, GetAvailableValues, GetLabel, GetMenu);

            void OnRemove(string tag, int idx)
            {
                SerializedProperty? freshProp = GetRefreshedProperty(prop.serializedObject, propPath);
                if (freshProp != null)
                {
                    freshProp.serializedObject.Update();

                    // Decrease the size of the array
                    freshProp.DeleteArrayElementAtIndex(idx);

                    freshProp.serializedObject.ApplyModifiedProperties();
                    freshProp.serializedObject.Update();
                }
                else
                {
                    Debug.LogError($"Failed to remove tag \"{tag}\" from property \"{propPath}\" (could not refresh property).");
                }
            }

            void OnAdd(string tag)
            {
                SerializedProperty? freshProp = GetRefreshedProperty(prop.serializedObject, propPath);
                if (freshProp != null)
                {
                    freshProp.serializedObject.Update();

                    // Increase the size of the array
                    freshProp.arraySize++;
                    int newIndex = freshProp.arraySize - 1;

                    // Set the value of the new element
                    SerializedProperty newElement = freshProp.GetArrayElementAtIndex(newIndex);
                    newElement.stringValue = tag;

                    freshProp.serializedObject.ApplyModifiedProperties();
                    freshProp.serializedObject.Update();
                }
                else
                {
                    Debug.LogError($"Failed to add tag \"{tag}\" to property \"{propPath}\" (could not refresh property).");
                }
            }

            IEnumerable<string> GetAvailableValues() => SceneToolbarSettings.Tags.Except(tags);
            GUIContent GetLabel(string tag) => EditorGUIUtility.TrTextContent(tag);

            GenericMenu GetMenu()
            {
                GenericMenu menu = new();
                menu.AddItem(EditorGUIUtility.TrTextContent("New Tag"), false, BeginTagCreation);
                if (GetAvailableValues().Any())
                {
                    menu.AddSeparator(string.Empty);
                }
                return menu;
            }

            void BeginTagCreation()
            {
                TagCreatorWindow.Show(OnCreate, tags);
                void OnCreate(string tag) => OnAdd(tag);
            }
        }

        private static SerializedProperty? GetRefreshedProperty(SerializedObject serializedObject, string propertyPath) =>
            serializedObject.FindProperty(propertyPath);

        private static IEnumerable<string> IterateStrings(SerializedProperty prop)
        {
            Debug.Assert(prop.isArray, $"{prop.name} ({prop.propertyPath}) is not an array; actual type: {prop.propertyType}");
            SerializedProperty iterator = prop.Copy();
            int ln = iterator.arraySize;

            for (int idx = 0; idx < ln; idx++)
            {
                SerializedProperty element = iterator.GetArrayElementAtIndex(idx);
                if (element.propertyType != SerializedPropertyType.String) { continue; }
                yield return element.stringValue;
            }
        }
    }

    [Icon(iconName)]
    sealed class TagCreatorWindow : EditorWindow
    {
        private const string iconName = "FilterByLabel";

        private const string tagFieldID = "TagField";

        private string[] currentTags = Array.Empty<string>();
        private Action<string> onCreate = _ => { };

        private bool shouldFocus, tagExistsWarning;

        private string tag = string.Empty;

        private void OnGUI()
        {
            if (shouldFocus)
            {
                shouldFocus = false;
                EditorApplication.update += SetFocus;
            }

            GUI.SetNextControlName(tagFieldID);
            EditorGUILayout.BeginHorizontal();
            {
                EditorUtilities.BeginLabelWidth(50f);
                tag = SceneTagCollection.Normalize(
                    EditorGUILayout.TextField(EditorGUIUtility.TrTextContent("Tag", "The tag to create."), tag)
                );
                EditorUtilities.EndLabelWidth();

                tagExistsWarning = currentTags.Contains(tag);
                if (tagExistsWarning)
                {
                    EditorGUILayout.LabelField(EditorGUIUtility.TrIconContent("console.warnicon.sml", "This tag is already in use."), GUILayout.Width(20f));
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                const float BtnW = 100f;
                EditorUtilities.BeginDisabled(tagExistsWarning);
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Create", "Create the tag."), GUILayout.Width(BtnW)))
                {
                    CreateTag(tag);
                }
                EditorUtilities.EndDisabled();
            }
            EditorGUILayout.EndHorizontal();

            HandleEnterKeyPress();
        }

        private static void SetFocus()
        {
            GUI.FocusControl(tagFieldID);
            EditorApplication.update -= SetFocus;
        }

        private void HandleEnterKeyPress()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (GUI.GetNameOfFocusedControl() == tagFieldID && !tagExistsWarning)
                {
                    CreateTag(tag);
                }
            }
        }

        private void CreateTag(string tag)
        {
            onCreate(tag);
            Close();
        }

        /// <summary> Shows the tag creator window. </summary>
        /// <param name="onCreate"> The callback to invoke when a tag is created. </param>
        /// <param name="currentTags"> The currently used tags. </param>
        public static void Show(Action<string> onCreate, IEnumerable<string> currentTags)
        {
            var window = CreateInstance<TagCreatorWindow>();
            window.onCreate = onCreate;
            window.currentTags = currentTags.ToArray();
            window.titleContent = EditorGUIUtility.TrTextContent("Create Tag", "Create a new tag.", iconName);

            Vector2 size = new(300f, EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing * 3f);
            window.minSize = size;
            window.maxSize = size;

            window.ShowModal();
        }
    }
}
