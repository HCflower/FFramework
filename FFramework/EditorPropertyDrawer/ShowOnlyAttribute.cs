using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
/// <summary>
/// Read Only attribute.
/// Attribute is use only to mark ReadOnly properties.
/// </summary>
public class ShowOnlyAttribute : PropertyAttribute { }