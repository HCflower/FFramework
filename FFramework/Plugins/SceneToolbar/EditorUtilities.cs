using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using SysPath = System.IO.Path;

// ReSharper disable once RedundantNullableDirective
#nullable enable

namespace Plugins.Neonalig.SceneToolbar
{
    static class EditorUtilities
    {

        private const float chipPadding = 4f;

        private static readonly Stack<bool> enabledStack = new();

        private static readonly Stack<float> labelWidthStack = new();
        private static GUIStyle ChipLabelStyle
        {
            get => EditorStyles.miniButtonLeft;
        }
        private static GUIStyle ChipDismissStyle
        {
            get => EditorStyles.miniButtonRight;
        }

        /// <summary> Draws a horizontally-aligned collection of buttons for the specified enum. </summary>
        /// <typeparam name="TEnum"> The type of the enum. </typeparam>
        /// <param name="r"> The rect to draw the buttons in. </param>
        /// <param name="lbl"> The label of the buttons. Value may be <see langword="null" />. </param>
        /// <param name="value"> The value of the enum. </param>
        /// <param name="changed"> [out] Whether the value was changed. </param>
        /// <returns> The new value of the enum. </returns>
        internal static TEnum DrawEnumToolbar<TEnum>(Rect r, GUIContent? lbl, TEnum value, out bool changed) where TEnum : Enum
        {
            bool hasLbl = lbl is not null;
            if (hasLbl)
            {
                r = EditorGUI.PrefixLabel(r, lbl);
            }

            int oldIdx = Enum<TEnum>.IndexOf(value);
            int newIdx = GUI.Toolbar(r, oldIdx, Enum<TEnum>.Labels /*, EditorStyles.miniButton*/);
            if (newIdx != oldIdx)
            {
                changed = true;
                return Enum<TEnum>.ElementAt(newIdx);
            }
            changed = false;
            return value;
        }
        /// <summary> Begins a GUI enabled group. </summary>
        /// <param name="enabled"> Whether the group should be enabled. </param>
        internal static void BeginEnabled(bool enabled = true)
        {
            enabledStack.Push(GUI.enabled);
            GUI.enabled = enabled;
        }
        /// <summary> Begins a GUI disabled group. </summary>
        /// <param name="disabled"> Whether the group should be disabled. </param>
        internal static void BeginDisabled(bool disabled = true) => BeginEnabled(!disabled);

        /// <summary> Ends a GUI enabled group. </summary>
        internal static void EndEnabled() => GUI.enabled = enabledStack.Pop();
        /// <summary> Ends a GUI disabled group. </summary>
        internal static void EndDisabled() => EndEnabled();

        /// <summary> Draws an object field with a dropdown menu. </summary>
        /// <typeparam name="TObject"> The type of the object. </typeparam>
        /// <param name="r"> The rect to draw the field in. </param>
        /// <param name="lbl"> The label of the field. Value may be <see langword="null" />. </param>
        /// <param name="value"> The value of the field. </param>
        /// <param name="onSelection"> The action to perform when an item is selected. </param>
        /// <param name="allowAssets"> Whether to allow assets to be selected. </param>
        /// <param name="allowSceneObjects"> Whether to allow scene objects to be selected. </param>
        internal static void DrawObjectFieldWithDropdown<TObject>(Rect r, GUIContent? lbl, TObject? value, Action<TObject?> onSelection, bool allowAssets = true, bool allowSceneObjects = true) where TObject : Object
        {
            DrawFieldWithDropdown(r, lbl, value, onSelection, DrawField, GetItems, GetObjectLabel, true);

            IEnumerable<TObject> GetItems()
            {
                if (allowAssets)
                {
                    foreach (TObject item in AssetDatabase.FindAssets($"t:{typeof(TObject).Name}").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<TObject>))
                    {
                        yield return item;
                    }
                }

                if (allowSceneObjects)
                {
                    foreach (TObject item in Resources.FindObjectsOfTypeAll<TObject>())
                    {
                        yield return item;
                    }
                }
            }

            TObject? DrawField(Rect r, TObject? v)
            {
                var newV = (TObject?)EditorGUI.ObjectField(r, v, typeof(TObject), allowSceneObjects);
                if (newV != v && newV != null && !allowAssets && AssetDatabase.Contains(newV))
                {
                    return null;
                }
                return newV;
            }
        }

