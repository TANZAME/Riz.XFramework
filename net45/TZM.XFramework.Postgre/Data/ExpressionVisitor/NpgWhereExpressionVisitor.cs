using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// WHERE 表达式解析器
    /// </summary>
    public class NpgWhereExpressionVisitor : WhereExpressionVisitor
    {
        Expression _expression = null;
        /// <summary>
        /// 初始化 <see cref="NpgWhereExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="dbExpression">要访问的表达式</param>
        public NpgWhereExpressionVisitor(IDbQueryProvider provider, TableAlias aliases, DbExpression dbExpression)
            : base(provider, aliases, dbExpression)
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
