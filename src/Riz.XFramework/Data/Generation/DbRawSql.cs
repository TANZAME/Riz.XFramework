
namespace Riz.XFramework.Data
{
    /// <summary>
    /// 原始 SQL 命令
    /// </summary>
    public sealed class DbRawSql
    {
        /// <summary>
        /// 当前查询上下文
        /// </summary>
        public IDbContext DbContext { get; private set; }

        /// <summary>
        /// SQL 脚本
        /// </summary>
        public string CommandText { get; private set; }

        /// <summary>
        /// 参数列表
        /// </summary>
        public object[] Parameters { get; private set; }

        /// <summary>
        /// 初始化 <see cref="DbRawSql"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        /// <param name="commandText">SQL 脚本</param>
        /// <param name="parameters">参数列表</param>
        internal DbRawSql(IDbContext context, string commandText, params object[] parameters)
        {
            this.DbContext = context;
            this.CommandText = commandText;
            this.Parameters = parameters;
        }
    }
}
