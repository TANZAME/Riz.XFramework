
using System.Collections;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 成员反射器集合
    /// </summary>
    public class MemberAccessorCollection : IEnumerable<MemberAccessorBase>
    {
        private IDictionary<string, MemberAccessorBase> _collection = null;

        /// <summary>
        /// 根据名称获取元素
        /// </summary>
        /// <param name="memberName">成员名称</param>
        /// <returns></returns>
        public MemberAccessorBase this[string memberName]
        {
            get { return _collection[memberName]; }
            set { _collection[memberName] = value; }
        }

        /// <summary>
        /// 包含的成员数量
        /// </summary>
        public int Count { get { return _collection.Count; } }

        /// <summary>
        /// 实例化<see cref="MemberAccessorCollection"/>类的新实例
        /// </summary>
        public MemberAccessorCollection()
        {
            _collection = new Dictionary<string, MemberAccessorBase>(8);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        IEnumerator<MemberAccessorBase> IEnumerable<MemberAccessorBase>.GetEnumerator()
        {
            var enumerator = (Dictionary<string, MemberAccessorBase>.Enumerator)_collection.GetEnumerator();
            return new Enumerator<MemberAccessorBase>(enumerator);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            var enumerator = (Dictionary<string, MemberAccessorBase>.Enumerator)_collection.GetEnumerator();
            return new Enumerator<MemberAccessorBase>(enumerator);
        }

        /// <summary>
        /// 向集合添加一个成员
        /// </summary>
        /// <param name="m">成员访问器</param>
        public void Add(MemberAccessorBase m)
        {
            if (!_collection.ContainsKey(m.Name)) _collection.Add(m.Name, m);
            else
            {
                // 具有相同名称的成员，可能是属性/方法使用 new 关键字或者是方法重载
                if (m.MemberType != System.Reflection.MemberTypes.Method)
                {
                    //new 关键字后，DeclaringType 和 ReflectedType 一样
                    if (m.Member.DeclaringType == m.Member.ReflectedType) _collection[m.Name] = m;
                }
                else
                {
                    var accessor = (MethodAccessor)_collection[m.Name];
                    if (accessor.Overrides == null) accessor.Overrides = new List<MethodAccessor>();
                    accessor.Overrides.Add((MethodAccessor)m);
                }
            }
        }

        /// <summary>
        /// 确定是否包含指定成员
        /// </summary>
        /// <param name="memberName">成员名称</param>
        public bool Contains(string memberName)
        {
            return _collection.ContainsKey(memberName);
        }

        /// <summary>
        /// 获取指定名称的成员
        /// </summary>
        /// <param name="memberName">成员名称</param>
        /// <param name="m">成员访问器</param>
        /// <returns></returns>
        public bool TryGetValue(string memberName, out MemberAccessorBase m)
        {
            return _collection.TryGetValue(memberName, out m);
        }
    }
}
