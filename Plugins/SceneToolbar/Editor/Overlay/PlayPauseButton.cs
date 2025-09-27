using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Plugins.Neonalig.SceneToolbar.Overlay
{
    /// <summary> The play/pause button. </summary>
    [EditorToolbarElement(ID, typeof(SceneView))]
    sealed class PlayPauseButton : EditorToolbarButton
    {
        /// <summary> The ID. </summary>
        internal const string ID = SceneToolbar.ID + "." + nameof(PlayPauseButton);

        public PlayPauseButton()
        {
            UpdateIcon();
            clicked += OnClick;

            // Register context menu items.
            RegisterCallback<ContextClickEvent>(ContextMenuCallback);

            // Listen for changes that make us need to swap to play/pause icons.
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static int CurrentSceneIndex
        {
            get => SceneManager.GetActiveScene().buildIndex;
        }

        private static bool IsPlaying
        {
            get => EditorApplication.isPlaying;
        }

        private static void ContextMenuCallback(ContextClickEvent evt)
        {
            GenericMenu menu = new();
            menu.AddItem(EditorGUIUtility.TrTextContent("Play the First Scene in the Build Index"), SceneToolbarSettings.PlayButtonMethod == PlayButtonMethod.PlayBuild, PlayBuild_Clicked);
            menu.AddItem(EditorGUIUtility.TrTextContent("Play the Current Scene"), SceneToolbarSettings.PlayButtonMethod == PlayButtonMethod.PlayCurrent, PlayCurrent_Clicked);
            menu.ShowAsContext();

            void PlayBuild_Clicked() => SceneToolbarSettings.PlayButtonMethod = PlayButtonMethod.PlayBuild;
            void PlayCurrent_Clicked() => SceneToolbarSettings.PlayButtonMethod = PlayButtonMethod.PlayCurrent;
        }

        ~PlayPauseButton()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            UpdateIcon();
        }

        private static Texture2D GetIcon([LocalizationRequired(false)] string name) => (Texture2D)EditorGUIUtility.IconContent(name).image;

        private void UpdateIcon()
        {
            if (IsPlaying)
            {
                icon = GetIcon("PlayButton On");
                tooltip = "Stops the main scene.";
                MarkDirtyRepaint();
            }
            else
            {
                icon = GetIcon("PlayButton");
                tooltip = SceneToolbarSettings.PlayButtonMethod switch
                {
                    PlayButtonMethod.PlayBuild => "Plays the main scene.",
                    PlayButtonMethod.PlayCurrent => "Plays the current scene.",
                    _ => throw new NotImplementedException(),
                };
                MarkDirtyRepaint();
            }
        }

        private void OnClick()
        {
            if (IsPlaying)
            {
                Stop();
                return;
            }

            switch (SceneToolbarSettings.PlayButtonMethod)
            {
                case PlayButtonMethod.PlayBuild:
                    PlayBuild();
                    break;
                case PlayButtonMethod.PlayCurrent:
                    PlayCurrent();
                    break;
                default:
                    Debug.LogError("Unknown " + nameof(PlayButtonMethod) + " value: " + SceneToolbarSettings.PlayButtonMethod);
                    break;
            }
        }

        private void PlayBuild()
        {
            if (EditorBuildSettings.scenes.Length == 0)
            {
                PlayCurrent();
                return;
            }
            SwapToScene(0, true);
        }

        private void SwapToScene(int index, bool play, OpenSceneMode mode = OpenSceneMode.Single)
        {
            if (index < 0 || index >= EditorBuildSettings.scenes.Length)
            {
                Debug.LogError("Invalid scene index: " + index);
                return;
            }
            int currentIndex = CurrentSceneIndex;
            if (currentIndex != index)
            {
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[index].path, mode);
            }

            if (play)
            {
                PlayCurrent();
            }
        }

        private void PlayCurrent()
        {
            EditorApplication.isPlaying = true;
            UpdateIcon();
        }

        private void Stop()
        {
            EditorApplication.isPlaying = false;
            UpdateIcon();
        }
    }

    enum PlayButtonMethod
    {
        /// <summary> Plays the scene at build index 0. </summary>
        PlayBuild,
        /// <summary> Plays the currently open scene. </summary>
        PlayCurrent,
    }
}
