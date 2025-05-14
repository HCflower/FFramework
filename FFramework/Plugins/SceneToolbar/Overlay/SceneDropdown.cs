using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using SysPath = System.IO.Path;

namespace Plugins.Neonalig.SceneToolbar.Overlay
{
    /// <summary> The scene dropdown. </summary>
    [EditorToolbarElement(ID, typeof(SceneView))]
    sealed class SceneDropdown : EditorToolbarDropdown
    {
        /// <summary> The ID. </summary>
        internal const string ID = SceneToolbar.ID + "." + nameof(SceneDropdown);

        public SceneDropdown()
        {
            UpdateView();
            clicked += ShowMenu;

            // Listen to scene changes.
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        }

        ~SceneDropdown()
        {
            // Stop listening to scene changes.
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene current, Scene next) => UpdateView();

        private void UpdateLabel(bool requestRepaint = true)
        {
            text = SceneManager.GetActiveScene().name;
            if (requestRepaint)
                MarkDirtyRepaint();
        }

        private void UpdateIcon(bool requestRepaint = true)
        {
            icon = GetIcon("SceneAsset Icon");
            if (requestRepaint)
                MarkDirtyRepaint();
        }

        private void UpdateView()
        {
            UpdateLabel(false);
            UpdateIcon(false);
            MarkDirtyRepaint();
        }

        private GenericMenu GetMenu()
        {
            GenericMenu menu = new();
            menu.AddItem(
                EditorGUIUtility.TrTextContent("Open Additive"), SceneToolbarSettings.OpenAdditive, () =>
                {
                    SceneToolbarSettings.OpenAdditive = !SceneToolbarSettings.OpenAdditive;
                }
            );
            menu.AddItem(
                EditorGUIUtility.TrTextContent("Include not in Build"), SceneToolbarSettings.IncludeNotInBuild, () =>
                {
                    SceneToolbarSettings.IncludeNotInBuild = !SceneToolbarSettings.IncludeNotInBuild;
                }
            );
            menu.AddSeparator(string.Empty);

            string[] paths = GetScenes(SceneToolbarSettings.IncludeNotInBuild).ToArray();
            if (paths.Length == 0 || paths.All(string.IsNullOrEmpty))
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("No scenes found."));
                return menu;
            }

            bool duplicateNames = paths.GroupBy(SysPath.GetFileNameWithoutExtension).Any(group => group.Count() > 1);

            ISet<string> activePaths = GetActiveScenes();
            int activeLn = activePaths.Count;

            foreach (string path in paths)
            {
                bool isActive = activePaths.Contains(path);
                menu.AddItem(EditorUtilities.GetSceneLabel(path, duplicateNames), isActive, OnSelection);

                void OnSelection()
                {
                    if (isActive)
                    {
                        if (activeLn > 1)
                        {
                            if (SceneToolbarSettings.OpenAdditive)
                            {
                                CloseScene(path);
                                return;
                            }
                        }
                        else
                            return;
                    }

                    OpenScene(path);
                }
            }

            return menu;
        }

        internal static ISet<string> GetActiveScenes()
        {
            HashSet<string> paths = new();
            int ln = SceneManager.sceneCount;
            for (int I = 0; I < ln; I++)
            {
                Scene scene = SceneManager.GetSceneAt(I);
                if (!string.IsNullOrEmpty(scene.path))
                {
                    paths.Add(scene.path);
                }
            }
            return paths;
        }

        private void ShowMenu() => GetMenu().ShowAsContext();

        private void OpenScene(string path)
        {
            // If we aren't in play mode, and the current scene has changes, prompt the user to save.
            if (!EditorApplication.isPlaying && SceneManager.GetActiveScene().isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
            }

            EditorSceneManager.OpenScene(path, SceneToolbarSettings.OpenAdditive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            UpdateView();
        }

        private void CloseScene(string path)
        {
            EditorSceneManager.CloseScene(SceneManager.GetSceneByPath(path), true);
            UpdateView();
        }

        private static IEnumerable<string> GetScenes(bool includeNotInBuildSettings)
        {
            HashSet<string> paths = new();

            int buildScenes = SceneManager.sceneCountInBuildSettings;
            for (int I = 0; I < buildScenes; I++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(I);
                if (string.IsNullOrEmpty(path)) { continue; }
                paths.Add(path);
            }

            if (includeNotInBuildSettings)
            {
                SceneAsset[] assets = AssetDatabase.FindAssets("t:SceneAsset")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<SceneAsset>)
                    .ToArray();
                foreach (SceneAsset asset in assets)
                {
                    if (asset == null) { continue; }
                    string path = AssetDatabase.GetAssetPath(asset);
                    if (string.IsNullOrEmpty(path)) { continue; }
                    paths.Add(path);
                }
            }

            return paths;
        }

        private static Texture2D GetIcon([LocalizationRequired(false)] string name) => (Texture2D)EditorGUIUtility.IconContent(name).image;
    }
}
