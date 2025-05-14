using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar
{
    [Serializable]
    internal sealed class SceneTagCollection : IReadOnlyList<string>
    {
        [SerializeField, Tooltip("The tags."), LocalizationRequired(false)]
        private string[] tags = Array.Empty<string>();

        /// <summary> Gets the tags. </summary>
        [LocalizationRequired(false)]
        internal IReadOnlyList<string> Tags
        {
            get => tags;
        }

        #region Implementation of IReadOnlyCollection<out string>

        /// <inheritdoc />
        int IReadOnlyCollection<string>.Count
        {
            get => tags.Length;
        }

        #endregion

        #region Implementation of IReadOnlyList<out string>

        /// <inheritdoc />
        string IReadOnlyList<string>.this[int index]
        {
            get => tags[index];
        }

        #endregion

        /// <summary> Normalizes the specified tag. </summary>
        /// <param name="tag"> The tag. </param>
        /// <returns> The normalized tag. </returns>
        [Pure]
        internal static string Normalize([LocalizationRequired(false)] string tag)
        {
            // Tags must be in lower kebab-case.
            tag = tag.ToLowerInvariant();
            tag = tag.Replace(' ', '-');
            return tag;
        }

        /// <summary> Gets whether the specified tag is normalized. </summary>
        /// <param name="tag"> The tag. </param>
        /// <returns> <see langword="true" /> if the specified tag is normalized; otherwise, <see langword="false" />. </returns>
        internal static bool IsNormalized([LocalizationRequired(false)] string tag) => tag.Any(c => char.IsUpper(c) || char.IsWhiteSpace(c));

        /// <summary> Gets whether the scene has the specified tag. </summary>
        /// <param name="tag"> The tag. </param>
        /// <returns> <see langword="true" /> if the scene has the specified tag; otherwise, <see langword="false" />. </returns>
        internal bool HasTag([LocalizationRequired(false)] string tag)
        {
            Debug.Assert(!IsNormalized(tag), "Tag must be normalized before checking if it is present.");
            return Array.Exists(tags, entryTag => string.Equals(entryTag, tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary> Gets whether the scene has the specified tag(s). </summary>
        /// <param name="tags">
        ///     The tag(s). If multiple tags are specified, all must be present for this to return
        ///     <see langword="true" />.
        /// </param>
        /// <returns> <see langword="true" /> if the scene has all the specified tag(s); otherwise, <see langword="false" />. </returns>
        internal bool HasTags([LocalizationRequired(false)] params string[] tags)
        {
            foreach (string tag in tags)
            {
                if (!HasTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary> Adds the specified tag, if it is not already present. </summary>
        /// <param name="tag"> The tag to add. </param>
        /// <returns> <see langword="true" /> if the tag was added; otherwise, <see langword="false" />. </returns>
        internal bool AddTag([LocalizationRequired(false)] string tag)
        {
            Debug.Assert(!IsNormalized(tag), "Tag must be normalized before adding.");
            if (HasTag(tag))
            {
                return false;
            }

            Array.Resize(ref tags, tags.Length + 1);
            tags[^1] = tag;
            return true;
        }

        /// <summary> Removes the specified tag, if it is present. </summary>
        /// <param name="tag"> The tag to remove. </param>
        /// <returns> <see langword="true" /> if the tag was removed; otherwise, <see langword="false" />. </returns>
        internal bool RemoveTag([LocalizationRequired(false)] string tag)
        {
            Debug.Assert(!IsNormalized(tag), "Tag must be normalized before removing.");
            int idx = Array.FindIndex(tags, entryTag => string.Equals(entryTag, tag, StringComparison.OrdinalIgnoreCase));
            if (idx == -1)
            {
                return false;
            }

            Array.Copy(tags, idx + 1, tags, idx, tags.Length - idx - 1);
            Array.Resize(ref tags, tags.Length - 1);
            return true;
        }

        #region Implementation of IEnumerable

        /// <inheritdoc />
        // ReSharper disable once NotDisposedResourceIsReturned // Jutification: Weakly-typed GetEnumerator method is missing required method annotations.
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => ((IEnumerable<string>)tags).GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => tags.GetEnumerator();

        #endregion

    }
}
