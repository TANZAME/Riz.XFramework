using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SqlMethod方法解析服务
    /// </summary>
    public class OracleSqlMethodExpressionVisitor : SqlMethodExpressionVisitor
    {
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 实例化 <see cref="OracleSqlMethodExpressionVisitor"/> 类的新实例
        /// </summary>
        public OracleSqlMethodExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
            _builder = visitor.SqlBuilder;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected override Expression VisitNewGuid(MethodCallExpression m)
        {
            _builder.Append("UUID()");
            return m;
        }
    }
}
