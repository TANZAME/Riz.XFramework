using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    public class OracleUpdateExpressionVisitor : UpdateExpressionVisitor
    {
        /// <summary>
        /// 初始化 <see cref="OracleUpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasResolver">表别名解析器</param>
        /// <param name="expression">查询语义</param>
        public OracleUpdateExpressionVisitor(TableAliasResolver aliasResolver, Expression expression)
            : base(aliasResolver, expression)
        {
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

            for (int index = 0; index < node.Bindings.Count; ++index)
            {
                var m = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember("t0", m.Member, node.Type);
                _builder.Append(" = ");
                _builder.AppendMember("t1", m.Member, node.Type);

                if (index < node.Bindings.Count - 1)
                {
                    _builder.Append(',');
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
                var member = node.Members[index];
                _builder.AppendMember("t0", member, node.Type);
                _builder.Append(" = ");
                _builder.AppendMember("t1", member, node.Type);

                if (index < node.Arguments.Count - 1)
                {
                    _builder.Append(',');
                    _builder.AppendNewLine();
                }
            }

            return node;
        }
    }
}
