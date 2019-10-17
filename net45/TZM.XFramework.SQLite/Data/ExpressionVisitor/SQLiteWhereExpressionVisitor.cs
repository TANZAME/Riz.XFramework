using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// WHERE 表达式解析器
    /// </summary>
    public class SQLiteWhereExpressionVisitor : WhereExpressionVisitor
    {
        private Expression _expression = null;
        private string _alias = null;

        /// <summary>
        /// 初始化 <see cref="WhereExpressionVisitor"/> 类的新实例
        /// </summary>
        public SQLiteWhereExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, DbExpression dbExpression, string alias)
            : base(provider, aliases, dbExpression)
        {
            _expression = base.Expression;
            _alias = alias;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node == null) return node;

            if (string.IsNullOrEmpty(_alias))
            {
                // 嵌套
                _builder.AppendMember(_alias, node.Member.Name);
                return node;
            }
            else
            {
                return base.VisitMember(node);
            }
        }
    }
}