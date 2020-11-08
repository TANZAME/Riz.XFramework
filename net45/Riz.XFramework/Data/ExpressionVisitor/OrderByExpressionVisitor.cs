
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// Order By 表达式解析器
    /// </summary>
    public class OrderByExpressionVisitor : LinqExpressionVisitor
    {
        private List<DbExpression> _orderBy = null;
        private DbExpression _groupBy = null;
        private string _alias = null;

        /// <summary>
        /// 初始化 <see cref="OrderByExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasResolver">表别名解析器</param>
        /// <param name="orderBy">ORDER BY 子句</param>
        /// <param name="groupBy">GROUP BY 子句</param>
        /// <param name="alias">指定的表别名</param>
        public OrderByExpressionVisitor(TableAliasResolver aliasResolver, List<DbExpression> orderBy, DbExpression groupBy = null, string alias = null)
            : base(aliasResolver, null)
        {
            _orderBy = orderBy;
            _groupBy = groupBy;
            _alias = alias;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入 SQL 生成器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="newLine">表示是否需要强制换行</param>
        public void Write(ISqlBuilder builder, bool newLine)
        {
            if (_builder == null) this.Initialize(builder);
            if (_orderBy.Count > 0)
            {
                if (newLine) _builder.AppendNewLine();
                _builder.Append("ORDER BY ");

                for (int i = 0; i < _orderBy.Count; i++)
                {
                    this.VisitWithoutRemark(_ => this.Visit(_orderBy[i].Expressions[0]));
                    if (_orderBy[i].DbExpressionType == DbExpressionType.OrderByDescending || _orderBy[i].DbExpressionType == DbExpressionType.ThenByDescending)
                    {
                        builder.Append(" DESC");
                    }
                    if (i < _orderBy.Count - 1) builder.Append(',');
                }
            }
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入 SQL 生成器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            this.Initialize(builder);
            this.Write(builder, true);
        }

        /// <summary>
        /// 访问字段或者属性表达式
        /// </summary>
        /// <param name="node">字段或者成员表达式</param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node == null) return node;

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
                    //
                    //
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    // group xx by new { Name = a.CompanyName  }
                    NewExpression newExpression = body as NewExpression;
                    int index = newExpression.Members.IndexOf(x => x.Name == memberName);
                    exp = newExpression.Arguments[index];
                }
                else if (body.NodeType == ExpressionType.MemberInit)
                {
                    // group xx by new App { Name = a.CompanyName  }
                    MemberInitExpression initExpression = body as MemberInitExpression;
                    int index = initExpression.Bindings.IndexOf(x => x.Member.Name == memberName);
                    exp = ((MemberAssignment)initExpression.Bindings[index]).Expression;
                }

                return this.Visit(exp);
            }

            // 嵌套
            if (!string.IsNullOrEmpty(_alias))
                _builder.AppendMember(_alias, node.Member, node.Expression.Type);
            else
                base.VisitMember(node);

            return node;
        }

        /// <summary>
        /// 访问构造函数表达式，如 => order by new  {Id = p.Id}}
        /// </summary>
        /// <param name="node">构造函数调用的表达式</param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            if (node != null)
            {
                if (node.Arguments.Count == 0) throw new XFrameworkException("'NewExpression' do not have any arguments.");

                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    this.Visit(node.Arguments[i]);
                    if (i < node.Arguments.Count - 1) _builder.Append(',');
                }
            }

            return node;
        }
    }
}
