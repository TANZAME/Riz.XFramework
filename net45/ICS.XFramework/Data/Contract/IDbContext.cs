
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ICS.XFramework.Data
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
        void Update<T>(Expression<Func<T, T>> action, Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T>(Expression<Func<T, T>> action, IDbQueryable<T> query);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T, TFrom>(Expression<Func<T, TFrom, T>> action, IDbQueryable<T> query);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T, TFrom1, TFrom2>(Expression<Func<T, TFrom1, TFrom2, T>> action, IDbQueryable<T> query);

        /// <summary>
        /// 更新记录
        /// </summary>
        void Update<T, TFrom1, TFrom2, TFrom3>(Expression<Func<T, TFrom1, TFrom2, TFrom3, T>> action, IDbQueryable<T> query);

        /// <summary>
        /// 附加查询项
        /// </summary>
        void AddQuery(string query, params object[] args);

        /// <summary>
        /// 附加查询项
        /// </summary>
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
        /// <returns></returns>
        int SubmitChanges<T1, T2>(out List<T1> result1, out List<T2> result2);

        /// <summary>
        /// 返回特定类型的对象的集合，其中类型由 T 参数定义
        /// </summary>
        IDbQueryable<T> GetTable<T>();
    }
}
