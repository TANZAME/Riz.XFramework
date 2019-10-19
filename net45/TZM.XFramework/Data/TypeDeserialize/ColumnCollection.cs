
using System.Collections;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 选择列集合
    /// </summary>
    public class ColumnCollection : IEnumerable<Column>
    {
        private IDictionary<string, Column> _collection = null;

        /// <summary>
        /// 根据名称获取元素
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        public Column this[string newName]
        {
            get { return _collection[newName]; }
        }

        /// <summary>
        /// 包含的元素数
        /// </summary>
        public int Count { get { return _collection.Count; } }

        /// <summary>
        /// 实例化<see cref="ColumnCollection"/>类的新实例
        /// </summary>
        public ColumnCollection()
        {
            _collection = new Dictionary<string, Column>(8);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        IEnumerator<Column> IEnumerable<Column>.GetEnumerator()
        {
            var enumerator = (Dictionary<string, Column>.Enumerator)_collection.GetEnumerator();
            return new Enumerator<Column>(enumerator);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            var enumerator = (Dictionary<string, Column>.Enumerator)_collection.GetEnumerator();
            return new Enumerator<Column>(enumerator);
        }

        /// <summary>
        /// 添加项，如果字段存在，则产生一个新名称
        /// </summary>
        /// <param name="name">字段名称</param>
        /// <param name="alias">所属性表别名</param>
        /// <returns></returns>
        public string Add(string name, string alias)
        {
            // ATTENTION：此方法不能在 VisitMember 方法里调用
            // 因为 VisitMember 方法不一定是最后SELECT的字段
            // 返回最终确定的唯一的列名

            string newName = name;
            int dup = 0;
            while (this.Contains(newName))
            {
                var column = this[newName];
                column.DupCount += 1;

                newName = newName + column.DupCount.ToString();
                dup = column.DupCount;
            }

            this.Add(new Column
            {
                Name = name,
                NewName = newName,
                DupCount = dup,
                TableAlias = alias
            });
            return newName;
        }

        /// <summary>
        /// 添加一个带有所提供的键和值的元素。
        /// </summary>
        public void Add(Column column)
        {
            _collection.Add(column.NewName, column);
        }

        /// <summary>
        /// 是否包含具有指定键的元素
        /// </summary>
        public bool Contains(string newName)
        {
            return _collection.ContainsKey(newName);
        }

        /// <summary>
        /// 获取与指定的键相关联的值。
        /// </summary>
        public bool TryGetValue(string newName, out Column column)
        {
            return _collection.TryGetValue(newName, out column);
        }
    }
}
