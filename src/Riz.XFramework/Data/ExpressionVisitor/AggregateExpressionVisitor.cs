
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 聚合函数表达式解析器
    /// </summary>
    internal class AggregateExpressionVisitor : DbExpressionVisitor
    {
        private string _alias = null;
        private string _columnName = "";
        private ISqlBuilder _builder = null;
        private DbExpression _groupBy = null;

        /// <summary>
        /// 统计的列名 在嵌套统计时使用
        /// </summary>
        public string AggregateName => _columnName;

        /// <summary>
        /// 初始化 <see cref="AggregateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="groupBy">Group by 子句</param>
        /// <param name="alias">指定的别名</param>
        public AggregateExpressionVisitor(AliasGenerator ag, ISqlBuilder builder, DbExpression groupBy, string alias)
            : base(ag, builder)
        {
            _alias = alias;
            _builder = builder;
            _groupBy = groupBy;
        }

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="aggregate">聚合函数表达式</param>
        public override Expression Visit(DbExpression aggregate)
        {
            if (aggregate != null)
            {
                Expression expression = aggregate.DbExpressionType == DbExpressionType.Count
                    ? Expression.Constant(1)
                    : aggregate.Expressions[0];
                if (expression.NodeType == ExpressionType.Lambda)
                    expression = (expression as LambdaExpression).Body;
                // q.Average(a => a);
                // 这种情况下g.Key 一定是单个字段，否则解析出来的SQL执行不了
                if (expression.NodeType == ExpressionType.Parameter)
                    expression = _groupBy.Expressions[0];

                _builder.Append(ExpressionExtensions.Aggregates[aggregate.DbExpressionType]);
                _builder.Append("(");
                base.Visit(expression);
                _builder.Append(")");
            }
            return aggregate != null && aggregate.HasExpression ? aggregate.Expressions[0] : null;
        }

        /// <summary>
        /// 访问成员
        /// </summary>
        /// <param name="node">字段或属性表达式</param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node == null) return node;

            _columnName = node.Member.Name;
            // Group By 解析
            if (_groupBy != null && node.IsGrouping())
            {
                string memberName = node.Member.Name;

                // CompanyName = g.Key.Name
                LambdaExpression keySelector = _groupBy.Expressions[0] as LambdaExpression;
                Expression exp = null;
                Expression body = keySelector.Body;


                if (body.NodeType == ExpressionType.MemberAccess)
                {
                    // group xx by a.CompanyName
                    exp = body;
                    //
                    //
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    // group xx by new { Name = a.CompanyName  }
                    NewExpression newExp = body as NewExpression;
                    int index = newExp.Members.IndexOf(x => x.Name == memberName);
                    exp = newExp.Arguments[index];
                }

                return this.Visit(exp);
            }

            if (!string.IsNullOrEmpty(_alias))
                _builder.AppendMember(_alias, node.Member, node.Expression != null ? node.Expression.Type : null);
            else
                base.VisitMember(node);
            return node;
        }
    }
}
