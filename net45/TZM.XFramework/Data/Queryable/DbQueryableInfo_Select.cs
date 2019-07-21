using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;查&gt; 操作的语义表示
    /// </summary>
    public class DbQueryableInfo_Select<T> : DbQueryableInfo<T>, IDbQueryableInfo_Select
    {
        private List<DbExpression> _join = null;
        private List<DbExpression> _orderBy = null;
        private List<DbExpression> _include = null;
        private DbExpression _groupBy = null;

        /// <summary>
        /// JOIN 表达式集合
        /// </summary>
        public List<DbExpression> Join
        {
            get { return _join; }
            set { _join = value; }
        }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        public List<DbExpression> OrderBy
        {
            get { return _orderBy; }
            set { _orderBy = value; }
        }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        public List<DbExpression> Include
        {
            get { return _include; }
            set { _include = value; }
        }

        /// <summary>
        /// GROUP BY 表达式集合
        /// </summary>
        public DbExpression GroupBy
        {
            get { return _groupBy; }
            set { _groupBy = value; }
        }

        /// <summary>
        /// SQL 命令是否含 DISTINCT 
        /// </summary>
        public bool HaveDistinct { get; set; }

        /// <summary>
        /// 表达式是否是 Any 表达式
        /// </summary>
        public bool HaveAny { get; set; }

        /// <summary>
        /// 表达式是否包含 一对多 类型的导航属性
        /// </summary>
        public bool HaveListNavigation { get; set; }

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
        /// 统计函数表达式，包括如：COUNT,MAX,MIN,AVG,SUM
        /// </summary>
        public DbExpression Statis { get; set; }

        /// <summary>
        /// 嵌套查询语义
        /// 注意，T 可能不是 参数T 所表示的类型
        /// </summary>
        public override IDbQueryableInfo<T> SubQueryInfo { get; set; }

        /// <summary>
        /// 是否是由一对多导航产生的嵌套查询
        /// </summary>
        public bool ResultByListNavigation { get; set; }

        /// <summary>
        /// 并集
        /// <para>
        /// 注意，T 可能不是 参数T 所表示的类型
        /// </para>
        /// </summary>
        public List<IDbQueryableInfo<T>> Union { get; set; }

        /// <summary>
        /// 初始化 <see cref="DbQueryableInfo_Select"/> 类的新实例
        /// </summary>
        public DbQueryableInfo_Select()
        {
            _join = new List<DbExpression>();
            _orderBy = new List<DbExpression>();
            _include = new List<DbExpression>();
        }
    }
}