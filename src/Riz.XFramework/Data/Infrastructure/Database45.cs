
#if !net40

using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Riz.XFramework.Data
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
                _connection = _provider.CreateConnection();
                _connection.ConnectionString = this.ConnectionString;
            }
            if (isOpen && _connection.State != ConnectionState.Open) await ((DbConnection)_connection).OpenAsync();
            return _connection;
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(string sql, params object[] args)
        {
            IDbCommand command = this.CreateCommand(sql, parameters: this.GetParameters(sql, args));
            return await this.ExecuteNonQueryAsync(command);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        public async Task<int> ExecuteNonQueryAsync(List<DbRawCommand> sqlList)
        {
            int rowCount = 0;
            await this.DoExecuteAsync<int>(sqlList, async p => rowCount += await this.ExecuteNonQueryAsync(p));
            return rowCount;
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(IDbCommand command) 
            => await this.DoExecuteAsync<int>(command, async p => await p.ExecuteNonQueryAsync(), command.Transaction == null);

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(string sql, params object[] args)
        {
            IDbCommand command = this.CreateCommand(sql, parameters: this.GetParameters(sql, args));
            return await this.ExecuteScalarAsync(command);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(List<DbRawCommand> sqlList) => await this.DoExecuteAsync<object>(sqlList, this.ExecuteScalarAsync);

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(IDbCommand command) 
            => await this.DoExecuteAsync<object>(command, async p => await p.ExecuteScalarAsync(), command.Transaction == null);

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(string sql, params object[] args)
        {
            IDbCommand command = this.CreateCommand(sql, parameters: this.GetParameters(sql, args));
            return await this.ExecuteReaderAsync(command);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(List<DbRawCommand> sqlList) 
            => await this.DoExecuteAsync<DbDataReader>(sqlList, async command => await this.ExecuteReaderAsync(command) as DbDataReader);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(IDbCommand command) 
            => await this.DoExecuteAsync<DbDataReader>(command, async p => await p.ExecuteReaderAsync(CommandBehavior.SequentialAccess), false);

        /// <summary>
        /// 异步执行SQL 语句，并返回由 T 指定的对象
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        public virtual async Task<T> ExecuteAsync<T>(string sql, params object[] args)
        {
            IDbCommand command = this.CreateCommand(sql, parameters: this.GetParameters(sql, args));
            return await this.ExecuteAsync<T>(command);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        public virtual async Task<T> ExecuteAsync<T>(IDbQueryable query)
        {
            DbRawCommand cmd = query.Translate();
            IDbCommand command = this.CreateCommand(cmd);
            return await this.ExecuteAsync<T>(command, cmd as IMapInfo);
        }

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="sqlList">查询语句</param>
        /// <returns></returns>
        public virtual async Task<T> ExecuteAsync<T>(List<DbRawCommand> sqlList) 
            => await this.DoExecuteAsync<T>(sqlList, async p => await this.ExecuteAsync<T>(p, sqlList.FirstOrDefault(x => x is IMapInfo) as IMapInfo));

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="sqlList">查询语句</param>
        /// <param name="action">执行SQL命令动作</param>
        /// <returns></returns>
        public virtual async Task<T> ExecuteAsync<T>(List<DbRawCommand> sqlList, Func<IDbCommand, Task<T>> action) => await this.DoExecuteAsync<T>(sqlList, action);

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public virtual async Task<T> ExecuteAsync<T>(IDbCommand command) => await this.ExecuteAsync<T>(command, null);

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="command">连接到数据源时执行的 SQL 语句</param>
        /// <param name="map">实体映射描述</param>
        /// <returns></returns>
        protected virtual async Task<T> ExecuteAsync<T>(IDbCommand command, IMapInfo map)
        {
            IDataReader reader = null;
            T result = default(T);

            try
            {
                reader = await this.ExecuteReaderAsync(command);
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

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        public virtual async Task<Tuple<List<T1>, List<T2>>> ExecuteAsync<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2)
        {
            List<DbRawCommand> sqlList = query1.Provider.Translate(new List<object> { query1, query2 });
            var result = await this.DoExecuteAsync<Tuple<List<T1>, List<T2>, List<None>, List<None>, List<None>, List<None>, List<None>>>(sqlList,
               async p => await this.ExecuteAsync<T1, T2, None, None, None, None, None>(p, sqlList.ToList(x => x as IMapInfo)));
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        public virtual async Task<Tuple<List<T1>, List<T2>, List<T3>>> ExecuteAsync<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3)
        {
            List<DbRawCommand> sqlList = query1.Provider.Translate(new List<object> { query1, query2, query3 });
            var result = await this.DoExecuteAsync<Tuple<List<T1>, List<T2>, List<T3>, List<None>, List<None>, List<None>, List<None>>>(sqlList,
                async p => await this.ExecuteAsync<T1, T2, T3, None, None, None, None>(p, sqlList.ToList(x => x as IMapInfo)));
            return new Tuple<List<T1>, List<T2>, List<T3>>(result.Item1, result.Item2, result.Item3);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <typeparam name="T1">第一个列表的元素类型</typeparam>
        /// <typeparam name="T2">第二个列表的元素类型</typeparam>
        /// <typeparam name="T3">第三个列表的元素类型</typeparam>
        /// <typeparam name="T4">第四个列表的元素类型</typeparam>
        /// <typeparam name="T5">第五个列表的元素类型</typeparam>
        /// <typeparam name="T6">第六个列表的元素类型</typeparam>
        /// <typeparam name="T7">第七个列表的元素类型</typeparam>
        /// <param name="command">SQL 命令</param>
        public virtual async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command)
        {
            return await this.ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(command, null);
        }

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
        protected virtual async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command, List<IMapInfo> maps = null)
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
                reader = await this.ExecuteReaderAsync(command);
                conn = command != null ? command.Connection : null;

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
        /// 执行 SQL 命令
        /// </summary>
        /// <typeparam name="T">返回的元素类型</typeparam>
        /// <param name="sqlList">SQL 命令列表</param>
        /// <param name="func">执行命令的委托</param>
        /// <returns></returns>
        protected virtual async Task<T> DoExecuteAsync<T>(List<DbRawCommand> sqlList, Func<System.Data.Common.DbCommand, Task<T>> func)
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

                if (_connection == null) await this.CreateConnectionAsync();
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

                    // 组织命令参数
                    IList<IDbDataParameter> prevParameters = null;
                    foreach (var cmd in myList)
                    {
                        if (cmd.Parameters != null)
                        {
                            if (prevParameters == null || cmd.Parameters != prevParameters)
                            {
                                command.Parameters.AddRange(cmd.Parameters);
                                prevParameters = cmd.Parameters;
                            }
                        }
                    }

                    // 外层不捕获异常，由内层Func去捕获
                    result = await this.DoExecuteAsync<T>(command, func, false, false);

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
        /// <typeparam name="T">返回的元素类型</typeparam>
        /// <param name="command">SQL 命令</param>
        /// <param name="func">执行命令的委托</param>
        /// <param name="wasClosed">执行SQL命令后是否自动关闭连接</param>
        /// <param name="interceptException">指示外层是否捕获异常</param>
        /// <returns></returns>
        protected virtual async Task<T> DoExecuteAsync<T>(IDbCommand command, Func<System.Data.Common.DbCommand, Task<T>> func, bool wasClosed, bool interceptException = true)
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

                if (command.Connection == null) command.Connection = await this.CreateConnectionAsync();
                if (command.Connection.State != ConnectionState.Open) await ((DbConnection)command.Connection).OpenAsync();

                if (interceptException) DbInterception.OnExecuting(command);
                TResult = await func((System.Data.Common.DbCommand)command);
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
    }
}

#endif
