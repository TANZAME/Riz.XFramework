using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 表示一项以命令目录树形式表示的查询操作
    /// </summary>
    public class DbQuerySelectTree : DbQueryTree
    {
        private List<DbExpression> _joins = null;
        private List<DbExpression> _orderBys = null;
        private List<DbExpression> _includes = null;
        private List<DbQuerySelectTree> _unions = null;
        private DbExpression _groupByExpression = null;

        /// <summary>
        /// JOIN 表达式集合
        /// </summary>
        public List<DbExpression> Joins
        {
            get { return _joins; }
            set { _joins = value; }
        }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        public List<DbExpression> OrderBys
        {
            get { return _orderBys; }
            set { _orderBys = value; }
        }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        public List<DbExpression> Includes
        {
            get { return _includes; }
            set { _includes = value; }
        }

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
        public Type FromType { get; set; }

        /// <summary>
        /// SELECT 字段表达式
        /// </summary>
        public DbExpression Select { get; set; }

        /// <summary>
        /// WHERE 表达式
        /// </summary>
        public DbExpression Where { get; set; }

        /// <summary>
        /// HAVING 表达式
        /// </summary>
        public DbExpression Having { get; set; }

        /// <summary>
        /// 聚合函数表达式，包括如：COUNT,MAX,MIN,AVG,SUM
        /// </summary>
        public DbExpression Aggregate { get; set; }

        /// <summary>
        /// GROUP BY 表达式集合
        /// </summary>
        public DbExpression GroupBy
        {
            get { return _groupByExpression; }
            set { _groupByExpression = value; }
        }

        /// <summary>
        /// 子查询语义
        /// </summary>
        public DbQuerySelectTree Subquery { get; set; }

        /// <summary>
        /// 是否是由一对多导航产生的嵌套查询
        /// </summary>
        public bool IsParsedByMany { get; set; }

        /// <summary>
        /// 并集操作，翻译成 UNION ALL
        /// </summary>
        public List<DbQuerySelectTree> Unions
        {
            get { return _unions; }
            set { _unions = value; }
        }

        /// <summary>
        /// 初始化 <see cref="DbQuerySelectTree"/> 类的新实例
        /// </summary>
        public DbQuerySelectTree()
        {
            _joins = new List<DbExpression>(0);
            _orderBys = new List<DbExpression>(0);
            _includes = new List<DbExpression>(0);
            _unions = new List<DbQuerySelectTree>(0);
        }
    }
}