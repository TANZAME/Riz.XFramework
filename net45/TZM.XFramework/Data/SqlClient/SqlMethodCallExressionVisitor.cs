

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
            base.Container.Replace(typeof(string), (provider2, visitor2) => new SqlStringExpressionVisitor(provider2, visitor2));
        }

        /// <summary>
        /// 字符串类型方法解析服务
        /// </summary>
        public class SqlStringExpressionVisitor : StringExpressionVisitor
        {
            /// <summary>
            /// 实例化 <see cref="SqlStringExpressionVisitor"/> 类的新实例
            /// </summary>
            public SqlStringExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
                : base(provider, visitor)
            {
            }

            /// <summary>
            /// 判断指定类型是否是unicode
            /// </summary>
            protected override bool IsUnicode(object dbType)
            {
                return dbType == null ? true : SqlDbTypeInfo.Create(dbType).IsUnicode;
            }
        }
    }
}