        /// <summary> Gets the label for the specified object. </summary>
        /// <typeparam name="TObject"> The type of the object. </typeparam>
        /// <param name="item"> The item. </param>
        /// <returns> The label for the specified object. </returns>
        internal static GUIContent GetObjectLabel<TObject>(TObject item) where TObject : Object =>
            EditorGUIUtility.ObjectContent(item, typeof(TObject));

        /// <summary> Draws a scene field with a dropdown menu. </summary>
        /// <param name="r"> The rect to draw the field in. </param>
        /// <param name="lbl"> The label of the field. Value may be <see langword="null" />. </param>
        /// <param name="value"> The value of the field. </param>
        /// <param name="onSelection"> The action to perform when an item is selected. </param>
        internal static void DrawSceneFieldWithDropdown(Rect r, GUIContent? lbl, SceneAsset? value, Action<SceneAsset?> onSelection)
        {
            DrawFieldWithDropdown(r, lbl, value, onSelection, DrawField, GetItems, GetSceneLabel, true);

            IEnumerable<SceneAsset> GetItems()
            {
                foreach (string path in AssetDatabase.FindAssets("t:SceneAsset").Select(AssetDatabase.GUIDToAssetPath))
                {
                    yield return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                }
            }

            SceneAsset? DrawField(Rect r, SceneAsset? v) => (SceneAsset?)EditorGUI.ObjectField(r, v, typeof(SceneAsset), false);
        }

        /// <summary> Draws a scene field with a dropdown menu. </summary>
        /// <param name="r"> The rect to draw the field in. </param>
        /// <param name="lbl"> The label of the field. Value may be <see langword="null" />. </param>
        /// <param name="value"> The value of the field. </param>
        /// <param name="onSelection"> The action to perform when an item is selected. </param>
        internal static void DrawSceneFieldWithDropdown(Rect r, GUIContent? lbl, string value, Action<string> onSelection)
        {
            bool? duplicateNames = null;
            DrawFieldWithDropdown(r, lbl, value, onSelection!, DrawField!, GetItems, GetSceneLabel, true);

            IEnumerable<string> GetItems()
            {
                foreach (string path in AssetDatabase.FindAssets("t:SceneAsset").Select(AssetDatabase.GUIDToAssetPath))
                {
                    yield return path;
                }
            }

            static string DrawField(Rect r, string v) => DrawFilePathPicker(r, null, v, "unity", "Assets");

            bool DuplicateNames() => duplicateNames ??= GetItems().GroupBy(SysPath.GetFileNameWithoutExtension).Any(group => group.Count() > 1);

            GUIContent GetSceneLabel(string path)
            {
                GUIContent lbl = EditorUtilities.GetSceneLabel(path, DuplicateNames());
                return lbl;
            }
        }

        /// <summary> Gets the label for the specified scene. </summary>
        /// <param name="scene"> The scene. </param>
        /// <returns> The label for the specified scene. </returns>
        internal static GUIContent GetSceneLabel(SceneAsset scene) =>
            EditorGUIUtility.TrTextContentWithIcon(scene.name, AssetDatabase.GetAssetPath(scene), AssetPreview.GetMiniTypeThumbnail(typeof(SceneAsset)));

        /// <summary> Gets the label for the specified scene. </summary>
        /// <param name="path"> The path to the scene. </param>
        /// <param name="nameIsPath">
        ///     Whether the name of the scene is the path (<see langword="true" />) or the file name (
        ///     <see langword="false" />).
        /// </param>
        /// <returns> The label for the specified scene. </returns>
        internal static GUIContent GetSceneLabel(string path, bool nameIsPath = true) =>
            EditorGUIUtility.TrTextContentWithIcon(nameIsPath ? path : SysPath.GetFileNameWithoutExtension(path), path, AssetPreview.GetMiniTypeThumbnail(typeof(SceneAsset)));

        private static void DrawFieldWithDropdown<T>(
            Rect r,
            GUIContent? lbl,
            T? value,
            Action<T?> onSelection,
            Func<Rect, T?, T?> drawField,
            Func<IEnumerable<T>> getItems,
            Func<T, GUIContent> getLabel,
            bool allowNone = true,
            IEqualityComparer<T?>? comparer = null
        )
        {
            const float BtnWidth = 20f;
            Rect btnR = new(r.xMax - BtnWidth, r.y, BtnWidth, r.height);
            r.width -= BtnWidth;

            bool hasLbl = lbl is not null;
            if (hasLbl)
            {
                r = EditorGUI.PrefixLabel(r, lbl);
            }

            comparer ??= EqualityComparer<T?>.Default;

            T? newValue = drawField(r, value);
            if (!comparer.Equals(newValue, value))
            {
                onSelection(newValue);
            }

            if (GUI.Button(btnR, GUIContent.none, EditorStyles.popup))
            {
                GenericMenu menu = new();
                if (allowNone)
                {
                    menu.AddItem(EditorGUIUtility.TrTextContent("None"), value == null, () => onSelection(default(T)));
                    menu.AddSeparator(string.Empty);
                }

                foreach (T item in getItems())
                {
                    menu.AddItem(getLabel(item), comparer.Equals(item, value), () => onSelection(item));
                }

                menu.DropDown(btnR);
            }
        }

