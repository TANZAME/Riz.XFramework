
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// Group By 表达式解析器
    /// </summary>
    public class GroupByExpressionVisitor : LinqExpressionVisitor
    {
        /// <summary>
        /// 初始化 <see cref="GroupByExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasGenerator">表别名解析器</param>
        /// <param name="groupBy">GROUP BY 子句</param>
        public GroupByExpressionVisitor(AliasGenerator aliasGenerator, DbExpression groupBy)
            : base(aliasGenerator, groupBy != null ? groupBy.Expressions[0] : null)
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
                this.VisitWithoutRemark(_ => this.Visit(arguments[index]));
                if (index < arguments.Count - 1) _builder.Append(",");
            }
            return node;
        }

        /// <summary>
        /// 访问成员初始化表达式，如 => new App { Name = "Name" }
        /// </summary>
        /// <param name="node">要访问的成员初始化表达式</param>
        /// <returns></returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var A = this;
            // New 表达式
            if (node.NewExpression != null)
                this.VisitNew(node.NewExpression);

            // 赋值表达式
            for (int index = 0; index < node.Bindings.Count; index++)
            {
                this.VisitWithoutRemark(_ => this.Visit(((MemberAssignment)node.Bindings[index]).Expression));
                if (index < node.Bindings.Count - 1) _builder.Append(",");
            }

            return node;
        }
    }
}
