using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    public static class IconLoader
    {
        public static Texture2D LoadIcon(string iconName)
        {
            string iconPath = $"{GetEditorPath()}/Icons/{iconName}.png";

            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }

        public static string GetEditorPath()
        {
            string assemblyName = "BrunoMikoski.AnimationSequencer.Editor";
            string assemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
            string directoryPath = System.IO.Path.GetDirectoryName(assemblyPath);
            return directoryPath.Replace("\\", "/");
        }
    }
}