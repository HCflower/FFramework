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
    /// <summary> The favorite scenes dropdown. </summary>
    [EditorToolbarElement(ID, typeof(SceneView))]
    sealed class FavoriteScenesDropdown : EditorToolbarDropdown
    {
        /// <summary> The ID. </summary>
        internal const string ID = SceneToolbar.ID + "." + nameof(FavoriteScenesDropdown);

        public FavoriteScenesDropdown()
        {
            text = string.Empty; //"Favorites";
            icon = GetIcon("Favorite Icon");
            clicked += ShowMenu;

            // Listen to scene changes.
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        }

        private static string CurrentScene
        {
            get
            {
                string current = SceneManager.GetActiveScene().path;
                return current;
            }
        }

        ~FavoriteScenesDropdown()
        {
            // Stop listening to scene changes.
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene current, Scene next) => UpdateIcon();

        private void UpdateIcon(bool requestRepaint = true)
        {
            icon = GetIcon(SceneToolbarSettings.GetIsFavorite(CurrentScene));
            if (requestRepaint)
                MarkDirtyRepaint();
        }

        private void UpdateView()
        {
            UpdateIcon(false);
            MarkDirtyRepaint();
        }

        private GenericMenu GetMenu()
        {
            GenericMenu menu = new();

            bool currentIsFavorite = SceneToolbarSettings.GetIsFavorite(CurrentScene);
            if (!currentIsFavorite)
            {
                menu.AddItem(
                    EditorGUIUtility.TrTextContent("Add to Favorites"),
                    false,
                    MakeCurrentFavorite
                );

                void MakeCurrentFavorite()
                {
                    SceneToolbarSettings.SetIsFavorite(CurrentScene, true);
                    UpdateView();
                }
            }
            else
            {
                menu.AddItem(
                    EditorGUIUtility.TrTextContent("Remove from Favorites"),
                    false,
                    MakeCurrentNotFavorite
                );

                void MakeCurrentNotFavorite()
                {
                    SceneToolbarSettings.SetIsFavorite(CurrentScene, false);
                    UpdateView();
                }
            }

            menu.AddItem(
                EditorGUIUtility.TrTextContent("Open Additive"), SceneToolbarSettings.OpenAdditive, () =>
                {
                    SceneToolbarSettings.OpenAdditive = !SceneToolbarSettings.OpenAdditive;
                    UpdateView();
                }
            );
            menu.AddSeparator(string.Empty);

            string[] paths = GetScenesInProject().ToArray();
            if (paths.Length == 0)
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("No scenes found."));
                return menu;
            }

            bool duplicateNames = paths.GroupBy(SysPath.GetFileNameWithoutExtension).Any(group => group.Count() > 1);

            ISet<string> activePaths = SceneDropdown.GetActiveScenes();
            int activeLn = activePaths.Count;

            foreach (string path in SceneToolbarSettings.Favorites.Select(entry => entry.Path))
            {
                string name = SysPath.GetFileNameWithoutExtension(path);
                if (duplicateNames)
                {
                    name = SysPath.GetFileName(path);
                }

                bool isActive = activePaths.Contains(path);
                GUIContent content = EditorGUIUtility.TrTextContent(name, path);

                menu.AddItem(content, isActive, OnClick);

                void OnClick()
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
                        {
                            return;
                        }
                    }

                    OpenScene(path);
                }
            }

            return menu;
        }

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

        private void ShowMenu() => GetMenu().ShowAsContext();

        private static IEnumerable<string> GetScenesInProject()
        {
            IEnumerable<string> paths = AssetDatabase.FindAssets("t:SceneAsset")
                .Select(AssetDatabase.GUIDToAssetPath);
            return paths;
        }

        private static Texture2D GetIcon([LocalizationRequired(false)] string name) =>
            (Texture2D)EditorGUIUtility.IconContent(name).image;

        private static Texture2D GetIcon(bool isFavorite) =>
            isFavorite
                ? GetIcon("Favorite On Icon")
                : GetIcon("Favorite Icon");
    }
}
