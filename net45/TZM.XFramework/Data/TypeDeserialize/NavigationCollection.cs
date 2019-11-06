
using System.Collections;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 导航属性描述集合
    /// </summary>
    public class NavigationCollection : IEnumerable<KeyValuePair<string, Navigation>>
    {
        private IDictionary<string, Navigation> _collection = null;
        private int? _minIndex;

        /// <summary>
        /// 所有导航属性的最小开始索引
        /// </summary>
        public int MinIndex { get { return _minIndex == null ? 0 : _minIndex.Value; } }

        /// <summary>
        /// 包含的元素数
        /// </summary>
        public int Count { get { return _collection.Count; } }

        /// <summary>
        /// 实例化<see cref="NavigationCollection"/>类的新实例
        /// </summary>
        public NavigationCollection()
        {
            _collection = new Dictionary<string, Navigation>(8);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, Navigation>> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        /// <summary>
        /// 添加一个带有所提供的键和值的元素。
        /// </summary>
        public void Add(string key, Navigation nav)
        {
            _collection.Add(key, nav);
            if (nav != null && nav.FieldCount != 0)
            {
                if (_minIndex == null)
                {
                    _minIndex = nav.Start;
                }
                else
                {
                    if (nav.Start < _minIndex.Value) _minIndex = nav.Start;
                }
            }
        }

        /// <summary>
        /// 是否包含具有指定键的元素
        /// </summary>
        public bool ContainsKey(string key)
        {
            return _collection.ContainsKey(key);
        }

        /// <summary>
        /// 获取与指定的键相关联的值。
        /// </summary>
        public bool TryGetValue(string key, out Navigation descriptor)
        {
            return _collection.TryGetValue(key, out descriptor);
        }
    }
}
