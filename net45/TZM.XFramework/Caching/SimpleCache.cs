using System;
using System.Collections.Generic;

namespace TZM.XFramework.Caching
{
    /// <summary>
    /// 简单键值对缓存器，键 一般为字符类型
    /// </summary>
    public class SimpleCache<TKey, TValue> : ICache<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _cache;

        /// <summary>
        /// 缓存项目计数
        /// </summary>
        public int Count
        {
            get
            {
                return this._cache.Count;
            }
        }

        /// <summary>
        /// 实例化 <see cref="SimpleCache"/> 类的新实例
        /// </summary>
        public SimpleCache()
        {
            this._cache = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// 尝试获取指定键值的缓存项，若缓存项不存在，则使用指定委托创建
        /// </summary>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> creator = null)
        {
            TValue obj1;
            if (this._cache.TryGetValue(key, out obj1) || creator == null)
                return obj1;

            TValue obj2 = creator(key);
            this._cache[key] = obj2;
            return obj2;
        }

        /// <summary>
        /// 若指定的键值存在，则使用指定委托更新，否则使用指定委托创建
        /// </summary>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> creator, Func<TKey, TValue> updator = null)
        {
            TValue value;
            if (!this._cache.TryGetValue(key, out value))
            {
                if (creator == null)
                    return default(TValue);

                TValue obj1 = creator(key);
                this._cache[key] = obj1;
                return obj1;
            }
            else
            {
                if (updator == null)
                    return default(TValue);

                TValue obj2 = updator(key);
                this._cache[key] = obj2;
                return obj2;
            }
        }

        /// <summary>
        /// 尝试获取指定键值的缓存项
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            return this._cache.TryGetValue(key, out value);
        }

        /// <summary>
        /// 移除指定键值的缓存项
        /// </summary>
        public void Remove(TKey key)
        {
            this._cache.Remove(key);
        }
    }
}
