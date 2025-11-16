// =============================================================
// 描述：黑板系统
// 作者：HCFlower
// 创建时间：2025-11-15 18:49:00
// 版本：1.0.0
// =============================================================
using System.Collections.Generic;
using UnityEngine;
using System;

[SerializeField]
public readonly struct BlackboardKey : IEquatable<BlackboardKey>
{
    readonly string Name;
    readonly int hashedKey;
    public BlackboardKey(string name)
    {
        Name = name;
        hashedKey = name.ComputeFNV1Hash();
    }
    public bool Equals(BlackboardKey other) => hashedKey == other.hashedKey;
    public override bool Equals(object obj) => obj is BlackboardKey other && Equals(other);
    // 获取哈希键
    public override int GetHashCode() => hashedKey;
    // 获取名称
    public override string ToString() => Name;
    // == 
    public static bool operator ==(BlackboardKey left, BlackboardKey right) => left.hashedKey == right.hashedKey;
    // !=
    public static bool operator !=(BlackboardKey left, BlackboardKey right) => !(left == right);
}

[Serializable]
public class BlackboardEntry<T>
{
    public BlackboardKey Key { get; }
    public T Value { get; }
    public Type ValueType { get; }

    public BlackboardEntry(BlackboardKey key, T value)
    {
        Key = key;
        Value = value;
        ValueType = typeof(T);
    }

    public override bool Equals(object obj) => obj is BlackboardEntry<T> other && other.Key == Key;
    public override int GetHashCode() => Key.GetHashCode();
}

[Serializable]
public class Blackboard
{
    Dictionary<string, BlackboardKey> keyRegistry = new Dictionary<string, BlackboardKey>();

    Dictionary<BlackboardKey, object> entries = new Dictionary<BlackboardKey, object>();

    public List<Action> Preconditions { get; } = new List<Action>();

    public void AddAction(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action), "Action 不能为 null");
        Preconditions.Add(action);
    }

    public void ClearActions()
    {
        Preconditions.Clear();
    }

    // 尝试获取值
    public bool TryGetValue<T>(BlackboardKey key, out T value)
    {
        if (entries.TryGetValue(key, out var obj) && obj is BlackboardEntry<T> entry)
        {
            value = entry.Value;
            return true;
        }
        value = default;
        return false;
    }

    // 设置值 T泛型
    public void SetValue<T>(BlackboardKey key, T value)
    {
        var entry = new BlackboardEntry<T>(key, value);
        entries[key] = entry;
    }

    // 设置值
    public BlackboardKey GetOrRegisterKey(string keuName)
    {
        if (string.IsNullOrEmpty(keuName))
            throw new ArgumentException("Key name cannot be null or empty.", nameof(keuName));

        if (!keyRegistry.TryGetValue(keuName, out var key))
        {
            key = new BlackboardKey(keuName);
            keyRegistry.Add(keuName, key);
        }
        return key;
    }

    public bool ContainsKey(BlackboardKey key) => entries.ContainsKey(key);
    public void RemoveKey(BlackboardKey key) => entries.Remove(key);

    // 调试输出
    public void Debug()
    {
        foreach (var entry in entries)
        {
            var entryType = entry.Value.GetType();

            if (entryType.IsGenericType && entryType.GetGenericTypeDefinition() == typeof(BlackboardEntry<>))
            {
                var valueProperty = entryType.GetProperty("Value");
                if (valueProperty == null) continue;
                var value = valueProperty.GetValue(entry.Value);
                UnityEngine.Debug.Log($"Key: {entry.Key}, Value: {value}");
            }
        }
    }
}

/// <summary>
/// 字符串扩展方法,用于计算FNV-1哈希值
/// </summary>
public static class StringExtension
{
    public static int ComputeFNV1Hash(this string str)
    {
        uint hash = 2166136261;
        foreach (char c in str)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return unchecked((int)hash);
    }
}