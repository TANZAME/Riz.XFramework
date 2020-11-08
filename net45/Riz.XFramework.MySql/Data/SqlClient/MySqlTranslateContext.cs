
namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// MSSQL 语句建造器
    /// </summary>
    internal class MySqlTranslateContext : TranslateContext
    {
        /// <summary>
        /// 当前查询语义是删除意义
        /// </summary>
        public bool IsDelete { get; set; }

        /// <summary>
        /// 实例化 <see cref="MySqlTranslateContext"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        public MySqlTranslateContext(IDbContext context)
            : base(context)
        {

        }
    }
}
