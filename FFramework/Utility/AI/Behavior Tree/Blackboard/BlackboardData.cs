// =============================================================
// 描述：黑板数据
// 作者：HCFlower
// 创建时间：2025-11-15 18:49:00
// 版本：1.0.0
// =============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlackboardData", menuName = "FFramework/AI/Behavior Tree/Blackboard Data", order = 1)]
public class BlackboardData : ScriptableObject
{
    public List<BlackboardEntryData> entries = new();
    public void SetValuesOnBlackboard(Blackboard blackboard)
    {
        foreach (var entry in entries)
        {
            entry.SetValueOnBlackboard(blackboard);
        }
    }

    [System.Serializable]
    public class BlackboardEntryData : ISerializationCallbackReceiver
    {
        public string keyName;
        public AnyValue.ValueType valueType;
        public AnyValue value;

        // 设置黑板值
        public void SetValueOnBlackboard(Blackboard blackboard)
        {
            var key = blackboard.GetOrRegisterKey(keyName);
            setValueDispatchTable[value.type](blackboard, key, value);
        }

        // 类型分发表
        static Dictionary<AnyValue.ValueType, Action<Blackboard, BlackboardKey, AnyValue>> setValueDispatchTable = new()
        {
            { AnyValue.ValueType.Bool,      (blackboard, key, anyValue) => blackboard.SetValue<bool>(key, anyValue) },
            { AnyValue.ValueType.Int,       (blackboard, key, anyValue) => blackboard.SetValue<int>(key, anyValue) },
            { AnyValue.ValueType.Float,     (blackboard, key, anyValue) => blackboard.SetValue<float>(key, anyValue) },
            { AnyValue.ValueType.String,    (blackboard, key, anyValue) => blackboard.SetValue<string>(key, anyValue) },
            { AnyValue.ValueType.Vector3,   (blackboard, key, anyValue) => blackboard.SetValue<Vector3>(key, anyValue) },
            { AnyValue.ValueType.GameObject,(blackboard, key, anyValue) => blackboard.SetValue<GameObject>(key, anyValue) },
        };
        public void OnAfterDeserialize() => value.type = valueType;

        public void OnBeforeSerialize()
        {

        }
    }

    [Serializable]
    public struct AnyValue
    {
        public enum ValueType
        {
            Int,
            Float,
            String,
            Bool,
            Vector3,
            GameObject,
        }
        public ValueType type;

        // 序列化字段
        public int intValue;
        public float floatValue;
        public string stringValue;
        public bool boolValue;
        public Vector3 vector3Value;
        public GameObject gameObjectValue;
        // 隐式转换操作符
        public static implicit operator bool(AnyValue anyValue) => anyValue.ConvertValue<bool>();
        public static implicit operator int(AnyValue anyValue) => anyValue.ConvertValue<int>();
        public static implicit operator float(AnyValue anyValue) => anyValue.ConvertValue<float>();
        public static implicit operator string(AnyValue anyValue) => anyValue.ConvertValue<string>();
        public static implicit operator Vector3(AnyValue anyValue) => anyValue.ConvertValue<Vector3>();
        public static implicit operator GameObject(AnyValue anyValue) => anyValue.ConvertValue<GameObject>();

        T ConvertValue<T>()
        {
            return type switch
            {
                ValueType.Bool => AsType<T>(boolValue),
                ValueType.Int => AsType<T>(intValue),
                ValueType.Float => AsType<T>(floatValue),
                ValueType.String => AsType<T>(stringValue),
                ValueType.Vector3 => AsType<T>(vector3Value),
                ValueType.GameObject => AsType<T>(gameObjectValue),
                _ => default,
            };
        }

        T AsType<T>(object value) => value is T correctType ? correctType : default;
    }
}