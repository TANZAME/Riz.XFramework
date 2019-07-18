using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SqlMethod方法解析服务
    /// </summary>
    public class SqlMethodExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;

        /// <summary>
        /// 实例化 <see cref="SqlMethodExpressionVisitor"/> 类的新实例
        /// </summary>
        public SqlMethodExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
        }

        /// <summary>
        /// 访问 RowNumber 方法
        /// </summary>
        protected virtual Expression VisitSQLRowNumber(MethodCallExpression m)
        {
            if (m == null) return m;

            _builder.Append("ROW_NUMBER() Over(Order By ");
            _visitor.Visit(m.Arguments[0]);
            if (m.Arguments.Count > 1)
            {
                var c = (ConstantExpression)m.Arguments[1];
                if (!((bool)c.Value)) _builder.Append(" DESC");
            }

            _builder.Append(')');

            return m;
        }

        /// <summary>
        /// 访问 RowNumber 方法
        /// </summary>
        protected virtual Expression VisitSQLPartitionRowNumber(MethodCallExpression m)
        {
            if (m == null) return m;

            _builder.Append("ROW_NUMBER() Over(");

            // PARTITION BY
            _builder.Append("PARTITION BY ");
            _visitor.Visit(m.Arguments[0]);

            // ORDER BY
            _builder.Append(" ORDER BY ");
            _visitor.Visit(m.Arguments[1]);
            if (m.Arguments.Count > 2)
            {
                var c = (ConstantExpression)m.Arguments[2];
                if (!((bool)c.Value)) _builder.Append(" DESC");
            }

            _builder.Append(')');

            return m;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected virtual Expression VisitNewGuid(MethodCallExpression m)
        {
            _builder.Append("NEWID()");
            return m;
        }
    }
}
