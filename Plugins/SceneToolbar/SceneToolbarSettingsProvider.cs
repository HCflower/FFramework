using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once RedundantNullableDirective
#nullable enable

namespace Plugins.Neonalig.SceneToolbar
{
    /// <summary> The settings provider for the scene toolbar. </summary>
    sealed class SceneToolbarSettingsProvider : SettingsProvider
    {

        /// <summary> The path to the settings window. </summary>
        internal const string Path = "Preferences/Neonalig/Scene Toolbar";

        /// <summary> The scope of the settings window. </summary>
        internal const SettingsScope Scope = SettingsScope.User;

        /// <summary> The URL to the asset store page for the overlay. </summary>
        internal const string AssetStoreURL = "https://assetstore.unity.com/packages/slug/270768?aid=1011lvXjM";

        /// <summary> The URL to my website :) </summary>
        internal const string WebsiteURL = "https://neonalig.github.io";

        /// <summary> The serialized settings object. </summary>
        private SerializedObject? serialised;

        /// <inheritdoc />
        private SceneToolbarSettingsProvider() : base(Path, Scope) { }

        /// <summary> Creates the settings provider for the overlay. </summary>
        /// <returns> The settings provider for the overlay. </returns>
        [SettingsProvider]
        internal static SettingsProvider CreateSceneToolbarSettingsProvider()
        {
            SceneToolbarSettingsProvider provider = new()
            {
                keywords = GetSearchKeywordsFromSerializedObject(new SerializedObject(SceneToolbarSettings.instance)),
            };
            return provider;
        }

        /// <summary> Container class for the styles used by the settings window. </summary>
        internal static class Styles
        {
            /// <summary> The centered link label style. </summary>
            internal static readonly GUIStyle CenteredLinkLabel;

            /// <summary> The bold centered label style. </summary>
            internal static readonly GUIStyle BoldCenteredLabel;

            /// <summary> Initialises the styles used by the settings window. </summary>
            static Styles()
            {
                CenteredLinkLabel = new GUIStyle(EditorStyles.linkLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                };
                BoldCenteredLabel = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                };
            }
        }

        #region Overrides of SettingsProvider

