
using System.Collections;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 成员反射器集合
    /// </summary>
    public class MemberInvokerCollection : IEnumerable<MemberInvokerBase>
    {
        private IDictionary<string, MemberInvokerBase> _collection = null;

        /// <summary>
        /// 根据名称获取元素
        /// </summary>
        /// <param name="memberName">成员名称</param>
        /// <returns></returns>
        public MemberInvokerBase this[string memberName]
        {
            get { return _collection[memberName]; }
            set { _collection[memberName] = value; }
        }

        /// <summary>
        /// 包含的元素数
        /// </summary>
        public int Count { get { return _collection.Count; } }

        /// <summary>
        /// 实例化<see cref="MemberInvokerCollection"/>类的新实例
        /// </summary>
        public MemberInvokerCollection()
        {
            _collection = new Dictionary<string, MemberInvokerBase>(8);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        IEnumerator<MemberInvokerBase> IEnumerable<MemberInvokerBase>.GetEnumerator()
        {
            var enumerator = (Dictionary<string, MemberInvokerBase>.Enumerator)_collection.GetEnumerator();
            return new Enumerator<MemberInvokerBase>(enumerator);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            var enumerator = (Dictionary<string, MemberInvokerBase>.Enumerator)_collection.GetEnumerator();
            return new Enumerator<MemberInvokerBase>(enumerator);
        }

        /// <summary>
        /// 添加一个带有所提供的键和值的元素。
        /// </summary>
        public void Add(MemberInvokerBase invoker)
        {
            _collection.Add(invoker.Name, invoker);
        }

        /// <summary>
        /// 是否包含具有指定键的元素
        /// </summary>
        public bool Contains(string memberName)
        {
            return _collection.ContainsKey(memberName);
        }

        /// <summary>
        /// 获取与指定的键相关联的值。
        /// </summary>
        public bool TryGetValue(string memberName, out MemberInvokerBase invoker)
        {
            return _collection.TryGetValue(memberName, out invoker);
        }
    }
}
