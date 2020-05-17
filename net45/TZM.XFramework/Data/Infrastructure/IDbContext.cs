
using System;
using System.Data;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据上下文，表示 Xfw 框架的主入口点
    /// </summary>
    public partial interface IDbContext : IDisposable
    {
        /// <summary>
        /// <see cref="IDbQueryable"/> 的解析执行提供程序
        /// </summary>
        IDbQueryProvider Provider { get; }

        /// <summary>
        /// 数据库对象，持有当前上下文的会话
        /// </summary>
        IDatabase Database { get; }

        /// <summary>
        /// 调试模式，模式模式下生成的SQL会有换行
        /// </summary>
        bool IsDebug { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        /// 运行事务超时时间
        /// </summary>
        int? CommandTimeout { get; set; }

        /// <summary>
        /// 事务隔离级别，默认 ReadCommitted
        /// </summary>
        IsolationLevel? IsolationLevel { get; set; }

        /// <summary>
        /// 新增记录
        /// </summary>
        void Insert<T>(T TEntity);

        /// <summary>
        /// 批量新增记录
        /// </summary>
        void Insert<T>(IEnumerable<T> collection);

        /// <summary>
        /// 批量新增记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">批量插入列表</param>
        /// <param name="entityColumns">指定插入的列</param>
        void Insert<T>(IEnumerable<T> collection, IList<Expression> entityColumns);

        /// <summary>
        /// 批量新增记录
        /// </summary>
        void Insert<T>(IDbQueryable<T> query);

        /// <summary>
        /// 删除记录
        /// </summary>
        void Delete<T>(T TEntity);

        /// <summary>
        /// 删除记录
        /// </summary>
        void Delete<T>(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 删除记录
        /// </summary>
        void Delete<T>(IDbQueryable<T> query);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T>(T TEntity);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T>(Expression<Func<T, object>> action, Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T>(Expression<Func<T, object>> action, IDbQueryable<T> source);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T, TSource>(Expression<Func<T, TSource, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T, TSource1, TSource2>(Expression<Func<T, TSource1, TSource2, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T, TSource1, TSource2, TSource3>(Expression<Func<T, TSource1, TSource2, TSource3, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 添加额外查询
        /// </summary>
        /// <param name="sql">SQL 命令</param>
        /// <param name="args">参数列表</param>
        void AddQuery(string sql, params object[] args);

        /// <summary>
        /// 添加额外查询
        /// </summary>
        /// <param name="query">查询语义</param>
        void AddQuery(IDbQueryable query);

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <returns></returns>
        int SubmitChanges();

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="result">提交更改并查询数据</param>
        /// <returns></returns>
        int SubmitChanges<T>(out List<T> result);

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <typeparam name="T1">T</typeparam>
        /// <typeparam name="T2">T</typeparam>
        /// <param name="result1">提交更改并查询数据</param>
        /// <param name="result2">提交更改并查询数据</param>
        /// <returns></returns>
        int SubmitChanges<T1, T2>(out List<T1> result1, out List<T2> result2);

        /// <summary>
        /// 返回特定类型的对象的集合，其中类型由 T 参数定义
        /// </summary>
        IDbQueryable<T> GetTable<T>();

        /// <summary>
        /// 解析 SQL 命令
        /// </summary>
        List<RawCommand> Resolve();
    }
}
