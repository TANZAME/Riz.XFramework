
using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据库操作对象
    /// </summary>
    public partial class Database : IDatabase, IDisposable
    {
        #region 私有字段

        // 批量执行SQL时每次执行命令条数
        private int _commandExecuteSize = 200;
        private string _connString = string.Empty;
        private DbProviderFactory _dbProviderFactory = null;
        private IDbConnection _connection = null;
        private IDbTransaction _transaction = null;
        // 如果不是外部调用BeginTransaction，则执行完命令后需要自动提交-释放事务
        private bool _autoComplete = false;

        #endregion

        #region 公开属性

        /// <summary>
        /// 数据源类提供者
        /// </summary>
        public DbProviderFactory DbProviderFactory
        {
            get { return _dbProviderFactory; }
        }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get { return _connString; } }

        /// <summary>
        /// 执行命令超时时间
        /// </summary>
        public int? CommandTimeout { get; set; }

        /// <summary>
        /// ~批次执行的SQL数量，默认200个查询语句
        /// </summary>
        public int CommanExecuteSize { get { return _commandExecuteSize; } }

        /// <summary>
        /// 当前连接会话
        /// </summary>
        public IDbConnection Connection { get { return _connection; } }

        /// <summary>
        /// 获取或者设置当前会话事务
        /// </summary>
        public IDbTransaction Transaction
        {
            get { return _transaction; }
            set
            {
                // 使用外部事务
                if (value == null) throw new ArgumentNullException("value");
                if (_transaction != null && _transaction != value) throw new XFrameworkException("There are currently uncommitted transactions");
                if (_transaction != value)
                {
                    _transaction = value;
                    _connection = _transaction.Connection;
                }
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="Database"/> 类的新实例
        /// </summary>
        /// <param name="providerFactory">数据源提供者</param>
        /// <param name="connectionString">数据库连接字符串</param>
        public Database(DbProviderFactory providerFactory, string connectionString)
        {
            _connString = connectionString;
            _dbProviderFactory = providerFactory;
        }

        #endregion

        #region 接口实现

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <param name="isOpen">是否打开连接</param>
        /// <returns></returns>
        public IDbConnection CreateConnection(bool isOpen = false)
        {
            if (_connection == null)
            {
                _connection = this.DbProviderFactory.CreateConnection();
                _connection.ConnectionString = this.ConnectionString;
            }
            if (isOpen && _connection.State != ConnectionState.Open)
            {
                if (string.IsNullOrEmpty(_connection.ConnectionString)) _connection.ConnectionString = _connString;
                _connection.Open();
            }
            return _connection;
        }

        /// <summary>
        /// 创建事务
        /// </summary>
        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_transaction == null)
            {
                this.CreateConnection(true);
                _transaction = _connection.BeginTransaction(isolationLevel);
            }
            return _transaction;
        }

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="definition">命令描述</param>
        /// <returns></returns>
        public IDbCommand CreateCommand(DbCommandDefinition definition)
        {
            return this.CreateCommand(definition.CommandText, definition.CommandType, definition.Parameters);
        }

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="commandText">SQL 语句</param>
        /// <param name="commandType">命令类型</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public IDbCommand CreateCommand(string commandText, CommandType? commandType = null, IEnumerable<IDbDataParameter> parameters = null)
        {
            IDbCommand cmd = this.DbProviderFactory.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandTimeout = this.CommandTimeout != null ? this.CommandTimeout.Value : 300; // 5分钟
            if (commandType != null) cmd.CommandType = commandType.Value;
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            if (_transaction != null)
            {
                cmd.Connection = _transaction.Connection;
                cmd.Transaction = _transaction;
            }

            return cmd;
        }

        /// <summary>
        /// 创建命令参数
        /// </summary>
        /// <returns></returns>
        public virtual IDbDataParameter CreateParameter()
        {
            return this.DbProviderFactory.CreateParameter();
        }

        /// <summary>
        /// 创建命令参数
        /// </summary>
        /// <param name="name">参数名称</param>
        /// <param name="value">参数值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="size">参数大小</param>
        /// <param name="precision">精度</param>
        /// <param name="scale">小数位</param>
        /// <param name="direction">方向</param>
        /// <returns></returns>
        public virtual IDbDataParameter CreateParameter(string name, object value,
            DbType? dbType = null, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            IDbDataParameter parameter = this.DbProviderFactory.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;

            if (dbType != null) parameter.DbType = dbType.Value;
            if (size != null && (size.Value > 0 || size.Value == -1)) parameter.Size = size.Value;
            if (precision != null && precision.Value > 0) parameter.Precision = (byte)precision.Value;
            if (scale != null && scale.Value > 0) parameter.Scale = (byte)scale.Value;
            if (direction != null) parameter.Direction = direction.Value;
            else parameter.Direction = ParameterDirection.Input;

            // 补充字符串的长度
            if (value != null && value.GetType() == typeof(string) && size == null)
            {
                string s = value.ToString();
                if (dbType == null) parameter.DbType = DbType.String;
                if (parameter.DbType == DbType.String || parameter.DbType == DbType.StringFixedLength ||
                    parameter.DbType == DbType.AnsiString || parameter.DbType == DbType.AnsiStringFixedLength)
                {
                    if (s.Length <= 256) parameter.Size = 256;
                    else if (s.Length <= 512) parameter.Size = 512;
                    else if (s.Length <= 1024) parameter.Size = 1024;
                    else if (s.Length <= 4000) parameter.Size = 4000;
                    else if (s.Length <= 8000) parameter.Size = 8000;
                    else parameter.Size = -1;
                }
            }

            // 返回创建的参数
            return parameter;
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="query">SQL 命令</param>
        public int ExecuteNonQuery(IDbQueryable query)
        {
            var define = query.Resolve();
            IDbCommand cmd = this.CreateCommand(define);
            return this.ExecuteNonQuery(cmd);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        public int ExecuteNonQuery(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return this.ExecuteNonQuery(cmd);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        public int ExecuteNonQuery(List<DbCommandDefinition> sqlList)
        {
            int rowCount = 0;
            this.DoExecute<int>(sqlList, cmd => rowCount += this.ExecuteNonQuery(cmd));
            return rowCount;
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public int ExecuteNonQuery(IDbCommand cmd)
        {
            return this.DoExecute<int>(cmd, p => p.ExecuteNonQuery(), cmd.Transaction == null);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="query">查询语义</param>        
        /// <returns></returns>
        public object ExecuteScalar(IDbQueryable query)
        {
            IDbCommand cmd = this.CreateCommand(query.Resolve());
            return this.ExecuteScalar(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public object ExecuteScalar(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return this.ExecuteScalar(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public object ExecuteScalar(List<DbCommandDefinition> sqlList)
        {
            return this.DoExecute<object>(sqlList, this.ExecuteScalar);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public object ExecuteScalar(IDbCommand cmd)
        {
            return this.DoExecute<object>(cmd, p => p.ExecuteScalar(), cmd.Transaction == null);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="query">查询语义</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(IDbQueryable query)
        {
            IDbCommand cmd = this.CreateCommand(query.Resolve());
            return this.ExecuteReader(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return this.ExecuteReader(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(List<DbCommandDefinition> sqlList)
        {
            return this.DoExecute<IDataReader>(sqlList, this.ExecuteReader);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(IDbCommand cmd)
        {
            return this.DoExecute<IDataReader>(cmd, p => p.ExecuteReader(CommandBehavior.SequentialAccess), false);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="commandText">查询语句</param>
        /// <returns></returns>
        public T Execute<T>(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return this.Execute<T>(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="query">查询语句</param>
        /// <returns></returns>
        public T Execute<T>(IDbQueryable<T> query)
        {
            DbCommandDefinition definition = query.Resolve();
            IDbCommand cmd = this.CreateCommand(definition);
            return this.Execute<T>(cmd, definition as DbCommandDefinition_Select);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="sqlList">查询语句</param>
        /// <returns></returns>
        public T Execute<T>(List<DbCommandDefinition> sqlList)
        {
            return this.DoExecute<T>(sqlList, cmd => this.Execute<T>(cmd, sqlList.FirstOrDefault(x => x is DbCommandDefinition_Select) as DbCommandDefinition_Select));
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        public T Execute<T>(List<DbCommandDefinition> sqlList, Func<IDbCommand, T> func)
        {
            return this.DoExecute<T>(sqlList, func);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public T Execute<T>(IDbCommand cmd)
        {
            return this.Execute<T>(cmd, null);
        }

        // 执行SQL 语句，并返回单个实体对象
        T Execute<T>(IDbCommand cmd, DbCommandDefinition_Select definition)
        {
            IDataReader reader = null;

            try
            {
                reader = this.ExecuteReader(cmd);
                TypeDeserializer deserializer = new TypeDeserializer(reader, definition);
                List<T> result = deserializer.Deserialize<T>();
                return result.FirstOrDefault();
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        public virtual Tuple<List<T1>, List<T2>> ExecuteMultiple<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2)
        {
            List<DbCommandDefinition> sqlList = query1.Provider.Resolve(new List<object> { query1, query2 });
            var result = this.DoExecute<Tuple<List<T1>, List<T2>, List<None>, List<None>, List<None>, List<None>, List<None>>>(sqlList,
                cmd => this.ExecuteMultiple<T1, T2, None, None, None, None, None>(cmd, sqlList.ToList(x => x as DbCommandDefinition_Select)));
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        public virtual Tuple<List<T1>, List<T2>, List<T3>> ExecuteMultiple<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3)
        {
            List<DbCommandDefinition> sqlList = query1.Provider.Resolve(new List<object> { query1, query2, query3 });
            var result = this.DoExecute<Tuple<List<T1>, List<T2>, List<T3>, List<None>, List<None>, List<None>, List<None>>>(sqlList,
                cmd => this.ExecuteMultiple<T1, T2, T3, None, None, None, None>(cmd, sqlList.ToList(x => x as DbCommandDefinition_Select)));
            return new Tuple<List<T1>, List<T2>, List<T3>>(result.Item1, result.Item2, result.Item3);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        public virtual Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ExecuteMultiple<T1, T2, T3, T4, T5, T6, T7>(IDbCommand cmd)
        {
            return this.ExecuteMultiple<T1, T2, T3, T4, T5, T6, T7>(cmd, null);
        }

        // 执行 SQL 语句，并返回多个实体集合
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ExecuteMultiple<T1, T2, T3, T4, T5, T6, T7>(IDbCommand cmd, List<DbCommandDefinition_Select> defines = null)
        {
            IDataReader reader = null;
            List<T1> q1 = null;
            List<T2> q2 = null;
            List<T3> q3 = null;
            List<T4> q4 = null;
            List<T5> q5 = null;
            List<T6> q6 = null;
            List<T7> q7 = null;

            TypeDeserializer deserializer1 = null;
            TypeDeserializer deserializer2 = null;
            TypeDeserializer deserializer3 = null;
            TypeDeserializer deserializer4 = null;
            TypeDeserializer deserializer5 = null;
            TypeDeserializer deserializer6 = null;
            TypeDeserializer deserializer7 = null;

            try
            {
                int i = 0;
                reader = this.ExecuteReader(cmd);

                do
                {
                    i += 1;

                    switch (i)
                    {
                        #region 元组赋值

                        case 1:
                            if (deserializer1 == null) deserializer1 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q1 = deserializer1.Deserialize<T1>();

                            break;

                        case 2:
                            if (deserializer2 == null) deserializer2 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q2 = deserializer2.Deserialize<T2>();

                            break;

                        case 3:
                            if (deserializer3 == null) deserializer3 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q3 = deserializer3.Deserialize<T3>();

                            break;

                        case 4:
                            if (deserializer4 == null) deserializer4 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q4 = deserializer4.Deserialize<T4>();

                            break;

                        case 5:
                            if (deserializer5 == null) deserializer5 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q5 = deserializer5.Deserialize<T5>();

                            break;

                        case 6:
                            if (deserializer6 == null) deserializer6 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q6 = deserializer6.Deserialize<T6>();

                            break;

                        case 7:
                            if (deserializer7 == null) deserializer7 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q7 = deserializer7.Deserialize<T7>();

                            break;

                            #endregion
                    }
                }
                while (reader.NextResult());
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>(q1 ?? new List<T1>(), q2 ?? new List<T2>(), q3 ?? new List<T3>(), q4 ?? new List<T4>(), q5 ?? new List<T5>(), q6 ?? new List<T6>(), q7 ?? new List<T7>());
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public List<T> ExecuteList<T>(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return this.ExecuteList<T>(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        public List<T> ExecuteList<T>(IDbQueryable<T> query)
        {
            DbCommandDefinition definition = query.Resolve();
            IDbCommand cmd = this.CreateCommand(definition);
            return this.ExecuteList<T>(cmd, definition as DbCommandDefinition_Select);
        }

        /// <summary>
        /// 执行SQL 语句，并返回并返回单结果集集合
        /// <para>使用第一个 <see cref="DbCommandDefinition_Select"/> 做为实体反序列化描述</para>
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public List<T> ExecuteList<T>(List<DbCommandDefinition> sqlList)
        {
            return this.DoExecute<List<T>>(sqlList, cmd => this.ExecuteList<T>(cmd, sqlList.FirstOrDefault(x => x is DbCommandDefinition_Select) as DbCommandDefinition_Select));
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public List<T> ExecuteList<T>(IDbCommand cmd)
        {
            return this.ExecuteList<T>(cmd, null);
        }

        // 执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        List<T> ExecuteList<T>(IDbCommand cmd, DbCommandDefinition_Select definition)
        {
            IDataReader reader = null;
            List<T> objList = new List<T>();

            try
            {
                reader = this.ExecuteReader(cmd);
                TypeDeserializer deserializer = new TypeDeserializer(reader, definition);
                objList = deserializer.Deserialize<T>();
            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();
                this.InternalDispose();
            }

            return objList;
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return this.ExecuteDataTable(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(IDbQueryable query)
        {
            DbCommandDefinition definition = query.Resolve();
            IDbCommand cmd = this.CreateCommand(definition);
            return this.ExecuteDataTable(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(List<DbCommandDefinition> sqlList)
        {
            return this.DoExecute<DataTable>(sqlList, this.ExecuteDataTable);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(IDbCommand cmd)
        {
            IDataReader reader = null;
            DataTable result = null;

            try
            {
                reader = this.ExecuteReader(cmd);
                result = new DataTable();
                result.Load(reader);
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }

            return result;
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return this.ExecuteDataSet(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(List<DbCommandDefinition> sqlList)
        {
            return this.DoExecute<DataSet>(sqlList, this.ExecuteDataSet);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(IDbCommand cmd)
        {
            IDataReader reader = null;
            DataSet result = null;

            try
            {
                reader = this.ExecuteReader(cmd);
                result = new XDataSet();
                result.Load(reader, LoadOption.OverwriteChanges, null, new DataTable[] { });
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }

            return result;
        }

        /// <summary>
        /// 强制释放所有资源
        /// </summary>
        public void Dispose()
        {
            if (_transaction != null) _transaction.Dispose();
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }

            _transaction = null;
            _connection = null;
            _autoComplete = false;
        }

        /// <summary>
        /// 执行与释放或重置非托管资源关联的应用程序定义的任务
        /// <para>
        /// 供内部和继承类使用
        /// </para>
        /// </summary>
        protected void InternalDispose()
        {
            // 释放事务
            if (_autoComplete)
            {
                if (_transaction != null) _transaction.Dispose();
                _transaction = null;
                _autoComplete = false;
            }
            // 释放链接
            if (_transaction == null && _connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 执行 SQL 命令
        /// </summary>
        protected virtual T DoExecute<T>(List<DbCommandDefinition> sqlList, Func<IDbCommand, T> func)
        {
            if (sqlList == null || sqlList.Count == 0) return default(T);

            T result = default(T);
            IDbCommand cmd = null;

            try
            {
                #region 命令分组

                var queue = new Queue<List<DbCommandDefinition>>(8);
                if (sqlList.Any(x => x == null || (x.Parameters != null && x.Parameters.Count > 0)))
                {
                    // 参数化分批
                    if (!sqlList.Any(x => x == null)) queue.Enqueue(sqlList);
                    else
                    {
                        var myList = new List<DbCommandDefinition>(8);
                        foreach (var define in sqlList)
                        {
                            if (define != null) myList.Add(define);
                            else
                            {
                                queue.Enqueue(myList);
                                myList = null;
                                myList = new List<DbCommandDefinition>(8);
                            }
                        }
                        // 剩下部分
                        if (myList != null && myList.Count > 0) queue.Enqueue(myList);
                    }
                }
                else
                {
                    // 无参数化分批
                    int pages = sqlList.Page(_commandExecuteSize);
                    for (int i = 1; i <= pages; i++)
                    {
                        var myList = sqlList.Count > _commandExecuteSize
                           ? sqlList.Skip((i - 1) * _commandExecuteSize).Take(_commandExecuteSize).ToList()
                           : sqlList;
                        queue.Enqueue(myList);
                    }
                }

                #endregion

                #region 执行命令

                if (_connection == null) this.CreateConnection();
                if (sqlList.Count > 1 && _transaction == null)
                {
                    this.BeginTransaction(IsolationLevel.ReadCommitted);
                    // 内部维护的事务，在执行完命令后需要自动提交-释放事务
                    _autoComplete = true;
                }

                while (queue.Count > 0)
                {
                    var myList = queue.Dequeue();
                    string commandText = string.Join(Environment.NewLine, myList.Select(x => x.CommandText));
                    cmd = this.CreateCommand(commandText);
                    if (cmd.Connection == null) cmd.Connection = _connection;

                    // 组织命令参数
                    List<IDbDataParameter> prevParameters = null;
                    foreach (var shape in myList)
                    {
                        if (shape.Parameters != null)
                        {
                            if (prevParameters == null || shape.Parameters != prevParameters)
                            {
                                cmd.Parameters.AddRange(shape.Parameters);
                                prevParameters = shape.Parameters;
                            }
                        }
                    }

                    // 外层不捕获异常，由内层Func去捕获
                    result = this.DoExecute<T>(cmd, func, false, false);

                    // 释放当前的cmd
                    if (cmd != null) cmd.Dispose();
                }

                #endregion

                if (_autoComplete) _transaction.Commit();
                return result;
            }
            catch
            {
                if (_autoComplete) _transaction.Rollback();
                throw;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (typeof(T) != typeof(IDataReader)) this.InternalDispose();
            }

        }

        /// <summary>
        /// 执行 SQL 命令
        /// </summary>
        protected virtual T DoExecute<T>(IDbCommand cmd, Func<IDbCommand, T> func, bool wasClosed, bool interceptException = true)
        {
            T TResult = default(T);

            try
            {
                if (cmd.Transaction != null && _transaction != null && cmd.Transaction != _transaction)
                {
                    throw new XFrameworkException("DoExecute: IDbCommand.Transaction does not equals to current transaction.");
                }
                if (cmd.Connection != null && _connection != null && cmd.Connection != _connection)
                {
                    throw new XFrameworkException("DoExecute: IDbCommand.Connection does not equals to current connection.");
                }

                if (cmd.Connection == null) cmd.Connection = this.CreateConnection();
                if (cmd.Connection.State != ConnectionState.Open)
                {
                    if (string.IsNullOrEmpty(cmd.Connection.ConnectionString)) cmd.Connection.ConnectionString = _connString;
                    cmd.Connection.Open();
                }

                if (interceptException) DbInterception.OnExecuting(cmd);
                TResult = func(cmd);
                if (interceptException) DbInterception.OnExecuted(cmd);
            }
            catch (DbException e)
            {
                // 外层不捕获异常，由内层Func去捕获
                if (interceptException) DbInterception.OnException(new DbCommandException(e.Message, e, cmd));
                throw;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (wasClosed) this.InternalDispose();
            }

            return TResult;
        }

        #endregion

        #region 嵌套类型

        /// <summary>
        /// 数据适配器，扩展Fill方法
        /// .NET的DataSet.Load方法，底层调用DataAdapter.Fill(DataTable[], IDataReader, int, int)
        /// Dapper想要返回DataSet，需要重写Load方法，不必传入DataTable[]，因为数组长度不确定
        /// </summary>
        class XLoadAdapter : DataAdapter
        {
            public XLoadAdapter()
            {
            }

            public int FillFromReader(DataSet ds, IDataReader dataReader, int startRecord, int maxRecords)
            {
                return this.Fill(ds, "Table", dataReader, startRecord, maxRecords);
            }
        }

        /// <summary>
        /// 扩展Load方法
        /// </summary>
        class XDataSet : DataSet
        {
            public override void Load(IDataReader reader, LoadOption loadOption, FillErrorEventHandler handler, params DataTable[] tables)
            {
                XLoadAdapter adapter = new XLoadAdapter
                {
                    FillLoadOption = loadOption,
                    MissingSchemaAction = MissingSchemaAction.AddWithKey
                };
                if (handler != null)
                {
                    adapter.FillError += handler;
                }
                adapter.FillFromReader(this, reader, 0, 0);
                if (!reader.IsClosed && !reader.NextResult())
                {
                    reader.Close();
                }
            }
        }

        #endregion
    }
}
