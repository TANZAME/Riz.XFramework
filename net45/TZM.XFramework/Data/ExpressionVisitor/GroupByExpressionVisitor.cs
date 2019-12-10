
using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// Group By 表达式解析器
    /// </summary>
    public class GroupByExpressionVisitor : ExpressionVisitorBase
    {
        /// <summary>
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="groupBy">GROUP BY 子句</param>
        public GroupByExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, DbExpression groupBy)
            : base(provider, aliases, groupBy != null ? groupBy.Expressions[0] : null)
        {

        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            if (base.Expression != null)
            {
                builder.AppendNewLine();
                builder.Append("GROUP BY ");
            }

            base.Write(builder);
        }

        /// <summary>
        /// 访问构造函数表达式，如 => new  {Id = p.Id}}
        /// </summary>
        /// <param name="node">构造函数表达式</param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            ReadOnlyCollection<Expression> arguments = node.Arguments;
            for (int index = 0; index < arguments.Count; ++index)
            {
                this.VisitWithoutRemark(x => this.Visit(arguments[index]));
                if (index < arguments.Count - 1) _builder.Append(",");
            }

            return node;
        }
    }
}
