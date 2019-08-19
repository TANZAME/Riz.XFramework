

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SqlMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class SqlMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        /// <summary>
        /// 实例化 <see cref="SqlMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public SqlMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
        }

        /// <summary>
        /// 判断指定类型是否是unicode
        /// </summary>
        protected override bool IsUnicode(object dbType)
        {
            return DbTypeUtils.IsUnicode(dbType);
        }
    }
}
