using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    public class UpdateExpressionVisitor : ExpressionVisitorBase
    {
        private IDbQueryProvider _provider = null;
        private TableAliasCache _aliases = null;

        /// <summary>
        /// 初始化 <see cref="UpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        public UpdateExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, Expression exp)
            : base(provider, aliases, exp)
        {
            _provider = provider;
            _aliases = aliases;
        }

        //{new App() {Id = p.Id}}
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings == null || node.Bindings.Count == 0)
                throw new XFrameworkException("Update<T> at least update one member.");

            for (int index = 0; index < node.Bindings.Count; ++index)
            {
                MemberAssignment member = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember("t0", member.Member.Name);
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

                if (node.Arguments[index].CanEvaluate())
                    _builder.Append(node.Arguments[index].Evaluate().Value, member, node.Type);
                else
                    base.Visit(node.Arguments[index]);

                if (index < node.Arguments.Count - 1)
                {
                    _builder.Append(",");
                    _builder.AppendNewLine();
                }
            }

            return node;
        }
    }
}