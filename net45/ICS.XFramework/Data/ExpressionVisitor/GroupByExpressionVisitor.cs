using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// Group By 表达式解析器
    /// </summary>
    public class GroupByExpressionVisitor : ExpressionVisitorBase
    {
        /// <summary>
        /// 初始化 <see cref="GroupByExpressionVisitor"/> 类的新实例
        /// </summary>
        public GroupByExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, DbExpression groupBy)
            : base(provider, aliases, groupBy != null ? groupBy.Expressions[0] : null, false)
        {
            
        }
                
        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public override void Write(ISqlBuilder builder)
        {
            if (base.Expression != null)
            {
                builder.AppendNewLine();
                builder.Append("GROUP BY ");
            }

            base.Write(builder);
        }

        //{new  {Id = p.Id}} 
        protected override Expression VisitNew(NewExpression node)
        {
            ReadOnlyCollection<Expression> arguments = node.Arguments;
            for (int index = 0; index < arguments.Count; ++index)
            {
                this.Visit(arguments[index]);
                if (index < arguments.Count - 1) _builder.Append(",");
            }

            return node;
        }
    }
}
