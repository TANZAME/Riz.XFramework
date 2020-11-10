
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 聚合函数表达式解析器
    /// </summary>
    public class AggregateExpressionVisitor : LinqExpressionVisitor
    {
        private string _alias = null;
        private string _columnName = string.Empty;
        private DbExpression _groupBy = null;
        private DbExpression _aggregate = null;
        private static IDictionary<DbExpressionType, string> _aggregateMethods = null;

        /// <summary>
        /// 统计的列名 在嵌套统计时使用
        /// </summary>
        public string AggregateName { get { return _columnName; } }

        static AggregateExpressionVisitor()
        {
            _aggregateMethods = new Dictionary<DbExpressionType, string>
            {
                { DbExpressionType.Count,"COUNT" },
                { DbExpressionType.Max,"MAX" },
                { DbExpressionType.Min,"MIN" },
                { DbExpressionType.Average,"AVG" },
                { DbExpressionType.Sum,"SUM" }
            };
        }

        /// <summary>
        /// 初始化 <see cref="AggregateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasResolver">表别名解析器</param>
        /// <param name="aggregate">聚合函数表达式</param>
        /// <param name="groupBy">Group by 子句</param>
        /// <param name="alias">指定的别名</param>
        public AggregateExpressionVisitor(TableAliasResolver aliasResolver, DbExpression aggregate, DbExpression groupBy = null, string alias = null)
            : base(aliasResolver, aggregate.Expressions != null ? aggregate.Expressions[0] : null)
        {
            _aggregate = aggregate;
            _groupBy = groupBy;
            _alias = alias;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            this.Initialize(builder);
            if (_aggregate != null)
            {

                Expression exp = _aggregate.DbExpressionType == DbExpressionType.Count ? Expression.Constant(1) : base.Expression;
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;

                // q.Average(a => a);
                // 这种情况下g.Key 一定是单个字段，否则解析出来的SQL执行不了
                if (exp.NodeType == ExpressionType.Parameter) exp = _groupBy.Expressions[0];

                builder.Append(_aggregateMethods[_aggregate.DbExpressionType]);
                builder.Append("(");
                base.Visit(exp);
                builder.Append(")");
            }
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
