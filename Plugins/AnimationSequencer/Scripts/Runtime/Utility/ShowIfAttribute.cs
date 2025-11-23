using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string Condition { get; }
        public object ComparisonValue { get; }

        /// <summary>
        /// The attribute controls the conditional display of properties in the Inspector.
        /// </summary>
        /// <param name="condition">The name of the property or expression that will be evaluated to determine if the associated property should be displayed.</param>
        /// <param name="comparisonValue">An optional value to compare the condition against. If not provided, 
        /// the property will be evaluated as true (for booleans) or not null (for objects).</param>
        public ShowIfAttribute(string condition, object comparisonValue = null)
        {
            Condition = condition;
            ComparisonValue = comparisonValue;
        }
    }
}
