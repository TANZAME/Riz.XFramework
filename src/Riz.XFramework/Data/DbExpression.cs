
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 数据查询表达式
    /// </summary>
    public class DbExpression
    {
        /// <summary>
        /// 查询类型
        /// </summary>
        public DbExpressionType DbExpressionType { get; internal set; }

        /// <summary>
        /// 此查询语义所包含的所有表达式
        /// </summary>
        public Expression[] Expressions { get; internal set; }

        /// <summary>
        /// 指示是否有表达式
        /// </summary>
        public bool HasExpression => this.Expressions != null && this.Expressions.Length > 0;

        /// <summary>
        /// 实例化<see cref="DbExpression"/>类的新实例
        /// </summary>
        public DbExpression()
            : this(DbExpressionType.None)
        {
        }

        /// <summary>
        /// 实例化<see cref="DbExpression"/>类的新实例
        /// </summary>
        /// <param name="dbExpressionType">表达式类型</param>
        /// <param name="expression">查询表达式</param>
        public DbExpression(DbExpressionType dbExpressionType, Expression expression = null)
        {
            this.DbExpressionType = dbExpressionType;
            if (expression != null) Expressions = new[] { expression };
        }

        /// <summary>
        /// 实例化<see cref="DbExpression"/>类的新实例
        /// </summary>
        /// <param name="dbExpressionType">表达式类型</param>
        /// <param name="expressions">查询表达式</param>
        public DbExpression(DbExpressionType dbExpressionType, Expression[] expressions)
        {
            this.DbExpressionType = dbExpressionType;
            this.Expressions = expressions;
        }
    }
}
