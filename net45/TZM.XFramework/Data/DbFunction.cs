using System;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据库方法
    /// </summary>
    public class DbFunction
    {
        /// <summary>
        /// 解析成行号
        /// <para>row_number() over()</para>
        /// </summary>
        /// <param name="order">排序的字段，默认顺序</param>
        /// <returns></returns>
        public static T RowNumber<T>(Expression<Func<object, object>> order)
        {
            return default(T);
        }

        /// <summary>
        /// 解析成行号
        /// <para>
        /// row_number() over()
        /// </para>
        /// </summary>
        /// <param name="order">order by ***</param>
        /// <param name="asc">顺序</param>
        /// <returns></returns>
        public static T RowNumber<T>(Expression<Func<object, object>> order, bool asc)
        {
            return default(T);
        }

        /// <summary>
        /// 解析成行号
        /// <para>row_number() over()</para>
        /// </summary>
        /// <param name="partition">分组的字段</param>
        /// <param name="order">排序的字段，默认顺序</param>
        /// <returns></returns>
        public static T PartitionRowNumber<T>(Expression<Func<object, object>> partition, Expression<Func<object, object>> order)
        {
            return default(T);
        }

        /// <summary>
        /// 解析成行号
        /// <para>row_number() over()</para>
        /// </summary>
        /// <param name="partition">分组的字段</param>
        /// <param name="order">排序的字段，默认顺序</param>
        /// <param name="asc">顺序</param>
        /// <returns></returns>
        public static T PartitionRowNumber<T>(Expression<Func<object, object>> partition, Expression<Func<object, object>> order, bool asc)
        {
            return default(T);
        }

        /// <summary>
        /// 解析成转换函数辅助类 cast( a as dbtype)
        /// </summary>
        /// <typeparam name="TField">转换的字段</typeparam>
        /// <typeparam name="TResult">转换后的类型</typeparam>
        /// <param name="value">字段表达式</param>
        /// <param name="dataType">数据库类型</param>
        /// <returns></returns>
        public static TResult Cast<TField, TResult>(TField value, string dataType)
        {
            return default(TResult);
        }
    }
}
