using System;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 查询语义扩展方法
    /// </summary>
    public static class DbQueryableExtensions
    {
        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        public static bool Any<TSource>(this IDbQueryable<TSource> source)
        {
            return source.Any(null);
        }

        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        public static bool Any<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            IDbQueryable<bool> query = source.CreateQuery<bool>(DbExpressionType.Any, predicate);
            return query.DbContext.Database.Execute(query);
        }

        /// <summary>
        /// 强制使用嵌套查询
        /// </summary>
        public static IDbQueryable<TSource> AsSubQuery<TSource>(this IDbQueryable<TSource> source)
        {
            return source.CreateQuery<TSource>(DbExpressionType.AsSubQuery);
        }

        /// <summary>
        /// 返回序列中的元素数量
        /// </summary>
        public static int Count<TSource>(this IDbQueryable<TSource> source)
        {
            return source.Count(null);
        }

        /// <summary>
        /// 返回序列中的元素数量，不立即执行
        /// </summary>
        public static IDbQueryable<int> LazyCount<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, object>> keySelector)
        {
            return source.CreateQuery<int>>(new DbExpression(DbExpressionType.LazyCount, keySelector));
        }

        /// <summary>
        /// 返回指定序列中满足条件的元素数量
        /// </summary>
        public static int Count<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            IDbQueryable<int> query = source.CreateQuery<int>(DbExpressionType.Count, predicate);
            return query.DbContext.Database.Execute(query);
        }

        /// <summary>
        /// 使用默认的相等比较器确定序列是否包含指定的元素
        /// </summary>
        public static bool Contains<TSource>(this IDbQueryable<TSource> source, TSource item)
        {
            return false;
        }

