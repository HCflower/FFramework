using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

namespace FFramework
{
    /// <summary>
    /// IOC依赖注入容器
    /// 用于管理单例对象
    /// </summary>
    public interface IIOCContainer
    {
        //简单注册类型
        void Register<T>();
        //注册为单例
        void RegisterInstance(object instance);
        //注册为泛型单例
        void RegisterInstance<T>(object instance);
        //注册依赖
        void Register<TBase, TConcrete>() where TConcrete : TBase;
        //获取实例
        T Get<T>();
        //依赖注入
        void Inject(object obj);
        //清理IOC
        void Clear();
    }

    //标记需要注入的字段
    public class IOCInjectAttribute : Attribute { }

    public class IOCContainer : IIOCContainer
    {
        //注册类型
        private HashSet<Type> registeredTypes = new HashSet<Type>();
        //实例字典
        private Dictionary<Type, object> instanceDic = new Dictionary<Type, object>();
        //依赖管理
        private Dictionary<Type, Type> dependenciesDic = new Dictionary<Type, Type>();

        public void Register<T>()
        {
            registeredTypes.Add(typeof(T));
        }

        public void RegisterInstance(object instance)
        {
            Type type = instance.GetType();
            if (!registeredTypes.Contains(type))
            {
                instanceDic.Add(type, instance);
            }
        }

        public void RegisterInstance<T>(object instance)
        {
            if (!instanceDic.ContainsKey(typeof(T)))
            {
                instanceDic.Add(typeof(T), instance);
            }
        }

        public void Register<TBase, TConcrete>() where TConcrete : TBase
        {
            Type baseType = typeof(TBase);
            Type concreteType = typeof(TConcrete);
            if (!dependenciesDic.ContainsKey(baseType))
            {
                dependenciesDic.Add(baseType, concreteType);
            }
        }

        private Object Get(Type type)
        {
            if (instanceDic.ContainsKey(type))
            {
                return instanceDic[type];
            }
            if (dependenciesDic.ContainsKey(type))
            {
                return Activator.CreateInstance(dependenciesDic[type]);
            }
            if (registeredTypes.Contains(type))
            {
                return Activator.CreateInstance(type);
            }
            return default;
        }

        public T Get<T>()
        {
            Type type = typeof(T);
            return (T)Get(type);
        }

        public void Inject(object obj)
        {
            foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties()
                    .Where(p => p.GetCustomAttributes(typeof(IOCInjectAttribute)).Any()))
            {
                Object instance = Get(propertyInfo.PropertyType);
                if (instance != null)
                {
                    propertyInfo.SetValue(obj, instance);
                }
                else
                {
                    UnityEngine.Debug.Log($"<color=red>未找到类型为{propertyInfo.PropertyType}的实例</color>");
                }
            }
        }

        public void Clear()
        {
            registeredTypes.Clear();
            instanceDic.Clear();
            dependenciesDic.Clear();
        }
    }
}
