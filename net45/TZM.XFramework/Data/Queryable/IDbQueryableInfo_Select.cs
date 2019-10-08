using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;查&gt; 操作的语义表示
    /// </summary>
    public interface IDbQueryableInfo_Select
    {
        /// <summary>
        /// JOIN 表达式集合
        /// </summary>
        List<DbExpression> Joins { get; set; }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        List<DbExpression> OrderBys { get; set; }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        List<DbExpression> Includes { get; set; }

        /// <summary>
        /// GROUP BY 表达式集合
        /// </summary>
        DbExpression GroupByExpression { get; set; }

        /// <summary>
        /// SQL 命令是否含 DISTINCT 
        /// </summary>
        bool HasDistinct { get; set; }

        /// <summary>
        /// 表达式是否是 Any 表达式
        /// </summary>
        bool HasAny { get; set; }

        /// <summary>
        /// 表达式是否包含 1:n 类型的导航属性
        /// </summary>
        bool HasMany { get; set; }

        /// <summary>
        /// 跳过序列中指定数量的元素
        /// </summary>
        int Skip { get; set; }

        /// <summary>
        /// 从序列的开头返回指定数量的连续元素
        /// </summary>
        int Take { get; set; }

        /// <summary>
        /// 指示 SELECT FROM 子句表对应类型
        /// </summary>
        Type FromType { get; set; }

        /// <summary>
        /// SELECT 字段表达式，空表示选取 <see cref="FromType"/> 的所有字段
        /// </summary>
        DbExpression SelectExpression { get; set; }

        /// <summary>
        /// WHERE 表达式
        /// </summary>
        DbExpression WhereExpression { get; set; }

        /// <summary>
        /// HAVING 表达式
        /// </summary>
        DbExpression HavingExpression { get; set; }

        /// <summary>
        /// 统计函数表达式，包括如：COUNT,MAX,MIN,AVG,SUM
        /// </summary>
        DbExpression StatisExpression { get; set; }
        
        /// <summary>
        /// 是否是由一对多导航产生的嵌套查询
        /// <para>=true 时此查询一定是子查询</para>
        /// </summary>
        bool SubQueryByMany { get; set; }
    }
}