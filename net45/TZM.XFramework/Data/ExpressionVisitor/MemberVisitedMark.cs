
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
        private Stack<MemberExpression> _binaryMembers = null;

        /// <summary>
        /// 当前访问的成员
        /// </summary>
        public MemberExpression Current
        {
            get
            {
                if (_binaryMembers != null && _binaryMembers.Count > 0) return _binaryMembers.Peek();
                //else if (_columnMembers.Count > 0) return _columnMembers[0];
                else return null;
            }
        }

        /// <summary>
        /// 访问链成员数量
        /// </summary>
        public int Count
        {
            get
            {
                return _binaryMembers == null ? 0 : _binaryMembers.Count;
            }
        }

        /// <summary>
        /// 添加已访问成员
        /// </summary>
        public void Add(MemberExpression m)
        {
            if (_binaryMembers == null) _binaryMembers = new Stack<MemberExpression>();
            _binaryMembers.Push(m);
        }

        /// <summary>
        /// 添加已访问成员
        /// </summary>
        public void Add(MemberInfo member, ParameterExpression p)
        {
            MemberExpression m = Expression.MakeMemberAccess(p, member);
            this.Add(m);
        }

        /// <summary>
        /// 添加已访问成员
        /// </summary>
        public void Add(MemberInfo member, string alias)
        {
            ParameterExpression p = Expression.Parameter(member.ReflectedType, alias);
            this.Add(member, p);
        }

        /// <summary>
        /// 删除指定数量的访问足迹
        /// </summary>
        public void Remove(int count)
        {
            if (_binaryMembers != null)
            {
                int qty = 0;
                while (count > qty)
                {
                    _binaryMembers.Pop();
                    qty += 1;
                }
            }
        }
    }
}