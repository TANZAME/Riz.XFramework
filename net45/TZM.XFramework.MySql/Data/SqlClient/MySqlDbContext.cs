
using MySql.Data.MySqlClient;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据上下文，表示 Xfw 框架的主入口点
    /// </summary>
    public class MyMySqlDbContext : DbContextBase
    {
        /// <summary>
        /// 初始化 <see cref="MySqlDbContext"/> 类的新实例
        /// <para>
        /// 默认读取 XFrameworkConnString 配置里的连接串
        /// </para>
        /// </summary>
        public MyMySqlDbContext()
            : base()
        {
        }

        /// <summary>
        /// 初始化 <see cref="MySqlDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// </summary>
        public MyMySqlDbContext(string connString)
            : base(connString)
        {
        }

        /// <summary>
        /// 初始化 <see cref="MySqlDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// </summary>
        public MyMySqlDbContext(string connString, int? commandTimeout)
            : base(connString, commandTimeout)
        {
        }

        // 创建据库会话实例
        protected override IDatabase CreateDatabase(string connString, int? commandTimeout)
        {
            return new Database(MySqlClientFactory.Instance, connString)
            {
                CommandTimeout = commandTimeout
            };
        }

        // 创建数据查询提供者对象实例
        protected override IDbQueryProvider CreateQueryProvider()
        {
            return SqlClient.MySqlDbQueryProvider.Instance;
        }
    }
}
