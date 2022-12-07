
using System.Linq.Expressions;
using System.Collections.Generic;
using System;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// HAVING 表达式解析器
    /// </summary>
    internal class HavingExpressionVisitor : DbExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private DbExpression _groupBy = null;

        /// <summary>
        /// 初始化 <see cref="HavingExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="groupBy">GROUP BY 子句</param>
        public HavingExpressionVisitor(AliasGenerator ag, ISqlBuilder builder, DbExpression groupBy)
            : base(ag, builder)
        {
            _groupBy = groupBy;
            _builder = builder;
        }

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="havings">分组筛选表达式</param>
        public override Expression Visit(List<DbExpression> havings)
        {
            if (havings != null && havings.Count > 0)
            {
                _builder.AppendNewLine();
                _builder.Append("Having ");

                for (int index = 0; index < havings.Count; index++)
                {
                    DbExpression d = havings[index];                    
                    var node = d.Expressions[0];
                    if (node.NodeType == ExpressionType.Lambda)
                        node = ((LambdaExpression)node).Body;
                    node = BooleanUnaryToBinary(node);

                    base.Visit(node);
                    if (index < havings.Count - 1) _builder.Append(" AND ");
                }
            }

            return null;
        }

        /// <summary>
        /// 访问方法表达式，如 g.Max(a=>a.Level)
        /// </summary>
        /// <param name="node">方法表达式</param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_groupBy != null && node.IsGrouping())
            {
                DbExpressionType dbExpressionType = DbExpressionType.None;
                Enum.TryParse(node.Method.Name, out dbExpressionType);
                Expression exp = dbExpressionType == DbExpressionType.Count
                    ? Expression.Constant(1)
                    : (node.Arguments.Count == 1 ? null : node.Arguments[1]);
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;

                // 常量，Lambda lam = value;
                if (exp.CanEvaluate() && dbExpressionType != DbExpressionType.Count)
                {
                    exp = exp.Evaluate();
                    if (exp is ConstantExpression) exp = (LambdaExpression)(((ConstantExpression)exp).Value);
                }
                // 如果是 a=> a 这种表达式，那么一定会指定 elementSelector
                if (exp.NodeType == ExpressionType.Parameter) exp = _groupBy.Expressions[1];
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;

                _builder.Append(ExpressionExtensions.Aggregates[dbExpressionType]);
                _builder.Append("(");
                this.Visit(exp);
                _builder.Append(")");

                return node;
            }

            return base.VisitMethodCall(node);
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
                    // a.Name
                    exp = body;

                    //
                    //
                    //
                    //
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    // new { Name = a.Name  }
                    NewExpression newExp = body as NewExpression;
                    int index = newExp.Members.IndexOf(x => x.Name == memberName);
                    exp = newExp.Arguments[index];
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

            return base.VisitMember(node);
        }
    }
}
