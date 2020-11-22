
using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 已访问成员栈，用于计算常量的数据库类型
    /// </summary>
    public class MemberVisitedStack
    {
        private Stack<VisitedMember> _visitedMembers = null;

        /// <summary>
        /// 当前访问的成员
        /// </summary>
        public VisitedMember Current
        {
            get
            {
                if (_visitedMembers != null && _visitedMembers.Count > 0)
                    return _visitedMembers.Peek();
                else
                    return null;
            }
        }

        /// <summary>
        /// 访问链成员数量
        /// </summary>
        public int Count => _visitedMembers == null ? 0 : _visitedMembers.Count;
        /// <summary>
        /// 添加已访问成员
        /// </summary>
        /// <param name="m">即将添加的成员</param>
        public void Add(MemberExpression m)
        {
            if (_visitedMembers == null) _visitedMembers = new Stack<VisitedMember>();
            _visitedMembers.Push(new VisitedMember(m.Member, m.Expression != null ? m.Expression.Type : null));
        }

        /// <summary>
        /// 添加已访问成员
        /// </summary>
        /// <param name="m">即将添加的成员</param>
        /// <param name="reflectedType">访问字段或者属性的实际类型</param>
        public void Add(MemberInfo m, Type reflectedType)
        {
            if (_visitedMembers == null) _visitedMembers = new Stack<VisitedMember>();
            _visitedMembers.Push(new VisitedMember(m, reflectedType));
        }

        /// <summary>
        /// 删除指定数量的访问足迹
        /// </summary>
        public void Remove(int count)
        {
            if (_visitedMembers != null)
            {
                int qty = 0;
                while (count > qty)
                {
                    _visitedMembers.Pop();
                    qty += 1;
                }
            }
        }

        /// <summary>
        /// 已访问过的成员
        /// MemberExpression.Member.ReflectedType 在继承的情况下返回的值不是子类
        /// 所以需要用此类来承载 MemberExpression.Member 所在的实际类型
        /// </summary>
        public class VisitedMember
        {
            /// <summary>
            /// 字段或者属性
            /// </summary>
            public MemberInfo Member { get; set; }

            /// <summary>
            /// 字段或属性的类对象
            /// </summary>
            public Type ReflectedType { get; set; }

            /// <summary>
            /// 当前访问成员的类型
            /// 如果当前访问成员为空，则返回空
            /// </summary>
            public Type DataType
            {
                get
                {
                    if (this.Member == null) return null;
                    else
                    {
                        Type result = null;
                        MemberInfo m = this.Member;
                        if (m.MemberType == MemberTypes.Property)
                            result = ((PropertyInfo)m).PropertyType;
                        else if (m.MemberType == MemberTypes.Field)
                            result = ((FieldInfo)m).FieldType;
                        return result;
                    }
                }
            }

            internal VisitedMember(MemberInfo m, Type type)
            {
                this.Member = m;
                this.ReflectedType = type;
                if (this.ReflectedType == null) this.ReflectedType = this.Member.ReflectedType ?? this.Member.DeclaringType;
            }
        }
    }
}