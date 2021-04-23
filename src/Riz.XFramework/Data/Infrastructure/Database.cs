
using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Riz.XFramework.Data
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
        private string _parameterPrefix = string.Empty;
        // 当前连接对象
        private IDbConnection _connection = null;
        // 当前事务对象
        private IDbTransaction _transaction = null;
        // 如果不是外部调用BeginTransaction，则执行完命令后需要自动提交-释放事务
        private bool _autoComplete = false;
        private IDbContext _context = null;
        private DbProviderFactory _provider = null;

        #endregion

        #region 公开属性

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString => _connString;

        /// <summary>
        /// 执行命令超时时间
        /// </summary>
        public int? CommandTimeout { get; set; }

        /// <summary>
        /// 事务隔离级别
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }

        /// <summary>
        /// ~批次执行的SQL数量，默认200个查询语句
        /// </summary>
        public int CommanExecuteSize
        {
            get { return _commandExecuteSize; }
            set { _commandExecuteSize = value; }
        }

        /// <summary>
        /// 当前连接会话
        /// </summary>
        public IDbConnection Connection => _connection;

        /// <summary>
        /// 获取或者设置当前会话事务
        /// </summary>
        public IDbTransaction Transaction
        {
            get { return _transaction; }
            set
            {
                // 使用外部事务
                if (value == null) throw new ArgumentNullException("Transaction");
                if (_transaction != null && _transaction != value) throw new XFrameworkException("There have currently uncommitted transactions");
                if (_transaction != value)
                {
                    _transaction = value;
                    _connection = _transaction.Connection;
                }
            }
        }

        ///// <summary>
        ///// 版本号
        ///// </summary>
        //public virtual string Version => throw new NotImplementedException();

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="Database"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        public Database(IDbContext context)
        {
            _context = context;
            _parameterPrefix = _context.Provider.ParameterPrefix;
            _connString = _context.ConnectionString;
            _provider = _context.Provider.DbProvider;

            this.CommandTimeout = _context.CommandTimeout;
            this.IsolationLevel = _context.IsolationLevel;
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
                _connection = _provider.CreateConnection();
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
        public IDbTransaction BeginTransaction()
        {
            if (_transaction == null)
            {
                this.CreateConnection(true);
                _transaction = _connection.BeginTransaction(this.IsolationLevel != null ? this.IsolationLevel.Value : System.Data.IsolationLevel.ReadCommitted);
            }
            return _transaction;
        }

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="cmd">命令描述</param>
        /// <returns></returns>
        public IDbCommand CreateCommand(DbRawCommand cmd) => this.CreateCommand(cmd.CommandText, cmd.CommandType, cmd.Parameters);

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="commandText">SQL 语句</param>
        /// <param name="commandType">命令类型</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public IDbCommand CreateCommand(string commandText, CommandType? commandType = null, IEnumerable<IDbDataParameter> parameters = null)
        {
            IDbCommand command = _provider.CreateCommand();
            command.CommandText = commandText;
            command.CommandTimeout = this.CommandTimeout != null ? this.CommandTimeout.Value : 300; // 5分钟
            if (commandType != null) command.CommandType = commandType.Value;
            if (parameters != null) command.Parameters.AddRange(parameters);
            if (_transaction != null)
            {
                command.Connection = _transaction.Connection;
                command.Transaction = _transaction;
            }

            return command;
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="query">SQL 命令</param>
        public int ExecuteNonQuery(IDbQueryable query)
        {
            var cmd = query.Translate();
            IDbCommand command = this.CreateCommand(cmd);
            return this.ExecuteNonQuery(command);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        public int ExecuteNonQuery(string sql, params object[] args)
        {
            IDbCommand command = this.CreateCommand(sql, parameters: this.GetParameters(sql, args));
            return this.ExecuteNonQuery(command);
        }

        ///// <summary>
        ///// 执行 SQL 语句，并返回受影响的行数
        ///// </summary>
        ///// <param name="sql">SQL 命令</param>
        ///// <param name="obj">命令参数</param>
        //public int ExecuteNonQuery(string sql, object obj)
        //{
        //    var parameters = this.GetParameters(obj);
        //    IDbCommand command = this.CreateCommand(sql, CommandType.Text, parameters);
        //    return this.ExecuteNonQuery(command);
        //}

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        public int ExecuteNonQuery(List<DbRawCommand> sqlList)
        {
            int rowCount = 0;
            this.DoExecute<int>(sqlList, p => rowCount += this.ExecuteNonQuery(p));
            return rowCount;
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public int ExecuteNonQuery(IDbCommand command)
        {
            return this.DoExecute<int>(command, p => p.ExecuteNonQuery(), command.Transaction == null);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="query">查询语义</param>        
        /// <returns></returns>
        public object ExecuteScalar(IDbQueryable query)
        {
            IDbCommand cmd = this.CreateCommand(query.Translate());
            return this.ExecuteScalar(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        public object ExecuteScalar(string sql, params object[] args)
        {
            IDbCommand command = this.CreateCommand(sql, parameters: this.GetParameters(sql, args));
            return this.ExecuteScalar(command);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public object ExecuteScalar(List<DbRawCommand> sqlList) => this.DoExecute<object>(sqlList, this.ExecuteScalar);

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public object ExecuteScalar(IDbCommand command) => this.DoExecute<object>(command, p => p.ExecuteScalar(), command.Transaction == null);

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="query">查询语义</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(IDbQueryable query)
        {
            IDbCommand cmd = this.CreateCommand(query.Translate());
            return this.ExecuteReader(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(string sql, params object[] args)
        {
            IDbCommand command = this.CreateCommand(sql, parameters: this.GetParameters(sql, args));
            return this.ExecuteReader(command);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(List<DbRawCommand> sqlList) => this.DoExecute<IDataReader>(sqlList, this.ExecuteReader);

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(IDbCommand command) 
            => this.DoExecute<IDataReader>(command, p => p.ExecuteReader(CommandBehavior.SequentialAccess), false);

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="sql">查询语句</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        public virtual T Execute<T>(string sql, params object[] args)
        {
            IDbCommand command = this.CreateCommand(sql, parameters: this.GetParameters(sql, args));
            return this.Execute<T>(command);
        }

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="query">查询语句</param>
        /// <returns></returns>
        public virtual T Execute<T>(IDbQueryable query)
        {
            DbRawCommand command = query.Translate();
            IDbCommand cmd = this.CreateCommand(command);
            return this.Execute<T>(cmd, command as IMapDescriptor);
        }

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="sqlList">查询语句</param>
        /// <returns></returns>
        public virtual T Execute<T>(List<DbRawCommand> sqlList) 
            => this.DoExecute<T>(sqlList, cmd => this.Execute<T>(cmd, sqlList.FirstOrDefault(a => a is IMapDescriptor) as IMapDescriptor));

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="sqlList">查询语句</param>
        /// <param name="action">执行SQL命令动作</param>
        /// <returns></returns>
        public virtual T Execute<T>(List<DbRawCommand> sqlList, Func<IDbCommand, T> action) => this.DoExecute<T>(sqlList, action);

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public virtual T Execute<T>(IDbCommand command) => this.Execute<T>(command, null);

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="command">SQL 命令</param>
        /// <param name="map">实体映射描述</param>
        /// <returns></returns>
        protected virtual T Execute<T>(IDbCommand command, IMapDescriptor map)
        {
            IDataReader reader = null;
            T result = default(T);

            try
            {
                reader = this.ExecuteReader(command);
                result = this.GetResult<T>(reader, map);
            }
            finally
            {
                if (command != null) command.Dispose();
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }

            return result;
        }

        // datareader 转实体
        T GetResult<T>(IDataReader reader, IMapDescriptor map)
        {
            T result = default(T);
            if (typeof(T) == typeof(DataTable))
            {
                DataTable table = new DataTable();
                table.Load(reader);
                result = (T)(object)table;
            }
            else if (typeof(T) == typeof(DataSet))
            {
                InternalDataSet data = new InternalDataSet();
                data.Load(reader, LoadOption.OverwriteChanges, null, new DataTable[] { });
                result = (T)(object)data;
            }
            else
            {
                TypeDeserializer deserializer = new TypeDeserializer(_context, reader, map);
                result = deserializer.Deserialize<T>();
            }
            return result;
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        public virtual Tuple<List<T1>, List<T2>> Execute<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2)
        {
            List<DbRawCommand> sqlList = query1.Provider.Translate(new List<object> { query1, query2 });
            var result = this.DoExecute<Tuple<List<T1>, List<T2>, List<None>, List<None>, List<None>, List<None>, List<None>>>(sqlList,
                p => this.Execute<T1, T2, None, None, None, None, None>(p, sqlList.ToList(x => x as IMapDescriptor)));
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        public virtual Tuple<List<T1>, List<T2>, List<T3>> Execute<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3)
        {
            List<DbRawCommand> sqlList = query1.Provider.Translate(new List<object> { query1, query2, query3 });
            var result = this.DoExecute<Tuple<List<T1>, List<T2>, List<T3>, List<None>, List<None>, List<None>, List<None>>>(sqlList,
                p => this.Execute<T1, T2, T3, None, None, None, None>(p, sqlList.ToList(x => x as IMapDescriptor)));
            return new Tuple<List<T1>, List<T2>, List<T3>>(result.Item1, result.Item2, result.Item3);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="command">SQL 命令</param>
        public virtual Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> Execute<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command) 
            => this.Execute<T1, T2, T3, T4, T5, T6, T7>(command, null);

        /// <summary>
        /// 执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <typeparam name="T1">第一个列表的元素类型</typeparam>
        /// <typeparam name="T2">第二个列表的元素类型</typeparam>
        /// <typeparam name="T3">第三个列表的元素类型</typeparam>
        /// <typeparam name="T4">第四个列表的元素类型</typeparam>
        /// <typeparam name="T5">第五个列表的元素类型</typeparam>
        /// <typeparam name="T6">第六个列表的元素类型</typeparam>
        /// <typeparam name="T7">第七个列表的元素类型</typeparam>
        /// <param name="command">SQL 命令</param>
        /// <param name="maps">实体映射描述列表</param>
        /// <returns></returns>
        protected virtual Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> Execute<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command, List<IMapDescriptor> maps = null)
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
                reader = this.ExecuteReader(command);

                do
                {
                    i += 1;

                    switch (i)
                    {
                        #region 元组赋值

                        case 1:
                            if (deserializer1 == null) deserializer1 = new TypeDeserializer(_context, reader, maps != null && maps.Count > i - 1 ? maps[i - 1] : null);
                            q1 = deserializer1.Deserialize<List<T1>>();

                            break;

                        case 2:
                            if (deserializer2 == null) deserializer2 = new TypeDeserializer(_context, reader, maps != null && maps.Count > i - 1 ? maps[i - 1] : null);
                            q2 = deserializer2.Deserialize<List<T2>>();

                            break;

                        case 3:
                            if (deserializer3 == null) deserializer3 = new TypeDeserializer(_context, reader, maps != null && maps.Count > i - 1 ? maps[i - 1] : null);
                            q3 = deserializer3.Deserialize<List<T3>>();

                            break;

                        case 4:
                            if (deserializer4 == null) deserializer4 = new TypeDeserializer(_context, reader, maps != null && maps.Count > i - 1 ? maps[i - 1] : null);
                            q4 = deserializer4.Deserialize<List<T4>>();

                            break;

                        case 5:
                            if (deserializer5 == null) deserializer5 = new TypeDeserializer(_context, reader, maps != null && maps.Count > i - 1 ? maps[i - 1] : null);
                            q5 = deserializer5.Deserialize<List<T5>>();

                            break;

                        case 6:
                            if (deserializer6 == null) deserializer6 = new TypeDeserializer(_context, reader, maps != null && maps.Count > i - 1 ? maps[i - 1] : null);
                            q6 = deserializer6.Deserialize<List<T6>>();

                            break;

                        case 7:
                            if (deserializer7 == null) deserializer7 = new TypeDeserializer(_context, reader, maps != null && maps.Count > i - 1 ? maps[i - 1] : null);
                            q7 = deserializer7.Deserialize<List<T7>>();

                            break;

                            #endregion
                    }
                }
                while (reader.NextResult());
            }
            finally
            {
                if (command != null) command.Dispose();
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>(q1 ?? new List<T1>(), q2 ?? new List<T2>(), q3 ?? new List<T3>(), q4 ?? new List<T4>(), q5 ?? new List<T5>(), q6 ?? new List<T6>(), q7 ?? new List<T7>());
        }

        /// <summary>
        /// 强制释放所有资源
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// 强制释放所有资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_transaction != null) _transaction.Dispose();
            if (_connection != null && disposing)
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
        protected virtual T DoExecute<T>(List<DbRawCommand> sqlList, Func<IDbCommand, T> func)
        {
            if (sqlList == null || sqlList.Count == 0) return default(T);

            T result = default(T);
            IDbCommand command = null;

            try
            {
                #region 命令分组

                var queue = new Queue<List<DbRawCommand>>(8);
                if (sqlList.Any(x => x == null || (x.Parameters != null && x.Parameters.Count > 0)))
                {
                    // 参数化分批
                    if (!sqlList.Any(x => x == null)) queue.Enqueue(sqlList);
                    else
                    {
                        var myList = new List<DbRawCommand>(8);
                        foreach (var cmd in sqlList)
                        {
                            if (cmd != null) myList.Add(cmd);
                            else
                            {
                                queue.Enqueue(myList);
                                myList = null;
                                myList = new List<DbRawCommand>(8);
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
                    this.BeginTransaction();
                    // 内部维护的事务，在执行完命令后需要自动提交-释放事务
                    _autoComplete = true;
                }

                while (queue.Count > 0)
                {
                    var myList = queue.Dequeue();
                    string sql = string.Join(Environment.NewLine, myList.Select(x => x.CommandText));
                    command = this.CreateCommand(sql);
                    if (command.Connection == null) command.Connection = _connection;

                    // 组织命令参数，参数不能重复使用
                    var used = new HashSet<IList<IDbDataParameter>>();
                    foreach (var cmd in myList)
                    {
                        if (cmd.Parameters == null) continue;
                        else if (used.Contains(cmd.Parameters)) continue;
                        else
                        {
                            command.Parameters.AddRange(cmd.Parameters);
                            used.Add(cmd.Parameters);
                        }
                    }

                    // 外层不捕获异常，由内层Func去捕获
                    result = this.DoExecute<T>(command, func, false, false);

                    // 释放当前的cmd
                    if (command != null) command.Dispose();
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
                if (command != null) command.Dispose();
                if (typeof(T) != typeof(IDataReader)) this.InternalDispose();
            }

        }

        /// <summary>
        /// 执行 SQL 命令
        /// </summary>
        protected virtual T DoExecute<T>(IDbCommand command, Func<IDbCommand, T> func, bool wasClosed, bool interceptException = true)
        {
            T TResult = default(T);

            try
            {
                if (command.Transaction != null && _transaction != null && command.Transaction != _transaction)
                {
                    throw new XFrameworkException("DoExecute: IDbCommand.Transaction does not equals to current transaction.");
                }
                if (command.Connection != null && _connection != null && command.Connection != _connection)
                {
                    throw new XFrameworkException("DoExecute: IDbCommand.Connection does not equals to current connection.");
                }

                if (command.Connection == null) command.Connection = this.CreateConnection();
                if (command.Connection.State != ConnectionState.Open)
                {
                    if (string.IsNullOrEmpty(command.Connection.ConnectionString)) command.Connection.ConnectionString = _connString;
                    command.Connection.Open();
                }

                if (interceptException) DbInterception.OnExecuting(command);
                TResult = func(command);
                if (interceptException) DbInterception.OnExecuted(command);
            }
            catch (DbException e)
            {
                // 外层不捕获异常，由内层Func去捕获
                if (interceptException) DbInterception.OnException(new DbCommandException(e.Message, e, command));
                throw;
            }
            finally
            {
                if (command != null) command.Dispose();
                if (wasClosed) this.InternalDispose();
            }

            return TResult;
        }

        /// <summary>
        /// 解析生成参数
        /// </summary>
        /// <param name="component">SQL 参数源</param>
        /// <returns></returns>
        protected IEnumerable<IDbDataParameter> GetParameters(object component)
        {
            List<IDbDataParameter> result = null;
            PropertyDescriptorCollection descriptors = TypeDescriptor.GetProperties(component);
            foreach (PropertyDescriptor property in descriptors)
            {
                if (result == null) result = new List<IDbDataParameter>(descriptors.Count);
                object value = property.GetValue(component);
                result.Add(_provider.CreateParameter(string.Format("{0}{1}", _parameterPrefix, property.Name), value));
            }

            return result;
        }

        /// <summary>
        /// 解析生成参数
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        protected IEnumerable<IDbDataParameter> GetParameters(string sql, params object[] args)
        {
            if (args == null || args.Length == 0) return null;
            XFrameworkException.Check.NotNull(sql, "sql");

            string pattern = string.Format("(?<ParameterName>{0}[0-9a-zA-Z_]+)", _parameterPrefix);
            var matches = Regex.Matches(sql, pattern);
            if (matches == null || matches.Count == 0) return null;
            else
            {
                if (matches.Count != args.Length) throw new XFrameworkException("numers of parameter not match args number.");

                var result = new List<IDbDataParameter>(matches.Count);
                for (int i = 0; i < matches.Count; i++)
                {
                    var m = matches[i];
                    var parameter = _provider.CreateParameter(string.Format("{0}{1}", _parameterPrefix, m.Groups["ParameterName"].Value), args[i]);
                    result.Add(parameter);
                }
                return result;
            }
        }

        #endregion

        #region 嵌套类型

        /// <summary>
        /// 数据适配器，扩展Fill方法
        /// .NET的DataSet.Load方法，底层调用DataAdapter.Fill(DataTable[], IDataReader, int, int)
        /// Dapper想要返回DataSet，需要重写Load方法，不必传入DataTable[]，因为数组长度不确定
        /// </summary>
        class InternalLoadAdapter : DataAdapter
        {
            public InternalLoadAdapter()
            {
            }

            public int FillFromReader(DataSet result, IDataReader dataReader, int startRecord, int maxRecords) => this.Fill(result, "Table", dataReader, startRecord, maxRecords);
        }

        /// <summary>
        /// 扩展Load方法
        /// </summary>
        class InternalDataSet : DataSet
        {
            public override void Load(IDataReader reader, LoadOption loadOption, FillErrorEventHandler handler, params DataTable[] tables)
            {
                InternalLoadAdapter adapter = new InternalLoadAdapter
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
                adapter.Dispose();
            }
        }

        #endregion
    }
}
