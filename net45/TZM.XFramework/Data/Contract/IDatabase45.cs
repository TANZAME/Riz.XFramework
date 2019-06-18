
using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TZM.XFramework.Data
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
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        Task<int> ExecuteNonQueryAsync(string commandText);

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        Task<int> ExecuteNonQueryAsync(List<DbCommandDefinition> sqlList);

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        Task<int> ExecuteNonQueryAsync(IDbCommand cmd);

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(string commandText);

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(List<DbCommandDefinition> sqlList);

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(IDbCommand cmd);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        Task<IDataReader> ExecuteReaderAsync(string commandText);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        Task<IDataReader> ExecuteReaderAsync(List<DbCommandDefinition> sqlList);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd);

        /// <summary>
        /// 异步执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(string commandText);

        /// <summary>
        /// 异步执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(IDbQueryable<T> query);

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="sqlList">查询语句</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(List<DbCommandDefinition> sqlList);

        /// <summary>
        /// 异步执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(IDbCommand cmd);

        /// <summary>
        /// 异步执行SQL 语句，并返回并返回单结果集集合
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        Task<List<T>> ExecuteListAsync<T>(string commandText);

        /// <summary>
        /// 异步执行SQL 语句，并返回并返回单结果集集合
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        Task<List<T>> ExecuteListAsync<T>(List<DbCommandDefinition> sqlList);

        /// <summary>
        /// 异步执行SQL 语句，并返回并返回单结果集集合
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        Task<List<T>> ExecuteListAsync<T>(IDbQueryable<T> query);

        /// <summary>
        /// 异步执行SQL 语句，并返回并返回单结果集集合
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        Task<List<T>> ExecuteListAsync<T>(IDbCommand cmd);

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        Task<Tuple<List<T1>, List<T2>>> ExecuteMultipleAsync<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2);

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        Task<Tuple<List<T1>, List<T2>, List<T3>>> ExecuteMultipleAsync<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3);

        /// <summary>
        /// 异步执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand cmd);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        Task<DataTable> ExecuteDataTableAsync(string commandText);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        Task<DataTable> ExecuteDataTableAsync(List<DbCommandDefinition> sqlList);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="query">SQL 命令</param>
        /// <returns></returns>
        Task<DataTable> ExecuteDataTableAsync(IDbQueryable query);

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        Task<DataTable> ExecuteDataTableAsync(IDbCommand cmd);

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <returns></returns>
        Task<DataSet> ExecuteDataSetAsync(string commandText);

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        Task<DataSet> ExecuteDataSetAsync(List<DbCommandDefinition> sqlList);

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <returns></returns>
        Task<DataSet> ExecuteDataSetAsync(IDbCommand cmd);
    }
}
