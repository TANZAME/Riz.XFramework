
using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 已访问成员描述类
    /// </summary>
    public class MemberVisitedMark
    {
        private List<MemberExpression> _binaryMembers = null;
        private List<MemberInfo> _columnMembers = null;
        private bool _clearImmediately = true;

        /// <summary>
        /// 当前访问的成员
        /// </summary>
        public MemberExpression Current
        {
            get 
            {
                if (_binaryMembers.Count > 0) return _binaryMembers[0];
                //else if (_columnMembers.Count > 0) return _columnMembers[0];
                else return null;
            }
        }

        /// <summary>
        /// 当调用 Clear 方法时，标志Clear方法是否马上执行
        /// <para>
        /// 使用场景：当多个常数表达式共用一个 Member 时，那么只需要在访问最后一个常数表达式时清除访问列表
        /// </para>
        /// </summary>
        public bool ClearImmediately
        {
            get { return _clearImmediately; }
            set { _clearImmediately = value; }
        }

        /// <summary>
        /// 实例化 <see cref="MemberVisitedMark"/> 类的新实例
        /// </summary>
        public MemberVisitedMark()
        {
            _binaryMembers = new List<MemberExpression>();
            _columnMembers = new List<MemberInfo>();
        }

        /// <summary>
        /// 添加已访问成员
        /// </summary>
        public void Add(MemberExpression m)
        {
            _binaryMembers.Add(m);
        }

        ///// <summary>
        ///// 添加已访问成员
        ///// </summary>
        //public void Add(MemberInfo m)
        //{
        //    _columnMembers.Add(m);
        //}

        /// <summary>
        /// 清空已访问成员
        /// </summary>
        public void Clear() 
        {
            if (_clearImmediately)
            {
                _binaryMembers.Clear();
                _columnMembers.Clear(); 
            }
        }
    }
}