using System;

namespace Riz.XFramework.Caching
{
    /// <summary>
    /// 缓存接口
    /// </summary>
    public interface ICache<TKey, TValue>
    {
        /// <summary>
        /// 缓存项目计数
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 尝试获取指定键值的缓存项，若缓存项不存在，则使用指定委托创建
        /// </summary>
        TValue GetOrAdd(TKey key, Func<TKey, TValue> creator = null);

        /// <summary>
        /// 若指定的键值存在，则使用指定委托更新，否则使用指定委托创建
        /// </summary>
        TValue AddOrUpdate(TKey key, Func<TKey, TValue> creator, Func<TKey, TValue> updator = null);

        /// <summary>
        /// 尝试获取指定键值的缓存项
        /// </summary>
        bool TryGet(TKey key, out TValue value);

        /// <summary>
        /// 移除指定键值的缓存项
        /// </summary>
        void Remove(TKey key);
    }
}
