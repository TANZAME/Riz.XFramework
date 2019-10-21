
namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    public class MySqlSqlBuilder : SqlBuilder
    {
        /// <summary>
        /// 实例化 <see cref="MySqlSqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public MySqlSqlBuilder(IDbQueryProvider provider, ResolveToken token)
            : base(provider, token)
        {

        }
    }
}
