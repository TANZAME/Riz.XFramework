using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 查询语义扩展方法
    /// </summary>
    public static class DbQueryableExtensions
    {
        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns>如果源序列中存在元素通过了指定谓词中的测试，则为 true；否则为 false</returns>
        public static bool Any<TSource>(this IDbQueryable<TSource> source) => source.Any(null);

        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns>如果源序列中存在元素通过了指定谓词中的测试，则为 true；否则为 false</returns>
        public static bool Any<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            DbQueryable<bool> query = (DbQueryable<bool>)source.CreateQuery<bool>(DbExpressionType.Any, predicate);
            return query.Execute<bool>();
        }

        /// <summary>
        /// 强制使用嵌套查询
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> AsSubquery<TSource>(this IDbQueryable<TSource> source) => source.CreateQuery<TSource>(DbExpressionType.AsSubquery, null);

        /// <summary>
        /// 强制使用嵌套查询
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TResult">keySelector 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于提取每个元素的键的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> AsSubquery<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            var query = source.CreateQuery<TResult>(DbExpressionType.AsSubquery, null);
            query = query.CreateQuery<TResult>(DbExpressionType.Select, keySelector);
            return query;
        }

        /// <summary>
        /// 返回序列中的元素数量
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static int Count<TSource>(this IDbQueryable<TSource> source) => source.Count(null);

        /// <summary>
        /// 返回指定序列中满足条件的元素数量
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static int Count<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            DbQueryable<int> query = (DbQueryable<int>)source.CreateQuery<int>(DbExpressionType.Count, predicate);
            return query.Execute<int>();
        }

        /// <summary>
        /// 使用默认的相等比较器确定序列是否包含指定的元素
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="value">要在序列中定位的值</param>
        /// <returns></returns>
        public static bool Contains<TSource>(this IDbQueryable<TSource> source, TSource value) => true;

