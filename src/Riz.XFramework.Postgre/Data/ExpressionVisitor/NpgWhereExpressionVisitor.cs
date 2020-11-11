using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// WHERE 表达式解析器
    /// </summary>
    internal class NpgWhereExpressionVisitor : WhereExpressionVisitor
    {
        Expression _expression = null;
        /// <summary>
        /// 初始化 <see cref="NpgWhereExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasGenerator">表别名解析器</param>
        /// <param name="dbExpression">要访问的表达式</param>
        public NpgWhereExpressionVisitor(AliasGenerator aliasGenerator, DbExpression dbExpression)
            : base( aliasGenerator, dbExpression)
        {
            _expression = dbExpression != null && dbExpression.Expressions != null ? dbExpression.Expressions[0] : null;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            if (builder.Length > 0 && _expression != null) builder.Append(" AND ");
            base.WriteImpl(builder);
        }
    }
}
