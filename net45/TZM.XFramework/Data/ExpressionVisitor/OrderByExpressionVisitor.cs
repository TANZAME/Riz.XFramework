using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// Order By 表达式解析器
    /// </summary>
    public class OrderByExpressionVisitor : ExpressionVisitorBase
    {
        private List<DbExpression> _qOrder = null;
        private DbExpression _groupBy = null;
        private TableAliasCache _aliases = null;
        private string _alias = null;
        private IDbQueryProvider _provider = null;

        /// <summary>
        /// 初始化 <see cref="OrderByExpressionVisitor"/> 类的新实例
        /// </summary>
        public OrderByExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, List<DbExpression> qOrder, DbExpression groupBy = null, string alias = null)
            : base(provider, aliases, null, false)
        {
            _qOrder = qOrder;
            _aliases = aliases;
            _groupBy = groupBy;
            _alias = alias;
            _provider = provider;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public void Write(ITextBuilder builder, bool newLine)
        {
            if (_qOrder.Count > 0)
            {
                base._builder = builder;
                if (base._methodVisitor == null) base._methodVisitor = _provider.CreateMethodCallVisitor(this);

                if (newLine) _builder.AppendNewLine();
                _builder.Append("ORDER BY ");


                //foreach (DbExpression qj in _qOrder)
                for (int i = 0; i < _qOrder.Count; i++)
                {
                    this.Visit(_qOrder[i].Expressions[0]);
                    if (_qOrder[i].DbExpressionType == DbExpressionType.OrderByDescending || _qOrder[i].DbExpressionType == DbExpressionType.ThenByDescending) builder.Append(" DESC");
                    if (i < _qOrder.Count - 1) builder.Append(',');
                }
            }
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public override void Write(ITextBuilder builder)
        {
            this.Write(builder, true);
        }

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
            {
                // 嵌套
                _builder.AppendMember(_alias, node.Member.Name);
                return node;
            }
            else
            {
                return base.VisitMember(node);
            }
        }

        //=> order by new  {Id = p.Id}}
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
