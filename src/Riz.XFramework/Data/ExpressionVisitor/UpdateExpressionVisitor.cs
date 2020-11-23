
using System;
using System.Reflection;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    internal class UpdateExpressionVisitor : DbExpressionVisitor
    {
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 初始化 <see cref="UpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public UpdateExpressionVisitor(AliasGenerator ag, ISqlBuilder builder)
            : base(ag, builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// 访问成员初始化表达式，如 => new App() { Id = p.Id }
        /// </summary>
        /// <param name="node">要访问的成员初始化表达式</param>
        /// <returns></returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings == null || node.Bindings.Count == 0)
                throw new XFrameworkException("The Update<T> method requires at least one field to be updated.");

            for (int index = 0; index < node.Bindings.Count; index++)
            {
                var m = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember("t0", m.Member, node.Type);
                _builder.Append(" = ");

                // => Name = "Name" 
                this.VisitWithoutRemark(_ => this.VisitObjectMember(node.Type, m.Member, m.Expression));

                if (index < node.Bindings.Count - 1)
                {
                    _builder.Append(",");
                    _builder.AppendNewLine();
                }
            }
            return node;
        }

        /// <summary>
        /// 访问构造函数表达式，如 =>new  { Id = p.Id }
        /// </summary>
        /// <param name="node">构造函数调用的表达式</param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            // 匿名类的New
            if (node == null) return node;
            if (node.Arguments == null || node.Arguments.Count == 0 || node.Members.Count == 0)
                throw new XFrameworkException("The Update<T> method requires at least one field to be updated.");

            for (int index = 0; index < node.Arguments.Count; index++)
            {
                _builder.AppendMember("t0", node.Members[index], node.Type);
                _builder.Append(" = ");

                // => Name = "Name" 
                this.VisitWithoutRemark(_ => this.VisitObjectMember(node.Type, node.Members[index], node.Arguments[index]));

                if (index < node.Arguments.Count - 1)
                {
                    _builder.Append(',');
                    _builder.AppendNewLine();
                }
            }

            return node;
        }

        // 访问对象成员
        private Expression VisitObjectMember(Type newType, MemberInfo member, Expression expression)
        {
            // 先添加当前字段的访问痕迹标记
            base.VisitedStack.Add(member, newType);
            return base.Visit(expression);
        }
    }
}