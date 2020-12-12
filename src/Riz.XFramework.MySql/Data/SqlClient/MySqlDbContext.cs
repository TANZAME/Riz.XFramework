
#if net40
using MySql.Data.MySqlClient;
#else
using System.Data;
using System.Collections.Generic;
using MySqlConnector;
using System.Threading.Tasks;
#endif

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

#if !net40
        /// <summary>
        /// 使用 MYSQL 的BulkCopy语法批量写入数据，其中 DestinationTableName 等于 DataTable.TableName
        /// <para>
        /// 1. 参看：https://mysqlconnector.net/api/mysqlconnector/mysqlbulkcopytype/ 
        /// 2. 需要开启 local_infile 参数
        /// </para>
        /// </summary>
        /// <param name="table">数据源</param>
        public void BulkCopy(DataTable table) => this.BulkCopy(table, null);

        /// <summary>
        /// 使用 MYSQL 的BulkCopy语法批量写入数据，其中 DestinationTableName 等于 DataTable.TableName
        /// </summary>
        /// <param name="table">数据源</param>
        /// <param name="mappings">数据源中的列与目标表中的列之间的映射集合</param>
        public void BulkCopy(DataTable table, IEnumerable<MySqlBulkCopyColumnMapping> mappings)
        {
            bool wasClosed = false;
            try
            {
                var bulkCopy = this.CreateBulkCopy(table, mappings, out wasClosed);
                bulkCopy.WriteToServer(table);
            }
            finally
            {
                if (wasClosed) base.Dispose();
            }
        }

        /// <summary>
        /// 使用 MYSQL 的BulkCopy语法批量写入数据，其中 DestinationTableName 等于 DataTable.TableName
        /// </summary>
        /// <param name="table">数据源</param>
        public async Task BulkCopyAsync(DataTable table) => await this.BulkCopyAsync(table, null);

        /// <summary>
        /// 使用 MYSQL 的BulkCopy语法批量写入数据，其中 DestinationTableName 等于 DataTable.TableName
        /// </summary>
        /// <param name="table">数据源</param>
        /// <param name="mappings">数据源中的列与目标表中的列之间的映射集合</param>
        public async Task BulkCopyAsync(DataTable table, IEnumerable<MySqlBulkCopyColumnMapping> mappings)
        {
            bool wasClosed = false;
            try
            {
                var bulkCopy = this.CreateBulkCopy(table, mappings, out wasClosed);
                await bulkCopy.WriteToServerAsync(table);
            }
            finally
            {
                if (wasClosed) base.Dispose();
            }
        }

        // 创建批量写入对象
        private MySqlBulkCopy CreateBulkCopy(DataTable table, IEnumerable<MySqlBulkCopyColumnMapping> mapping, out bool wasClosed)
        {
            wasClosed = false;
            var conn = this.Database.Connection;
            if (conn == null)
            {
                conn = base.Database.CreateConnection(true);
                wasClosed = true;
            }

            var bulkCopy = new MySqlBulkCopy((MySqlConnection)conn, this.Database.Transaction as MySqlTransaction);
            bulkCopy.DestinationTableName = table.TableName;
            if (mapping == null)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                    bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, table.Columns[i].ColumnName));
            }
            else
            {
                foreach (var m in mapping)
                    bulkCopy.ColumnMappings.Add(m);
            }

            return bulkCopy;
        }
#endif

        //配置my.ini文件
        //bulk_insert_buffer_size=120M 或者更大
        //将insert语句的长度设为最大。
        //Max_allowed_packet=1M
        //Net_buffer_length=8k
    }
}
