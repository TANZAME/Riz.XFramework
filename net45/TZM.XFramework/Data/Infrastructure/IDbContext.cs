
using System;
using System.Data;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据上下文，框架的主入口点，非线程安全
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
        /// 将给定实体添加到基础上下文中，当调用 SubmitChanges 时，会将该实体插入到数据库中
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="TEntity">要添加的实体</param>
        void Insert<T>(T TEntity);

        /// <summary>
        /// 批量将给定实体集合添加到基础上下文中，当调用 SubmitChanges 时，会将该实体插入到数据库中
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="collection">要添加的实体集合</param>
        void Insert<T>(IEnumerable<T> collection);

        /// <summary>
        /// 批量将给定实体集合添加到基础上下文中，当调用 SubmitChanges 时，会将该实体插入到数据库中
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="collection">要添加的实体集合</param>
        /// <param name="entityColumns">指定插入的列</param>
        void Insert<T>(IEnumerable<T> collection, IList<Expression> entityColumns);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将该实体插入到数据库中
        /// <para>
        /// 实用场景：
        /// Insert Into Table1(Field1,Field2)
        /// Select xx1 AS Field1,xx2 AS Field2
        /// From Table2 a
        /// Join Table2 b ON xx=xx
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="query">要添加的实体查询语义</param>
        void Insert<T>(IDbQueryable<T> query);

        /// <summary>
        /// 将给定实体从基础上下文中删除，当调用 SubmitChanges 时，会将该实体从数据库中删除
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="TEntity">要删除的实体</param>
        void Delete<T>(T TEntity);

        /// <summary>
        /// 基于谓词批量将给定实体从基础上下文中删除，当调用 SubmitChanges 时，会将每一个满足条件的实体从数据库中删除
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        void Delete<T>(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体从数据库中删除
        /// <para>
        /// 实用场景：
        /// Delete a 
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="source">要删除的实体查询语义</param>
        void Delete<T>(IDbQueryable<T> source);

        /// <summary>
        /// 将给定实体更新到基础上下文中，当调用 SubmitChanges 时，会将该实体更新到数据库中
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="TEntity">要添加的实体</param>
        void Update<T>(T TEntity);

        /// <summary>
        /// 基于谓词批量更新到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        void Update<T>(Expression<Func<T, object>> updateExpression, Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// <para>
        /// 实用场景：
        /// UPDATE a SET a.Field1=xxx
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="source">要更新的实体查询语义</param>
        void Update<T>(Expression<Func<T, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// <para>
        /// 实用场景，将其它表的字段更新回目标表中
        /// UPDATE a SET a.Field1=b.Field
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// <para>
        /// 使用示例：
        /// context.Update&lt;T,T1&gt;((a,b)=>new { a.Field1=b.Field1 },query)
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <typeparam name="T1">其它实体类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="source">要更新的实体查询语义</param>
        void Update<T, T1>(Expression<Func<T, T1, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// <para>
        /// 实用场景，将其它表的字段更新回目标表中
        /// UPDATE a SET a.Field1=b.Field
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// <para>
        /// 使用示例：
        /// context.Update&lt;T,T1&gt;((a,b)=>new { a.Field1=b.Field1 },query)
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <typeparam name="T1">其它实体类型</typeparam>
        /// <typeparam name="T2">其它实体类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="source">要更新的实体查询语义</param>
        void Update<T, T1, T2>(Expression<Func<T, T1, T2, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// <para>
        /// 实用场景，将其它表的字段更新回目标表中
        /// UPDATE a SET a.Field1=b.Field
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// <para>
        /// 使用示例：
        /// context.Update&lt;T,T1&gt;((a,b)=>new { a.Field1=b.Field1 },query)
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <typeparam name="T1">其它实体类型</typeparam>
        /// <typeparam name="T2">其它实体类型</typeparam>
        /// <typeparam name="T3">其它实体类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="source">要更新的实体查询语义</param>
        void Update<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// <para>
        /// 实用场景，将其它表的字段更新回目标表中
        /// UPDATE a SET a.Field1=b.Field
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// <para>
        /// 使用示例：
        /// context.Update&lt;T,T1&gt;((a,b)=>new { a.Field1=b.Field1 },query)
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <typeparam name="T1">其它实体类型</typeparam>
        /// <typeparam name="T2">其它实体类型</typeparam>
        /// <typeparam name="T3">其它实体类型</typeparam>
        /// <typeparam name="T4">其它实体类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="source">要更新的实体查询语义</param>
        void Update<T, T1, T2, T3, T4>(Expression<Func<T, T1, T2, T3, T4, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// <para>
        /// 实用场景，将其它表的字段更新回目标表中
        /// UPDATE a SET a.Field1=b.Field
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// <para>
        /// 使用示例：
        /// context.Update&lt;T,T1&gt;((a,b)=>new { a.Field1=b.Field1 },query)
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <typeparam name="T1">其它实体类型</typeparam>
        /// <typeparam name="T2">其它实体类型</typeparam>
        /// <typeparam name="T3">其它实体类型</typeparam>
        /// <typeparam name="T4">其它实体类型</typeparam>
        /// <typeparam name="T5">其它实体类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="source">要更新的实体查询语义</param>
        void Update<T, T1, T2, T3, T4, T5>(Expression<Func<T, T1, T2, T3, T4, T5, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// <para>
        /// 实用场景，将其它表的字段更新回目标表中
        /// UPDATE a SET a.Field1=b.Field
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// <para>
        /// 使用示例：
        /// context.Update&lt;T,T1&gt;((a,b)=>new { a.Field1=b.Field1 },query)
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <typeparam name="T1">其它实体类型</typeparam>
        /// <typeparam name="T2">其它实体类型</typeparam>
        /// <typeparam name="T3">其它实体类型</typeparam>
        /// <typeparam name="T4">其它实体类型</typeparam>
        /// <typeparam name="T5">其它实体类型</typeparam>
        /// <typeparam name="T6">其它实体类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="source">要更新的实体查询语义</param>
        void Update<T, T1, T2, T3, T4, T5, T6>(Expression<Func<T, T1, T2, T3, T4, T5, T6, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 批量将实体查询语义添加到基础上下文中，当调用 SubmitChanges 时，会将每一个满足条件的实体更新到数据库中
        /// <para>
        /// 实用场景，将其它表的字段更新回目标表中
        /// UPDATE a SET a.Field1=b.Field
        /// From Table1
        /// Join Table2 b ON xx=xx
        /// </para>
        /// <para>
        /// 使用示例：
        /// context.Update&lt;T,T1&gt;((a,b)=>new { a.Field1=b.Field1 },query)
        /// </para>
        /// </summary>
        /// <typeparam name="T">给定实体的类型</typeparam>
        /// <typeparam name="T1">其它实体类型</typeparam>
        /// <typeparam name="T2">其它实体类型</typeparam>
        /// <typeparam name="T3">其它实体类型</typeparam>
        /// <typeparam name="T4">其它实体类型</typeparam>
        /// <typeparam name="T5">其它实体类型</typeparam>
        /// <typeparam name="T6">其它实体类型</typeparam>
        /// <typeparam name="T7">其它实体类型</typeparam>
        /// <param name="updateExpression">更新表达式，指定要更新的字段以及这些字段的值</param>
        /// <param name="source">要更新的实体查询语义</param>
        void Update<T, T1, T2, T3, T4, T5, T6, T7>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, object>> updateExpression, IDbQueryable<T> source);

        /// <summary>
        /// 添加额外查询
        /// <para>
        /// 例：SELECT FieldName FROM TableName WHERE Condition={0}
        /// </para>
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
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改。
        /// 同时返回通过 AddQuery 添加的查询语义对应的实体序列。
        /// 如果通过 AddQuery 添加了多个查询语义，只返回第一个查询语义对应的实体序列。
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="result">通过 AddQuery 添加的第一个查询语义对应的实体序列</param>
        /// <returns></returns>
        int SubmitChanges<T>(out List<T> result);

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改。
        /// 同时返回通过 AddQuery 添加的查询语义对应的实体序列。
        /// 如果通过 AddQuery 添加了多个查询语义，只返回第一个和第二个查询语义对应的实体序列。
        /// </summary>
        /// <typeparam name="T1">要返回的元素类型</typeparam>
        /// <typeparam name="T2">要返回的元素类型</typeparam>
        /// <param name="result1">第一个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result2">第二个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <returns></returns>
        int SubmitChanges<T1, T2>(out List<T1> result1, out List<T2> result2);

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改。
        /// 同时返回通过 AddQuery 添加的查询语义对应的实体序列。
        /// 如果通过 AddQuery 添加了多个查询语义，只返回第一至第三个查询语义对应的实体序列。
        /// </summary>
        /// <typeparam name="T1">要返回的元素类型</typeparam>
        /// <typeparam name="T2">要返回的元素类型</typeparam>
        /// <typeparam name="T3">要返回的元素类型</typeparam>
        /// <param name="result1">第一个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result2">第二个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result3">第三个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <returns></returns>
        int SubmitChanges<T1, T2, T3>(out List<T1> result1, out List<T2> result2, out List<T3> result3);

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改。
        /// 同时返回通过 AddQuery 添加的查询语义对应的实体序列。
        /// 如果通过 AddQuery 添加了多个查询语义，只返回第一至第七个查询语义对应的实体序列。
        /// </summary>
        /// <typeparam name="T1">要返回的元素类型</typeparam>
        /// <typeparam name="T2">要返回的元素类型</typeparam>
        /// <typeparam name="T3">要返回的元素类型</typeparam>
        /// <typeparam name="T4">要返回的元素类型</typeparam>
        /// <typeparam name="T5">要返回的元素类型</typeparam>
        /// <typeparam name="T6">要返回的元素类型</typeparam>
        /// <typeparam name="T7">要返回的元素类型</typeparam>
        /// <param name="result1">第一个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result2">第二个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result3">第三个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result4">第四个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result5">第五个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result6">第六个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <param name="result7">第七个通过 AddQuery 添加的查询语义对应的实体序列</param>
        /// <returns></returns>
        int SubmitChanges<T1, T2, T3, T4, T5, T6, T7>(out List<T1> result1, out List<T2> result2, out List<T3> result3, out List<T4> result4, out List<T5> result5, out List<T6> result6, out List<T7> result7);

        /// <summary>
        /// 返回特定类型的对象的集合，其中类型由 T 参数定义
        /// </summary>
        /// <typeparam name="T">对象的基类型</typeparam>
        /// <returns></returns>
        IDbQueryable<T> GetTable<T>();

        /// <summary>
        /// 返回特定类型的对象的集合，其中类型由 T 参数定义
        /// <para>
        /// 适用场景：
        /// 多个导航属性类型相同，用此重载可解决 a.Navigation.Property 的表别名定位问题
        /// </para>
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <typeparam name="TProperty">导航属性类型</typeparam>
        /// <param name="path">导航属性，注意在一组上下文中第一个 GetTable 的这个参数将被自动忽略</param>
        /// <returns></returns>
        IDbQueryable<TProperty> GetTable<T, TProperty>(Expression<Func<T, TProperty>> path);

        /// <summary>
        /// 解析 SQL 命令
        /// </summary>
        List<RawCommand> Resolve();
    }
}
