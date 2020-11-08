
using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 定义数据库对象接口
    /// </summary>
    public partial interface IDatabase
    {
        /// <summary>
        /// 异步创建数据库连接
        /// </summary>
        /// <param name="isOpen">是否打开连接</param>
        /// <returns></returns>
        Task<IDbConnection> CreateConnectionAsync(bool isOpen);

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        Task<int> ExecuteNonQueryAsync(string sql, params object[] args);

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        Task<int> ExecuteNonQueryAsync(List<DbRawCommand> sqlList);

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        Task<int> ExecuteNonQueryAsync(IDbCommand command);

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(string sql, params object[] args);

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(List<DbRawCommand> sqlList);

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(IDbCommand command);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition=@Condition
        /// </para>
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">命令参数</param>
        /// <returns></returns>
        Task<IDataReader> ExecuteReaderAsync(string sql, params object[] args);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        Task<IDataReader> ExecuteReaderAsync(List<DbRawCommand> sqlList);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        Task<IDataReader> ExecuteReaderAsync(IDbCommand command);

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
        Task<T> ExecuteAsync<T>(string sql, params object[] args);

        /// <summary>
        /// 异步执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(IDbQueryable query);

        /// <summary>
        /// 异步执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="sqlList">查询语句</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(List<DbRawCommand> sqlList);

        /// <summary>
        /// 异步执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="sqlList">查询语句</param>
        /// <param name="action">执行SQL命令动作</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(List<DbRawCommand> sqlList, Func<IDbCommand, Task<T>> action);

        /// <summary>
        /// 异步执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <typeparam name="T">基元类型、单实体、列表（List&lt;T&gt;）、DataTable、DataSet</typeparam>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(IDbCommand command);

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        Task<Tuple<List<T1>, List<T2>>> ExecuteAsync<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2);

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        Task<Tuple<List<T1>, List<T2>, List<T3>>> ExecuteAsync<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3);

        /// <summary>
        /// 异步执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="command">SQL 命令</param>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command);
    }
}
