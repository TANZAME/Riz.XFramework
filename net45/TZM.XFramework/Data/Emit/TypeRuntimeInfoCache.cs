
using System;
using System.Reflection;
using TZM.XFramework.Caching;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 类型运行时元数据缓存
    /// </summary>
    public static class TypeRuntimeInfoCache
    {
        private static readonly ICache<Type, TypeRuntimeInfo> _typeRuntimeCache = new ReaderWriterCache<Type, TypeRuntimeInfo>(MemberInfoComparer<Type>.Default);

        /// <summary>
        /// 取指定类型的运行时元数据
        /// </summary>
        /// <param name="type">类型实例</param>
        /// <returns></returns>
        public static TypeRuntimeInfo GetRuntimeInfo(Type type)
        {
            return TypeRuntimeInfoCache.GetRuntimeInfo(type, false);
        }

        /// <summary>
        /// 取指定类型的运行时元数据
        /// </summary>
        /// <param name="type">类型实例</param>
        /// <param name="private">包含私有成员</param>
        /// <returns></returns>
        public static TypeRuntimeInfo GetRuntimeInfo(Type type, bool @private)
        {
            return _typeRuntimeCache.GetOrAdd(type, p => new TypeRuntimeInfo(p, @private));
        }

        /// <summary>
        /// 取指定类型的运行时元数据
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <returns></returns>
        public static TypeRuntimeInfo GetRuntimeInfo<T>()
        {
            return TypeRuntimeInfoCache.GetRuntimeInfo(typeof(T));
        }

        /// <summary>
        /// 清空所有运行时缓存项目
        /// </summary>
        public static void Clear()
        {
            IDisposable d = _typeRuntimeCache as IDisposable;
            if (d != null) d.Dispose();
        }
    }

}
