using System;
using System.Collections.Generic;
using System.Threading;

namespace Riz.XFramework.Caching
{
    /// <summary>
    /// 管理缓存过期行为的缓存器
    /// </summary>
    public class ExpirationCache<TKey, TValue> : ReaderWriterCache<TKey, ExpirationCache<TKey, TValue>.CacheItem>, ICache<TKey, TValue>
    {
        /// <summary>
        /// 缓存项
        /// </summary>
        public class CacheItem
        {
            /// <summary>
            /// 键
            /// </summary>
            public TKey Key { get; set; }

            /// <summary>
            /// 值
            /// </summary>
            public TValue Value { get; set; }

            /// <summary>
            /// 最后访问时间
            /// </summary>
            public DateTime LastAccessTime { get; set; }

            /// <summary>
            /// 设置缓存项的最后访问时间
            /// </summary>
            public void SetLastAccess()
            {
                this.LastAccessTime = DateTime.Now;
            }
        }

        private readonly System.Threading.Timer _timer;
        private TimeSpan _timeout;
        private TimeSpan _period;

        /// <summary>
        /// 缓存过期时间，默认30分钟
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
        }

        /// <summary>
        /// 添加缓存时间间隔，默认1秒钟
        /// </summary>
        public TimeSpan Period
        {
            get { return _period; }
        }

        /// <summary>
        /// 实例化 ExpirationCache 类的新实例
        /// </summary>
        public ExpirationCache()
            : this(TimeSpan.FromMinutes(30.0), TimeSpan.FromSeconds(1))
        {
        }

        /// <summary>
        /// 实例化 ExpirationCache 类的新实例
        /// </summary>
        public ExpirationCache(TimeSpan timeout, TimeSpan period)
            : this(null, timeout, period)
        {
        }

        /// <summary>
        /// 实例化 ExpirationCache 类的新实例
        /// </summary>
        public ExpirationCache(IEqualityComparer<TKey> comparer, TimeSpan timeout, TimeSpan period)
            : base(comparer)
        {
            this._timeout = timeout;
            this._period = period;
            this._timer = new Timer(ClearExpired, null, TimeSpan.FromMilliseconds(100), period);
        }

        /// <summary>
        /// 尝试获取指定键值的缓存项，若缓存项不存在，则使用指定委托创建
        /// </summary>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> creator = null)
        {
            this._rwLock.EnterReadLock();
            try
            {
                CacheItem obj;
                if (this._innerCache.TryGetValue(key, out obj))
                {
                    obj.SetLastAccess();
                    return obj.Value;
                }
            }
            finally
            {
                this._rwLock.ExitReadLock();
            }

            if (creator == null)
                return default(TValue);

            this._rwLock.EnterWriteLock();
            try
            {
                CacheItem obj1;
                if (this._innerCache.TryGetValue(key, out obj1))
                {
                    obj1.SetLastAccess();
                    return obj1.Value;
                }

                obj1 = new CacheItem { LastAccessTime = DateTime.Now };
                obj1.Key = key;
                obj1.Value = creator(key);
                this._innerCache[key] = obj1;
                return obj1.Value;
            }
            finally
            {
                this._rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 若指定的键值存在，则使用指定委托更新，否则使用指定委托创建
        /// </summary>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> creator, Func<TKey, TValue> updator = null)
        {
            this._rwLock.EnterWriteLock();
            try
            {
                CacheItem obj;
                if (!this._innerCache.TryGetValue(key, out obj))
                {
                    if (creator == null)
                        return default(TValue);

                    CacheItem obj1 = new CacheItem { LastAccessTime = DateTime.Now };
                    obj1.Key = key;
                    obj1.Value = creator(key);
                    this._innerCache[key] = obj1;
                    return obj1.Value;
                }
                else
                {
                    if (updator == null)
                        return default(TValue);

                    TValue obj2 = updator(key);
                    obj.Value = default(TValue);
                    obj.Value = obj2;
                    obj.SetLastAccess();
                    this._innerCache[key] = obj;
                    return obj2;
                }
            }
            finally
            {
                this._rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 尝试获取指定键值的缓存项
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            this._rwLock.EnterReadLock();
            try
            {
                CacheItem obj;
                if (this._innerCache.TryGetValue(key, out obj))
                {
                    obj.SetLastAccess();
                    value = obj.Value;
                    return true;
                }
            }
            finally
            {
                this._rwLock.ExitReadLock();
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) this._timer.Dispose();
        }

        // 清除过期的缓存
        private void ClearExpired(object state)
        {
            try
            {
                CacheItem[] array;
                this._rwLock.EnterWriteLock();
                try
                {
                    array = new CacheItem[this._innerCache.Count];
                    this._innerCache.Values.CopyTo(array, 0);
                    //Buffer.BlockCopy(this._cache.Values, 0, array, 0, array.Length);
                }
                finally
                {
                    this._rwLock.ExitWriteLock();
                }

                DateTime now = DateTime.Now;
                for (int i = 0; i < array.Length; i++)
                {
                    bool expired = now - array[i].LastAccessTime > _timeout;
                    if (expired) this.Remove(array[i].Key);
                }
            }
            catch
            {

            }
        }
    }
}
