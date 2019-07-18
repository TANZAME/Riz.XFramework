using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SqlMethod方法解析服务
    /// </summary>
    public class MySqlMethodExpressionVisitor : SqlMethodExpressionVisitor
    {
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 实例化 <see cref="MySqlMethodExpressionVisitor"/> 类的新实例
        /// </summary>
        public MySqlMethodExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
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
