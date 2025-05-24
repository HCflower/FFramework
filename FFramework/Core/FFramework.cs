using System.Collections.Generic;
using System;

namespace FFramework
{
    #region Core架构层
    /// <summary>
    /// 架构接口
    /// </summary>
    public interface IArchitecture
    {
        //注册Model到IOC容器中
        public void RegisterModel<T>(T model) where T : IModel;

        //注册Utility到IOC容器中
        public void RegisterUtility<T>(T utility) where T : IUtility;

        //注册系统到IOC容器中
        public void RegisterSystem<T>(T system) where T : ISystem;

        //获取System
        public T GetSystem<T>() where T : class, ISystem;

        //获取Model
        public T GetModel<T>() where T : class, IModel;

        //获取Utility
        public T GetUtility<T>() where T : class, IUtility;

        //发送命令
        public void SendCommand<T>() where T : ICommand, new();

        //发送具体命令
        public void SendCommand<T>(T command) where T : ICommand;

        //发送事件
        public void SendEvent<T>() where T : new();

        //发送具体事件
        public void SendEvent<T>(T Event);

        //注册事件
        public IRemoveListener RegisterEvent<T>(Action<T> onEvent);

        //注销事件
        public void UnRegisterEvent<T>(Action<T> onEvent);

        //发射查询
        T SendQuery<T>(IQuery<T> query);
    }

    /// <summary>
    /// 架构基类
    /// </summary>
    public abstract class Architecture<A> : IArchitecture where A : Architecture<A>, new()
    {
        private static A architecture;
        public static IArchitecture Interface
        {
            get
            {
                if (architecture == null)
                {
                    MakeSureArchitecture();
                }
                return architecture;
            }
        }
        //是否初始化
        private bool isInit = false;
        //模块注册之后,初始化之前调用
        public static Action<A> OnRegisterPatch = architecture => { };
        //IOC容器
        private IOCContainer mIOCContainer = new IOCContainer();
        //Model - 数据列表
        private List<IModel> modelList = new List<IModel>();
        //System - 系统列表
        private List<ISystem> systemList = new List<ISystem>();
        //事件系统             
        private EventSystem typeEventSystem = new EventSystem();

        private static void MakeSureArchitecture()
        {
            if (architecture == null)
            {
                architecture = new A();
                architecture.Init();
                OnRegisterPatch?.Invoke(architecture);

                //初始化所有Model - 数据优先初始化
                foreach (IModel model in architecture.modelList)
                {
                    model.Init();
                }
                //清理Model列表
                architecture.modelList.Clear();

                //初始化所有System
                foreach (ISystem system in architecture.systemList)
                {
                    system.Init();
                }
                //清理System列表
                architecture.systemList.Clear();

                //初始化完成
                architecture.isInit = true;
            }
        }

        /// <summary>
        /// 获取IOC容器中的实例
        /// T -> 实例类型 
        /// </summary>
        public static T Get<T>() where T : class
        {
            MakeSureArchitecture();
            return architecture.mIOCContainer.Get<T>();
        }

        /// <summary>
        /// 注册实例到IOC容器中
        /// T -> 实例类型 
        /// </summary>
        public static void Register<T>(T instance)
        {
            MakeSureArchitecture();
            architecture.mIOCContainer.RegisterInstance<T>(instance);
        }

        /// <summary>
        /// 注册Model到IOC容器中
        /// T -> Model类型
        /// </summary>
        public void RegisterModel<T>(T model) where T : IModel
        {
            model.SetArchitecture(this);
            mIOCContainer.RegisterInstance<T>(model);
            //缓存Model
            if (!isInit)
                modelList.Add(model);
            else
                model.Init();
        }

        /// <summary>
        /// 注册System到IOC容器中
        /// T -> System类型
        /// </summary>
        public void RegisterSystem<T>(T system) where T : ISystem
        {
            system.SetArchitecture(this);
            mIOCContainer.RegisterInstance<T>(system);
            //缓存Model
            if (!isInit)
                systemList.Add(system);
            else
                system.Init();
        }

        /// <summary>
        /// 获取System
        /// T -> System类型
        /// </summary>
        public T GetSystem<T>() where T : class, ISystem
        {
            return mIOCContainer.Get<T>();
        }

