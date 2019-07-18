using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SqlMethod方法解析服务
    /// </summary>
    public class NpgSqlMethodExpressionVisitor : SqlMethodExpressionVisitor
    {
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 实例化 <see cref="SqlMethodVisitor"/> 类的新实例
        /// </summary>
        public NpgSqlMethodExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
            _builder = visitor.SqlBuilder;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected override Expression VisitNewGuid(MethodCallExpression m)
        {
            System.Guid guid = System.Guid.NewGuid();
            _builder.Append(guid, null);
            return m;
        }
    }
}
