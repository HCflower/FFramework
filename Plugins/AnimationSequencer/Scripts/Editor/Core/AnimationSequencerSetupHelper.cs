using System;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [InitializeOnLoad]
    public static class AnimationSequencerSetupHelper
    {
        private static string SCRIPTING_DEFINE_SYMBOL = "DOTWEEN_ENABLED";
        private static string DOTWEEN_ASSEMBLY_NAME = "DOTween.Modules";

        static AnimationSequencerSetupHelper()
        {
            Assembly[] availableAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);

            bool foundDOTween = false;
            for (int i = availableAssemblies.Length - 1; i >= 0; i--)
            {
                if (availableAssemblies[i].name.IndexOf(DOTWEEN_ASSEMBLY_NAME, StringComparison.Ordinal) > -1)
                {
                    foundDOTween = true;
                    break;
                }
            }

            if (foundDOTween)
            {
                AddScriptingDefineSymbol();
            }
            else
            {
                RemoveScriptingDefineSymbol();
                Debug.LogWarning("No DOTween found, Animation Sequencer will be disabled until DOTween setup is complete and asmdef files are created.");
            }
        }

        private static void AddScriptingDefineSymbol()
        {
            string scriptingDefineSymbols = GetScriptingDefineSymbols();
            if (scriptingDefineSymbols.Contains(SCRIPTING_DEFINE_SYMBOL))
                return;

            SetScriptingDefineSymbols($"{scriptingDefineSymbols};{SCRIPTING_DEFINE_SYMBOL}");

            Debug.Log($"Adding {SCRIPTING_DEFINE_SYMBOL} for {EditorUserBuildSettings.selectedBuildTargetGroup}");
        }

        private static void RemoveScriptingDefineSymbol()
        {
            string scriptingDefineSymbols = GetScriptingDefineSymbols();
            if (!scriptingDefineSymbols.Contains(SCRIPTING_DEFINE_SYMBOL))
                return;

            scriptingDefineSymbols = scriptingDefineSymbols.Replace(SCRIPTING_DEFINE_SYMBOL, string.Empty);
            SetScriptingDefineSymbols(scriptingDefineSymbols);
        }

        private static string GetScriptingDefineSymbols()
        {
#if UNITY_2022_1_OR_NEWER
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            return PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);     
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
        }

        private static void SetScriptingDefineSymbols(string scriptingDefineSymbols)
        {
#if UNITY_2022_1_OR_NEWER
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, scriptingDefineSymbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, scriptingDefineSymbols);
#endif
        }
    }
}
