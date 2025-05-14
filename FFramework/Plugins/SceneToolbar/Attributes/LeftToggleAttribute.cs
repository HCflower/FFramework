using System;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar.Attributes
{
    /// <summary> Decorates a bool field as a left toggle. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    sealed class LeftToggleAttribute : PropertyAttribute
    {
    }
}
