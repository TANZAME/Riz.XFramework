
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework
{
    /// <summary>
    /// 扩展帮助类
    /// </summary>
    public static class XFrameworkExtensions
    {
        #region 表达式树

        /// <summary>
        /// 返回真表达式
        /// </summary>
        public static Expression<Func<T, bool>> True<T>()
            where T : class
        {
            return f => true;
        }

        /// <summary>
        /// 返回假表达式
        /// </summary>
        public static Expression<Func<T, bool>> False<T>()
            where T : class
        {
            return f => false;
        }

        /// <summary>
        /// 拼接真表达式
        /// </summary>
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) where T : class
        {
            if (left == null) return right;
            if (right == null) return left;

            var expression = Expression.Invoke(right, left.Parameters);
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, expression), left.Parameters);
        }

        /// <summary>
        /// 拼接假表达式
        /// </summary>
        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right) where T : class
        {
            if (left == null) return right;
            if (right == null) return left;

            var expression = Expression.Invoke(right, left.Parameters);
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, expression), left.Parameters);
        }

        #endregion

        #region 列表扩展

        /// <summary>
        /// 取指定列表中符合条件的元素索引
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int i = -1;
            foreach (T value in collection)
            {
                i++;
                if (predicate(value)) return i;
            }

            return -1;
        }

        /// <summary>
        /// 对集合中的每个元素执行指定操作
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (action == null) return;
            foreach (var item in collection) action.Invoke(item);
        }

        /// <summary>
        /// 创建一个集合
        /// </summary>
        public static List<TResult> ToList<T, TResult>(this IEnumerable<T> collection, Func<T, TResult> selector)
        {
            return collection.Select(selector).ToList();
        }

        /// <summary>
        /// 列表转换扩展
        /// </summary>
        public static List<T> ToList<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (predicate != null) source = source.Where(predicate);
            return source.ToList();
        }

        /// <summary>
        /// 列表转换扩展
        /// </summary>
        public static List<TResult> ToList<T, TResult>(this IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
        {
            if (predicate != null) source = source.Where(predicate);
            return source.Select(selector).ToList();
        }

        /// <summary>
        /// 计算总页码
        /// </summary>
        /// <param name="collection">数据集合</param>
        /// <param name="pageSize">页码</param>
        /// <returns></returns>
        public static int Page<T>(this IEnumerable<T> collection, int pageSize)
        {
            int rowCount = 0;
            if ((collection as ICollection<T>) != null) rowCount = (collection as ICollection<T>).Count;
            else if ((collection as T[]) != null) rowCount = (collection as T[]).Length;
            else rowCount = collection.Count();

            return ~~((rowCount - 1) / pageSize) + 1;
        }

        /// <summary>
        /// 批量添加命令参数
        /// </summary>
        public static void AddRange(this IDataParameterCollection sources, IEnumerable<IDbDataParameter> parameters)
        {
            if (parameters != null)
            {
                foreach (var p in parameters) sources.Add(p);
            }
        }

        #endregion
    }
}
