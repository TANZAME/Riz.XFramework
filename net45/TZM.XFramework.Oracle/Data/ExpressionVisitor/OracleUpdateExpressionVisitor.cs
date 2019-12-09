using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    public class OracleUpdateExpressionVisitor : UpdateExpressionVisitor
    {
        private IDbQueryProvider _provider = null;
        private TableAliasCache _aliases = null;

        /// <summary>
        /// 初始化 <see cref="OracleUpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="expression">要访问的表达式</param>
        public OracleUpdateExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, Expression expression)
            : base(provider, aliases, expression)
        {
            _provider = provider;
            _aliases = aliases;
        }

        /// <summary>
        /// 访问成员初始化表达式，如 =>{new App() {Id = p.Id}}
        /// </summary>
        /// <param name="node">要访问的成员初始化表达式</param>
        /// <returns></returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings == null || node.Bindings.Count == 0)
                throw new XFrameworkException("Update<T> at least update one member.");

            for (int index = 0; index < node.Bindings.Count; ++index)
            {
                MemberAssignment member = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember("t0", member.Member.Name);
                _builder.Append(" = ");
                _builder.AppendMember("t1", member.Member.Name);

                if (index < node.Bindings.Count - 1)
                {
                    _builder.Append(',');
                    _builder.AppendNewLine();
                }
            }
            return node;
        }

        /// <summary>
        /// 访问构造函数表达式，如 =>new  {Id = p.Id}}
        /// </summary>
        /// <param name="node">构造函数调用的表达式</param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            // 匿名类的New
            if (node == null) return node;
            if (node.Arguments == null || node.Arguments.Count == 0)
                throw new XFrameworkException("Update<T> at least update one member.");

            for (int index = 0; index < node.Arguments.Count; index++)
            {
                var member = node.Members[index];
                _builder.AppendMember("t0", member.Name);
                _builder.Append(" = ");
                _builder.AppendMember("t1", member.Name);

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
