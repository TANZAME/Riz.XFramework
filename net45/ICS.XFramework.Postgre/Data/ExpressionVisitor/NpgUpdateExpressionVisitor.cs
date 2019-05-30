using System.Linq.Expressions;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    public class NpgUpdateExpressionVisitor : ExpressionVisitorBase
    {
        private IDbQueryProvider _provider = null;
        private TableAliasCache _aliases = null;

        /// <summary>
        /// 初始化 <see cref="NpgUpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        public NpgUpdateExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, Expression exp)
            : base(provider, aliases, exp)
        {
            _provider = provider;
            _aliases = aliases;
        }

        //{new App() {Id = p.Id}} 
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings.Count == 0) throw new XFrameworkException("Update<T>(Expression<Func<T, T>> action, Expression<Func<T, bool>> predicate) at least update one member.");

            for (int index = 0; index < node.Bindings.Count; ++index)
            {
                MemberAssignment member = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember(member.Member.Name);
                _builder.Append(" = ");

                if (member.Expression.CanEvaluate())
                    _builder.Append(member.Expression.Evaluate().Value, member.Member, node.Type);
                else
                    base.Visit(member.Expression);

                if (index < node.Bindings.Count - 1)
                {
                    _builder.Append(",");
                    _builder.AppendNewLine();
                }
            }
            return node;
        }
    }
}
