
using System;
using Riz.XFramework.Caching;

namespace Riz.XFramework
{
    /// <summary>
    /// 默认 Ioc 容器
    /// </summary>
    public sealed class DefaultContainer : IContainer
    {
        private static ICache<Type, Activator> _cache = new ReaderWriterCache<Type, Activator>(MemberInfoComparer<Type>.Default);

        /// <summary>
        /// 简单容器-默认实例
        /// </summary>
        public static readonly DefaultContainer Instance = new DefaultContainer();

        /// <summary>
        /// 实例化类 <see cref="DefaultContainer"/> 的新实例
        /// </summary>
        private DefaultContainer()
        { 
        }

        /// <summary>
        /// 向容器注册单例实例
        /// </summary>
        public void Register<T>(Func<T> func) where T : class => this.Register<T>(func, true);

        /// <summary>
        /// 向容器注册类型
        /// </summary>
        public void Register<T>(Func<T> func, bool isSingleton) where T : class => this.Register(typeof(T), func, isSingleton);

        /// <summary>
        /// 向容器注册类型
        /// </summary>
        public void Register(Type type, Func<object> func, bool isSingleton)
        {
            object instance = isSingleton ? func() : null;
            _cache.AddOrUpdate(type, t => new Activator(instance, func), t => new Activator(instance, func));
        }

        /// <summary>
        /// 从容器中解析指定类型
        /// </summary>
        public T Resolve<T>() where T : class
        {
            Activator activator;
            if (!_cache.TryGet(typeof(T), out activator))
                throw new XFrameworkException(typeof(T).FullName + " is not registered");

            if (activator.Instance != null) return (T)activator.Instance;

            Func<object> func = activator.Creator;
            if (func == null)
                XFrameworkException.Throw(typeof(T).FullName + " is not registered");
            return (T)func();
        }

        /// <summary>
        /// 从容器中解析指定类型
        /// </summary>
        public object Resolve(Type type)
        {
            Activator activator;
            if (!_cache.TryGet(type, out activator))
                throw new XFrameworkException(type.FullName + " is not registered");

            if (activator.Instance != null) return activator.Instance;

            Func<object> creator = activator.Creator;
            if (creator == null)
                XFrameworkException.Throw(type.FullName + " is not registered");
            return creator();
        }

        /// <summary>
        /// 返回指定类型是否已经在容器中
        /// </summary>
        public bool IsRegistered<T>() where T : class => this.IsRegistered(typeof(T));

        /// <summary>
        /// 返回指定类型是否已经在容器中
        /// </summary>
        public bool IsRegistered(Type type)
        {
            Activator activator;
            return _cache.TryGet(type, out activator);
        }

        /// <summary>
        /// 注册器
        /// </summary>
        class Activator
        {
            /// <summary>
            /// 单例
            /// </summary>
            public object Instance { get; set; }

            /// <summary>
            /// 非单例
            /// </summary>
            public Func<object> Creator { get; set; }

            public Activator(object instance, Func<object> func)
            {
                this.Instance = instance;
                this.Creator = func;
            }
        }
    }
}
