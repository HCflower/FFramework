using System;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar.Attributes
{
    /// <summary> Decorates a string field as a scene dropdown. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    sealed class SceneDropdownAttribute : PropertyAttribute
    {
    }
}
