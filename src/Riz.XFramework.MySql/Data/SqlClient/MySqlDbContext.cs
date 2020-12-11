
using MySql.Data.MySqlClient;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据上下文，框架的主入口点，非线程安全
    /// </summary>
    public class MySqlDbContext : DbContextBase
    {
        ///// <summary>
        ///// 无阻塞 WITH(NOLOCK)
        ///// </summary>
        //public bool NoLock { get; set; }

        //SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

        //SELECT* FROM bas_client;
        //SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;

        /// <summary>
        /// 查询语义提供者
        /// </summary>
        public override IDbQueryProvider Provider => MySqlDbQueryProvider.Instance;

        /// <summary>
        /// 初始化 <see cref="MySqlDbContext"/> 类的新实例
        /// <para>
        /// 默认读取 XFrameworkConnString 配置里的连接串
        /// </para>
        /// </summary>
        public MySqlDbContext()
            : base()
        {
        }

        /// <summary>
        /// 初始化 <see cref="MySqlDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// </summary>
        public MySqlDbContext(string connString)
            : base(connString)
        {
        }

        /// <summary>
        /// 初始化 <see cref="MySqlDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// </summary>
        public MySqlDbContext(string connString, int? commandTimeout)
            : base(connString, commandTimeout)
        {
        }

        // TODO MySqlBulkCopy

        //配置my.ini文件
        //bulk_insert_buffer_size=120M 或者更大
        //将insert语句的长度设为最大。
        //Max_allowed_packet=1M
        //Net_buffer_length=8k
    }
}
