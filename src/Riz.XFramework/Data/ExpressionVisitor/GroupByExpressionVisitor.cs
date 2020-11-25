
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// Group By 表达式解析器
    /// </summary>
    internal class GroupByExpressionVisitor : DbExpressionVisitor
    {
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 初始化 <see cref="GroupByExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public GroupByExpressionVisitor(AliasGenerator ag, ISqlBuilder builder)
            : base(ag, builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="groupBy">分组表达式</param>
        public override Expression Visit(DbExpression groupBy)
        {
            if (groupBy != null && groupBy.HasExpression)
            {
                _builder.AppendNewLine();
                _builder.Append("GROUP BY ");
                base.Visit(groupBy);
            }
            return null;
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
