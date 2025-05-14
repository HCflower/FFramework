using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Plugins.Neonalig.SceneToolbar.Overlay;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable once RedundantNullableDirective
#nullable enable

namespace Plugins.Neonalig.SceneToolbar
{
    /// <summary> Contains the settings for the scene toolbar, for a given project. </summary>
    [Serializable]
    sealed class SceneToolbarSettingsPerProject
    {
        [SerializeField, Tooltip("The entries of the scene toolbar.")]
        private List<SceneEntry> entries = new();

        [SerializeField, Tooltip("The play/pause button method.")]
        private PlayButtonMethod playButtonMethod = PlayButtonMethod.PlayBuild;

        [SerializeField, Tooltip("Whether the dropdown opens the scene additively (appends to the current scene) or not (replaces the current scene).")]
        private bool openAdditive = false;

        [SerializeField, Tooltip("Whether to include scenes not present in the build settings when the dropdown is opened.")]
        private bool includeNotInBuild = false;

        /// <summary> Gets the entries of the scene toolbar. </summary>
        /// <returns> The entries of the scene toolbar. </returns>
        internal IReadOnlyList<SceneEntry> Entries
        {
            get => entries;
        }

        /// <summary> Gets or sets the play/pause button method. </summary>
        internal PlayButtonMethod PlayButtonMethod
        {
            get => playButtonMethod;
            set => playButtonMethod = value;
        }

        /// <summary>
        ///     Gets or sets whether the dropdown opens the scene additively (appends to the current scene) or not (replaces
        ///     the current scene).
        /// </summary>
        internal bool OpenAdditive
        {
            get => openAdditive;
            set => openAdditive = value;
        }

        /// <summary> Gets or sets whether to include scenes not present in the build settings when the dropdown is opened. </summary>
        internal bool IncludeNotInBuild
        {
            get => includeNotInBuild;
            set => includeNotInBuild = value;
        }

        private static bool SaveAsText
        {
            get => EditorSettings.serializationMode == SerializationMode.ForceText;
        }

        /// <summary> Gets the number of entries in the scene toolbar. </summary>
        /// <returns> The number of entries in the scene toolbar. </returns>
        internal int Count
        {
            get => entries.Count;
        }

        /// <summary> Gets all currently used tags. </summary>
        /// <returns> All currently used tags. </returns>
        internal IEnumerable<string> Tags
        {
            get => entries.SelectMany(entry => entry.Tags).Distinct();
        }

        /// <summary> Gets the entries of the scene toolbar that are marked as favorite. </summary>
        /// <returns> The entries of the scene toolbar that are marked as favorite. </returns>
        internal IEnumerable<SceneEntry> Favorites
        {
            get => entries.Where(entry => entry.IsFavorite);
        }

        /// <summary> Adds the specified entry to the scene toolbar. </summary>
        /// <param name="entry"> The entry to add. </param>
        internal void AddEntry(SceneEntry entry)
        {
            entries.Add(entry);
        }

        /// <summary> Removes the specified entry from the scene toolbar. </summary>
        /// <param name="entry"> The entry to remove. </param>
        /// <returns> <see langword="true" /> if the entry was removed; otherwise, <see langword="false" />. </returns>
        internal bool RemoveEntry(SceneEntry entry)
        {
            return entries.Remove(entry);
        }

        /// <summary> Gets the index of the specified entry. </summary>
        /// <param name="entry"> The entry. </param>
        /// <returns> The index of the specified entry. <c> -1 </c> if the entry is not found. </returns>
        internal int IndexOf(SceneEntry entry) => entries.IndexOf(entry);

        /// <summary> Gets the entry at the specified index. </summary>
        /// <param name="idx"> The index. </param>
        /// <returns> The entry at the specified index. </returns>
        /// <exception cref="System.IndexOutOfRangeException"> Thrown if the index is out of range. </exception>
        internal SceneEntry ElementAt(int idx) => entries[idx];

        /// <summary> Gets whether the specified scene is marked as favorite. </summary>
        /// <param name="path"> The path to the scene. </param>
        /// <returns> <see langword="true" /> if the specified scene is marked as favorite; otherwise, <see langword="false" />. </returns>
        internal bool GetIsFavorite([LocalizationRequired(false)] string path)
        {
            if (string.IsNullOrEmpty(path)) { return false; }
            return entries.Any(entry => entry.Path == path && entry.IsFavorite);
        }

        /// <summary> Sets whether the specified scene is marked as favorite. </summary>
        /// <param name="path"> The path to the scene. </param>
        /// <param name="isFavorite"> Whether the scene is marked as favorite. </param>
        internal void SetIsFavorite([LocalizationRequired(false)] string path, bool isFavorite)
        {
            Debug.Assert(!string.IsNullOrEmpty(path), "Path must not be null or empty.");
            SceneEntry? entry = entries.FirstOrDefault(entry => entry.Path == path);
            if (entry == null)
            {
                entry = new SceneEntry(path, isFavorite);
                entries.Add(entry);
            }
            else
            {
                entry.IsFavorite = isFavorite;
            }
        }
    }

}
