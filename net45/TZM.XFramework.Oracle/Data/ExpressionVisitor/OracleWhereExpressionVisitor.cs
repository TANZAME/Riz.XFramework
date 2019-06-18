using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// WHERE 表达式解析器
    /// </summary>
    public class OracleWhereExpressionVisitor : WhereExpressionVisitor
    {
        Expression _expression = null;
        /// <summary>
        /// 初始化 <see cref="OracleWhereExpressionVisitor"/> 类的新实例
        /// </summary>
        public OracleWhereExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, DbExpression exp)
            : base(provider, aliases, exp)
        {
            _expression = exp != null && exp.Expressions != null ? exp.Expressions[0] : null;
        }

        /// <summary>
        /// 写入SQL构造器
        /// </summary>
        public override void Write(ISqlBuilder builder)
        {
            if (builder.Length > 0 && _expression != null) builder.Append(" AND ");
            base.WriteImpl(builder);
        }
    }
}
