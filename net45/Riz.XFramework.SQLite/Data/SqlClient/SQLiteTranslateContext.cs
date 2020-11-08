
namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// SQLite 解析SQL命令上下文
    /// </summary>
    internal class SQLiteTranslateContext : TranslateContext
    {
        /// <summary>
        /// 当前查询语义是删除意义
        /// </summary>
        public bool IsDelete { get; set; }

        /// <summary>
        /// 实例化 <see cref="SQLiteTranslateContext"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        public SQLiteTranslateContext(IDbContext context)
            : base(context)
        {

        }
    }
}
