
namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// MSSQL 语句建造器
    /// <para>非线程安全</para>
    /// </summary>
    public class SqlServerSqlBuilder : SqlBuilder
    {
        /// <summary>
        /// 实例化 <see cref="SqlServerSqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public SqlServerSqlBuilder(IDbQueryProvider provider, ResolveToken token)
            : base(provider, token)
        {

        }
    }
}