        /// <summary> Draws a file path picker. </summary>
        /// <param name="r"> The rect to draw the field in. </param>
        /// <param name="lbl"> The label of the field. Value may be <see langword="null" />. </param>
        /// <param name="value"> The value of the field. </param>
        /// <param name="extension"> The extension of the file. </param>
        /// <param name="location"> The location of the file. </param>
        /// <returns> The new value of the field. </returns>
        internal static string DrawFilePathPicker(Rect r, GUIContent? lbl, string value, string extension, string location)
        {
            bool hasLbl = lbl is not null;
            if (hasLbl)
            {
                r = EditorGUI.PrefixLabel(r, lbl);
            }

            const float BtnWd = 20f;
            Rect btnR = new(r.xMax - BtnWd, r.y, BtnWd, r.height);
            r.width -= BtnWd;

            string newValue = EditorGUI.TextField(r, value);
            if (GUI.Button(btnR, EditorGUIUtility.TrTextContent("..."), EditorStyles.miniButtonRight))
            {
                string path = EditorUtility.SaveFilePanel("Select File", location, string.Empty, extension);
                if (!string.IsNullOrEmpty(path))
                {
                    // Make path relative to project folder
                    path = path.Replace(Application.dataPath, "Assets");
                    if (path.StartsWith("Assets/"))
                    {
                        newValue = path;
                    }
                    else
                    {
                        Debug.LogWarning($"Path \"{path}\" is not in the project folder.");
                    }
                }
            }
            return newValue;
        }

        private static float CalculateChipWidth(GUIContent label) =>
            ChipLabelStyle.CalcSize(label).x + chipPadding * 2f
            + ChipDismissStyle.CalcSize(EditorGUIUtility.TrIconContent("Toolbar Minus")).x;

        /// <summary> Draws a chip. </summary>
        /// <param name="r"> The rect to draw the chip in. </param>
        /// <param name="label"> The label of the chip. </param>
        /// <returns> <see langword="true" /> if the chip remove button was clicked; otherwise, <see langword="false" />. </returns>
        internal static bool DrawChip(Rect r, GUIContent label)
        {
            const float BtnWd = 20f;
            Rect btnR = new(r.xMax - BtnWd, r.y, BtnWd - chipPadding, r.height);
            r.width -= BtnWd;

            BeginDisabled();
            GUI.Label(r, label, ChipLabelStyle);
            EndDisabled();
            return GUI.Button(btnR, EditorGUIUtility.TrIconContent("Toolbar Minus"), ChipDismissStyle);
        }

        /// <summary> Draws a chip bar. </summary>
        /// <typeparam name="T"> The type of the values. </typeparam>
        /// <param name="r"> The rect to draw the chip bar in. </param>
        /// <param name="lbl"> The label of the chip bar. Value may be <see langword="null" />. </param>
        /// <param name="values"> The values of the chip bar. </param>
        /// <param name="onRemove"> The action to perform when a chip is removed. </param>
        /// <param name="onAdd"> The action to perform when a chip is added. </param>
        /// <param name="getAvailableValues"> The function to get the available values for the chip bar. </param>
        /// <param name="getLabel"> The function to get the label for a value. </param>
        /// <param name="getMenu">
        ///     The function to generate a generic menu for the chip bar. Can be overridden to add custom menu
        ///     items.
        /// </param>
        /// <param name="comparer"> The comparer to use for the values. </param>
        internal static void DrawChipBar<T>(
            Rect r,
            GUIContent? lbl,
            IReadOnlyList<T> values,
            Action<T, int> onRemove,
            Action<T> onAdd,
            Func<IEnumerable<T>> getAvailableValues,
            Func<T, GUIContent> getLabel,
            Func<GenericMenu>? getMenu = null,
            IEqualityComparer<T>? comparer = null
        )
        {
            const float BtnWd = 20f;
            Rect btnR = new(r.xMax - BtnWd, r.y, BtnWd, r.height);
            r.width -= BtnWd;

            bool hasLbl = lbl is not null;
            if (hasLbl)
            {
                r = EditorGUI.PrefixLabel(r, lbl);
            }

            comparer ??= EqualityComparer<T>.Default;

            float[] widths = values.Select(getLabel).Select(CalculateChipWidth).ToArray();
            const float BetweenPadding = 2f;

            int idx = 0;
            foreach (T value in values)
            {
                float wd = widths[idx];
                Rect chipR = new(r.x, r.y, wd, r.height);
                if (DrawChip(chipR, getLabel(value)))
                {
                    onRemove(value, idx);
                }

                r.x += wd + BetweenPadding;
                idx++;
            }

            if (GUI.Button(btnR, EditorGUIUtility.TrIconContent("Toolbar Plus"), EditorStyles.iconButton))
            {
                GenericMenu menu = getMenu?.Invoke() ?? new GenericMenu();
                foreach (T value in getAvailableValues())
                {
                    if (values.Contains(value, comparer))
                    {
                        continue;
                    }

                    menu.AddItem(getLabel(value), false, () => onAdd(value));
                }
                menu.DropDown(btnR);
            }
        }
        /// <summary> Begins a label width group. </summary>
        /// <param name="f"> The label width. </param>
        internal static void BeginLabelWidth(float f)
        {
            labelWidthStack.Push(EditorGUIUtility.labelWidth);
            EditorGUIUtility.labelWidth = f;
        }