#if !net40

        /// <summary>
        /// 返回序列中的元素数量
        /// </summary>
        public static async Task<int> CountAsync<TSource>(this IDbQueryable<TSource> source)
        {
            int num = await source.CountAsync(null);
            return num;
        }

        /// <summary>
        /// 返回指定序列中满足条件的元素数量
        /// </summary>
        public static async Task<int> CountAsync<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            IDbQueryable<int> query = source.CreateQuery<int>(DbExpressionType.Count, predicate);
            return await query.DbContext.Database.ExecuteAsync(query);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TSource&gt;"/> 创建一个数组
        /// </summary>
        public static async Task<TSource[]> ToArrayAsync<TSource>(this IDbQueryable<TSource> source)
        {
            IList<TSource> listAsync = await source.ToListAsync<TSource>();
            return ((List<TSource>)listAsync).ToArray();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建一个数组
        /// </summary>
        public static async Task<TElement[]> ToArrayAsync<TElement>(this IDbQueryable<TElement> source, int index, int pageSize)
        {
            if (index < 1) index = 1;
            TElement[] arrayAsync = await source.Skip((index - 1) * pageSize).Take(pageSize).ToArrayAsync();
            return arrayAsync;
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="List&lt;TElement&gt;"/>
        /// </summary>
        public static async Task<List<TElement>> ToListAsync<TElement>(this IDbQueryable<TElement> source)
        {
            List<TElement> elementList = await source.DbContext.Database.ExecuteListAsync(source);
            return elementList;
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="List&lt;TElement&gt;"/>
        /// </summary>
        public static async Task<IList<TElement>> ToListAsync<TElement>(this IDbQueryable<TElement> source, int index, int pageSize)
        {
            if (index < 1) index = 1;
            IList<TElement> listAsync = await source.Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return listAsync;
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="DataTable"/>
        /// </summary>
        public static async Task<DataTable> ToDataTableAsync<TElement>(this IDbQueryable<TElement> source)
        {
            return await source.DbContext.Database.ExecuteDataTableAsync(source);
        }

        /// <summary>
        /// 异步从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建分页记录 <see cref="PagedList&lt;TElement&gt;"/>
        /// </summary>
        /// <typeparam name="TElement">返回类型</typeparam>
        /// <param name="source">数据来源</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页长，1024表示取所有记录</param>
        /// <returns></returns>
        public static async Task<PagedList<TElement>> ToPagedListAsync<TElement>(this IDbQueryable<TElement> source, int pageIndex, int pageSize = 10)
        {
            IList<TElement> result = null;
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
                pages = rowCount / pageSize;
                if (rowCount % pageSize > 0) ++pages;
                if (pageIndex > pages) pageIndex = pages;
                if (pageIndex < 1) pageIndex = 1;
                result = await source.ToListAsync(pageIndex, pageSize);
            }

            return new PagedList<TElement>(result, pageIndex, pageSize, rowCount);
        }

        /// <summary>
        /// 异步从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建分页记录 <see cref="PagedList&lt;TElement&gt;"/>
        /// </summary>
        /// <typeparam name="TElement">返回类型</typeparam>
        /// <param name="source">数据来源</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页长，1024表示取所有记录</param>
        /// <param name="rowCount">总记录数</param>
        /// <returns></returns>
        public static async Task<PagedList<TElement>> ToPagedListAsync<TElement>(this IDbQueryable<TElement> source, int pageIndex, int pageSize, int rowCount)
        {
            IList<TElement> result = null;
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
                pages = rowCount / pageSize;
                if (rowCount % pageSize > 0) ++pages;
                if (pageIndex > pages) pageIndex = pages;
                if (pageIndex < 1) pageIndex = 1;
                result = await source.ToListAsync(pageIndex, pageSize);
            }

            return new PagedList<TElement>(result, pageIndex, pageSize, rowCount);
        }

        /// <summary>
        ///  返回序列中满足指定条件的第一个元素，如果未找到这样的元素，则返回默认值
        /// </summary>
        public static async Task<TSource> FirstOrDefaultAsync<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate = null)
        {
            IDbQueryable<TSource> query = source.CreateQuery<TSource>(DbExpressionType.FirstOrDefault, predicate);
            return await query.DbContext.Database.ExecuteAsync(query);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="DataSet"/>
        /// </summary>
        public static async Task<DataSet> ToDataSetAsync<TElement>(this IDbQueryable<TElement> source)
        {
            return await source.DbContext.Database.ExecuteDataSetAsync(new List<Command> { source.Resolve() });
        }

#endif

        /// <summary>
        /// 根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值
        /// </summary>
        public static IDbQueryable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<System.Linq.IGrouping<TKey, TSource>>(new DbExpression(DbExpressionType.GroupBy, keySelector));
        }

        /// <summary>
        /// 根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值
        /// </summary>
        public static IDbQueryable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector)
        {
            return source.CreateQuery<System.Linq.IGrouping<TKey, TElement>>(new DbExpression(DbExpressionType.GroupBy, new Expression[] { keySelector, elementSelector }));
        }

        /// <summary>
        ///  返回指定序列的元素；如果序列为空，则返回单一实例集合中的类型参数的默认值
        /// </summary>
        public static IDbQueryable<TSource> DefaultIfEmpty<TSource>(this IDbQueryable<TSource> source)
        {
            return source.CreateQuery<TSource>(DbExpressionType.DefaultIfEmpty);
        }

        /// <summary>
        /// 返回指定序列的元素；如果序列为空，则返回单一实例集合中的类型参数的默认值
        /// </summary>
        /// <param name="source">查询语义</param>
        /// <param name="g">是否为右联</param>
        /// <returns></returns>
        public static IDbQueryable<TSource> DefaultIfEmpty<TSource>(this IDbQueryable<TSource> source, bool g)
        {
            return source.CreateQuery<TSource>(DbExpressionType.DefaultIfEmpty);
        }

        /// <summary>
        ///  通过使用默认的相等比较器对值进行比较返回序列中的非重复元素
        /// </summary>
        public static IDbQueryable<TSource> Distinct<TSource>(this IDbQueryable<TSource> source)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Distinct);
        }

        /// <summary>
        ///  返回序列中满足指定条件的第一个元素，如果未找到这样的元素，则返回默认值
        /// </summary>
        public static TSource FirstOrDefault<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate = null)
        {
            IDbQueryable<TSource> query = source.CreateQuery<TSource>(DbExpressionType.FirstOrDefault, predicate);
            return query.DbContext.Database.Execute(query);
        }

        /// <summary>
        ///  基于键相等对两个序列的元素进行左关联并对结果进行分组
        /// </summary>
        public static IDbQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IDbQueryable<TOuter> outer, IDbQueryable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IDbQueryable<TInner>, TResult>> resultSelector)
        {
            return outer.CreateQuery<TResult>(new DbExpression(DbExpressionType.GroupJoin, new Expression[] {
                Expression.Constant(inner),
                outerKeySelector,
                innerKeySelector,
                resultSelector
            }));
        }

        /// <summary>
        ///  指示查询应该包含外键
        /// </summary>
        public static IDbQueryable<TResult> Include<TResult, TProperty>(this IDbQueryable<TResult> source, Expression<Func<TResult, TProperty>> path)
        {
            return source.CreateQuery<TResult>(DbExpressionType.Include, path);
        }

        /// <summary>
        /// 指示查询应该包含外键
        /// </summary>
        /// <typeparam name="TResult">主表类型</typeparam>
        /// <typeparam name="TProperty">外键类型</typeparam>
        /// <param name="source">主表</param>
        /// <param name="path">外键</param>
        /// <param name="selector">选择字段</param>
        /// <returns></returns>
        public static IDbQueryable<TResult> Include<TResult, TProperty>(this IDbQueryable<TResult> source, Expression<Func<TResult, TProperty>> path, Expression<Func<TProperty, object>> selector)
        {
            return source.CreateQuery<TResult>(new DbExpression(DbExpressionType.Include, new Expression[] { path, selector }));
        }

        /// <summary>
        ///  基于匹配键对两个序列的元素进行关联。使用默认的相等比较器对键进行比较
        /// </summary>
        public static IDbQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IDbQueryable<TOuter> outer, IDbQueryable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
        {
            return outer.CreateQuery<TResult>(new DbExpression(DbExpressionType.Join, new Expression[] {
                Expression.Constant(inner),
                outerKeySelector,
                innerKeySelector,
                resultSelector
            }));
        }

        /// <summary>
        /// 返回泛型 IDbQueryable&lt;TResult&gt; 中的最大值
        /// </summary>
        public static TResult Max<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            IDbQueryable<TResult> query = source.CreateQuery<TResult>(DbExpressionType.Max, keySelector);
            return query.DbContext.Database.Execute(query);
        }

        /// <summary>
        /// 返回泛型 IDbQueryable&lt;TResult&gt; 中的最小值
        /// </summary>
        public static TResult Min<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            IDbQueryable<TResult> query = source.CreateQuery<TResult>(DbExpressionType.Min, keySelector);
            return query.DbContext.Database.Execute(query);
        }

        /// <summary>
        /// 返回泛型 IDbQueryable&lt;TResult&gt; 中的平均值
        /// </summary>
        public static TResult Average<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            IDbQueryable<TResult> query = source.CreateQuery<TResult>(DbExpressionType.Average, keySelector);
            return query.DbContext.Database.Execute(query);
        }

        /// <summary>
        /// 返回泛型 IDbQueryable&lt;TResult&gt; 中的所有值之和
        /// </summary>
        public static TResult Sum<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> keySelector)
        {
            IDbQueryable<TResult> query = source.CreateQuery<TResult>(DbExpressionType.Sum, keySelector);
            return query.DbContext.Database.Execute(query);
        }

        /// <summary>
        ///  根据键按升序对序列的元素排序
        /// </summary>
        public static IDbQueryable<TSource> OrderBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<TSource>(DbExpressionType.OrderBy, keySelector);
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.OrderBy, keySelector));
            //return source;
        }

        /// <summary>
        ///  根据键按升序对序列的元素排序
        /// </summary>
        public static IDbQueryable<TSource> OrderBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, string ordering)
        {
            if (string.IsNullOrEmpty(ordering)) ordering = "ASC";
            DbExpressionType t = ordering == "ASC" ? DbExpressionType.OrderBy : DbExpressionType.OrderByDescending;

            return source.CreateQuery<TSource>(t, keySelector);
        }

        /// <summary>
        ///  根据键按升序对序列的元素排序
        /// </summary>
        public static IDbQueryable<TSource> OrderBy<TSource>(this IDbQueryable<TSource> source, string ordering)
        {
            if (string.IsNullOrEmpty(ordering)) return source;

            // a.Product.BuyDate ASC
            string[] syntaxes = ordering.Split(' ');
            string[] segs = syntaxes[0].Split('.');
            if (segs.Length <= 1) return source;

            ParameterExpression parameter = Expression.Parameter(typeof(TSource), segs[0]);
            Expression node = parameter;
            for (int i = 1; i < segs.Length; i++) node = Expression.Property(node, segs[i]);

            LambdaExpression lambda = Expression.Lambda(node, parameter);
            DbExpressionType d = DbExpressionType.OrderBy;
            if (syntaxes.Length > 1 && (syntaxes[1] ?? string.Empty).ToUpper() == "DESC") d = DbExpressionType.OrderByDescending;

            return source.CreateQuery<TSource>(d, lambda);
        }

        /// <summary>
        ///  根据键按降序对序列的元素排序
        /// </summary>
        public static IDbQueryable<TSource> OrderByDescending<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<TSource>(DbExpressionType.OrderByDescending, keySelector);
        }

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        public static IDbQueryable<TResult> Select<TSource, TResult>(this IDbQueryable<TSource> source)
        {
            return source.Select<TSource, TResult>(null);
        }

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        public static IDbQueryable<TResult> Select<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            return source.CreateQuery<TResult>(DbExpressionType.Select, selector);
        }

        /// <summary>
        ///  将序列的每个元素投影并将结果序列组合为一个序列
        /// </summary>
        public static IDbQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, IDbQueryable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
        {
            return source.CreateQuery<TResult>(new DbExpression(DbExpressionType.SelectMany, new Expression[] { collectionSelector, resultSelector }));
        }

        /// <summary>
        ///  跳过序列中指定数量的元素，然后返回剩余的元素
        /// </summary>
        public static IDbQueryable<TSource> Skip<TSource>(this IDbQueryable<TSource> source, int count)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Skip, Expression.Constant(count));
        }

        /// <summary>
        ///  从序列的开头返回指定数量的连续元素
        /// </summary>
        public static IDbQueryable<TSource> Take<TSource>(this IDbQueryable<TSource> source, int count)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Take, Expression.Constant(count));
        }

        /// <summary>
        /// 通过使用默认的相等比较器生成两个序列的并集。
        /// </summary>
        public static IDbQueryable<TSource> Union<TSource>(this IDbQueryable<TSource> source, IDbQueryable<TSource> u)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Union, Expression.Constant(u));
        }

        /// <summary>
        ///  根据某个键按升序对序列中的元素执行后续排序
        /// </summary>
        public static IDbQueryable<TSource> ThenBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<TSource>(DbExpressionType.ThenBy, keySelector);
        }

        /// <summary>
        ///  根据某个键按降序对序列中的元素执行后续排序
        /// </summary>
        public static IDbQueryable<TSource> ThenByDescending<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<TSource>(DbExpressionType.ThenByDescending, keySelector);
        }

        /// <summary>
        ///  基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        public static IDbQueryable<TSource> Where<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Where, predicate);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TSource&gt;"/> 创建一个数组
        /// </summary>
        public static TSource[] ToArray<TSource>(this IDbQueryable<TSource> source)
        {
            return source.ToList<TSource>().ToArray();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建一个数组
        /// </summary>
        public static TElement[] ToArray<TElement>(this IDbQueryable<TElement> source, int index, int pageSize)
        {
            if (index < 1) index = 1;
            return source.Skip((index - 1) * pageSize).Take(pageSize).ToArray();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="List&lt;TElement&gt;"/>
        /// </summary>
        public static List<TElement> ToList<TElement>(this IDbQueryable<TElement> source)
        {
            return source.DbContext.Database.ExecuteList(source);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="List&lt;TElement&gt;"/>
        /// </summary>
        public static List<TElement> ToList<TElement>(this IDbQueryable<TElement> source, int index, int pageSize)
        {
            if (index < 1) index = 1;
            return source.Skip((index - 1) * pageSize).Take(pageSize).ToList();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="DataTable"/>
        /// </summary>
        public static DataTable ToDataTable<TElement>(this IDbQueryable<TElement> source)
        {
            return source.DbContext.Database.ExecuteDataTable(source);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="DataSet"/>
        /// </summary>
        public static DataSet ToDataSet<TElement>(this IDbQueryable<TElement> source)
        {
            return source.DbContext.Database.ExecuteDataSet(new List<Command> { source.Resolve() });
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建分页记录 <see cref="PagedList&lt;TElement&gt;"/>
        /// </summary>
        /// <typeparam name="TElement">返回类型</typeparam>
        /// <param name="source">数据来源</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页长，1024表示取所有记录</param>
        /// <returns></returns>
        public static PagedList<TElement> ToPagedList<TElement>(this IDbQueryable<TElement> source, int pageIndex, int pageSize = 10)
        {
            IList<TElement> result = null;
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
                if (rowCount == 0) result = new List<TElement>(0);
                else
                {
                    pages = rowCount / pageSize;
                    if (rowCount % pageSize > 0) ++pages;
                    if (pageIndex > pages) pageIndex = pages;
                    if (pageIndex < 1) pageIndex = 1;
                    result = source.ToList(pageIndex, pageSize);
                }
            }

            return new PagedList<TElement>(result, pageIndex, pageSize, rowCount);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建分页记录 <see cref="PagedList&lt;TElement&gt;"/>
        /// </summary>
        /// <typeparam name="TElement">返回类型</typeparam>
        /// <param name="source">数据来源</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页长，1024表示取所有记录</param>
        /// <param name="rowCount">总记录数</param>
        /// <returns></returns>
        public static PagedList<TElement> ToPagedList<TElement>(this IDbQueryable<TElement> source, int pageIndex, int pageSize, int rowCount)
        {
            IList<TElement> result = null;
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
                if (rowCount == 0) result = new List<TElement>(0);
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

            return new PagedList<TElement>(result, pageIndex, pageSize, rowCount);
        }

    }
}
