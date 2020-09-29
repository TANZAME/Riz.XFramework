using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据库方法
    /// </summary>
    public class DbFunction
    {
        /// <summary>
        /// 行号函数，例如 MSSQL 解析成 Row_Number() Over(Order BY...)。默认正序，返回整形结果。
        /// </summary>
        /// <typeparam name="TOrderBy">排序键类型</typeparam>
        /// <param name="keySelector">用于从元素中提取排序键的函数</param>
        /// <returns></returns>
        public static int RowNumber<TOrderBy>(TOrderBy keySelector)
        {
            return DbFunction.RowNumber<int, TOrderBy>(keySelector, OrderBy.ASC);
        }

        /// <summary>
        /// 行号函数，例如 MSSQL 解析成 Row_Number() Over(Order BY...)。返回整形结果。
        /// </summary>
        /// <typeparam name="TOrderBy">排序键类型</typeparam>
        /// <param name="keySelector">用于从元素中提取排序键的函数</param>
        /// <param name="orderBy">排序枚举，默认正序</param>
        /// <returns></returns>
        public static int RowNumber<TOrderBy>(TOrderBy keySelector, OrderBy orderBy)
        {
            return DbFunction.RowNumber<int, TOrderBy>(keySelector, orderBy);
        }

        /// <summary>
        /// 行号函数，例如 MSSQL 解析成 Row_Number() Over(Order BY...)。返回指定类型结果。
        /// </summary>
        /// <typeparam name="TOrderBy">排序键类型</typeparam>
        /// <typeparam name="TNumber">行号类型，如 int,long。例如 Oracle 的 RowNumber 返回的类型是 long 类型</typeparam>
        /// <param name="keySelector">用于从元素中提取排序键的函数</param>
        /// <param name="orderBy">排序枚举，默认正序</param>
        /// <returns></returns>
        public static TNumber RowNumber<TNumber, TOrderBy>(TOrderBy keySelector, OrderBy orderBy)
        {
            return default(TNumber);
        }

        /// <summary>
        /// 分组行号函数，例如 MSSQL  Row_Number() Over(PARTITION BY ... ORDER BY ... )。默认正序，返回整形结果。
        /// </summary>
        /// <typeparam name="TParitionBy">分组键类型</typeparam>
        /// <typeparam name="TOrderBy">排序键类型</typeparam>
        /// <param name="partitionSelector">用于从元素中提取分组键的函数</param>
        /// <param name="keySelector">用于从元素中提取排序键的函数</param>
        /// <returns></returns>
        public static int PartitionRowNumber<TParitionBy, TOrderBy>(TParitionBy partitionSelector, TOrderBy keySelector)
        {
            return DbFunction.PartitionRowNumber<int, TParitionBy, TOrderBy>(partitionSelector, keySelector, OrderBy.ASC);
        }

        /// <summary>
        /// 分组行号函数，例如 MSSQL  Row_Number() Over(PARTITION BY ... ORDER BY ... )。返回整形结果。
        /// </summary>
        /// <typeparam name="TParitionBy">分组键类型</typeparam>
        /// <typeparam name="TOrderBy">排序键类型</typeparam>
        /// <param name="partitionSelector">用于从元素中提取分组键的函数</param>
        /// <param name="keySelector">用于从元素中提取排序键的函数</param>
        /// <param name="orderBy">排序枚举，默认正序</param>
        /// <returns></returns>
        public static int PartitionRowNumber<TParitionBy,TOrderBy>(TParitionBy partitionSelector, TOrderBy keySelector, OrderBy orderBy)
        {
            return DbFunction.PartitionRowNumber<int, TParitionBy, TOrderBy>(partitionSelector, keySelector, orderBy);
        }

        /// <summary>
        /// 分组行号函数，例如 MSSQL  Row_Number() Over(PARTITION BY ... ORDER BY ... )。返回指定类型结果。
        /// </summary>
        /// <typeparam name="TNumber">行号类型，如 int,long。例如 Oracle 的 RowNumber 返回的类型是 long 类型</typeparam>
        /// <typeparam name="TParitionBy">分组键类型</typeparam>
        /// <typeparam name="TOrderBy">排序键类型</typeparam>
        /// <param name="partitionSelector">用于从元素中提取分组键的函数</param>
        /// <param name="keySelector">用于从元素中提取排序键的函数</param>
        /// <param name="orderBy">排序枚举，默认正序</param>
        /// <returns></returns>
        public static TNumber PartitionRowNumber<TNumber,TParitionBy, TOrderBy>(TParitionBy partitionSelector, TOrderBy keySelector, OrderBy orderBy)
        {
            return default(TNumber);
        }

        /// <summary>
        /// 转换函数，如 MSSQL 解析成 CAST(*** AS expression)
        /// </summary>
        /// <param name="keySelector">用于从元素中提取键的函数</param>
        /// <param name="expression">转换成数据库目标类型，如 MSSQL 的 nvarchar(32)</param>
        /// <returns></returns>
        public static TResult Cast<TResult>(TResult keySelector, string expression)
        {
            return default(TResult);
        }

        /// <summary>
        /// 判断函数，如 MSSQL 解析成 CASE WHEN ... THEN ...
        /// 此函数必须以 .End() 结束。
        /// </summary>
        /// <param name="when">when 表达式</param>
        /// <param name="then">then 表达式</param>
        /// <returns></returns>
        public static WhenExpression<TResult> CaseWhen<TResult>(bool when, TResult then)
        {
            return default(WhenExpression<TResult>);
        }


        /// <summary>
        /// 
        /// 生成 CASE WHEN 表达式的子项
        /// .NET 原生的三目表达式不能生成多个 CASE WHEN ... THEN ... WHEN ... THEN ... ELSE ... END，故扩展此项
        /// </summary>
        /// <typeparam name="TResult">返回的结果类型</typeparam>
        public class WhenExpression<TResult>
        {
            /// <summary>
            /// 判断函数，如 MSSQL 解析成 WHEN ... THEN ...
            /// </summary>
            /// <param name="when">when 表达式</param>
            /// <param name="then">then 表达式</param>
            /// <returns></returns>
            public WhenExpression<TResult> When(bool when, TResult then)
            {
                return default(WhenExpression<TResult>);
            }

            /// <summary>
            /// 结束函数，如 MSSQL 解析成 Else ... End
            /// </summary>
            /// <param name="end">end 表达式</param>
            /// <returns></returns>
            public TResult End(TResult end)
            {
                return default(TResult);
            }
        }
    }
}
