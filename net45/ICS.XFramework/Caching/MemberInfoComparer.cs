
using System.Collections.Generic;
using System.Reflection;

namespace ICS.XFramework.Caching
{
    /// <summary>
    /// 成员比较器
    /// </summary>
    public sealed class MemberInfoComparer<T> : IEqualityComparer<T>, IComparer<T> where T : MemberInfo
    {
        private static readonly MemberInfoComparer<T> _comparer = new MemberInfoComparer<T>();

        /// <summary>
        /// 默认实例
        /// </summary>
        public static MemberInfoComparer<T> Default
        {
            get
            {
                return _comparer;
            }
        }

        /// <summary>
        /// 比较两个对象并返回一个值，指示一个对象是小于、等于还是大于另一个对象。
        /// </summary>
        public int Compare(T x, T y)
        {
            return x.MetadataToken - y.MetadataToken;
        }

        /// <summary>
        ///  确定指定的对象是否相等。
        /// </summary>
        public bool Equals(T x, T y)
        {
            return x == y;
        }

        /// <summary>
        /// 返回指定对象的哈希代码。
        /// </summary>
        public int GetHashCode(T obj)
        {
            return obj.MetadataToken;
        }
    }
}
