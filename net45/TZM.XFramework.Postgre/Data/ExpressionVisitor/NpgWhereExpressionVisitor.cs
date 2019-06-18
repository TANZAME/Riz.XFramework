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
        public NpgWhereExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, DbExpression exp)
            : base(provider, aliases, exp)
        {
            _expression = exp != null && exp.Expressions != null ? exp.Expressions[0] : null;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public override void Write(ISqlBuilder builder)
        {
            if (builder.Length > 0 && _expression != null) builder.Append(" AND ");
            base.WriteImpl(builder);
        }
    }
}
