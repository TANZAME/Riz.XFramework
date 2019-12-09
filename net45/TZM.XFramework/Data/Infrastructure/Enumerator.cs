using System;
using System.Collections;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 迭代器封装
    /// </summary>
    public struct Enumerator<T> : IEnumerator<T>, IEnumerator
    {
        private Dictionary<string, T>.Enumerator _enumerator;

        /// <summary>
        /// 当前项
        /// </summary>
        public T Current
        {
            get { return _enumerator.Current.Value; }
        }

        /// <summary>
        /// 当前项
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                object obj = ((IEnumerator)_enumerator).Current;
                if (obj is DictionaryEntry) return ((DictionaryEntry)obj).Value;
                else if (obj is KeyValuePair<string, T>) return ((KeyValuePair<string, T>)obj).Value;
                else return null;
            }
        }

        /// <summary>
        /// 实例化迭代器的新实例
        /// </summary>
        /// <param name="enumerator">枚举字典中的的元素</param>
        public Enumerator(Dictionary<string, T>.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// 迭代下一项
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            ((IEnumerator)_enumerator).Reset();
        }
    }
}
