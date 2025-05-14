using System.Reflection;
using System;

namespace FFramework
{
    /// <summary>
    /// 普通类单例基类
    /// </summary>
    public class Singleton<T> where T : Singleton<T>, new()
    {
        private static T mInstance;

        public static T Instance
        {
            get
            {
                if (mInstance == null)
                {
                    //反射获取
                    Type type = typeof(T);
                    //获取所有私有构造函数
                    ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
                    //获取无参构造函数
                    var ctor = Array.Find(ctors, c => c.GetParameters().Length == 0);

                    if (ctor == null) throw new Exception("Non-public parameterized constructor in " + type);

                    mInstance = ctor.Invoke(null) as T;
                }
                return mInstance;
            }
        }
    }
}
