using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Plugins.Neonalig.SceneToolbar.Overlay;
using UnityEditor;
using UnityEngine;

// ReSharper disable once RedundantNullableDirective
#nullable enable

namespace Plugins.Neonalig.SceneToolbar
{
    /// <summary> Contains the settings for the scene toolbar. </summary>
    [FilePath(SettingsPath, FilePathAttribute.Location.PreferencesFolder)]
    sealed class SceneToolbarSettings : ScriptableSingleton<SceneToolbarSettings>
    {
        /// <summary> The path to the settings file. </summary>
        internal const string SettingsPath = "Neonalig/SceneToolbar/Settings.yaml";

        [Serializable]
        internal sealed class ProjectMapping
        {
            [SerializeField]
            internal string projectIdentifier = string.Empty;

            [SerializeField]
            internal SceneToolbarSettingsPerProject settings = new();

            [UsedImplicitly]
            public ProjectMapping()
            {
            }

            public ProjectMapping(string projectIdentifier)
            {
                this.projectIdentifier = projectIdentifier;
            }

            public string ProjectIdentifier => projectIdentifier;
            public SceneToolbarSettingsPerProject Settings => settings;
        }

        [SerializeField]
        internal List<ProjectMapping> projectMappings = new();

        internal static string GetProjectIdentifier()
        {
            string projectIdentifier = Application.cloudProjectId;
            if (string.IsNullOrEmpty(projectIdentifier))
            {
                projectIdentifier = Application.productName;
            }
            return projectIdentifier;
        }

        internal static SceneToolbarSettingsPerProject GetSettingsForProject(string projectIdentifier)
        {
            ProjectMapping? mapping = instance.projectMappings.FirstOrDefault(mapping => mapping.ProjectIdentifier == projectIdentifier);
            if (mapping == null)
            {
                mapping = new ProjectMapping(projectIdentifier);
                instance.projectMappings.Add(mapping);
            }

            return mapping.Settings;
        }

        internal static int GetIndexOfCurrentProject()
        {
            string projectIdentifier = GetProjectIdentifier();
            for (int i = 0; i < instance.projectMappings.Count; i++)
            {
                if (instance.projectMappings[i].ProjectIdentifier == projectIdentifier)
                {
                    return i;
                }
            }

            return -1;
        }

        internal static SceneToolbarSettingsPerProject GetSettingsForCurrentProject()
        {
            return GetSettingsForProject(GetProjectIdentifier());
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.Entries" />
        internal static IReadOnlyList<SceneEntry> Entries
        {
            get => GetSettingsForCurrentProject().Entries;
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.PlayButtonMethod" />
        internal static PlayButtonMethod PlayButtonMethod
        {
            get => GetSettingsForCurrentProject().PlayButtonMethod;
            set
            {
                SceneToolbarSettingsPerProject settings = GetSettingsForCurrentProject();
                if (settings.PlayButtonMethod == value) { return; }
                settings.PlayButtonMethod = value;
                Save();
            }
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.OpenAdditive" />
        internal static bool OpenAdditive
        {
            get => GetSettingsForCurrentProject().OpenAdditive;
            set
            {
                SceneToolbarSettingsPerProject settings = GetSettingsForCurrentProject();
                if (settings.OpenAdditive == value) { return; }
                settings.OpenAdditive = value;
                Save();
            }
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.IncludeNotInBuild" />
        internal static bool IncludeNotInBuild
        {
            get => GetSettingsForCurrentProject().IncludeNotInBuild;
            set
            {
                SceneToolbarSettingsPerProject settings = GetSettingsForCurrentProject();
                if (settings.IncludeNotInBuild == value) { return; }
                settings.IncludeNotInBuild = value;
                Save();
            }
        }

        private static bool SaveAsText
        {
            get => EditorSettings.serializationMode == SerializationMode.ForceText;
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.Count" />
        internal static int Count
        {
            get => Entries.Count;
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.Tags" />
        internal static IEnumerable<string> Tags
        {
            get => Entries.SelectMany(entry => entry.Tags).Distinct();
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.Favorites" />
        internal static IEnumerable<SceneEntry> Favorites
        {
            get => Entries.Where(entry => entry.IsFavorite);
        }

        /// <summary> Gets whether this is the first run of the plugin. </summary>
        /// <returns> <see langword="true" /> if this is the first run of the plugin; otherwise, <see langword="false" />. </returns>
        internal static bool IsFirstRun
        {
            get => !File.Exists(GetFilePath());
        }

        /// <inheritdoc cref="ScriptableSingleton{T}.Save" />
        internal static void Save() => instance.Save(SaveAsText);

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.AddEntry" />
        internal static void AddEntry(SceneEntry entry)
        {
            SceneToolbarSettingsPerProject settings = GetSettingsForCurrentProject();
            settings.AddEntry(entry);
            Save();
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.RemoveEntry" />
        internal static bool RemoveEntry(SceneEntry entry)
        {
            if (GetSettingsForCurrentProject().RemoveEntry(entry))
            {
                Save();
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.IndexOf" />
        internal static int IndexOf(SceneEntry entry) => GetSettingsForCurrentProject().IndexOf(entry);

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.ElementAt" />
        internal static SceneEntry ElementAt(int idx) => GetSettingsForCurrentProject().ElementAt(idx);

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.GetIsFavorite" />
        internal static bool GetIsFavorite([LocalizationRequired(false)] string path)
        {
            if (string.IsNullOrEmpty(path)) { return false; }
            return GetSettingsForCurrentProject().GetIsFavorite(path);
        }

        /// <inheritdoc cref="SceneToolbarSettingsPerProject.SetIsFavorite" />
        internal static void SetIsFavorite([LocalizationRequired(false)] string path, bool isFavorite)
        {
            GetSettingsForCurrentProject().SetIsFavorite(path, isFavorite);
            Save();
        }
    }
}
