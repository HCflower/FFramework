using System;
using JetBrains.Annotations;
using Plugins.Neonalig.SceneToolbar.Attributes;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar
{
    /// <summary> A scene entry. </summary>
    [Serializable]
    internal sealed class SceneEntry
    {
        [SerializeField, Tooltip("The path to the scene."), LocalizationRequired(false), SceneDropdown]
        private string path;

        [SerializeField, Tooltip("Whether the scene is marked as favorite."), LeftToggle]
        private bool isFavorite = false;

        [SerializeField, Tooltip("The tags of the scene.")]
        private SceneTagCollection tags = new();

        /// <summary> Creates a new scene entry. </summary>
        /// <param name="path"> The path to the scene. </param>
        /// <param name="isFavorite"> Whether the scene is marked as favorite. </param>
        /// <param name="tags"> The tags of the scene. </param>
        internal SceneEntry([LocalizationRequired(false)] string path, bool isFavorite = false, params string[] tags)
        {
            this.path = path;
            this.isFavorite = isFavorite;
            foreach (string tag in tags)
            {
                this.tags.AddTag(tag);
            }
        }

        /// <summary> Gets or sets the path to the scene. </summary>
        [LocalizationRequired(false)]
        internal string Path
        {
            get => path;
            set => path = value;
        }

        /// <summary> Gets or sets whether the scene is marked as favorite. </summary>
        internal bool IsFavorite
        {
            get => isFavorite;
            set => isFavorite = value;
        }

        /// <summary> Gets the tag(s) of the scene. </summary>
        internal SceneTagCollection Tags
        {
            get => tags;
        }

        /// <summary> Gets whether the scene has the specified tag. </summary>
        /// <param name="tag"> The tag. </param>
        /// <returns> <see langword="true" /> if the scene has the specified tag; otherwise, <see langword="false" />. </returns>
        internal bool HasTag([LocalizationRequired(false)] string tag) => tags.HasTag(tag);

        /// <summary> Gets whether the scene has the specified tag(s). </summary>
        /// <param name="tags">
        ///     The tag(s). If multiple tags are specified, all must be present for this to return
        ///     <see langword="true" />.
        /// </param>
        /// <returns> <see langword="true" /> if the scene has all the specified tag(s); otherwise, <see langword="false" />. </returns>
        internal bool HasTags([LocalizationRequired(false)] params string[] tags) => this.tags.HasTags(tags);
    }
}