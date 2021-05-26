
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
        public void Register<T>(Func<T> creator) where T : class => this.Register<T>(creator, true);

        /// <summary>
        /// 向容器注册类型
        /// </summary>
        public void Register<T>(Func<T> creator, bool isSingleton) where T : class => this.Register(typeof(T), creator, isSingleton);

        /// <summary>
        /// 向容器注册类型
        /// </summary>
        public void Register(Type type, Func<object> creator, bool isSingleton)
        {
            object instance = isSingleton ? creator() : null;
            _cache.AddOrUpdate(type, t => new Activator(instance, creator), t => new Activator(instance, creator));
        }

        ///// <summary>
        ///// 向容器注册类型
        ///// </summary>
        //public void Register(string typeFullName, Func<object> creator, bool isSingleton)
        //{
        //    object instance = isSingleton ? creator() : null;
        //    _cache.AddOrUpdate(typeFullName, t => new Activator(instance, creator), t => new Activator(instance, creator));
        //}

        /// <summary>
        /// 从容器中解析指定类型
        /// </summary>
        public T Resolve<T>() where T : class => (T)this.Resolve(typeof(T));

        /// <summary>
        /// 从容器中解析指定类型
        /// </summary>
        public object Resolve(Type type)
        {
            Activator activator;
            if (!_cache.TryGet(type, out activator))
                throw new XFrameworkException("{0} is not registered", type.FullName);

            if (activator.Instance != null)
                return activator.Instance;

            Func<object> creator = activator.Creator;
            if (creator == null)
                throw new XFrameworkException("{0} is not registered", type.FullName);

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

        ///// <summary>
        ///// 返回指定类型是否已经在容器中
        ///// </summary>
        //public bool IsRegistered(string typeFullName)
        //{
        //    Activator activator;
        //    return _cache.TryGet(typeFullName, out activator);
        //}

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

            public Activator(object instance, Func<object> creator)
            {
                this.Instance = instance;
                this.Creator = creator;
            }
        }
    }
}