        /// <inheritdoc />
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            serialised = new SerializedObject(SceneToolbarSettings.instance);
        }

        private bool allProjectsFoldout = false;

        /// <inheritdoc />
        public override void OnGUI(string searchContext)
        {
            EditorGUI.indentLevel++;

            if (serialised == null)
            {
                EditorGUILayout.HelpBox(nameof(SceneToolbarSettingsProvider) + "." + nameof(OnGUI) + ": " + nameof(serialised) + " is null!", MessageType.Error);
            }
            else
            {
                serialised.Update();

                // OLD: Show all properties.
                // SerializedProperty iterator = serialised.GetIterator();
                // bool enterChildren = true;
                // while (iterator.NextVisible(enterChildren))
                // {
                //     if (iterator.name == "m_Script") { continue; }
                //     enterChildren = false;
                //     EditorGUILayout.PropertyField(iterator, true);
                // }

                // NEW: Show only the settings for the current project.
                int thisProject = SceneToolbarSettings.GetIndexOfCurrentProject();
                SerializedProperty projectMappings = serialised.FindProperty(nameof(SceneToolbarSettings.projectMappings));
                int arraySize = projectMappings.arraySize;
                if (thisProject < arraySize && thisProject >= 0)
                {
                    SerializedProperty projectMapping = projectMappings.GetArrayElementAtIndex(thisProject);
                    SerializedProperty settings = projectMapping.FindPropertyRelative(nameof(SceneToolbarSettings.ProjectMapping.settings));

                    // EditorGUILayout.PropertyField(settings, true);
                    bool enterChildren = true;
                    SerializedProperty iterator = settings.Copy();
                    while (iterator.NextVisible(enterChildren))
                    {
                        if (iterator.name == "m_Script") { continue; }
                        enterChildren = false;
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
                else
                {
                    // If we have not yet created a settings object for this project, create one.
                    SceneToolbarSettings.GetSettingsForCurrentProject();
                    Repaint();
                }

                if (serialised.ApplyModifiedProperties())
                {
                    SceneToolbarSettings.Save();
                }

                EditorGUILayout.Space();
                allProjectsFoldout = EditorGUILayout.Foldout(allProjectsFoldout, "All Projects", true, EditorStyles.foldoutHeader);
                if (allProjectsFoldout)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < arraySize; i++)
                    {
                        SerializedProperty projectMapping = projectMappings.GetArrayElementAtIndex(i);
                        string projectIdentifier = projectMapping.FindPropertyRelative(nameof(SceneToolbarSettings.ProjectMapping.projectIdentifier)).stringValue;
                        Rect rect = EditorGUILayout.GetControlRect();
                        const float deleteButtonWidth = 20f;
                        Rect idRect = new(rect) { width = rect.width - deleteButtonWidth - EditorGUIUtility.standardVerticalSpacing };
                        Rect deleteRect = new(rect) { x = idRect.xMax + EditorGUIUtility.standardVerticalSpacing, width = deleteButtonWidth };

                        EditorGUI.BeginDisabledGroup(i == thisProject);
                        EditorGUI.LabelField(idRect, projectIdentifier);
                        if (GUI.Button(deleteRect, EditorGUIUtility.TrIconContent("Toolbar Minus"), EditorStyles.iconButton))
                        {
                            EditorApplication.delayCall += () =>
                            {
                                projectMappings.DeleteArrayElementAtIndex(i);
                                if (thisProject == i)
                                    SceneToolbarSettings.GetSettingsForCurrentProject();
                                if (serialised != null && serialised.ApplyModifiedProperties())
                                    SceneToolbarSettings.Save();
                            };
                            break;
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Open First Time Window", "Info"), EditorStyles.miniButton))
            {
                FirstTimeWindow.ShowAsUtility();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField(EditorGUIUtility.TrTextContentWithIcon(Overlay.SceneToolbar.PackageTitle, Overlay.SceneToolbar.IconName), Styles.BoldCenteredLabel);
                EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent($"Version {Overlay.SceneToolbar.Version}"), EditorStyles.centeredGreyMiniLabel);

                EditorGUILayout.Space();
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Created by Neonalig \u2764\ufe0f", tooltip: WebsiteURL), Styles.CenteredLinkLabel, GUILayout.ExpandWidth(true)))
                {
                    Application.OpenURL(WebsiteURL);
                }
                if (!string.IsNullOrEmpty(AssetStoreURL))
                {
                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("Leave a Review", tooltip: AssetStoreURL, iconName: "Asset Store"), EditorStyles.toolbarButton))
                    {
                        Application.OpenURL(AssetStoreURL);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

    }

    sealed class FirstTimeWindow : EditorWindow
    {
        private const string message =
            "{0} v{1} was successfully installed!\n\n"
            + "This overlay allows you to quickly swap between scenes with a single dropdown.\n\n"
            + "Favorite scenes, create scene groups, and more!\n\n"
            + "You can show and hide overlays using the \u22ee button in the top right of the SceneView.\n\n"
            + "You can also right-click on the overlay to access the settings, or head to Preferences and find it there.\n\n"
            + "Enjoy!\n\n"
            + "P.S. If you like this overlay, please consider leaving a review on the Asset Store. It really helps!";

        private void OnGUI()
        {
            GUILayout.Label(Overlay.SceneToolbar.PackageTitle, Styles.TitleLabel);
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label(string.Format(message, Overlay.SceneToolbar.PackageTitle, Overlay.SceneToolbar.Version), Styles.WordWrappedCenteredLabel);
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Open Settings", "Settings"), EditorStyles.miniButtonLeft))
                {
                    SettingsService.OpenUserPreferences(SceneToolbarSettingsProvider.Path);
                    Close();
                }

                const string AssetStoreURL = SceneToolbarSettingsProvider.AssetStoreURL;
                bool hasStoreLink = !string.IsNullOrEmpty(AssetStoreURL);
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("P4_CheckOutLocal"), hasStoreLink ? EditorStyles.miniButtonMid : EditorStyles.miniButtonRight))
                {
                    Close();
                }

                if (hasStoreLink)
                {
                    if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Open Asset Store", "Asset Store"), EditorStyles.miniButtonRight))
                    {
                        Application.OpenURL(AssetStoreURL);
                        Close();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        internal static void ShowAsUtility()
        {
            var window = GetWindow<FirstTimeWindow>(true, Overlay.SceneToolbar.PackageTitle, true);
            window.titleContent = EditorGUIUtility.TrTextContentWithIcon(Overlay.SceneToolbar.PackageTitle, Overlay.SceneToolbar.IconName);
            window.minSize = new Vector2(400f, 350f);
            window.maxSize = new Vector2(400f, 350f);
            window.ShowUtility();
        }

        /// <summary> Container class for the styles used by the first time window. </summary>
        private static class Styles
        {

            /// <summary> The word-wrapped centered label style. </summary>
            internal static readonly GUIStyle WordWrappedCenteredLabel;

            /// <summary> The title label style. </summary>
            internal static readonly GUIStyle TitleLabel;

            /// <summary> Initialises the styles used by the first time window. </summary>
            static Styles()
            {
                WordWrappedCenteredLabel = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    // alignment = TextAnchor.MiddleCenter
                };
                TitleLabel = new GUIStyle(BoldCenteredLabel)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleLeft,
                };
            }
            /// <inheritdoc cref="SceneToolbarSettingsProvider.Styles.BoldCenteredLabel" />
            private static GUIStyle BoldCenteredLabel
            {
                get => SceneToolbarSettingsProvider.Styles.BoldCenteredLabel;
            }
        }
    }
}
