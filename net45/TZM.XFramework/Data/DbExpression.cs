

using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据查询表达式
    /// </summary>
    public class DbExpression
    {
        /// <summary>
        /// 查询类型
        /// </summary>
        public DbExpressionType DbExpressionType { get; set; }

        /// <summary>
        /// 表达式
        /// </summary>
        public Expression[] Expressions { get; set; }

        /// <summary>
        /// 实例化<see cref="DbExpression"/>类的新实例
        /// </summary>
        public DbExpression()
            : this(DbExpressionType.None)
        { }

        /// <summary>
        /// 实例化<see cref="DbExpression"/>类的新实例
        /// </summary>
        public DbExpression(DbExpressionType dbExpressionType, Expression exp = null)
        {
            this.DbExpressionType = dbExpressionType;
            if (exp != null) Expressions = new[] { exp };
        }
    }
}
