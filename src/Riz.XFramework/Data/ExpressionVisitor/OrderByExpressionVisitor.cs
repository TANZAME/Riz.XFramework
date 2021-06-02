
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// Order By 表达式解析器
    /// </summary>
    internal class OrderByExpressionVisitor : DbExpressionVisitor
    {
        private string _alias = null;
        private ISqlBuilder _builder = null;
        private DbExpression _groupBy = null;
        AliasGenerator _ag = null;

        /// <summary>
        /// 初始化 <see cref="OrderByExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="groupBy">GROUP BY 子句</param>
        /// <param name="alias">指定的表别名</param>
        public OrderByExpressionVisitor(AliasGenerator ag, ISqlBuilder builder, DbExpression groupBy, string alias)
            : base(ag, builder)
        {
            _ag = ag;
            _alias = alias;
            _builder = builder;
            _groupBy = groupBy;
        }

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="orderBys">排序表达式</param>
        public override Expression Visit(List<DbExpression> orderBys) => this.Visit(orderBys, true);

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="orderBys">排序表达式</param>
        /// <param name="newLine">是否需要换行</param>
        public Expression Visit(List<DbExpression> orderBys, bool newLine)
        {
            if (orderBys != null && orderBys.Count > 0)
            {
                if (newLine) _builder.AppendNewLine();
                _builder.Append("ORDER BY ");

                for (int i = 0; i < orderBys.Count; i++)
                {
                    this.VisitWithoutStack(_ => this.Visit(orderBys[i].Expressions[0]));
                    if (orderBys[i].DbExpressionType == DbExpressionType.OrderByDescending || orderBys[i].DbExpressionType == DbExpressionType.ThenByDescending)
                    {
                        _builder.Append(" DESC");
                    }
                    if (i < orderBys.Count - 1) _builder.Append(',');
                }
            }

            return null;
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
                return node;

            var propertyExpression = (node as ConstantExpression)?.Value as PropertyExpression;
            if (propertyExpression == null)
                return base.Visit(node);

            // Group By 解析 ?? 未测试
            if (_groupBy != null && node.IsGrouping())
            {
                string memberName = propertyExpression.PropertyName;

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

            string alias = _ag.GetTableAlias(propertyExpression.Parameter);
            if (!string.IsNullOrEmpty(_alias))
                alias = _alias;
            _builder.AppendMember(alias, propertyExpression.PropertyName);

            //// 嵌套
            //if (!string.IsNullOrEmpty(_alias))
            //    _builder.AppendMember(_alias, node.Member, node.Expression.Type);
            //else
            //    base.VisitMember(node);

            return node;
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
            if (node.Arguments.Count == 0) throw new XFrameworkException("'NewExpression' do not have any arguments.");
            else
            {
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