        /// <summary>
        /// 注册Utility到IOC容器中
        /// T -> Utility类型
        /// </summary>
        public void RegisterUtility<T>(T utility) where T : IUtility
        {
            mIOCContainer.RegisterInstance<T>(utility);
        }

        /// <summary>
        /// 获取Model数据
        /// T -> Model类型 
        /// </summary>
        public T GetModel<T>() where T : class, IModel
        {
            return mIOCContainer.Get<T>();
        }

        /// <summary>
        /// 获取工具类
        /// T -> 工具类类型 
        /// </summary>
        public T GetUtility<T>() where T : class, IUtility
        {
            return mIOCContainer.Get<T>();
        }

        ///<summary>
        /// 发送命令
        /// </summary>
        public void SendCommand<T>() where T : ICommand, new()
        {
            T command = new T();
            command.SetArchitecture(this);
            command.Execute();
        }

        /// <summary>
        /// 发送具体命令
        /// T -> 命令类型
        /// </summary>
        public void SendCommand<T>(T command) where T : ICommand
        {
            command.SetArchitecture(this);
            command.Execute();
        }

        /// <summary>
        /// 发送事件
        /// T -> 事件类型
        /// </summary>
        public void SendEvent<T>() where T : new()
        {
            typeEventSystem.Send<T>();
        }

        /// <summary>
        /// 发送具体事件
        /// T -> 事件类型
        /// </summary>
        public void SendEvent<T>(T Event)
        {
            typeEventSystem.Send<T>(Event);
        }

        ///<summary>
        /// 注册事件
        /// T -> 事件类型
        /// </summary>
        public IRemoveListener RegisterEvent<T>(Action<T> onEvent)
        {
            return typeEventSystem.AddListener<T>(onEvent);
        }

        ///<summary>
        /// 取消注册事件
        /// T -> 事件类型
        /// </summary>
        public void UnRegisterEvent<T>(Action<T> onEvent)
        {
            typeEventSystem.RemoveListener<T>(onEvent);
        }

        //初始化方法,子类实现
        protected abstract void Init();

        /// <summary>
        /// 发射查询
        /// T -> 查询类型
        /// </summary>
        public T SendQuery<T>(IQuery<T> query)
        {
            query.SetArchitecture(this);
            return query.Do();
        }
    }

    #endregion

    #region View控制层

    /// <summary>
    /// 视觉层控制器接口
    /// </summary>
    public interface IViewController : IGetArchitecture, ISendCommand, IGetSystem, IGetModel, IRegisterEvent, ISendQuery
    {

    }
    #endregion

    #region System系统层

    /// <summary>
    /// 系统层接口
    /// </summary>  
    public interface ISystem : IGetArchitecture, ISetArchitecture, IGetModel, IGetSystem, IGetUtility, IRegisterEvent, ISendEvent
    {
        public void Init();
    }

    public abstract class AbstractSystem : ISystem
    {
        private IArchitecture architecture;

        IArchitecture IGetArchitecture.GetArchitecture()
        {
            return architecture;
        }

        void ISetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            this.architecture = architecture;
        }

        void ISystem.Init()
        {
            OnInit();
        }

