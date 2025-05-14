using System;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar.Attributes
{
    /// <summary> Decorates an enum field as a toolbar picker. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    sealed class EnumToolbarAttribute : PropertyAttribute
    {
    }
}
