
using System.Reflection;
using System.Collections.Generic;

namespace ICS.XFramework.Data
{
    internal sealed class MemberInfoComparer<T> : IEqualityComparer<T>, IComparer<T> where T : MemberInfo
    {
        private static readonly MemberInfoComparer<T> _comparer = new MemberInfoComparer<T>();

        public static MemberInfoComparer<T> Default
        {
            get
            {
                return _comparer;
            }
        }

        public int Compare(T x, T y)
        {
            return x.MetadataToken - y.MetadataToken;
        }

        public bool Equals(T x, T y)
        {
            return x == y;
        }

        public int GetHashCode(T obj)
        {
            return obj.MetadataToken;
        }
    }
}
