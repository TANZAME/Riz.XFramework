using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 表示一项以命令目录树形式表示的查询操作
    /// </summary>
    public class DbQuerySelectTree : IDbQueryTree
    {
        /// <summary>
        /// SQL 命令是否含 DISTINCT 
        /// </summary>
        public bool HasDistinct { get; set; }

        /// <summary>
        /// 表达式是否是 Any 表达式
        /// </summary>
        public bool HasAny { get; set; }

        /// <summary>
        /// 表达式是否包含 一对多 类型的导航属性
        /// </summary>
        public bool HasMany { get; set; }

        /// <summary>
        /// 跳过序列中指定数量的元素
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// 从序列的开头返回指定数量的连续元素
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// 指示 SELECT FROM 子句表对应类型
        /// </summary>
        public Type From { get; set; }

        /// <summary>
        /// SELECT 字段表达式
        /// </summary>
        public DbExpression Select { get; set; }

        /// <summary>
        /// 聚合函数表达式，包括如：COUNT,MAX,MIN,AVG,SUM
        /// </summary>
        public DbExpression Aggregate { get; set; }

        /// <summary>
        /// JOIN 表达式集合
        /// </summary>
        public List<DbExpression> Joins { get; set; }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        public List<DbExpression> OrderBys { get; set; }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        public List<DbExpression> Includes { get; set; }

        /// <summary>
        /// GROUP BY 表达式集合
        /// </summary>
        public DbExpression GroupBy { get; set; }

        /// <summary>
        /// WHERE 表达式
        /// </summary>
        public List<DbExpression> Wheres { get; set; }

        /// <summary>
        /// HAVING 表达式
        /// </summary>
        public List<DbExpression> Havings { get; set; }

        /// <summary>
        /// 子查询语义
        /// </summary>
        public DbQuerySelectTree Subquery { get; set; }

        /// <summary>
        /// 是否是由一对多导航产生的嵌套查询
        /// </summary>
        public bool ParsedByMany { get; set; }

        /// <summary>
        /// 并集操作，翻译成 UNION ALL
        /// </summary>
        public List<DbQuerySelectTree> Unions { get; set; }
    }
}