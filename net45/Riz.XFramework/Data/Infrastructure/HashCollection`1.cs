using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 哈希集合，内部是一个 string 键的字典。
    /// 适用于 value 有一个 string 属性，用于省掉字典的 keyvalue 写法
    /// </summary>
    public class HashCollection<T> : IEnumerable<T> where T : class, IStringKey
    {
        private IDictionary<string, T> _collection = null;

        /// <summary>
        /// 根据键值获取对应元素
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public T this[string key]
        {
            get { return _collection.ContainsKey(key) ? _collection[key] : default(T); }
        }

        /// <summary>
        /// 包含的元素数
        /// </summary>
        public int Count { get { return _collection.Count; } }

        /// <summary>
        /// 实例化<see cref="ColumnDescriptorCollection"/>类的新实例
        /// </summary>
        public HashCollection()
        {
            _collection = new Dictionary<string, T>(8);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var enumerator = (Dictionary<string, T>.Enumerator)_collection.GetEnumerator();
            return new Enumerator<T>(enumerator);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            var enumerator = (Dictionary<string, T>.Enumerator)_collection.GetEnumerator();
            return new Enumerator<T>(enumerator);
        }

        /// <summary>
        /// 在集合中添加一个带有所提供的键和值的元素
        /// </summary>
        public virtual void Add(T value)
        {
            _collection.Add(value.Key, value);
        }

        /// <summary>
        /// 确定是否包含具有指定键的元素
        /// </summary>
        public bool Contains(string key)
        {
            return _collection.ContainsKey(key);
        }

        /// <summary>
        /// 确定是否包含具有指定键的元素
        /// </summary>
        public bool Contains(T value)
        {
            XFrameworkException.Check.NotNull(value, "value");
            return _collection.ContainsKey(value.Key);
        }

        /// <summary>
        /// 获取与指定的键相关联的值
        /// </summary>
        public bool TryGetValue(string key, out T value)
        {
            return _collection.TryGetValue(key, out value);
        }
    }
}