        protected abstract void OnInit();
    }

    #endregion

    #region Model数据层

    /// <summary>
    /// Model接口
    /// </summary>
    public interface IModel : IGetArchitecture, ISetArchitecture, IGetUtility, ISendEvent
    {
        public void Init();
    }

    public abstract class AbstractModel : IModel
    {
        private IArchitecture architecture;

        IArchitecture IGetArchitecture.GetArchitecture()
        {
            return architecture;
        }

        void ISetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            this.architecture = architecture;
        }

        void IModel.Init()
        {
            OnInit();
        }

        protected abstract void OnInit();
    }

    #endregion

    #region Utility工具层

    /// <summary>
    /// 工具类接口
    /// </summary>
    public interface IUtility { }

    #endregion

    #region Command命令

    /// <summary>
    /// 命令接口
    /// </summary>
    public interface ICommand : IGetArchitecture, ISetArchitecture, IGetSystem, IGetModel, IGetUtility, ISendEvent, ISendCommand, ISendQuery
    {
        public void Execute();
    }

    public abstract class AbstractCommand : ICommand
    {
        private IArchitecture architecture;

        IArchitecture IGetArchitecture.GetArchitecture()
        {
            return architecture;
        }

        void ISetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            this.architecture = architecture;
        }

        void ICommand.Execute()
        {
            OnExecute();
        }

        protected abstract void OnExecute();
    }

    #endregion

    #region Query数据查询

    /// <summary>
    /// 查询接口
    /// </summary>
    public interface IQuery<T> : IGetArchitecture, ISetArchitecture, IGetModel, IGetSystem, ISendQuery
    {
        T Do();
    }

    /// <summary>
    /// 查询抽象类
    /// T -> 查询结果
    /// </summary>
    public abstract class AbstractQuery<T> : IQuery<T>
    {
        private IArchitecture architecture;

        /// <summary>
        /// 执行查询,并返回查询结果
        /// </summary>
        public T Do()
        {
            return OnDo();
        }

        /// <summary>
        /// 查询具体实现,需要子类实现
        /// </summary>
        protected abstract T OnDo();

        public IArchitecture GetArchitecture()
        {
            return architecture;
        }

        public void SetArchitecture(IArchitecture architecture)
        {
            this.architecture = architecture;
        }
    }

    #endregion

    #region 架构使用规则定义

    /// <summary>
    /// 获取从属架构接口
    /// </summary>
    public interface IGetArchitecture
    {
        public IArchitecture GetArchitecture();
    }

    /// <summary>
    /// 设置从属架构接口
    /// </summary>
    public interface ISetArchitecture
    {
        public void SetArchitecture(IArchitecture architecture);
    }

    /// <summary>
    /// 是否可以获取Model数据接口
    /// </summary>
    public interface IGetModel : IGetArchitecture { }
    public static class GetModelExtension
    {
        public static T GetModel<T>(this IGetModel self) where T : class, IModel
        {
            return self.GetArchitecture().GetModel<T>();
        }
    }

    /// <summary>
    /// 是否可以获取System接口
    /// </summary>
    public interface IGetSystem : IGetArchitecture { }
    public static class GetSystemExtension
    {
        public static T GetSystem<T>(this IGetSystem self) where T : class, ISystem
        {
            return self.GetArchitecture().GetSystem<T>();
        }
    }

    /// <summary>
    /// 是否可以获取工具类接口
    /// </summary>
    public interface IGetUtility : IGetArchitecture { }
    public static class GetUtilityExtension
    {
        public static T GetUtility<T>(this IGetUtility self) where T : class, IUtility
        {
            return self.GetArchitecture().GetUtility<T>();
        }
    }

    /// <summary>
    /// 是否可以注册事件接口
    /// </summary>
    public interface IRegisterEvent : IGetArchitecture { }
    public static class RegisterEventExtension
    {
        public static IRemoveListener RegisterEvent<T>(this IRegisterEvent self, Action<T> onEvent)
        {
            return self.GetArchitecture().RegisterEvent<T>(onEvent);
        }

        public static void UnRegisterEvent<T>(this IRegisterEvent self, Action<T> onEvent)
        {
            self.GetArchitecture().UnRegisterEvent<T>(onEvent);
        }
    }

    /// <summary>
    /// 是否可以发送命令接口
    /// </summary>
    public interface ISendCommand : IGetArchitecture { }
    public static class SendCommandExtension
    {
        public static void SendCommand<T>(this ISendCommand self) where T : ICommand, new()
        {
            self.GetArchitecture().SendCommand<T>();
        }

        public static void SendCommand<T>(this ISendCommand self, T command) where T : ICommand
        {
            self.GetArchitecture().SendCommand<T>(command);
        }
    }

    /// <summary>
    /// 是否可以发送事件接口
    /// </summary>
    public interface ISendEvent : IGetArchitecture { }
    public static class SendEventExtension
    {
        public static void SendEvent<T>(this ISendEvent self) where T : new()
        {
            self.GetArchitecture().SendEvent<T>();
        }

        public static void SendEvent<T>(this ISendEvent self, T Event)
        {
            self.GetArchitecture().SendEvent<T>(Event);
        }
    }

    /// <summary>
    /// 是否可以发送查询接口
    /// </summary>
    public interface ISendQuery : IGetArchitecture { }
    public static class SendQueryExtension
    {
        public static T SendQuery<T>(this ISendQuery self, IQuery<T> query)
        {
            return self.GetArchitecture().SendQuery(query);
        }
    }

    #endregion
}