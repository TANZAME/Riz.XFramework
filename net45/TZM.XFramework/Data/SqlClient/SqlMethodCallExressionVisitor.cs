

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SqlMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class SqlMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        Internal.MethodCallExressionVisitorContainer _internalVisitors = null;

        /// <summary>
        /// 解析方法调用的服务容器
        /// </summary>
        protected override Internal.MethodCallExressionVisitorContainer InternalVisitors
        {
            get
            {
                if (_internalVisitors == null)
                {
                    // 自定义字符串解析器
                    _internalVisitors = base.InternalVisitors;
                    _internalVisitors.Replace(typeof(string), (provider, visitor) => new StringMethodCallExpressionVisitor(provider, visitor));
                }
                return _internalVisitors;
            }
        }

        /// <summary>
        /// 实例化 <see cref="SqlMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public SqlMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
        }

        #region 解析服务

        /// <summary>
        /// 字符串类型方法解析服务
        /// </summary>
        public class StringMethodCallExpressionVisitor : Internal.StringMethodCallExpressionVisitor
        {
            /// <summary>
            /// 实例化 <see cref="StringMethodCallExpressionVisitor"/> 类的新实例
            /// </summary>
            public StringMethodCallExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
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

        #endregion
    }
}
