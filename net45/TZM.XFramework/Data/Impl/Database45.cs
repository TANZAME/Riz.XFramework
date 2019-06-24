
using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据查询提供者 提供一系列方法用以执行数据库操作
    /// </summary>
    public partial class Database
    {
        /// <summary>
        /// 异步创建数据库连接
        /// </summary>
        /// <param name="isOpen">是否打开连接</param>
        /// <returns></returns>
        public async Task<IDbConnection> CreateConnectionAsync(bool isOpen = false)
        {
            if (_connection == null)
            {
                _connection = this.DbProviderFactory.CreateConnection();
                _connection.ConnectionString = this.ConnectionString;
            }
            if (isOpen && _connection.State != ConnectionState.Open) await ((DbConnection)_connection).OpenAsync();
            return _connection;
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        public async Task<int> ExecuteNonQueryAsync(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return await this.ExecuteNonQueryAsync(cmd);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        public async Task<int> ExecuteNonQueryAsync(List<DbCommandDefinition> sqlList)
        {
            int rowCount = 0;
            await this.DoExecuteAsync<int>(sqlList, async cmd => rowCount += await this.ExecuteNonQueryAsync(cmd));
            return rowCount;
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(IDbCommand cmd)
        {
            return await this.DoExecuteAsync<int>(cmd, async p => await p.ExecuteNonQueryAsync(), cmd.Transaction == null);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return await this.ExecuteScalarAsync(cmd);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(List<DbCommandDefinition> sqlList)
        {
            return await this.DoExecuteAsync<object>(sqlList, this.ExecuteScalarAsync);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(IDbCommand cmd)
        {
            return await this.DoExecuteAsync<object>(cmd, async p => await p.ExecuteScalarAsync(), cmd.Transaction == null);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return await this.ExecuteReaderAsync(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(List<DbCommandDefinition> sqlList)
        {
            return await this.DoExecuteAsync<DbDataReader>(sqlList, async cmd => await this.ExecuteReaderAsync(cmd) as DbDataReader);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd)
        {
            return await this.DoExecuteAsync<DbDataReader>(cmd, async p => await p.ExecuteReaderAsync(CommandBehavior.SequentialAccess), false);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return await this.ExecuteAsync<T>(cmd);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(IDbQueryable<T> query)
        {
            DbCommandDefinition definition = query.Resolve();
            IDbCommand cmd = this.CreateCommand(definition);
            return await this.ExecuteAsync<T>(cmd, definition as DbCommandDefinition_Select);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="sqlList">查询语句</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(List<DbCommandDefinition> sqlList)
        {
            return await this.DoExecuteAsync<T>(sqlList, async cmd => await this.ExecuteAsync<T>(cmd, sqlList.FirstOrDefault(x => x is DbCommandDefinition_Select) as DbCommandDefinition_Select));
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(IDbCommand cmd)
        {
            return await this.ExecuteAsync<T>(cmd, null);
        }

        // 执行SQL 语句，并返回单个实体对象
        async Task<T> ExecuteAsync<T>(IDbCommand cmd, DbCommandDefinition_Select definition)
        {
            IDataReader reader = null;
            IDbConnection conn = null;

            try
            {
                reader = await this.ExecuteReaderAsync(cmd);
                conn = cmd != null ? cmd.Connection : null;
                TypeDeserializer deserializer = new TypeDeserializer(reader, definition);
                List<T> result = await deserializer.DeserializeAsync<T>();
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
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        public virtual async Task<Tuple<List<T1>, List<T2>>> ExecuteMultipleAsync<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2)
        {
            List<DbCommandDefinition> sqlList = query1.Provider.Resolve(new List<object> { query1, query2 });
            var result = await this.DoExecuteAsync<Tuple<List<T1>, List<T2>, List<None>, List<None>, List<None>, List<None>, List<None>>>(sqlList,
               async cmd => await this.ExecuteMultipleAsync<T1, T2, None, None, None, None, None>(cmd, sqlList.ToList(x => x as DbCommandDefinition_Select)));
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        public virtual async Task<Tuple<List<T1>, List<T2>, List<T3>>> ExecuteMultipleAsync<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3)
        {
            List<DbCommandDefinition> sqlList = query1.Provider.Resolve(new List<object> { query1, query2, query3 });
            var result = await this.DoExecuteAsync<Tuple<List<T1>, List<T2>, List<T3>, List<None>, List<None>, List<None>, List<None>>>(sqlList,
                async cmd => await this.ExecuteMultipleAsync<T1, T2, T3, None, None, None, None>(cmd, sqlList.ToList(x => x as DbCommandDefinition_Select)));
            return new Tuple<List<T1>, List<T2>, List<T3>>(result.Item1, result.Item2, result.Item3);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        public virtual async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand cmd)
        {
            return await this.ExecuteMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(cmd, null);
        }

        // 异步执行 SQL 语句，并返回多个实体集合
        async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand cmd, List<DbCommandDefinition_Select> defines = null)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
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
                reader = await this.ExecuteReaderAsync(cmd);
                conn = cmd != null ? cmd.Connection : null;

                do
                {
                    i += 1;
                    switch (i)
                    {
                        #region 元组赋值

                        case 1:
                            if (deserializer1 == null) deserializer1 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q1 = await deserializer1.DeserializeAsync<T1>();

                            break;

                        case 2:
                            if (deserializer2 == null) deserializer2 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q2 = await deserializer2.DeserializeAsync<T2>();

                            break;

                        case 3:
                            if (deserializer3 == null) deserializer3 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q3 = await deserializer3.DeserializeAsync<T3>();

                            break;

                        case 4:
                            if (deserializer4 == null) deserializer4 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q4 = await deserializer4.DeserializeAsync<T4>();

                            break;

                        case 5:
                            if (deserializer5 == null) deserializer5 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q5 = await deserializer5.DeserializeAsync<T5>();

                            break;

                        case 6:
                            if (deserializer6 == null) deserializer6 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q6 = await deserializer6.DeserializeAsync<T6>();

                            break;

                        case 7:
                            if (deserializer7 == null) deserializer7 = new TypeDeserializer(reader, defines != null ? defines[i - 1] : null);
                            q7 = await deserializer7.DeserializeAsync<T7>();

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
        /// 异步执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteListAsync<T>(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return await this.ExecuteListAsync<T>(cmd);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteListAsync<T>(IDbQueryable<T> query)
        {
            DbCommandDefinition definition = query.Resolve();
            IDbCommand cmd = this.CreateCommand(definition.CommandText, definition.CommandType, definition.Parameters);
            return await this.ExecuteListAsync<T>(cmd, definition as DbCommandDefinition_Select);
        }

        /// <summary>
        /// 执行SQL 语句，并返回并返回单结果集集合
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteListAsync<T>(List<DbCommandDefinition> sqlList)
        {
            return await this.DoExecuteAsync<List<T>>(sqlList, async cmd => await this.ExecuteListAsync<T>(cmd, sqlList.FirstOrDefault(x => x is DbCommandDefinition_Select) as DbCommandDefinition_Select));
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteListAsync<T>(IDbCommand cmd)
        {
            return await this.ExecuteListAsync<T>(cmd, null);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="definition">命令定义对象，用于解析实体的外键</param>
        /// <returns></returns>
        async Task<List<T>> ExecuteListAsync<T>(IDbCommand cmd, DbCommandDefinition_Select definition)
        {
            IDataReader reader = null;
            List<T> objList = new List<T>();

            try
            {
                reader = await this.ExecuteReaderAsync(cmd);
                TypeDeserializer deserializer = new TypeDeserializer(reader, definition);
                objList = await deserializer.DeserializeAsync<T>();
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }
            return objList;
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return await this.ExecuteDataTableAsync(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(IDbQueryable query)
        {
            DbCommandDefinition definition = query.Resolve();
            IDbCommand cmd = this.CreateCommand(definition);
            return await this.ExecuteDataTableAsync(cmd);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(List<DbCommandDefinition> sqlList)
        {
            return await this.DoExecuteAsync<DataTable>(sqlList, this.ExecuteDataTableAsync);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(IDbCommand cmd)
        {
            IDataReader reader = null;
            DataTable result = null;

            try
            {
                reader = await this.ExecuteReaderAsync(cmd);
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
        public async Task<DataSet> ExecuteDataSetAsync(string commandText)
        {
            IDbCommand cmd = this.CreateCommand(commandText);
            return await this.ExecuteDataSetAsync(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(List<DbCommandDefinition> sqlList)
        {
            return await this.DoExecuteAsync<DataSet>(sqlList, this.ExecuteDataSetAsync);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(IDbCommand cmd)
        {
            IDataReader reader = null;
            DataSet result = null;

            try
            {
                reader = await this.ExecuteReaderAsync(cmd);
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
        /// 提交上下文
        /// </summary>
        /// <param name="sqlList">命令集合</param>
        /// <returns></returns>
        internal async Task<List<int>> SubmitAsync(List<DbCommandDefinition> sqlList)
        {
            return await this.DoSubmitAsync(sqlList);
        }

        // 执行
        protected virtual async Task<List<int>> DoSubmitAsync(List<DbCommandDefinition> sqlList)
        {
            List<int> identitys = new List<int>();
            IDataReader reader = null;

            try
            {
                Func<DbCommand, Task<object>> func = async p =>
                {
                    reader = await this.ExecuteReaderAsync(p);
                    TypeDeserializer deserializer = new TypeDeserializer(reader, null);
                    do
                    {
                        List<int> result = null;
                        deserializer.Deserialize<int>(out result);
                        if (result != null && result.Count > 0) identitys.AddRange(result);
                    }
                    while (reader.NextResult());

                    // 释放当前的reader
                    if (reader != null) reader.Dispose();

                    return null;
                };

                await this.DoExecuteAsync<object>(sqlList, func);
                return identitys;
            }
            finally
            {
                if (reader != null) reader.Dispose();
            }
        }

        // 执行 SQL 命令
        protected virtual async Task<T> DoExecuteAsync<T>(List<DbCommandDefinition> sqlList, Func<DbCommand, Task<T>> func)
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

                if (_connection == null) await this.CreateConnectionAsync();
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
                    result = await this.DoExecuteAsync<T>(cmd, func, false, false);

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

        // 执行 SQL 命令
        protected virtual async Task<T> DoExecuteAsync<T>(IDbCommand cmd, Func<DbCommand, Task<T>> func, bool wasClosed, bool interceptException = true)
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

                if (cmd.Connection == null) cmd.Connection = await this.CreateConnectionAsync();
                if (cmd.Connection.State != ConnectionState.Open) await ((DbConnection)cmd.Connection).OpenAsync();

                if (interceptException) DbInterception.OnExecuting(cmd);
                TResult = await func((DbCommand)cmd);
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
    }


}
