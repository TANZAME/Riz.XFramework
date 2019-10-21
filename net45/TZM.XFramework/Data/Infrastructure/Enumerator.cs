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
                else if (obj is KeyValuePair<string, Column>) return ((KeyValuePair<string, Column>)obj).Value;
                else return null;
            }
        }

        /// <summary>
        /// 实例化 <see cref="Enumerator"/> 结构的新实例
        /// </summary>
        /// <param name="enumerator"></param>
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