        /// <summary> Ends a label width group. </summary>
        internal static void EndLabelWidth() => EditorGUIUtility.labelWidth = labelWidthStack.Pop();

        private static class Enum<TEnum> where TEnum : Enum
        {
            /// <summary> The values of the enum. </summary>
            private static readonly TEnum[] values;

            /// <summary> The labels of the enum. </summary>
            // ReSharper disable once StaticMemberInGenericType
            internal static readonly GUIContent[] Labels; // Ideally we wouldn't expose an array here, and instead use IReadOnlyList<T>, but Unity editor methods only support arrays.

            static Enum()
            {
                Array enumValues = Enum.GetValues(typeof(TEnum));
                int ln = enumValues.Length;
                values = new TEnum[ln];
                Labels = new GUIContent[ln];
                for (int I = 0; I < ln; I++)
                {
                    // 1. Find member via reflection
                    string memberName = enumValues.GetValue(I).ToString();
                    MemberInfo member = typeof(TEnum).GetMember(memberName).FirstOrDefault() ?? throw new MissingMemberException(typeof(TEnum).Name, memberName);
                    // 2. Get any custom display attributes (InspectorName, Tooltip, etc.)
                    string name, tooltip;
                    Texture? icon;
                    if (member.GetCustomAttribute<InspectorNameAttribute>() is { } inspectorName)
                    {
                        name = inspectorName.displayName;
                    }
                    else
                    {
                        name = ObjectNames.NicifyVariableName(memberName);
                    }
                    if (member.GetCustomAttribute<TooltipAttribute>() is { } tooltipAttribute)
                    {
                        tooltip = tooltipAttribute.tooltip;
                    }
                    else
                    {
                        tooltip = string.Empty;
                    }
                    if (member.GetCustomAttribute<IconAttribute>() is { } iconAttribute)
                    {
                        string path = iconAttribute.path;
                        icon = AssetDatabase.LoadAssetAtPath<Texture>(path);
                        if (icon == null)
                        {
                            Debug.LogWarning($"Icon not found at path \"{path}\".");
                        }
                    }
                    else
                    {
                        icon = null;
                    }
                    // 3. Create GUIContent
                    values[I] = (TEnum)enumValues.GetValue(I);
                    Labels[I] = EditorGUIUtility.TrTextContentWithIcon(name, tooltip, icon);
                }
            }

            /// <summary> Gets the number of elements in the enum. </summary>
            [Pure]
            internal static int Count
            {
                get => values.Length;
            }

            /// <summary> Gets the index of the specified value. </summary>
            /// <param name="value"> The value. </param>
            /// <returns> The index of the specified value. <c> -1 </c> if the value is not found. </returns>
            [Pure]
            internal static int IndexOf(TEnum value) => Array.IndexOf(values, value);

            /// <summary> Gets the value at the specified index. </summary>
            /// <param name="idx"> The index. </param>
            /// <returns> The value at the specified index. </returns>
            /// <exception cref="IndexOutOfRangeException"> Thrown if the index is out of range. </exception>
            [Pure]
            internal static TEnum ElementAt(int idx) => values[idx];
        }
    }
}
