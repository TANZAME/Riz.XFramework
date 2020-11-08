
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据上下文，框架的主入口点，非线程安全
    /// </summary>
    public class SqlServerDbContext : DbContextBase
    {
        private string _version = null;

        /// <summary>
        /// 数据库版本号
        /// </summary>
        public string Version
        {
            get
            {
                if (_version == null)
                    _version = this.Database.Execute<string>("SELECT SERVERPROPERTY('ProductVersion')");
                return _version;
            }
        }

        /// <summary>
        /// 无阻塞 WITH(NOLOCK)
        /// </summary>
        public bool NoLock { get; set; }

        /// <summary>
        /// 查询语义提供者
        /// </summary>
        public override IDbQueryProvider Provider { get { return SqlServerDbQueryProvider.Instance; } }

        /// <summary>
        /// 初始化 <see cref="SqlServerDbContext"/> 类的新实例
        /// <para>
        /// 默认读取 XFrameworkConnString 配置里的连接串
        /// </para>
        /// </summary>
        public SqlServerDbContext()
            : base()
        {
        }

        /// <summary>
        /// 初始化 <see cref="SqlServerDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// </summary>
        public SqlServerDbContext(string connString)
            : this(connString, null)
        {
        }

        /// <summary>
        /// 初始化 <see cref="SqlServerDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// </summary>
        public SqlServerDbContext(string connString, int? commandTimeout)
            : base(connString, commandTimeout)
        {
        }

        /// <summary>
        /// 更新自增列
        /// </summary>
        /// <param name="dbQueryables">查询语义集合</param>
        /// <param name="identitys">自动ID</param>
        protected override void SetIdentityValue(List<object> dbQueryables, List<int> identitys)
        {
            if (identitys == null || identitys.Count == 0) return;

            int index = -1;
            foreach (var obj in dbQueryables)
            {
                var dbQuery_s = obj as IDbQueryable;
                var bulkList = obj as List<IDbQueryable>;
                if (dbQuery_s == null && bulkList == null) continue;

                if (dbQuery_s != null)
                    bulkList = new List<IDbQueryable> { dbQuery_s };

                foreach (var dbQuery in bulkList)
                {
                    if (dbQuery.DbExpressions == null) continue;
                    else if (dbQuery.DbExpressions.Count == 0) continue;

                    var dbExpression = dbQuery.DbExpressions.FirstOrDefault(x => x.DbExpressionType == DbExpressionType.Insert);
                    if (dbExpression == null) continue;
                    else if (dbExpression.Expressions == null) continue;
                    else if (dbExpression.Expressions[0].NodeType != ExpressionType.Constant) continue;

                    var entity = (dbExpression.Expressions[0] as ConstantExpression).Value;
                    if (entity != null)
                    {
                        var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(entity.GetType());
                        if (typeRuntime.Identity != null)
                        {
                            index += 1;
                            var identity = identitys[index];
                            typeRuntime.Identity.Invoke(entity, identity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 使用 SQLSERVER 的BulkCopy语法批量写入数据，其中 DestinationTableName 等于 DataTable.TableName
        /// </summary>
        /// <param name="table">数据源</param>
        public void BulkCopy(DataTable table)
        {
            this.BulkCopy(table, null);
        }

        /// <summary>
        /// 使用 SQLSERVER 的BulkCopy语法批量写入数据，其中 DestinationTableName 等于 DataTable.TableName
        /// </summary>
        /// <param name="table">数据源</param>
        /// <param name="mapping">数据源中的列与目标表中的列之间的映射集合</param>
        public void BulkCopy(DataTable table, IEnumerable<SqlBulkCopyColumnMapping> mapping)
        {
            bool wasClosed = false;
            var conn = this.Database.Connection;
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
                    if (mapping == null)
                    {
                        for (int i = 0; i < table.Columns.Count; i++)
                        {
                            bulkCopy.ColumnMappings.Add(table.Columns[i].ColumnName, table.Columns[i].ColumnName);
                        }
                    }
                    else
                    {
                        foreach (var m in mapping)
                        {
                            bulkCopy.ColumnMappings.Add(m);
                        }
                    }
                    bulkCopy.WriteToServer(table);
                }
            }
            finally
            {
                if (wasClosed) base.Dispose();
            }
        }
    }
}