#if !net40

        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns>如果源序列中存在元素通过了指定谓词中的测试，则为 true；否则为 false</returns>
        public static async Task<bool> AnyAsync<TSource>(this IDbQueryable<TSource> source) => await source.AnyAsync(null);

        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns>如果源序列中存在元素通过了指定谓词中的测试，则为 true；否则为 false</returns>
        public static async Task<bool> AnyAsync<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            DbQueryable<bool> query = (DbQueryable<bool>)source.CreateQuery<bool>(DbExpressionType.Any, predicate);
            return await query.ExecuteAsync<bool>();
        }

        /// <summary>
        /// 返回序列中的元素数量
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static async Task<int> CountAsync<TSource>(this IDbQueryable<TSource> source) => await source.CountAsync(null);

        /// <summary>
        /// 返回指定序列中满足条件的元素数量
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static async Task<int> CountAsync<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            DbQueryable<int> query = (DbQueryable<int>)source.CreateQuery<int>(DbExpressionType.Count, predicate);
            return await query.ExecuteAsync<int>();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建一个数组
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static async Task<TSource[]> ToArrayAsync<TSource>(this IDbQueryable<TSource> source)
        {
            IList<TSource> listAsync = await source.ToListAsync<TSource>();
            return ((List<TSource>)listAsync).ToArray();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建一个数组
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页长</param>
        /// <returns></returns>
        public static async Task<TSource[]> ToArrayAsync<TSource>(this IDbQueryable<TSource> source, int pageIndex, int pageSize)
        {
            if (pageIndex < 1) pageIndex = 1;
            TSource[] arrayAsync = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToArrayAsync();
            return arrayAsync;
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建一个 <see cref="List{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static async Task<List<TSource>> ToListAsync<TSource>(this IDbQueryable<TSource> source)
            => await ((DbQueryable)source).DbContext.Database.ExecuteAsync<List<TSource>>(source);

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建 <see cref="List{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页长</param>
        /// <returns></returns>
        public static async Task<IList<TSource>> ToListAsync<TSource>(this IDbQueryable<TSource> source, int pageIndex, int pageSize)
        {
            if (pageIndex < 1) pageIndex = 1;
            return await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建 <see cref="DataTable"/>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static async Task<DataTable> ToDataTableAsync<TSource>(this IDbQueryable<TSource> source) => await ((DbQueryable)source).ExecuteAsync<DataTable>();

        /// <summary>
        /// 异步从 <see cref="IDbQueryable{TSource}"/> 创建分页记录 <see cref="PagedList{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">返回类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页长，1024表示取所有记录</param>
        /// <returns></returns>
        public static async Task<PagedList<TSource>> ToPagedListAsync<TSource>(this IDbQueryable<TSource> source, int pageIndex, int pageSize = 10)
        {
            IList<TSource> result = null;
            int rowCount = 0;
            int pages = 0;

            if (pageSize == 1024)
            {
                result = await source.ToListAsync();
                rowCount = result.Count;
                pageIndex = 1;
                pages = 1;
            }
            else
            {
                if (pageSize == 0) pageSize = 10;
                rowCount = await source.CountAsync();
                if (rowCount == 0) result = new List<TSource>(0);
                else
                {
                    pages = rowCount / pageSize;
                    if (rowCount % pageSize > 0) ++pages;
                    if (pageIndex > pages) pageIndex = pages;
                    if (pageIndex < 1) pageIndex = 1;
                    result = await source.ToListAsync(pageIndex, pageSize);
                }
            }

            return new PagedList<TSource>(result, pageIndex, pageSize, rowCount);
        }

        /// <summary>
        /// 异步从 <see cref="IDbQueryable{TSource}"/> 创建分页记录 <see cref="PagedList{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">返回类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页长，1024表示取所有记录</param>
        /// <param name="rowCount">总记录数</param>
        /// <returns></returns>
        public static async Task<PagedList<TSource>> ToPagedListAsync<TSource>(this IDbQueryable<TSource> source, int pageIndex, int pageSize, int rowCount)
        {
            IList<TSource> result = null;
            int pages = 0;

            if (pageSize == 1024)
            {
                result = await source.ToListAsync();
                rowCount = result.Count;
                pageIndex = 1;
                pages = 1;
            }
            else
            {
                if (rowCount == 0) result = new List<TSource>(0);
                else
                {
                    if (pageSize == 0) pageSize = 10;
                    pages = rowCount / pageSize;
                    if (rowCount % pageSize > 0) ++pages;
                    if (pageIndex > pages) pageIndex = pages;
                    if (pageIndex < 1) pageIndex = 1;
                    result = await source.ToListAsync(pageIndex, pageSize);
                }
            }

            return new PagedList<TSource>(result, pageIndex, pageSize, rowCount);
        }

        /// <summary>
        /// 返回序列中满足指定条件的第一个元素，如果未找到这样的元素，则返回默认值
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static async Task<TSource> FirstOrDefaultAsync<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate = null)
        {
            DbQueryable<TSource> query = (DbQueryable<TSource>)source.CreateQuery<TSource>(DbExpressionType.FirstOrDefault, predicate);
            return await query.ExecuteAsync<TSource>();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建 <see cref="DataSet"/>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static async Task<DataSet> ToDataSetAsync<TSource>(this IDbQueryable<TSource> source) => await ((DbQueryable<TSource>)source).ExecuteAsync<DataSet>();

#endif

        /// <summary>
        /// 根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于提取每个元素的键的函数</param>
        /// <returns></returns>
        public static IDbQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<IGrouping<TKey, TSource>>(new DbExpression(DbExpressionType.GroupBy, keySelector));
        }

        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <typeparam name="TElement">每个 <see cref="IGrouping{TKey, TElement}"/> 中的元素的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于提取每个元素的键的函数</param>
        /// <param name="elementSelector">用于将每个源元素映射到 <see cref="IGrouping{TKey, TElement}"/> 中的元素的函数</param>
        /// <returns></returns>
        public static IDbQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector)
        {
            return source.CreateQuery<IGrouping<TKey, TElement>>(new DbExpression(DbExpressionType.GroupBy, new Expression[] { keySelector, elementSelector }));
        }

        /// <summary>
        ///  返回指定序列的元素；如果序列为空，则返回单一实例集合中的类型参数的默认值
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> DefaultIfEmpty<TSource>(this IDbQueryable<TSource> source) => source.CreateQuery<TSource>(DbExpressionType.DefaultIfEmpty, null);

        /// <summary>
        /// 返回指定序列的元素；如果序列为空，则返回单一实例集合中的类型参数的默认值
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="rj">是否右关联</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> DefaultIfEmpty<TSource>(this IDbQueryable<TSource> source, bool rj) => source.CreateQuery<TSource>(DbExpressionType.DefaultIfEmpty, null);

        /// <summary>
        ///  通过使用默认的相等比较器对值进行比较返回序列中的非重复元素
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Distinct<TSource>(this IDbQueryable<TSource> source) => source.CreateQuery<TSource>(DbExpressionType.Distinct, null);

        /// <summary>
        /// 返回序列中满足指定条件的第一个元素，如果未找到这样的元素，则返回默认值
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static TSource FirstOrDefault<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate = null)
        {
            DbQueryable<TSource> query = (DbQueryable<TSource>)source.CreateQuery<TSource>(DbExpressionType.FirstOrDefault, predicate);
            return query.Execute<TSource>();
        }

        /// <summary>
        /// 基于键值等同性对两个序列的元素进行关联，并对结果进行分组。 使用指定的 <see cref="IEqualityComparer{TKey}"/> 对键进行比较
        /// </summary>
        /// <typeparam name="TOuter">第一个序列中的元素的类型</typeparam>
        /// <typeparam name="TInner">第二个序列中的元素的类型</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型</typeparam>
        /// <typeparam name="TResult">结果元素的类型</typeparam>
        /// <param name="outer">要联接的第一个序列</param>
        /// <param name="inner">要与第一个序列联接的序列</param>
        /// <param name="outerKeySelector">用于从第一个序列的每个元素提取联接键的函数</param>
        /// <param name="innerKeySelector">用于从第二个序列的每个元素提取联接键的函数</param>
        /// <param name="resultSelector">用于从第一个序列的元素和第二个序列的匹配元素集合中创建结果元素的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IDbQueryable<TOuter> outer, IDbQueryable<TInner> inner,
            Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IDbQueryable<TInner>, TResult>> resultSelector)
        {
            return outer.CreateQuery<TResult>(new DbExpression(DbExpressionType.LeftOuterJoin, new Expression[] {
                Expression.Constant(inner),
                outerKeySelector,
                innerKeySelector,
                resultSelector
            }));
        }

        /// <summary>
        /// 指示查询应该包含外键
        /// </summary>
        /// <typeparam name="TSource">主表类型</typeparam>
        /// <typeparam name="TProperty">外键类型</typeparam>
        /// <param name="source"></param>
        /// <param name="path">要在查询结果中返回的相关对象列表</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Include<TSource, TProperty>(this IDbQueryable<TSource> source, Expression<Func<TSource, TProperty>> path)
            => source.CreateQuery<TSource>(DbExpressionType.Include, path);

        /// <summary>
        /// 指示查询应该包含外键
        /// </summary>
        /// <typeparam name="TSource">主表类型</typeparam>
        /// <typeparam name="TProperty">外键类型</typeparam>
        /// <typeparam name="TResult">外键的结果元素（兼容 List 类型外键）</typeparam>
        /// <param name="source">主表</param>
        /// <param name="path">外键</param>
        /// <param name="keySelector">从表的字段选择器</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Include<TSource, TProperty, TResult>(this IDbQueryable<TSource> source,
            Expression<Func<TSource, TProperty>> path, Expression<Func<TSource, TResult>> keySelector)
            => source.CreateQuery<TSource>(new DbExpression(DbExpressionType.Include, new Expression[] { path, keySelector }));

        /// <summary>
        /// 指示查询应该包含外键
        /// </summary>
        /// <typeparam name="TSource">主表类型</typeparam>
        /// <typeparam name="TProperty">外键类型</typeparam>
        /// <typeparam name="TResult">外键的结果元素</typeparam>
        /// <param name="source">主表</param>
        /// <param name="path">外键</param>
        /// <param name="keySelector">从表的字段选择器</param>
        /// <param name="navFilter">
        /// 从表的过滤条件。注意这里的表达式不能含有诸如 a=> a.Nav.FieldName == value 这种有导航属性的过滤谓词。
        /// 解析的 SQL 会直接拼接在 On 后面，如 ON a.FieldName = b.FieldName AND navFilter。
        /// 如果想拼在 WHERE 后面，请使用 IDbQueryable.Where(a=>a.Nav.FieldName == condition) 语法。
        /// </param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Include<TSource, TProperty, TResult>(this IDbQueryable<TSource> source,
            Expression<Func<TSource, TProperty>> path, Expression<Func<TSource, TResult>> keySelector, Expression<Func<TSource, bool>> navFilter)
            => source.CreateQuery<TSource>(new DbExpression(DbExpressionType.Include, new Expression[] { path, keySelector, navFilter }));

        /// <summary>
        /// 基于匹配键对两个序列的元素进行关联。使用默认的相等比较器对键进行比较
        /// </summary>
        /// <typeparam name="TOuter">第一个序列中的元素的类型</typeparam>
        /// <typeparam name="TInner">第二个序列中的元素的类型</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型</typeparam>
        /// <typeparam name="TResult">结果元素的类型</typeparam>
        /// <param name="outer">要联接的第一个序列</param>
        /// <param name="inner">要与第一个序列联接的序列</param>
        /// <param name="outerKeySelector">用于从第一个序列的每个元素提取联接键的函数</param>
        /// <param name="innerKeySelector">用于从第二个序列的每个元素提取联接键的函数</param>
        /// <param name="resultSelector">用于从两个匹配元素创建结果元素的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IDbQueryable<TOuter> outer, IDbQueryable<TInner> inner,
            Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
        {
            return outer.CreateQuery<TResult>(new DbExpression(DbExpressionType.Join, new Expression[] {
                Expression.Constant(inner),
                outerKeySelector,
                innerKeySelector,
                resultSelector
            }));
        }

        /// <summary>
        /// 返回泛型 <see cref="IDbQueryable{TSource}"/> 中的最大值
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TResult">keySelector 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">应用于每个元素的转换函数</param>
        /// <returns></returns>
        public static TResult Max<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            DbQueryable<TResult> query = (DbQueryable<TResult>)source.CreateQuery<TResult>(DbExpressionType.Max, keySelector);
            return query.Execute<TResult>();
        }

        /// <summary>
        /// 返回泛型 <see cref="IDbQueryable{TSource}"/> 中的最小值
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TResult">keySelector 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">应用于每个元素的转换函数</param>
        /// <returns></returns>
        public static TResult Min<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            DbQueryable<TResult> query = (DbQueryable<TResult>)source.CreateQuery<TResult>(DbExpressionType.Min, keySelector);
            return query.Execute<TResult>();
        }

        /// <summary>
        /// 返回泛型 <see cref="IDbQueryable{TSource}"/> 中的平均值
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TResult">keySelector 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">应用于每个元素的转换函数</param>
        /// <returns></returns>
        public static TResult Average<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            DbQueryable<TResult> query = (DbQueryable<TResult>)source.CreateQuery<TResult>(DbExpressionType.Average, keySelector);
            return query.Execute<TResult>();
        }

        /// <summary>
        /// 返回泛型 <see cref="IDbQueryable{TSource}"/> 中的所有值之和
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TResult">keySelector 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">应用于每个元素的转换函数</param>
        /// <returns></returns>
        public static TResult Sum<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            DbQueryable<TResult> query = (DbQueryable<TResult>)source.CreateQuery<TResult>(DbExpressionType.Sum, keySelector);
            return query.Execute<TResult>();
        }

        /// <summary>
        ///  根据键按升序对序列的元素排序
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于从元素中提取键的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> OrderBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            => source.CreateQuery<TSource>(DbExpressionType.OrderBy, keySelector);

        /// <summary>
        ///  根据键按升序对序列的元素排序
        ///  <para>
        ///  示例： source = source.Where(predicate).OrderBy&lt;TSource, TSource2, int&gt;((a, b) => b.UserId != null ? b.Sequence : a.Sequence);
        ///  </para>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于从元素中提取键的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> OrderBy<TSource, TSource2, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TKey>> keySelector)
            => source.CreateQuery<TSource>(DbExpressionType.OrderBy, keySelector);

        /// <summary>
        ///  根据键按升序对序列的元素排序
        ///  <para>
        ///  示例： source = source.Where(predicate).OrderBy&lt;TSource, TSource2, TSource3, int&gt;((a, b, c) => b.UserId != null ? b.Sequence : a.Sequence);
        ///  </para>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于从元素中提取键的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> OrderBy<TSource, TSource2, TSource3, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource2, TKey>> keySelector)
            => source.CreateQuery<TSource>(DbExpressionType.OrderBy, keySelector);

        /// <summary>
        ///  根据键按给定顺序对序列的元素排序
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于从元素中提取键的函数</param>
        /// <param name="order">排序，ASC 或者 DESC</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> OrderBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, string order)
        {
            if (string.IsNullOrEmpty(order)) order = "ASC";
            DbExpressionType t = order == "ASC" ? DbExpressionType.OrderBy : DbExpressionType.OrderByDescending;

            return source.CreateQuery<TSource>(t, keySelector);
        }

        /// <summary>
        ///  根据键按给定顺序对序列的元素排序
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="sortText">排序说明，例：User.UserName ASC </param>
        /// <returns></returns>
        public static IDbQueryable<TSource> OrderBy<TSource>(this IDbQueryable<TSource> source, string sortText)
        {
            if (string.IsNullOrEmpty(sortText)) return source;

            // a.Product.BuyDate ASC
            string[] clauses = sortText.Split(' ');
            string[] segs = clauses[0].Split('.');
            if (segs.Length <= 1) return source;

            ParameterExpression parameterExpression = Expression.Parameter(typeof(TSource), segs[0]);
            Expression node = parameterExpression;
            for (int i = 1; i < segs.Length; i++) node = Expression.Property(node, segs[i]);

            LambdaExpression lambdaExpression = Expression.Lambda(node, parameterExpression);
            DbExpressionType dbExpressionType = DbExpressionType.OrderBy;
            if (clauses.Length > 1 && (clauses[1] ?? string.Empty).ToUpper() == "DESC") dbExpressionType = DbExpressionType.OrderByDescending;

            return source.CreateQuery<TSource>(dbExpressionType, lambdaExpression);
        }

        /// <summary>
        /// 根据键按给定顺序对序列的元素排序
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="sortTexts">排序说明，例：User.UserName ASC</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> OrderBy<TSource>(this IDbQueryable<TSource> source, IEnumerable<string> sortTexts)
        {
            if (sortTexts == null) return source;

            IDbQueryable<TSource> result = source;
            sortTexts.ForEach(text => result = result.OrderBy(text));
            return result;
        }

        /// <summary>
        ///  根据键按降序对序列的元素排序
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于从元素中提取键的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> OrderByDescending<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            => source.CreateQuery<TSource>(DbExpressionType.OrderByDescending, keySelector);

        ///// <summary>
        /////  通过合并元素的索引将序列的每个元素投影到新表中
        ///// </summary>
        ///// <typeparam name="TSource">source 的元素类型</typeparam>
        ///// <typeparam name="TResult">返回的值的类型</typeparam>
        ///// <param name="source">查询序列</param>
        ///// <returns></returns>
        //public static IDbQueryable<TResult> Select<TSource, TResult>(this IDbQueryable<TSource> source) => source.Select<TSource, TResult>(null);

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TResult">返回的值的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">一个应用于每个源元素的转换函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Select<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
            => source.CreateQuery<TResult>(DbExpressionType.Select, keySelector);

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TResult">返回的值的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">一个应用于每个源元素的转换函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Select<TSource, TSource2, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TResult>> keySelector)
            => source.CreateQuery<TResult>(DbExpressionType.Select, keySelector);

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TResult">返回的值的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">一个应用于每个源元素的转换函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Select<TSource, TSource2, TSource3, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TResult>> keySelector)
            => source.CreateQuery<TResult>(DbExpressionType.Select, keySelector);

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource4">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TResult">返回的值的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">一个应用于每个源元素的转换函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Select<TSource, TSource2, TSource3, TSource4, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TSource4, TResult>> keySelector)
            => source.CreateQuery<TResult>(DbExpressionType.Select, keySelector);

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource4">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource5">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TResult">返回的值的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">一个应用于每个源元素的转换函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Select<TSource, TSource2, TSource3, TSource4, TSource5, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TSource4, TSource5, TResult>> keySelector)
            => source.CreateQuery<TResult>(DbExpressionType.Select, keySelector);

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource4">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource5">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource6">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TResult">返回的值的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">一个应用于每个源元素的转换函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Select<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TResult>> keySelector)
            => source.CreateQuery<TResult>(DbExpressionType.Select, keySelector);

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource4">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource5">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource6">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource7">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TResult">返回的值的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">一个应用于每个源元素的转换函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Select<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TResult>> keySelector)
            => source.CreateQuery<TResult>(DbExpressionType.Select, keySelector);

        /// <summary>
        /// 将序列的每个元素投影并将结果序列组合为一个序列
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TCollection">collectionSelector 收集的中间元素的类型</typeparam>
        /// <typeparam name="TResult">结果序列的元素的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="collectionSelector">应用于输入序列的每个元素的转换函数</param>
        /// <param name="resultSelector">应用于中间序列的每个元素的转换函数</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IDbQueryable<TSource> source,
            Expression<Func<TSource, IDbQueryable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
            => source.CreateQuery<TResult>(new DbExpression(DbExpressionType.SelectMany, new Expression[] { collectionSelector, resultSelector }));

        /// <summary>
        /// 跳过序列中指定数量的元素，然后返回剩余的元素
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="count">返回剩余元素前要跳过的元素数量</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Skip<TSource>(this IDbQueryable<TSource> source, int count)
            => source.CreateQuery<TSource>(DbExpressionType.Skip, Expression.Constant(count));

        /// <summary>
        /// 从序列的开头返回指定数量的连续元素
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="count">要从其返回元素的序列</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Take<TSource>(this IDbQueryable<TSource> source, int count) => source.CreateQuery<TSource>(DbExpressionType.Take, Expression.Constant(count));

        /// <summary>
        /// 通过使用默认的相等比较器生成两个序列的并集。
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="second">第二个查询序列</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Union<TSource>(this IDbQueryable<TSource> source, IDbQueryable<TSource> second)
            => source.CreateQuery<TSource>(DbExpressionType.Union, Expression.Constant(second));

        /// <summary>
        /// 根据某个键按升序对序列中的元素执行后续排序
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> ThenBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            => source.CreateQuery<TSource>(DbExpressionType.ThenBy, keySelector);

        /// <summary>
        /// 根据某个键按降序对序列中的元素执行后续排序
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TKey">keySelector 返回的键的类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> ThenByDescending<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            => source.CreateQuery<TSource>(DbExpressionType.ThenByDescending, keySelector);

        /// <summary>
        /// 基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Where<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            => source.CreateQuery<TSource>(DbExpressionType.Where, predicate);

        /// <summary>
        /// 基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Where<TSource, TSource2>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, bool>> predicate)
            => source.CreateQuery<TSource>(DbExpressionType.Where, predicate);

        /// <summary>
        /// 基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Where<TSource, TSource2, TSource3>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, bool>> predicate)
            => source.CreateQuery<TSource>(DbExpressionType.Where, predicate);

        /// <summary>
        /// 基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource4">source 中的关联语义的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Where<TSource, TSource2, TSource3, TSource4>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TSource4, bool>> predicate)
            => source.CreateQuery<TSource>(DbExpressionType.Where, predicate);

        /// <summary>
        /// 基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource4">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource5">source 中的关联语义的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Where<TSource, TSource2, TSource3, TSource4, TSource5>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TSource4, TSource5, bool>> predicate)
            => source.CreateQuery<TSource>(DbExpressionType.Where, predicate);

        /// <summary>
        /// 基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource4">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource5">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource6">source 中的关联语义的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Where<TSource, TSource2, TSource3, TSource4, TSource5, TSource6>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, bool>> predicate)
            => source.CreateQuery<TSource>(DbExpressionType.Where, predicate);

        /// <summary>
        /// 基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <typeparam name="TSource2">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource3">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource4">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource5">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource6">source 中的关联语义的元素类型</typeparam>
        /// <typeparam name="TSource7">source 中的关联语义的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> Where<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7>(this IDbQueryable<TSource> source, Expression<Func<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, bool>> predicate)
            => source.CreateQuery<TSource>(DbExpressionType.Where, predicate);

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建一个数组
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static TSource[] ToArray<TSource>(this IDbQueryable<TSource> source) => source.ToList<TSource>().ToArray();

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建一个数组
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页长</param>
        /// <returns></returns>
        public static TSource[] ToArray<TSource>(this IDbQueryable<TSource> source, int pageIndex, int pageSize)
        {
            if (pageIndex < 1) pageIndex = 1;
            return source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToArray();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建 <see cref="List{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static List<TSource> ToList<TSource>(this IDbQueryable<TSource> source) => ((DbQueryable<TSource>)source).Execute<List<TSource>>();

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建 <see cref="List{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页长</param>
        /// <returns></returns>
        public static List<TSource> ToList<TSource>(this IDbQueryable<TSource> source, int pageIndex, int pageSize)
        {
            if (pageIndex < 1) pageIndex = 1;
            return source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建 <see cref="DataTable"/>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static DataTable ToDataTable<TSource>(this IDbQueryable<TSource> source) => ((DbQueryable<TSource>)source).Execute<DataTable>();

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建 <see cref="DataSet"/>
        /// </summary>
        /// <typeparam name="TSource">source 的元素类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <returns></returns>
        public static DataSet ToDataSet<TSource>(this IDbQueryable<TSource> source) => ((DbQueryable<TSource>)source).Execute<DataSet>();

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建分页记录 <see cref="PagedList{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">返回类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页长，1024表示取所有记录</param>
        /// <returns></returns>
        public static PagedList<TSource> ToPagedList<TSource>(this IDbQueryable<TSource> source, int pageIndex, int pageSize = 10)
        {
            IList<TSource> result = null;
            int rowCount = 0;
            int pages = 0;

            if (pageSize == 1024)
            {
                result = source.ToList();
                rowCount = result.Count;
                pageIndex = 1;
                pages = 1;
            }
            else
            {
                if (pageSize == 0) pageSize = 10;
                rowCount = source.Count();
                if (rowCount == 0) result = new List<TSource>(0);
                else
                {
                    pages = rowCount / pageSize;
                    if (rowCount % pageSize > 0) ++pages;
                    if (pageIndex > pages) pageIndex = pages;
                    if (pageIndex < 1) pageIndex = 1;
                    result = source.ToList(pageIndex, pageSize);
                }
            }

            return new PagedList<TSource>(result, pageIndex, pageSize, rowCount);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable{TSource}"/> 创建分页记录 <see cref="PagedList{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">返回类型</typeparam>
        /// <param name="source">查询序列</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页长，1024表示取所有记录</param>
        /// <param name="rowCount">总记录数</param>
        /// <returns></returns>
        public static PagedList<TSource> ToPagedList<TSource>(this IDbQueryable<TSource> source, int pageIndex, int pageSize, int rowCount)
        {
            IList<TSource> result = null;
            int pages = 0;

            if (pageSize == 1024)
            {
                result = source.ToList();
                rowCount = result.Count;
                pageIndex = 1;
                pages = 1;
            }
            else
            {
                if (rowCount == 0) result = new List<TSource>(0);
                else
                {
                    if (pageSize == 0) pageSize = 10;
                    pages = rowCount / pageSize;
                    if (rowCount % pageSize > 0) ++pages;
                    if (pageIndex > pages) pageIndex = pages;
                    if (pageIndex < 1) pageIndex = 1;
                    result = source.ToList(pageIndex, pageSize);
                }
            }

            return new PagedList<TSource>(result, pageIndex, pageSize, rowCount);
        }

    }
}
