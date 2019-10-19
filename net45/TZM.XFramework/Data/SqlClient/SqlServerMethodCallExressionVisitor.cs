

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SqlServerMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class SqlServerMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        /// <summary>
        /// 实例化 <see cref="SqlServerMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public SqlServerMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
        }

        // CAST 如果转换时没有指定数据类型的长度，则SQServer自动提供长度为30
    }
}
