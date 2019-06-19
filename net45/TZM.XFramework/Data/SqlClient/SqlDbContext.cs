
using System.Data;
using System.Data.SqlClient;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据上下文，表示 Xfw 框架的主入口点
    /// </summary>
    public class SqlDbContext : DbContextBase
    {
        /// <summary>
        /// 无阻塞 WITH(NOLOCK)
        /// </summary>
        public bool NoLock { get; set; }

        /// <summary>
        /// 初始化 <see cref="SqlDbContext"/> 类的新实例
        /// <para>
        /// 默认读取 XFrameworkConnString 配置里的连接串
        /// </para>
        /// </summary>
        public SqlDbContext()
            : base()
        {
        }

        /// <summary>
        /// 初始化 <see cref="SqlDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// </summary>
        public SqlDbContext(string connString)
            : base(connString)
        {
        }

        /// <summary>
        /// 初始化 <see cref="SqlDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// </summary>
        public SqlDbContext(string connString, int? commandTimeout)
            : base(connString, commandTimeout)
        {
        }

        /// <summary>
        /// 使用 SQLSERVER 的BulkCopy语法批量写入数据
        /// </summary>
        /// <param name="table"></param>
        public void BulkCopy(DataTable table)
        {
            bool wasClosed = false;
            var conn = base.Database.Connection;
            if (conn == null)
            {
                conn = base.Database.CreateConnection(true);
                wasClosed = true;
            }

            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)conn, SqlBulkCopyOptions.Default, base.Database.Transaction as SqlTransaction))
                {
                    bulkCopy.DestinationTableName = table.TableName;
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        bulkCopy.ColumnMappings.Add(table.Columns[i].ColumnName, table.Columns[i].ColumnName);
                    }
                    bulkCopy.WriteToServer(table);
                }
            }
            finally
            {
                if (wasClosed) base.Dispose();
            }
        }

        // 创建据库会话实例
        protected override IDatabase CreateDatabase(string connString, int? commandTimeout)
        {
            return new Database(SqlClientFactory.Instance, connString)
            {
                CommandTimeout = commandTimeout
            };
        }

        // 创建数据查询提供者对象实例
        protected override IDbQueryProvider CreateQueryProvider()
        {
            return SqlClient.SqlDbQueryProvider.Instance;
        }
    }
}
