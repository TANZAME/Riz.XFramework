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
        /// 解析成行号 ROW_NUMBER() OVER()
        /// </summary>
        /// <param name="orderBy">排序的字段，默认正序</param>
        /// <returns></returns>
        public static TResult RowNumber<TResult>(object orderBy)
        {
            return default(TResult);
        }

        /// <summary>
        /// 解析成行号 ROW_NUMBER() OVER()
        /// </summary>
        /// <param name="orderBy">order by ***</param>
        /// <param name="asc">顺序</param>
        /// <returns></returns>
        public static TResult RowNumber<TResult>(object orderBy, bool asc)
        {
            return default(TResult);
        }

        /// <summary>
        /// 解析成行号 ROW_NUMBER() OVER(PARTITION BY ORDER BY )
        /// </summary>
        /// <param name="partitionBy">分组的字段</param>
        /// <param name="orderBy">排序的字段，默认顺序</param>
        /// <returns></returns>
        public static TResult PartitionRowNumber<TResult>(object partitionBy, object orderBy)
        {
            return default(TResult);
        }

        /// <summary>
        /// 解析成行号 ROW_NUMBER() OVER(PARTITION BY ORDER BY )
        /// </summary>
        /// <param name="partitionBy">分组的字段</param>
        /// <param name="orderBy">排序的字段</param>
        /// <param name="asc">正序，false 时为倒序</param>
        /// <returns></returns>
        public static TResult PartitionRowNumber<TResult>(object partitionBy, object orderBy, bool asc)
        {
            return default(TResult);
        }

        /// <summary>
        /// 解析成转换函数辅助类 cast( a as dbtype)
        /// </summary>
        /// <param name="source">字段表达式</param>
        /// <param name="expression">数据库类型表达式，如（nvarchar(32)）</param>
        /// <returns></returns>
        public static TResult Cast<TResult>(object source, string expression)
        {
            return default(TResult);
        }
    }
}
