using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar.Overlay
{
    /// <summary> Provides an overlay for quick scene switching. </summary>
    [Overlay(typeof(SceneView), ID, DisplayTitle, "", true), Icon(IconName)]
    sealed class SceneToolbar : ToolbarOverlay
    {
        /// <summary> The unique ID of this overlay. </summary>
        internal const string ID = "Neonalig." + nameof(SceneToolbar);

        /// <summary> The display title of this overlay. </summary>
        internal const string DisplayTitle = "Scene Toolbar";

        /// <summary> The title of the package. </summary>
        internal const string PackageTitle = "Scene Toolbar";

        /// <summary> The icon name of this overlay. </summary>
        internal const string IconName = "UnityEditor.SceneHierarchyWindow";

        /// <summary> The version of this overlay. </summary>
        internal static readonly Version Version = new(1, 1);

        /// <summary> Creates a new instance of <see cref="SceneToolbar" />. </summary>
        private SceneToolbar() : base(
            PlayPauseButton.ID,
            SceneDropdown.ID,
            FavoriteScenesDropdown.ID
        )
        {
        }

        #region Overrides of Overlay

        /// <inheritdoc />
        public override void OnCreated()
        {
            base.OnCreated();
            if (SceneToolbarSettings.IsFirstRun)
            {
                SceneToolbarSettings.Save();
                FirstTimeWindow.ShowAsUtility();
            }
        }

        #endregion

    }
}
