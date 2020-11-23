
namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据上下文，框架的主入口点，非线程安全
    /// </summary>
    public class SQLiteDbContext : DbContextBase
    {
        private IDatabase _database = null;
        private string _connString = null;
        private int? _commandTimeout = null;

        /// <summary>
        /// 查询语义提供者
        /// </summary>
        public override IDbQueryProvider Provider => SQLiteDbQueryProvider.Instance;

        /// <summary>
        /// 数据库对象，持有当前上下文的会话
        /// </summary>
        public override IDatabase Database
        {
            get
            {
                if (_database == null)
                    _database = new SQLiteDatabase(this);
                return _database;
            }
        }

        /// <summary>
        /// 初始化 <see cref="SQLiteDbContext"/> 类的新实例
        /// <para>
        /// 默认读取 XFrameworkConnString 配置里的连接串
        /// </para>
        /// </summary>
        public SQLiteDbContext()
            : base()
        {
        }

        /// <summary>
        /// 初始化 <see cref="SQLiteDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// </summary>
        public SQLiteDbContext(string connString)
            : this(connString, null)
        {
        }

        /// <summary>
        /// 初始化 <see cref="SQLiteDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// </summary>
        public SQLiteDbContext(string connString, int? commandTimeout)
            : base(connString, commandTimeout)
        {
            _connString = connString;
            _commandTimeout = commandTimeout;
        }
    }
}
